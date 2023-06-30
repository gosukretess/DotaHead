namespace DotaHead.Database;

public record MatchDbo
{
    public long MatchId { get; set; }
    public ulong GuildId { get; set; }
}