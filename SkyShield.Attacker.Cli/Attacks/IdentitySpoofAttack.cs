using SkyShield.Attacker.Cli.Services;

namespace SkyShield.Attacker.Cli.Attacks;

public class IdentitySpoofAttack : IAttack
{
    private readonly ApiClientService _api;
    private readonly SpoofingService _spoofing;

    public IdentitySpoofAttack(ApiClientService api, SpoofingService spoofing)
    {
        _api = api;
        _spoofing = spoofing;
    }

    public async Task ExecuteAsync(string icao)
    {
        Console.WriteLine("\n=== RUNNING: Identity Spoof Attack ===");
        var payloadActual = _spoofing.GenerateBaseTelemetry(icao);
        var payloadSpoofed = _spoofing.GenerateBaseTelemetry(icao);
        
        payloadSpoofed.Latitude += 2.0; 
        payloadSpoofed.Longitude -= 1.5;

        await _api.SendTelemetryAsync(payloadActual);
        await Task.Delay(500);
        
        payloadSpoofed.TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await _api.SendTelemetryAsync(payloadSpoofed);
        
        await Task.Delay(500);
        
        payloadActual.TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await _api.SendTelemetryAsync(payloadActual);
    }
}