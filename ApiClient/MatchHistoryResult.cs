using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

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