using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace DotaHead.ApiClient;

public class SteamApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseAddress = "https://api.steampowered.com/";
    private const string CdnAddress = "http://cdn.dota2.com/";
    private readonly string _steamToken;

    public SteamApiClient()
    {
        var appSettings = ReadConfiguration();
        _steamToken = appSettings.SteamToken;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseAddress)
        };
    }

    public async Task<MatchHistoryResult> GetMatchHistory(long accountId, int matchesCount = 20)
    {
        var apiUrl = $"IDOTA2Match_570/GetMatchHistory/v1/?account_id={accountId}&matches_requested={matchesCount}&key={_steamToken}";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var root = JsonSerializer.Deserialize<MatchHistoryRoot>(jsonResponse);
                return root.Result;
            }
            else
            {
                throw new Exception("Error: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error: " + ex.Message);
        }
    }

    private static AppSettings? ReadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables("DOTAHEAD_")
            .Build()
            .Get<AppSettings>();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class Match
{
    [JsonPropertyName("match_id")]
    public long MatchId { get; set; }

    [JsonPropertyName("match_seq_num")]
    public long MatchSeqNum { get; set; }

    [JsonPropertyName("start_time")]
    public int StartTime { get; set; }

    [JsonPropertyName("lobby_type")]
    public int LobbyType { get; set; }

    [JsonPropertyName("radiant_team_id")]
    public int RadiantTeamId { get; set; }

    [JsonPropertyName("dire_team_id")]
    public int DireTeamId { get; set; }

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; }
}

public class Player
{
    [JsonPropertyName("account_id")]
    public object AccountId { get; set; }

    [JsonPropertyName("player_slot")]
    public int PlayerSlot { get; set; }

    [JsonPropertyName("team_number")]
    public int TeamNumber { get; set; }

    [JsonPropertyName("team_slot")]
    public int TeamSlot { get; set; }

    [JsonPropertyName("hero_id")]
    public int HeroId { get; set; }
}

public class MatchHistoryResult
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("num_results")]
    public int NumResults { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    [JsonPropertyName("results_remaining")]
    public int ResultsRemaining { get; set; }

    [JsonPropertyName("matches")]
    public List<Match> Matches { get; set; }
}

public class MatchHistoryRoot
{
    [JsonPropertyName("result")]
    public MatchHistoryResult Result { get; set; }
}

