using Discord.WebSocket;

namespace DotaHead;

public class ScheduledTask
{
    private Timer _timer;
    private readonly AppSettings _appSettings;
    private readonly DiscordSocketClient _client;

    public ScheduledTask(AppSettings appSettings, DiscordSocketClient client)
    {
        _appSettings = appSettings;
        _client = client;
    }

    public async Task StartAsync()
    {
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, CalculateInterval());

        await Task.Delay(0);
    }

    private async void TimerCallback(object? state)
    {
        await ExecuteTaskAsync();

        var interval = CalculateInterval();
        _timer.Change(interval, interval);
    }

    private async Task ExecuteTaskAsync()
    {
        await _client.GetGuild(_appSettings.GuildId).GetTextChannel(_appSettings.ChannelId).SendMessageAsync("scheduled message");
    }

    private TimeSpan CalculateInterval()
    {
        var now = DateTime.Now.Hour;

        // Peak hours in one day
        if (_appSettings.PeakHoursEnd > _appSettings.PeakHoursStart)
        {
            if (now >= _appSettings.PeakHoursStart && now < _appSettings.PeakHoursEnd)
            {
                return TimeSpan.FromMinutes(_appSettings.PeakHoursRefreshTime);
            }
        }
        // Peak hours cross the midnight
        else
        {
            if (now >= _appSettings.PeakHoursStart || now < _appSettings.PeakHoursEnd)
            {
                return TimeSpan.FromMinutes(_appSettings.PeakHoursRefreshTime);
            }
        }


        return TimeSpan.FromMinutes(_appSettings.NormalRefreshTime);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }
}