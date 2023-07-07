using Discord;
using Discord.Interactions;
using DotaHead.Database;
using DotaHead.MatchMonitor;
using OpenDotaApi;

namespace DotaHead.Modules.Match;

public class MatchModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DataContext _dataContext;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;

    public MatchModule(DataContext dataContext, MatchDetailsBuilder matchDetailsBuilder)
    {
        _dataContext = dataContext;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    [SlashCommand("last-match", "Get data about last match")]
    public async Task LastMatch()
    {
        await DeferAsync();

        var currentUser = _dataContext.Players.FirstOrDefault(p => p.DiscordId == Context.User.Id && p.GuildId == Context.Guild.Id);
        if (currentUser == null) return;

        var openDotaClient = new OpenDota();
        var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(currentUser.DotaId);
        var lastMatch = recentMatches.FirstOrDefault();

        if (lastMatch?.MatchId == null) return;
        var playerDbos = _dataContext.Players.Where(p => p.GuildId == Context.Guild.Id).ToList();
        var embed = await _matchDetailsBuilder.Build(lastMatch.MatchId!.Value, playerDbos);

        await ModifyOriginalResponseAsync(r => r.Embed = embed);
    }
}