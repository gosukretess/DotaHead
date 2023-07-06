using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DotaHead.Infrastructure;

public static class StaticLoggerFactory
{
    private static ILoggerFactory? _loggerFactory;
    private static readonly ConcurrentDictionary<Type, ILogger> LoggerByType = new();

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        if (_loggerFactory is not null)
            throw new InvalidOperationException("StaticLogger already initialized!");

        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public static ILogger GetStaticLogger<T>()
    {
        if (_loggerFactory is null)
            throw new InvalidOperationException("StaticLogger is not initialized!");

        return LoggerByType
            .GetOrAdd(typeof(T), _loggerFactory.CreateLogger<T>());
    }
}