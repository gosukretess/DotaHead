using Discord.WebSocket;
using DotaHead.ApiClient;
using DotaHead.Database;
using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotaHead.MatchMonitor;

public class MonitorsContainer
{
    private readonly DiscordSocketClient _client;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private readonly List<MatchMonitor> _monitors = new();
    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<MonitorsContainer>();


    public MonitorsContainer(DiscordSocketClient client, MatchDetailsBuilder matchDetailsBuilder)
    {
        _client = client;
        _matchDetailsBuilder = matchDetailsBuilder;
    }


    public async Task AddMonitor(DataContext dataContext, ulong guildId)
    {
        if (_monitors.All(m => m.GuildId != guildId))
        {
            var monitor = new MatchMonitor(_client, dataContext, guildId, _matchDetailsBuilder);
            await monitor.StartAsync();
            _monitors.Add(monitor);
            Logger.LogInformation($"Successfully added match monitor for ServerId: {guildId}");
        }
        else
        {
            Logger.LogInformation($"Monitor already exists for ServerId: {guildId}");
        }
    }


    public void StopAll()
    {
        foreach (var matchMonitor in _monitors)
        {
            matchMonitor.Stop();
        }
    }
}