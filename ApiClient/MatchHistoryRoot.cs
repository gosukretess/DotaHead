using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

public class MatchHistoryRoot
{
    [JsonPropertyName("result")]
    public MatchHistoryResult Result { get; set; }
}