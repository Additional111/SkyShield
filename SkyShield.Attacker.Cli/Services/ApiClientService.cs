using System.Net.Http.Json;
using SkyShield.Attacker.Cli.Models;

namespace SkyShield.Attacker.Cli.Services;

public class ApiClientService
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "/api/flights";

    public ApiClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendTelemetryAsync(FlightTelemetryDto payload)
    {
        Console.WriteLine($"[INFO] Trimitere telemetrie pt ICAO: {payload.Icao} | Lat: {payload.Latitude}, Lon: {payload.Longitude}, Alt: {payload.AltitudeFeet}ft");

        try
        {
            var response = await _httpClient.PostAsJsonAsync(Endpoint, payload);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SUCCESS] API-ul a acceptat mesajul. Status: {(int)response.StatusCode}");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[WARNING] API-ul a respins mesajul. Status: {(int)response.StatusCode} | Motiv: {errorMsg}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] Nu se poate conecta la API pe {_httpClient.BaseAddress}. Asigură-te că SkyShield rulează.");
            Console.WriteLine($"[ERROR] Detalii: {ex.Message}");
        }
    }
}