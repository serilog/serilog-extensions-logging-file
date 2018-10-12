using Serilog.Core;
using Serilog.Events;
using System.Linq;

namespace Serilog.Extensions.Logging.File
{
    class ScopeEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!logEvent.Properties.ContainsKey("Scope"))
                return;

            var scopes = logEvent.Properties["Scope"] as SequenceValue;

            if (scopes == null)
                return;

            string scopeValues = string.Join(" => ", scopes.Elements.Select(x => x.ToString()));

            logEvent.AddOrUpdateProperty(new LogEventProperty("Scopes", new ScalarValue(scopeValues)));
        }
    }
}
