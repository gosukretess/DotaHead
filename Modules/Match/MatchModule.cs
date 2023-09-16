using Discord;
using Discord.Interactions;
using DotaHead.ApiClient;
using DotaHead.Database;
using DotaHead.Infrastructure;
using DotaHead.MatchMonitor;
using Microsoft.Extensions.Logging;
using OpenDotaApi;

namespace DotaHead.Modules.Match;

public class MatchModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DataContext _dataContext;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchModule>();

    public MatchModule(DataContext dataContext, MatchDetailsBuilder matchDetailsBuilder)
    {
        _dataContext = dataContext;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    [SlashCommand("last-match", "Get data about last match")]
    public async Task LastMatch()
    {
        await DeferAsync();

        var currentUser =
            _dataContext.Players.FirstOrDefault(p => p.DiscordId == Context.User.Id && p.GuildId == Context.Guild.Id);
        if (currentUser == null) return;

        var openDotaClient = new OpenDota();
        var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(currentUser.DotaId);
        var lastMatch = recentMatches.FirstOrDefault();

        if (lastMatch?.MatchId == null) return;

        Logger.LogInformation($"{Context.User.GlobalName} requested data about matchId: {lastMatch.MatchId}");

        var fetcher = new MatchDetailsFetcher();
        var matchDetails = await fetcher.GetMatchDetails(lastMatch.MatchId.Value);
        if (matchDetails == null)
        {
            Logger.LogWarning($"Failed to get match {lastMatch.MatchId.Value} details.");
            return;
        }
        if (matchDetails.Version == null)
        {
            Logger.LogInformation($"Match {lastMatch.MatchId.Value} replay not available. Will be re-fetched in next iteration.");
            await ModifyOriginalResponseAsync(r =>
            {
                r.Content = new Optional<string>(
                    $"Replay for match {lastMatch.MatchId.Value} is not yet available. Please try again later.");
            });
            return;
        }

        var playerDbos = _dataContext.Players.Where(p => p.GuildId == Context.Guild.Id).ToList();
        var embed = await _matchDetailsBuilder.Build(matchDetails, playerDbos);

        await ModifyOriginalResponseAsync(r =>
        {
            r.Embed = embed.Embed;
            r.Attachments = new Optional<IEnumerable<FileAttachment>>(new[] { new FileAttachment(embed.ImagePath) });
        });
    }

    [SlashCommand("match", "Get data about specific match")]
    public async Task GetMatch(long matchId)
    {
        await DeferAsync();

        Logger.LogInformation($"{Context.User.GlobalName} requested data about matchId: {matchId}");

        using var steamApiClient = new SteamApiClient();
        var steamMatch = await steamApiClient.GetMatchDetails(matchId);

        if (steamMatch == null)
        {
            await ModifyOriginalResponseAsync(r =>
            {
                r.Content = new Optional<string>(
                    $"There was an error reading data for match: {matchId}.");
            });
            return;
        }

        if (steamMatch.Error != null)
        {
            await ModifyOriginalResponseAsync(r =>
            {
                r.Content = new Optional<string>(
                    $"{steamMatch.Error} ({matchId})");
            });
            return;
        }

        var fetcher = new MatchDetailsFetcher();
        var matchDetails = await fetcher.GetMatchDetails(matchId);
        if (matchDetails == null)
        {
            Logger.LogWarning($"Failed to get match {matchId} details.");
            return;
        }
        if (matchDetails.Version == null)
        {
            Logger.LogInformation($"Match {matchId} replay not available. Will be re-fetched in next iteration.");
            await ModifyOriginalResponseAsync(r =>
            {
                r.Content = new Optional<string>(
                    $"Replay for match {matchId} is not yet available. Please try again later.");
            });
            return;
        }

        var playerDbos = _dataContext.Players.Where(p => p.GuildId == Context.Guild.Id).ToList();
        var embed = await _matchDetailsBuilder.Build(matchDetails, playerDbos);

        await ModifyOriginalResponseAsync(r =>
        {
            r.Embed = embed.Embed;
            r.Attachments = new Optional<IEnumerable<FileAttachment>>(new[] { new FileAttachment(embed.ImagePath) });
        });
    }

}