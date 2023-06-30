namespace DotaHead.Database;

public class ServerDbo  
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong? ChannelId { get; set; }
    public int PeakHoursStart { get; set; }
    public int PeakHoursEnd { get; set; }
    public int PeakHoursRefreshTime { get; set; }
    public int NormalRefreshTime { get; set; }
}