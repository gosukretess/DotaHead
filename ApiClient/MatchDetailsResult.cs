using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

public class MatchDetailsResult
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

}