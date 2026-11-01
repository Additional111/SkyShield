using System.Net.Http.Json;
using System.Text.Json;
using SkyShield.Attacker.Cli.Models;

namespace SkyShield.Attacker.Cli.Services;

/// <summary>Talks to the local SkyShield backend. The CLI never raises alerts itself.</summary>
public class ApiClientService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiClientService(HttpClient http) => _http = http;

    public Uri BaseAddress => _http.BaseAddress!;

    public async Task<HealthDto?> GetHealthAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<HealthDto>("/api/health", Json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    public async Task<List<TelemetryDto>> GetFlightsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TelemetryDto>>("/api/flights", Json) ?? new();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new();
        }
    }

    public async Task<SimulationResponse> SubmitAsync(string scenario, TelemetryDto message)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/simulation/events", new SimulationEventRequest(scenario, message), Json);

        var body = await response.Content.ReadFromJsonAsync<SimulationResponse>(Json)
                   ?? new SimulationResponse { Error = "Empty response from backend." };

        if (!response.IsSuccessStatusCode)
        {
            body.Success = false;
            body.Error ??= $"HTTP {(int)response.StatusCode}";
        }

        return body;
    }

    public async Task<bool> ResetAsync(bool broadcast)
    {
        var response = await _http.PostAsync($"/api/simulation/reset?broadcast={broadcast.ToString().ToLowerInvariant()}", null);
        return response.IsSuccessStatusCode;
    }
}
