using System.Text.Json.Serialization;

namespace DotaHead.ApiClient;

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