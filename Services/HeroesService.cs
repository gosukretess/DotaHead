using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDotaApi;
using OpenDotaApi.Enums;

namespace DotaHead.Services;

public class HeroesService
{
    public Dictionary<int, HeroRecord>? Heroes { get; set; }

    public async Task InitializeAsync()
    {
        var openDota = new OpenDota();
        var result = await openDota.Constants.GetGameConstantsAsync(EnumConstants.HeroNames);

        var heroDictionary = JsonSerializer.Deserialize<Dictionary<string, HeroRecord>>(result);

        Heroes = heroDictionary?.Values.ToDictionary(q => q.Id, q => q);
    }
}

public class HeroRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("localized_name")]
    public string LocalizedName { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}