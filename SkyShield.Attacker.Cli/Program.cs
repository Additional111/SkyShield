using SkyShield.Attacker.Cli.Attacks;
using SkyShield.Attacker.Cli.Services;

namespace SkyShield.Attacker.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===================================");
        Console.WriteLine(" SKYSHIELD - ATTACKER CLI (v1.0.0)");
        Console.WriteLine("===================================\n");

        string attackType = string.Empty;
        string icao = string.Empty;
        string apiUrl = "http://localhost:5000";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--attack" && i + 1 < args.Length) attackType = args[i + 1].ToLower();
            if (args[i] == "--icao" && i + 1 < args.Length) icao = args[i + 1].ToUpper();
        }

        if (string.IsNullOrEmpty(attackType) || string.IsNullOrEmpty(icao))
        {
            ShowUsage();
            return;
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
        var apiClient = new ApiClientService(httpClient);
        var spoofingService = new SpoofingService();

        IAttack? attack = attackType switch
        {
            "ghost" => new GhostAircraftAttack(apiClient, spoofingService),
            "teleport" => new TeleportAttack(apiClient, spoofingService),
            "altitude-jump" => new AltitudeJumpAttack(apiClient, spoofingService),
            "identity-spoof" => new IdentitySpoofAttack(apiClient, spoofingService),
            _ => null
        };

        if (attack == null)
        {
            Console.WriteLine($"[EROARE] Tipul de atac '{attackType}' este invalid.");
            ShowUsage();
            return;
        }

        await attack.ExecuteAsync(icao);
        Console.WriteLine("\n[INFO] Simulare încheiată.");
    }

    static void ShowUsage()
    {
        Console.WriteLine("Utilizare:");
        Console.WriteLine("  dotnet run -- --attack <tip_atac> --icao <cod_icao>\n");
        Console.WriteLine("Atacuri disponibile:");
        Console.WriteLine("  ghost          - Trimite un ICAO necunoscut.");
        Console.WriteLine("  teleport       - Trimite poziții la distanțe imposibile.");
        Console.WriteLine("  altitude-jump  - Modifică altitudinea nerealist.");
        Console.WriteLine("  identity-spoof - Emite de pe același ICAO din locații diferite simultan.");
    }
}