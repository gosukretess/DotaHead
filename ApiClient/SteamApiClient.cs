using System.Text.Json;
using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotaHead.ApiClient;

public class SteamApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseAddress = "https://api.steampowered.com/";
    private readonly string _steamToken;
    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<SteamApiClient>();

    public SteamApiClient()
    {
        var appSettings = ConfigurationLoader.Load();
        _steamToken = appSettings.SteamToken;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseAddress)
        };
    }

    public async Task<MatchHistoryResult?> GetMatchHistory(long accountId, int matchesCount = 20)
    {
        var apiUrl = $"IDOTA2Match_570/GetMatchHistory/v1/?account_id={accountId}&matches_requested={matchesCount}&key={_steamToken}";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<MatchHistoryRoot>(jsonResponse);

                if (root == null)
                {
                    Logger.LogWarning($"There was an error parsing SteamApi response. Response body: {jsonResponse}");
                }

                Logger.LogInformation($"Successful response from SteamApi for accountId {accountId}");
                return root?.Result;
            }

            throw new Exception("Unsuccessful response StatusCode: " + response.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error calling SteamApi. Exception message: {ex.Message}");
            return null;
        }
    }    
    
    public async Task<MatchDetailsResult?> GetMatchDetails(long matchId)
    {
        var apiUrl = $"IDOTA2Match_570/GetMatchDetails/v1/?match_id={matchId}&key={_steamToken}";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<MatchDetailsRoot>(jsonResponse);

                if (root == null)
                {
                    Logger.LogWarning($"There was an error parsing SteamApi response. Response body: {jsonResponse}");
                }

                Logger.LogInformation($"Successful response from SteamApi for matchId {matchId}");
                return root?.Result;
            }

            throw new Exception("Unsuccessful response StatusCode: " + response.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error calling SteamApi. Exception message: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}