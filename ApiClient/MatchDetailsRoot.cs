using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

public class MatchDetailsRoot
{
    [JsonPropertyName("result")]
    public MatchDetailsResult Result { get; set; }
}