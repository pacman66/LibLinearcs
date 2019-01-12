using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public static class ApplicationLogging {

    public static NLogLoggerFactory LoggerFactory {get;} = new NLogLoggerFactory();
    public static ILogger<T> CreateLogger<T>() =>
        LoggerFactory.CreateLogger<T>();
    }
}