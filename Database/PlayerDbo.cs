namespace DotaHead.Database;

public class PlayerDbo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public long DotaId { get; set; }
    public ulong DiscordId { get; set; }
}