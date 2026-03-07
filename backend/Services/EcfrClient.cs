using System.Text.Json;
using EcfrAnalyzer.DTOs;

namespace EcfrAnalyzer.Services;

public class EcfrClient
{
    private const string BaseUrl = "https://www.ecfr.gov";
    private readonly HttpClient _http;
    private readonly ILogger<EcfrClient> _log;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EcfrClient(HttpClient http, ILogger<EcfrClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<List<AgencyDto>> FetchAgenciesAsync()
    {
        var response = await GetJsonAsync<AgenciesResponse>($"{BaseUrl}/api/admin/v1/agencies");
        return response?.Agencies ?? [];
    }

    public async Task<List<TitleDto>> FetchTitlesAsync()
    {
        var response = await GetJsonAsync<TitlesResponse>($"{BaseUrl}/api/versioner/v1/titles");
        return response?.Titles ?? [];
    }

    public async Task<List<VersionDto>> FetchVersionsAsync(int titleNumber)
    {
        await Task.Delay(100); // Rate limit: don't hammer the eCFR API
        var response = await GetJsonAsync<VersionsResponse>(
            $"{BaseUrl}/api/versioner/v1/versions/title-{titleNumber}.json");
        return response?.ContentVersions ?? [];
    }

    public async Task<byte[]?> FetchContentAsync(string date, int titleNumber, string? chapter)
    {
        await Task.Delay(100); // Rate limit: don't hammer the eCFR API
        var url = $"{BaseUrl}/api/versioner/v1/full/{date}/title-{titleNumber}.xml";
        if (!string.IsNullOrEmpty(chapter))
            url += $"?chapter={chapter}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Content fetch failed {Url}: {Status}", url, response.StatusCode);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Content fetch error {Url}", url);
            return null;
        }
    }

    private async Task<T?> GetJsonAsync<T>(string url) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Fetch failed {Url}", url);
            return null;
        }
    }
}