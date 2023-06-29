using Discord.WebSocket;
using DotaHead.Database;
using OpenDotaApi;

namespace DotaHead;

public class ScheduledTask
{
    private Timer? _timer;
    private readonly AppSettings _appSettings;
    private readonly DiscordSocketClient _client;
    private readonly DataContext _dataContext;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;

    public ScheduledTask(AppSettings appSettings, DiscordSocketClient client, DataContext dataContext,
        MatchDetailsBuilder matchDetailsBuilder)
    {
        _appSettings = appSettings;
        _client = client;
        _dataContext = dataContext;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    public async Task StartAsync()
    {
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
        var matchIdsToRequest = new List<(long matchId, bool isParsed)>();
        var openDotaClient = new OpenDota();

        // Prepare list of matches to fetch
        foreach (var playerDotaId in _dataContext.Players.Select(p => p.DotaId))
        {
            var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(playerDotaId);
            var lastMatch = recentMatches.FirstOrDefault();

            if (lastMatch?.MatchId == null) continue;
            if (_dataContext.Matches.Any(m => m.MatchId == lastMatch.MatchId)) continue;
            if (matchIdsToRequest.Any(m => m.matchId == lastMatch.MatchId)) continue;

            matchIdsToRequest.Add((lastMatch.MatchId!.Value, lastMatch.Version != null));
        }

        if (matchIdsToRequest.Count == 0) return;

        foreach (var (matchId, isParsed) in matchIdsToRequest)
        {
            var embed = await _matchDetailsBuilder.Build(matchId, isParsed);
            await _client.GetGuild(_appSettings.GuildId).GetTextChannel(_appSettings.ChannelId)
                .SendMessageAsync(embed: embed);
            await _dataContext.Matches.AddAsync(new MatchDbo { MatchId = matchId });
            await _dataContext.SaveChangesAsync();
        }
    }

    private TimeSpan CalculateInterval()
    {
        var now = DateTime.Now.Hour;

        if (_appSettings.PeakHoursEnd > _appSettings.PeakHoursStart) // Peak hours in one day
        {
            if (now >= _appSettings.PeakHoursStart && now < _appSettings.PeakHoursEnd)
                return TimeSpan.FromMinutes(_appSettings.PeakHoursRefreshTime);
        }
        else // Peak hours cross the midnight
        {
            if (now >= _appSettings.PeakHoursStart || now < _appSettings.PeakHoursEnd)
                return TimeSpan.FromMinutes(_appSettings.PeakHoursRefreshTime);
        }

        return TimeSpan.FromMinutes(_appSettings.NormalRefreshTime);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}