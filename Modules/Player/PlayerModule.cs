using System.Text;
using Discord;
using Discord.Interactions;
using DotaHead.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DotaHead.Modules.Player;

public class PlayerModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DataContext _dataContext;

    public PlayerModule(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [SlashCommand("player-list", "Get players list")]
    public async Task GetAllPlayers()
    {
        await DeferAsync();

        var players = _dataContext.Players.Where(p => p.GuildId == Context.Guild.Id).ToList();
        var response = new StringBuilder();

        foreach (var player in players)
        {
            response.Append($"{player.Name} - [[OpenDota](https://www.opendota.com/players/{player.DotaId})] " +
                            $"[[DotaBuff](https://pl.dotabuff.com/players/{player.DotaId})] " +
                            $"[[Stratz](https://stratz.com/players/{player.DotaId})] \n");
        }

        var embed = new EmbedBuilder
        {
            Title = "Players list",
            Description = response.ToString(),
            Color = Color.Blue
        };

        await ModifyOriginalResponseAsync(r => r.Embed = embed.Build());
    }

    [SlashCommand("player-add", "Add player")]
    public async Task AddPlayer(string name, long dotaId, string discordId)
    {
        // TODO: Add discordId parse validation

        await DeferAsync();
        var player = await AddPlayerToDatabase(name, dotaId, ulong.Parse(discordId));
        await ModifyOriginalResponseAsync(r => r.Content = $"Player {player.Entity.Name} added!");
    }

    [SlashCommand("player-addme", "Add me as player")]
    public async Task AddMePlayer(long dotaId)
    {
        await DeferAsync();
        var player = await AddPlayerToDatabase(Context.User.GlobalName, dotaId, Context.User.Id);
        await ModifyOriginalResponseAsync(r => r.Content = $"Player {player.Entity.Name} added!");
    }

    [SlashCommand("player-remove", "Remove player")]
    public async Task RemovePlayer(string name)
    {
        await DeferAsync();
        var player = await _dataContext.Players.FirstOrDefaultAsync(p => p.Name == name && p.GuildId == Context.Guild.Id);
        if (player == null)
        {
            await ReplyAsync($"Player with a name {name} does not exist!");
            return;
        }
        _dataContext.Players.Remove(player);
        await _dataContext.SaveChangesAsync();
        await ModifyOriginalResponseAsync(r => r.Content = $"Player {name} removed!");
    }

    private async Task<EntityEntry<PlayerDbo>> AddPlayerToDatabase(string name, long dotaId, ulong discordId)
    {
        var player = await _dataContext.Players.AddAsync(
            new PlayerDbo
            {
                Name = name,
                DotaId = dotaId,
                DiscordId = discordId,
                GuildId = Context.Guild.Id
            });
        await _dataContext.SaveChangesAsync();
        return player;
    }
}