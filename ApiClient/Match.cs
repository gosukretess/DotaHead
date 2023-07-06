using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

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