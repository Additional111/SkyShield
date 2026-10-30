using SkyShield.Attacker.Cli.Services;

namespace SkyShield.Attacker.Cli.Attacks;

public class GhostAircraftAttack : IAttack
{
    private readonly ApiClientService _api;
    private readonly SpoofingService _spoofing;

    public GhostAircraftAttack(ApiClientService api, SpoofingService spoofing)
    {
        _api = api;
        _spoofing = spoofing;
    }

    public async Task ExecuteAsync(string icao)
    {
        Console.WriteLine("\n=== RUNNING: Ghost Aircraft Attack ===");
        var payload = _spoofing.GenerateBaseTelemetry(icao);
        await _api.SendTelemetryAsync(payload);
    }
}