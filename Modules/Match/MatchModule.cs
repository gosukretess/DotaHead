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

    [SlashCommand("icon-test", "Get data about last match")]
    public async Task IconTEst()
    {
        await DeferAsync();

        var embed = new EmbedBuilder
        {
            Title = "IconTest list",
            Color = Color.Green,
            Description = "<:dota_hero_morphling:448669324803047445>"
        };
        embed.AddField("fieldName", "jakis tekst", true);
        var path = "Assets/panorama/images/heroes/icons/npc_dota_hero_alchemist_png.png";
        // var path2 = "Assets/panorama/images/heroes/icons/npc_dota_hero_axe_png.png";
        embed.WithImageUrl(@$"attachment://{path}");
        // embed.WithImageUrl(@$"attachment://{path2}");

        var embedB = embed.Build();
        await Context.Channel.SendFilesAsync(new [] {new FileAttachment(path)}, null, false, null);

        
        await ModifyOriginalResponseAsync(q => q.Embed = embedB);
    }
}