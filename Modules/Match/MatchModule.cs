﻿using Discord.Interactions;
using DotaHead.Database;
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

        var currentUser = _dataContext.Players.FirstOrDefault(p => p.DiscordId == Context.User.Id);
        if (currentUser == null) return;

        var openDotaClient = new OpenDota();
        var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(currentUser.DotaId);
        var lastMatch = recentMatches.FirstOrDefault();

        if (lastMatch?.MatchId == null) return;
        var embed = await _matchDetailsBuilder.Build(lastMatch.MatchId!.Value, lastMatch.Version != null);

        await ModifyOriginalResponseAsync(r => r.Embed = embed);
    }
}