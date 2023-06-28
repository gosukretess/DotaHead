namespace DotaHead.Services;

public class PlayersService
{
    public List<UserRecord> PlayerIds { get; set; }

    public PlayersService(AppSettings appSettings)
    {
        PlayerIds = appSettings.Players;
    }
}

public class UserRecord
{
    public long DiscordId { get; set; }
    public long DotaId { get; set; }

}