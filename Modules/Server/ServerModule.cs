using Discord.Interactions;
using DotaHead.Database;

namespace DotaHead.Modules.Server;

public class ServerModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DataContext _dataContext;

    public ServerModule(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [SlashCommand("channel-set", "Sets this channel as match monitor target channel")]
    public async Task SetChannel()
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo== null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
        }

        serverDbo!.ChannelId = Context.Channel.Id;
        await _dataContext.SaveChangesAsync();

        await ModifyOriginalResponseAsync(r => r.Content = "Channel set successfully!");
    }
}