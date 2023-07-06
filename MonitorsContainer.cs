using Discord.WebSocket;
using DotaHead.ApiClient;
using DotaHead.Database;
using DotaHead.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotaHead;

public class MonitorsContainer
{
    private readonly DiscordSocketClient _client;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private readonly List<MatchMonitor> _monitors = new();
    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<SteamApiClient>();


    public MonitorsContainer(DiscordSocketClient client, MatchDetailsBuilder matchDetailsBuilder)
    {
        _client = client;
        _matchDetailsBuilder = matchDetailsBuilder;
    }


    public async Task AddMonitor(DataContext dataContext, ulong guildId)
    {
        var monitor = new MatchMonitor(_client, dataContext, guildId, _matchDetailsBuilder);
        await monitor.StartAsync();
        _monitors.Add(monitor);
        Logger.LogInformation($"Successfully added match monitor for ServerId: {guildId}");
    }


    public void StopAll()
    {
        foreach (var matchMonitor in _monitors)
        {
            matchMonitor.Stop();
        }
    }
}