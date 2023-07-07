using Discord.WebSocket;
using DotaHead.ApiClient;
using DotaHead.Database;
using DotaHead.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotaHead.MatchMonitor;

public class MatchMonitor
{
    private Timer? _timer;
    private readonly DiscordSocketClient _client;
    private readonly DataContext _dataContext;
    private readonly ulong _guildId;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private ServerDbo? _serverDbo;

    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchMonitor>();

    public MatchMonitor(DiscordSocketClient client, DataContext dataContext, ulong guildId,
        MatchDetailsBuilder matchDetailsBuilder)
    {
        _client = client;
        _dataContext = dataContext;
        _guildId = guildId;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    public async Task StartAsync()
    {
        _serverDbo = await _dataContext.Servers.FirstOrDefaultAsync(s => s.GuildId == _guildId);
        if (_serverDbo == null) return;

        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, CalculateInterval());
        await Task.Delay(0);
    }

    private async void TimerCallback(object? state)
    {
        await ExecuteTaskAsync();

        var interval = CalculateInterval();
        _timer?.Change(interval, interval);
    }

    private async Task ExecuteTaskAsync()
    {
        Logger.LogInformation($"Checking for new matches... (GuildId: {_guildId})");
        await _dataContext.Entry(_serverDbo!).ReloadAsync();
        if (_serverDbo!.ChannelId == null)
        {
            Logger.LogWarning($"ChannelId not configured for Guild: {_guildId}!");
            return;
        }

        var matchIdsToRequest = new List<long>();
        using var steamApiClient = new SteamApiClient();
        var playerDbos = _dataContext.Players.Where(p => p.GuildId == _guildId).ToList();

        // Prepare list of matches to fetch
        foreach (var player in playerDbos)
        {
            var recentMatches = await steamApiClient.GetMatchHistory(player.DotaId, 1);

            if (recentMatches?.Status == 15)
            {
                Logger.LogWarning($"{player.Name} profile is private, cannot fetch matches!");
                continue;
            }

            var lastMatch = recentMatches?.Matches.FirstOrDefault();

            if (lastMatch?.MatchId == null) continue;
            if (_dataContext.Matches.Where(m => m.GuildId == _guildId).Any(m => m.MatchId == lastMatch.MatchId)) continue;
            if (matchIdsToRequest.Any(m => m == lastMatch.MatchId)) continue;

            matchIdsToRequest.Add(lastMatch.MatchId);
        }

        if (matchIdsToRequest.Count == 0)
        {
            Logger.LogInformation("No new matches found.");
            return;
        }

        foreach (var matchId in matchIdsToRequest)
        {
            var embed = await _matchDetailsBuilder.Build(matchId, playerDbos);
            await _client.GetGuild(_guildId).GetTextChannel(_serverDbo!.ChannelId!.Value)
                .SendMessageAsync(embed: embed);
            await _dataContext.Matches.AddAsync(new MatchDbo { MatchId = matchId, GuildId = _guildId});
            await _dataContext.SaveChangesAsync();
        }

        Logger.LogInformation("Finished checking for new matches.");
    }

    private TimeSpan CalculateInterval()
    {
        var now = DateTime.Now.Hour;

        if (_serverDbo!.PeakHoursEnd > _serverDbo!.PeakHoursStart) // Peak hours in one day
        {
            if (now >= _serverDbo.PeakHoursStart && now < _serverDbo.PeakHoursEnd)
                return TimeSpan.FromMinutes(_serverDbo.PeakHoursRefreshTime);
        }
        else // Peak hours cross the midnight
        {
            if (now >= _serverDbo.PeakHoursStart || now < _serverDbo.PeakHoursEnd)
                return TimeSpan.FromMinutes(_serverDbo.PeakHoursRefreshTime);
        }

        return TimeSpan.FromMinutes(_serverDbo.NormalRefreshTime);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}