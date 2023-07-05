using DotaHead.Services;

namespace DotaHead;

public class AppSettings
{
    public string ConnectionString { get; set; }
    public string DiscordToken { get; set; }
    public string SteamToken { get; set; }
}