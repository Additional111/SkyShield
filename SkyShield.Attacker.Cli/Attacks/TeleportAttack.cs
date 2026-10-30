using SkyShield.Attacker.Cli.Services;

namespace SkyShield.Attacker.Cli.Attacks;

public class TeleportAttack : IAttack
{
    private readonly ApiClientService _api;
    private readonly SpoofingService _spoofing;

    public TeleportAttack(ApiClientService api, SpoofingService spoofing)
    {
        _api = api;
        _spoofing = spoofing;
    }

    public async Task ExecuteAsync(string icao)
    {
        Console.WriteLine("\n=== RUNNING: Teleport Attack ===");
        var payload1 = _spoofing.GenerateBaseTelemetry(icao);
        await _api.SendTelemetryAsync(payload1);

        await Task.Delay(1000);

        var payload2 = _spoofing.GenerateBaseTelemetry(icao);
        payload2.Latitude += 20.0; // Salt imposibil (aprox 2200 km)
        payload2.TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await _api.SendTelemetryAsync(payload2);
    }
}