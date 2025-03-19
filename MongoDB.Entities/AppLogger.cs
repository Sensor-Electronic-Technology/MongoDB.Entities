using Microsoft.Extensions.Logging;

namespace MongoDB.Entities;

internal class AppLogger {
    private static ILoggerFactory? _factory;
    private static ILoggerFactory LogFactory {
        get => _factory ??= LoggerFactory.Create(builder => builder.AddConsole()); 
        set => _factory = value;
    }
    public static ILogger CreateLogger(string categoryName) => LogFactory.CreateLogger(categoryName);
}