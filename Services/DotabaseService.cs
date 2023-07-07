using System.Text.Json;
using System.Text.Json.Serialization;
using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotaHead.Services;

public class DotabaseService
{
    public Dictionary<int, HeroRecord> Heroes { get; set; } = new();
    private Dictionary<string, long> Emojis { get; set; } = new();
    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<DotabaseService>();

    public async Task InitializeAsync()
    {
        await LoadHeroes();
        await LoadEmojis();
    }

    private async Task LoadHeroes()
    {
        var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/dotabase/heroes.json");
        Logger.LogInformation($"Loading heroes data from path: {jsonFilePath}");
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var result = JsonSerializer.Deserialize<IEnumerable<HeroRecord>>(jsonContent);
        if (result == null)
        {
            Logger.LogError("Error loading heroes data.");
            return;
        }
        Heroes = result.ToDictionary(q => q.Id, q => q);
        Logger.LogInformation($"Data for {Heroes?.Count ?? 0} heroes loaded.");

    }

    private async Task LoadEmojis()
    {
        var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/dotabase/emojis.json");
        Logger.LogInformation($"Loading emojis data from path: {jsonFilePath}");
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var result = JsonSerializer.Deserialize<Dictionary<string, long>>(jsonContent);
        if (result == null)
        {
            Logger.LogError("Error loading emojis data.");
            return;
        }

        Emojis = result;
        Logger.LogInformation($"Data for {Emojis?.Count ?? 0} emojis loaded.");
    }

    public string GetEmoji(string heroFullName)
    {
        var emojiName = heroFullName.Replace("npc_", "");
        return Emojis.TryGetValue(emojiName, out var emojiId) ? $"<:{emojiName}:{emojiId}>" : string.Empty;
    }
}

public class HeroRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("full_name")] 
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("localized_name")] 
    public string LocalizedName { get; set; } = string.Empty;

    [JsonPropertyName("icon")] 
    public string Icon { get; set; } = string.Empty;
}