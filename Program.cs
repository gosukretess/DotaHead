using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotaHead.Database;
using DotaHead.Infrastructure;
using DotaHead.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotaHead;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        var configuration = ConfigurationLoader.Load();
        var client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Debug
        });

        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
            })
            .ConfigureServices((_, services) =>
                services
                    .AddSingleton(configuration)
                    .AddSingleton(client)
                    .AddDbContext<DataContext>(options => { options.UseSqlite(configuration.ConnectionString); })
                    .AddSingleton<HeroesService>()
                    .AddSingleton<MatchDetailsBuilder>()
                    .AddSingleton<MonitorsContainer>()
                    // Used for slash commands and their registration with Discord
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                    // Required to subscribe to the various client events used in conjunction with Interactions
                    .AddSingleton<InteractionHandler>()
            )
            .Build();
        
        StaticLoggerFactory.Initialize(host.Services.GetRequiredService<ILoggerFactory>());

        await RunAsync(host);
    }

    public async Task RunAsync(IHost host)
    {
        using var serviceScope = host.Services.CreateScope();
        var provider = serviceScope.ServiceProvider;

        var dbContext = provider.GetRequiredService<DataContext>();
        var config = provider.GetRequiredService<AppSettings>();
        var monitorsContainer = provider.GetRequiredService<MonitorsContainer>();
        var client = provider.GetRequiredService<DiscordSocketClient>();
        var commands = provider.GetRequiredService<InteractionService>();

        await dbContext.Database.MigrateAsync();

        await provider.GetRequiredService<InteractionHandler>().InitializeAsync();
        await provider.GetRequiredService<HeroesService>().InitializeAsync();


        client.Log += message => LogEvent(provider.GetRequiredService<ILogger<Program>>(), message);
        commands.Log += message => LogEvent(provider.GetRequiredService<ILogger<InteractionService>>(), message);

        client.JoinedGuild += guild => OnJoinedGuild(guild, dbContext, commands, monitorsContainer);
        client.Ready += async () =>
        {
            foreach (var server in dbContext.Servers)
            {
                await commands.RegisterCommandsToGuildAsync(server.GuildId);
                await monitorsContainer.AddMonitor(dbContext, server.GuildId);
            }
        };

        await client.LoginAsync(TokenType.Bot, config.DiscordToken);
        await client.StartAsync();

        await Task.Delay(-1);
    }

    private static async Task OnJoinedGuild(SocketGuild guild, DataContext dataContext, 
        InteractionService interactionService, MonitorsContainer monitorsContainer)
    {
        if (!dataContext.Servers.Any(s => s.GuildId == guild.Id))
        {
            await dataContext.Servers.AddAsync(new ServerDbo
            {
                GuildId = guild.Id,
                ChannelId = null,
                PeakHoursStart = 20,
                PeakHoursEnd = 24,
                PeakHoursRefreshTime = 5,
                NormalRefreshTime = 30
            });
        }

        await dataContext.SaveChangesAsync();

        await interactionService.RegisterCommandsToGuildAsync(guild.Id);
        await monitorsContainer.AddMonitor(dataContext, guild.Id);
    }

    private static Task LogEvent(ILogger logger, LogMessage message)
    {
        logger.LogInformation(message.Message);
        return Task.CompletedTask;
    }
}