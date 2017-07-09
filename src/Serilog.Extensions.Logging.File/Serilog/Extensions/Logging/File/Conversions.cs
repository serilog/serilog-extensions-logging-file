using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Serilog.Extensions.Logging.File
{
    static class Conversions
    {
        public static LogEventLevel MicrosoftToSerilogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                // as there is no match for 'None' in Serilog, pick the least logging possible
                case LogLevel.None:
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                // ReSharper disable once RedundantCaseLabel
                case LogLevel.Trace:
                default:
                    return LogEventLevel.Verbose;
            }
        }
    }
}
