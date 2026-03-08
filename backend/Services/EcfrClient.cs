using System.Text.Json;
using EcfrAnalyzer.DTOs;

namespace EcfrAnalyzer.Services;

public class EcfrClient
{
    private const string BaseUrl = "https://www.ecfr.gov";
    private readonly HttpClient _http;
    private readonly ILogger<EcfrClient> _log;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public EcfrClient(HttpClient http, ILogger<EcfrClient> log) { _http = http; _log = log; }

    public async Task<List<AgencyDto>> FetchAgenciesAsync()
        => (await GetJsonAsync<AgenciesResponse>($"{BaseUrl}/api/admin/v1/agencies"))?.Agencies ?? [];

    public async Task<List<TitleDto>> FetchTitlesAsync()
        => (await GetJsonAsync<TitlesResponse>($"{BaseUrl}/api/versioner/v1/titles"))?.Titles ?? [];

    public async Task<List<VersionDto>> FetchVersionsAsync(int titleNumber)
    {
        await Task.Delay(100);
        return (await GetJsonAsync<VersionsResponse>(
            $"{BaseUrl}/api/versioner/v1/versions/title-{titleNumber}.json"))?.ContentVersions ?? [];
    }

    public async Task<byte[]?> FetchContentAsync(string date, int titleNumber, string? chapter)
    {
        await Task.Delay(100);
        var url = $"{BaseUrl}/api/versioner/v1/full/{date}/title-{titleNumber}.xml";
        if (!string.IsNullOrEmpty(chapter)) url += $"?chapter={chapter}";
        try
        {
            var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex) { _log.LogWarning(ex, "Content fetch error {Url}", url); return null; }
    }

    private async Task<T?> GetJsonAsync<T>(string url) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOpts);
        }
        catch (Exception ex) { _log.LogWarning(ex, "Fetch failed {Url}", url); return null; }
    }
}
