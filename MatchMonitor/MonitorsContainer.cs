using Discord.WebSocket;
using DotaHead.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotaHead.MatchMonitor;

public class MonitorsContainer
{
    private readonly DiscordSocketClient _client;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<MatchMonitor> _monitors = new();
    private static ILogger Logger => StaticLoggerFactory.GetStaticLogger<MonitorsContainer>();


    public MonitorsContainer(DiscordSocketClient client, MatchDetailsBuilder matchDetailsBuilder, IServiceScopeFactory scopeFactory)
    {
        _client = client;
        _matchDetailsBuilder = matchDetailsBuilder;
        _scopeFactory = scopeFactory;
    }


    public async Task AddMonitor(ulong guildId)
    {
        // TODO: Disable monitors for Emoji Servers

        if (_monitors.All(m => m.GuildId != guildId))
        {
            var monitor = new MatchMonitor(_client, guildId, _matchDetailsBuilder, _scopeFactory);
            await monitor.StartAsync();
            _monitors.Add(monitor);
            Logger.LogInformation($"Successfully added match monitor for GuildId: {guildId}");
        }
        else
        {
            Logger.LogInformation($"Monitor already exists for GuildId: {guildId}");
        }
    }


    public void StopAll()
    {
        foreach (var matchMonitor in _monitors)
        {
            matchMonitor.Dispose();
        }
    }
}