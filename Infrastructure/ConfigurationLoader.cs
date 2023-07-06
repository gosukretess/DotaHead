using Microsoft.Extensions.Configuration;

namespace DotaHead.Infrastructure;

public static class ConfigurationLoader
{
    public static AppSettings Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true);

#if DEBUG
        builder = builder.AddJsonFile("appsettings.Debug.json", true, true);
#endif

        var appSettings = builder.AddEnvironmentVariables("DOTAHEAD_")
            .Build()
            .Get<AppSettings>();

        if (appSettings == null)
            throw new ApplicationException("Cannot map appsettings.json content.");

        return appSettings;
    }
}