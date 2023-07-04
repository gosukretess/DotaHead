using System.Text;
using Discord;
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

    [SlashCommand("set-channel", "Sets this channel as match monitor target channel")]
    public async Task SetChannel()
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo== null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
            return;
        }

        serverDbo!.ChannelId = Context.Channel.Id;
        await _dataContext.SaveChangesAsync();

        await ModifyOriginalResponseAsync(r => r.Content = "Channel set successfully!");
    }

    [SlashCommand("server-config", "Displays bot configuration for this server")]
    public async Task DisplayServerConfig()
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo == null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
            return;
        }

        var response = new StringBuilder();

        response.AppendLine($"Peak hours: {serverDbo!.PeakHoursStart} - {serverDbo!.PeakHoursEnd}");
        response.AppendLine($"Peak hours refresh interval: {serverDbo!.PeakHoursRefreshTime}");
        response.AppendLine($"Normal hours refresh interval: {serverDbo!.NormalRefreshTime}");

       var embed = new EmbedBuilder
        {
            Title = "Server configuration",
            Description = response.ToString(),
            Color = Color.Blue
        };

        await ModifyOriginalResponseAsync(r => r.Embed = embed.Build());
    }

    [SlashCommand("set-peak-hours", "Set peak hours")]
    public async Task SetPeakHours(int start, int end)
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo == null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
            return;
        }

        await ValidateTime(start);
        await ValidateTime(end);

        serverDbo!.PeakHoursStart = start;
        serverDbo!.PeakHoursEnd = end;
        await _dataContext.SaveChangesAsync();

        await ModifyOriginalResponseAsync(r => r.Content = "Peak hours changed successfully!");

        async Task ValidateTime(int time)
        {
            if (time is < 1 or > 24)
            {
                await ModifyOriginalResponseAsync(r => r.Content = "Time must be between 1 and 24");
            }
        }
    }

    [SlashCommand("set-peak-hours-refresh", "Set peak hours refresh interval in minutes")]
    public async Task SetPeakHoursRefresh(int minutes)
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo == null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
            return;
        }

        if (minutes < 1)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Interval value must be bigger than 0");
        }

        serverDbo!.PeakHoursRefreshTime = minutes;
        await _dataContext.SaveChangesAsync();

        await ModifyOriginalResponseAsync(r => r.Content = "Peak hours refresh interval set successfully!");
    }

    [SlashCommand("set-normal-hours-refresh", "Set normal hours refresh interval in minutes")]
    public async Task SetNormalHoursRefresh(int minutes)
    {
        await DeferAsync();
        var serverDbo = _dataContext.Servers.FirstOrDefault(s => s.GuildId == Context.Guild.Id);
        if (serverDbo == null)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Error retrieving server data");
            return;
        }

        if (minutes < 1)
        {
            await ModifyOriginalResponseAsync(r => r.Content = "Interval value must be bigger than 0");
        }

        serverDbo!.NormalRefreshTime = minutes;
        await _dataContext.SaveChangesAsync();

        await ModifyOriginalResponseAsync(r => r.Content = "Normal hours refresh interval set successfully!");
    }
}