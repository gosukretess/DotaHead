using System.Text.Json;
using System.Text.Json.Serialization;
using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotaHead.Services;

public class DotabaseService
{
    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<DotabaseService>();
    public Dictionary<int, HeroRecord> Heroes { get; set; } = new();
    public Dictionary<long, ItemRecord> Items { get; set; } = new();
    private Dictionary<string, long> Emojis { get; set; } = new();

    public async Task InitializeAsync()
    {
        await LoadHeroes();
        await LoadEmojis();
        await LoadItems();
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
        Logger.LogInformation($"Data for {Heroes.Count} heroes loaded.");
    }

    private async Task LoadItems()
    {
        var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/dotabase/items.json");
        Logger.LogInformation($"Loading items data from path: {jsonFilePath}");
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var result = JsonSerializer.Deserialize<IEnumerable<ItemRecord>>(jsonContent);
        if (result == null)
        {
            Logger.LogError("Error loading items data.");
            return;
        }
        Items = result.ToDictionary(q => q.Id, q => q);
        Logger.LogInformation($"Data for {Items.Count} items loaded.");
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
        Logger.LogInformation($"Data for {Emojis.Count} emojis loaded.");
    }

    public string GetEmoji(string heroFullName)
    {
        var emojiName = heroFullName.Replace("npc_dota_hero_", "dota_");
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

    [JsonPropertyName("image")] 
    public string Image { get; set; } = string.Empty;
}

public class ItemRecord
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}