using DotaHead.Services;

namespace DotaHead;

public class AppSettings
{
    public string ConnectionString { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string DiscordToken { get; set; }
    public int PeakHoursStart { get; set; }
    public int PeakHoursEnd { get; set; }
    public int PeakHoursRefreshTime { get; set; }
    public int NormalRefreshTime { get; set; }
}