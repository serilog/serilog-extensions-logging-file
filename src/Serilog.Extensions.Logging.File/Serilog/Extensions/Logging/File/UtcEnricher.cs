using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Serilog.Extensions.Logging.File
{
    class UtcEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty("UtcDateTime", new ScalarValue(DateTime.UtcNow)));
        }
    }
}
