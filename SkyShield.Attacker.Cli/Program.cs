using SkyShield.Attacker.Cli.Models;
using SkyShield.Attacker.Cli.Scenarios;
using SkyShield.Attacker.Cli.Services;

namespace SkyShield.Attacker.Cli;

/// <summary>
/// Manual controller for SkyShield synthetic test scenarios. It only submits events to
/// the LOCAL backend simulation interface; detection, risk scoring and dashboard
/// alerts are produced by the backend. Nothing runs unless you type a command.
/// </summary>
internal static class Program
{
    private const string DefaultUrl = "http://localhost:5000";
    private const string GhostIcao = "BAD0C1";

    private static async Task<int> Main(string[] args)
    {
        string url = Environment.GetEnvironmentVariable("SKYSHIELD_URL") ?? DefaultUrl;
        string? icaoOverride = null;
        var positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--url" && i + 1 < args.Length) url = args[++i];
            else if (args[i] == "--icao" && i + 1 < args.Length) icaoOverride = args[++i].ToUpperInvariant();
            else positional.Add(args[i]);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri) || !baseUri.IsLoopback)
        {
            Console.Error.WriteLine("The CLI only talks to a local backend (localhost / 127.0.0.1).");
            return 1;
        }

        using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
        var api = new ApiClientService(http);

        if (positional.Count == 0)
        {
            PrintUsage();
            return 1;
        }

        return await RunAsync(api, positional, icaoOverride);
    }

    private static async Task<int> RunAsync(ApiClientService api, List<string> args, string? icaoOverride)
    {
        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "list":
                PrintScenarios();
                return 0;

            case "status":
                return await StatusAsync(api);

            case "reset":
                return await ResetAsync(api);

            case "simulate":
                if (args.Count < 2)
                {
                    Console.Error.WriteLine("Missing scenario name.\n");
                    PrintScenarios();
                    return 1;
                }
                return await SimulateAsync(api, args[1], icaoOverride);

            case "help" or "--help" or "-h":
                PrintUsage();
                return 0;

            default:
                Console.Error.WriteLine($"Unknown command '{args[0]}'.\n");
                PrintUsage();
                return 1;
        }
    }

    private static async Task<int> StatusAsync(ApiClientService api)
    {
        var health = await api.GetHealthAsync();
        if (health == null) return Unreachable(api);

        Console.WriteLine($"Backend reachable at {api.BaseAddress}");
        Console.WriteLine($"  Aircraft tracked : {health.AircraftCount}");
        Console.WriteLine($"  Normal monitoring: {(health.MonitoringActive ? "active" : "NOT active")}");
        return 0;
    }

    private static async Task<int> ResetAsync(ApiClientService api)
    {
        if (await api.GetHealthAsync() == null) return Unreachable(api);

        bool ok = await api.ResetAsync(broadcast: true);
        Console.WriteLine(ok
            ? "Simulation state cleared. Dashboard alerts and temporary aircraft removed; normal monitoring continues."
            : "Reset failed (is the simulation interface enabled?).");
        return ok ? 0 : 1;
    }

    private static async Task<int> SimulateAsync(ApiClientService api, string name, string? icaoOverride)
    {
        var scenario = ScenarioCatalog.Find(name);
        if (scenario == null)
        {
            Console.Error.WriteLine($"Unknown scenario '{name}'.\n");
            PrintScenarios();
            return 1;
        }

        Console.WriteLine($"Scenario: {scenario.Name}");
        Console.WriteLine($"  {scenario.Description}\n");

        var health = await api.GetHealthAsync();
        if (health == null) return Unreachable(api);
        Console.WriteLine($"[OK] Backend reachable ({health.AircraftCount} aircraft tracked).");

        var baseline = await BuildBaselineAsync(api, scenario, icaoOverride);
        if (baseline == null)
        {
            Console.Error.WriteLine("No aircraft available as a baseline. Is normal monitoring running?");
            return 1;
        }

        // Fresh, private simulation state for every run; the dashboard is not touched.
        if (!await api.ResetAsync(broadcast: false))
        {
            Console.Error.WriteLine("Could not prepare the simulation (is Simulation:Enabled true?).");
            return 1;
        }

        var messages = scenario.Build(baseline);
        var allAlerts = new List<AlertDto>();
        int topScore = 0;
        string topLevel = "Low";

        for (int i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            Console.WriteLine($"[{i + 1}/{messages.Count}] Submitting synthetic event for {m.Icao} " +
                              $"(lat {m.Latitude:F3}, lon {m.Longitude:F3}, alt {m.AltitudeFeet:F0} ft)");

            SimulationResponse result;
            try
            {
                result = await api.SubmitAsync(scenario.Name, m);
            }
            catch (HttpRequestException)
            {
                return Unreachable(api);
            }

            if (!result.Success)
            {
                Console.Error.WriteLine($"      Rejected by backend: {result.Error}");
                return 1;
            }

            if (result.AnomalyDetected)
            {
                foreach (var a in result.Alerts)
                    Console.WriteLine($"      ! {a.AnomalyType} - {a.Severity} (score {a.RiskScore}): {a.Description}");

                allAlerts.AddRange(result.Alerts);

                if (result.RiskScore != null && result.RiskScore.Score >= topScore)
                {
                    topScore = result.RiskScore.Score;
                    topLevel = result.RiskScore.RiskLevel;
                }
            }
            else
            {
                Console.WriteLine("      no anomaly");
            }

            if (i < messages.Count - 1)
                await Task.Delay(800);
        }

        Console.WriteLine();
        if (allAlerts.Count > 0)
            Console.WriteLine($"RESULT: anomaly DETECTED - {allAlerts.Count} alert(s), highest risk {topScore}/100 ({topLevel}). Alerts were broadcast to the dashboard.");
        else
            Console.WriteLine("RESULT: no anomaly detected.");

        return 0;
    }

    private static async Task<TelemetryDto?> BuildBaselineAsync(ApiClientService api, IScenario scenario, string? icaoOverride)
    {
        if (!scenario.UsesFleetAircraft)
        {
            // Ghost: an ICAO that is not in the fleet, placed inside the demo area.
            return new TelemetryDto
            {
                Icao = icaoOverride ?? GhostIcao,
                Latitude = 45.9, Longitude = 24.9,
                AltitudeFeet = 30000, GroundSpeedKnots = 420
            };
        }

        var flights = await api.GetFlightsAsync();
        var chosen = icaoOverride != null
            ? flights.FirstOrDefault(f => f.Icao.Equals(icaoOverride, StringComparison.OrdinalIgnoreCase))
            : flights.OrderBy(f => f.Icao).FirstOrDefault();

        if (chosen == null && icaoOverride != null)
            Console.Error.WriteLine($"Aircraft {icaoOverride} is not currently tracked.");

        return chosen;
    }

    private static int Unreachable(ApiClientService api)
    {
        Console.Error.WriteLine($"[ERROR] Cannot reach the SkyShield backend at {api.BaseAddress}.");
        Console.Error.WriteLine("        Start it first:  dotnet run --project SkyShield");
        return 2;
    }

    private static void PrintScenarios()
    {
        Console.WriteLine("Available scenarios (all synthetic, local only):");
        foreach (var s in ScenarioCatalog.All)
            Console.WriteLine($"  {s.Name,-20} {s.Description}");
        Console.WriteLine("\nRun one with:  dotnet run --project SkyShield.Attacker.Cli -- simulate <scenario>");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("SkyShield.Attacker.Cli - manual synthetic test scenarios (local only)\n");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project SkyShield.Attacker.Cli -- <command> [options]\n");
        Console.WriteLine("Commands:");
        Console.WriteLine("  list                    List available scenarios");
        Console.WriteLine("  simulate <scenario>     Run one scenario against the local backend");
        Console.WriteLine("  status                  Check that the backend is reachable");
        Console.WriteLine("  reset                   Clear temporary simulation state and dashboard alerts\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --icao <ICAO>           Use a specific aircraft (or ghost ICAO)");
        Console.WriteLine("  --url <address>         Backend address (default http://localhost:5000)\n");
        PrintScenarios();
    }
}
