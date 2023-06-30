using Discord.WebSocket;
using DotaHead.Database;
using Microsoft.Extensions.Logging;

namespace DotaHead;

public class MonitorsContainer
{
    private readonly DiscordSocketClient _client;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<MatchMonitor> _monitors = new();


    public MonitorsContainer(DiscordSocketClient client, MatchDetailsBuilder matchDetailsBuilder, ILoggerFactory loggerFactory)
    {
        _client = client;
        _matchDetailsBuilder = matchDetailsBuilder;
        _loggerFactory = loggerFactory;
    }


    public async Task AddMonitor(DataContext dataContext, ulong guildId)
    {
        var monitor = new MatchMonitor(_client, dataContext, guildId, _matchDetailsBuilder, _loggerFactory);
        await monitor.StartAsync();
        _monitors.Add(monitor);
    }


    public void StopAll()
    {
        foreach (var matchMonitor in _monitors)
        {
            matchMonitor.Stop();
        }
    }
}