using Discord;
using Discord.WebSocket;
using DotaHead.ApiClient;
using DotaHead.Database;
using DotaHead.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotaHead.MatchMonitor;

public class MatchMonitor
{
    public readonly ulong GuildId;
    private Timer? _timer;
    private readonly DiscordSocketClient _client;
    private readonly DataContext _dataContext;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private ServerDbo? _serverDbo;

    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchMonitor>();

    public MatchMonitor(DiscordSocketClient client, DataContext dataContext, ulong guildId,
        MatchDetailsBuilder matchDetailsBuilder)
    {
        GuildId = guildId;
        _client = client;
        _dataContext = dataContext;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    public async Task StartAsync()
    {
        _serverDbo = await _dataContext.Servers.FirstOrDefaultAsync(s => s.GuildId == GuildId);
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
        Logger.LogInformation($"Checking for new matches... (GuildId: {GuildId})");
        await _dataContext.Entry(_serverDbo!).ReloadAsync();
        if (_serverDbo!.ChannelId == null)
        {
            Logger.LogWarning($"ChannelId not configured. (Guild: {GuildId})");
            return;
        }

        var matchIdsToRequest = new List<long>();
        using var steamApiClient = new SteamApiClient();
        var playerDbos = _dataContext.Players.Where(p => p.GuildId == GuildId).ToList();

        // Prepare list of matches to fetch
        foreach (var player in playerDbos)
        {
            var recentMatches = await steamApiClient.GetMatchHistory(player.DotaId, 1);

            if (recentMatches == null)
            {
                Logger.LogError("There was a problem getting match history from SteamApi.");
                continue;
            }

            if (recentMatches.Status == 15)
            {
                Logger.LogWarning($"{player.Name} profile is private, cannot fetch matches!");
                continue;
            }

            var lastMatch = recentMatches.Matches.FirstOrDefault();

            if (lastMatch?.MatchId == null) continue;
            if (_dataContext.Matches.Where(m => m.GuildId == GuildId).Any(m => m.MatchId == lastMatch.MatchId)) continue;
            if (matchIdsToRequest.Any(m => m == lastMatch.MatchId)) continue;

            matchIdsToRequest.Add(lastMatch.MatchId);
        }

        if (matchIdsToRequest.Count == 0)
        {
            Logger.LogInformation($"No new matches found. (Guild: {GuildId})");
            return;
        }

        foreach (var matchId in matchIdsToRequest)
        {
            Logger.LogInformation($"Found new match to request: {matchId}. (Guild: {GuildId})");

            var fetcher = new MatchDetailsFetcher();
            var matchDetails = await fetcher.GetMatchDetails(matchId);
            if (matchDetails.Version == null)
            {
                Logger.LogInformation($"Match {matchId} replay not available. Will be re-fetched in next iteration.");
                continue;
            }   

            var embed = await _matchDetailsBuilder.Build(matchDetails, playerDbos);
            var channelContext = _client.GetGuild(GuildId).GetTextChannel(_serverDbo!.ChannelId!.Value);
            await channelContext.SendFileAsync(embed.ImagePath, embed: embed.Embed);
            await _dataContext.Matches.AddAsync(new MatchDbo { MatchId = matchId, GuildId = GuildId});
            await _dataContext.SaveChangesAsync();
            Logger.LogInformation($"Successfully sent message with details of match {matchId}. (Guild: {GuildId})");
        }

        Logger.LogInformation($"Finished checking for new matches. (Guild: {GuildId})");
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