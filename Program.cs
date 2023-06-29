using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DNet_V3_Tutorial;
using DotaHead.Database;
using DotaHead.Logger;
using DotaHead.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotaHead;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();


    public async Task MainAsync()
    {
        var configuration = ReadAppSettings();
        var client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Debug
        });

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
                services
                    .AddSingleton(configuration)
                    .AddSingleton(client)
                    .AddDbContext<DataContext>(options =>
                    {
                        options.UseSqlite(configuration.ConnectionString);
                    })
                    .AddSingleton<HeroesService>()
                    .AddTransient<ConsoleLogger>()
                    .AddSingleton<MatchDetailsBuilder>()
                    // Used for slash commands and their registration with Discord
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                    // Required to subscribe to the various client events used in conjunction with Interactions
                    .AddSingleton<InteractionHandler>()
                    .AddSingleton<ScheduledTask>()
                    )
            .Build();
        
        await RunAsync(host);
    }

    private static AppSettings ReadAppSettings()
    {
        var appSettings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build()
            .Get<AppSettings>();

        if (appSettings == null)
            throw new ApplicationException("Cannot map appsettings.json content.");

        return appSettings;
    }

    public async Task RunAsync(IHost host)
    {
        using var serviceScope = host.Services.CreateScope();
        var provider = serviceScope.ServiceProvider;

        var dbContext = provider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync();

        var commands = provider.GetRequiredService<InteractionService>();
        var client = provider.GetRequiredService<DiscordSocketClient>();
        var config = provider.GetRequiredService<AppSettings>();
        var scheduledTask = provider.GetRequiredService<ScheduledTask>();


        await provider.GetRequiredService<InteractionHandler>().InitializeAsync();
        await provider.GetRequiredService<HeroesService>().InitializeAsync();

        // Subscribe to client log events
        client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
        // Subscribe to slash command log events
        commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

        client.Ready += async () =>
        {
            await commands.RegisterCommandsToGuildAsync(config.GuildId);
            await scheduledTask.StartAsync();
        };


        await client.LoginAsync(TokenType.Bot, config.DiscordToken);
        await client.StartAsync();


        await Task.Delay(-1);
        scheduledTask.Stop();
    }
}