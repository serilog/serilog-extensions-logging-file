using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging.File;
using System.Linq;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using Serilog.Formatting;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extends <see cref="ILoggerFactory"/> with methods for configuring file logging.
    /// </summary>
    public static class FileLoggerExtensions
    {
        const long DefaultFileSizeLimitBytes = 1024 * 1024 * 1024;
        const int DefaultRetainedFileCountLimit = 31;

        /// <summary>
        /// Adds a file logger initialized from the supplied configuration section.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">A configuration section with file parameters.</param>
        /// <returns>A logger factory to allow further configuration.</returns>
        public static ILoggerFactory AddFile(this ILoggerFactory loggerFactory, IConfigurationSection configuration)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var pathFormat = configuration["PathFormat"];
            if (string.IsNullOrWhiteSpace(pathFormat))
            {
                SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
                return loggerFactory;
            }

            var json = configuration["Json"];
            var isJson = false;
            if (!string.IsNullOrWhiteSpace(json))
                bool.TryParse(json, out isJson);

            long? fileSizeLimitBytes = DefaultFileSizeLimitBytes;
            var fileSizeConfiguration = configuration.GetChildren().SingleOrDefault(ch => ch.Key == "FileSizeLimitBytes");
            if (fileSizeConfiguration != null)
            {
                fileSizeLimitBytes = null;
                if (!string.IsNullOrWhiteSpace(fileSizeConfiguration.Value))
                    fileSizeLimitBytes = long.Parse(fileSizeConfiguration.Value);
            }

            int? retainedFileCountLimit = DefaultRetainedFileCountLimit;
            var retainedLimitConfiguration = configuration.GetChildren().SingleOrDefault(ch => ch.Key == "RetainedFileCountLimit");
            if (retainedLimitConfiguration != null)
            {
                retainedFileCountLimit = null;
                if (!string.IsNullOrWhiteSpace(retainedLimitConfiguration.Value))
                    retainedFileCountLimit = int.Parse(retainedLimitConfiguration.Value);
            }

            var minimumLevel = LogLevel.Information;
            var levelSection = configuration.GetSection("LogLevel");
            var defaultLevel = levelSection["Default"];
            if (!string.IsNullOrWhiteSpace(defaultLevel))
            {
                if (!Enum.TryParse(defaultLevel, out minimumLevel))
                {
                    SelfLog.WriteLine("The minimum level setting `{0}` is invalid", defaultLevel);
                    minimumLevel = LogLevel.Information;
                }
            }

            var levelOverrides = new Dictionary<string, LogLevel>();
            foreach (var overr in levelSection.GetChildren().Where(cfg => cfg.Value != "Default"))
            {
                LogLevel value;
                if (!Enum.TryParse(overr.Value, out value))
                {
                    SelfLog.WriteLine("The level override setting `{0}` for `{1}` is invalid", overr.Value, overr.Key);
                    continue;
                }

                levelOverrides[overr.Key] = value;
            }

            return loggerFactory.AddFile(pathFormat, minimumLevel, levelOverrides, isJson, fileSizeLimitBytes, retainedFileCountLimit);
        }

        /// <summary>
        /// Adds a file logger initialized from the supplied configuration section.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="pathFormat">Filname to write. The filename may include {Date} to specify how the date portion of the 
        /// filename is calculated. May include environment variables.</param>
        /// <param name="minimumLevel">The level below which events will be suppressed (the default is <see cref="LogLevel.Information"/>).</param>
        /// <param name="levelOverrides">A dictionary mapping logger name prefixes to minimum logging levels.</param>
        /// <param name="isJson">If true, the log file will be written in JSON format.</param>
        /// <param name="fileSizeLimitBytes">The maximum size, in bytes, to which any single log file will be allowed to grow.
        /// For unrestricted growth, pass null. The default is 1 GB.</param>
        /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained, including the current
        /// log file. For unlimited retention, pass null. The default is 31.</param>
        /// <returns>A logger factory to allow further configuration.</returns>
        public static ILoggerFactory AddFile(
            this ILoggerFactory loggerFactory,
            string pathFormat,
            LogLevel minimumLevel = LogLevel.Information,
            IDictionary<string, LogLevel> levelOverrides = null,
            bool isJson = false,
            long? fileSizeLimitBytes = DefaultFileSizeLimitBytes,
            int? retainedFileCountLimit = DefaultRetainedFileCountLimit)
        {
            if (pathFormat == null) throw new ArgumentNullException(nameof(pathFormat));

            var formatter = isJson ?
                (ITextFormatter)new RenderedCompactJsonFormatter() :
                new MessageTemplateTextFormatter("{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}", null);

#if SHARING
            const bool sharingSupported = true;
#else
            const bool sharingSupported = false;
#endif

            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(Conversions.MicrosoftToSerilogLevel(minimumLevel))
                .Enrich.FromLogContext()
                .WriteTo.Async(w => w.RollingFile(
                    formatter, 
                    Environment.ExpandEnvironmentVariables(pathFormat),
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    retainedFileCountLimit: retainedFileCountLimit,
                    shared: sharingSupported, 
                    flushToDiskInterval: TimeSpan.FromSeconds(2)));

            if (!isJson)
            {
                configuration.Enrich.With<EventIdEnricher>();
            }

            foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
            {
                configuration.MinimumLevel.Override(levelOverride.Key, Conversions.MicrosoftToSerilogLevel(levelOverride.Value));
            }

            var logger = configuration.CreateLogger();
            return loggerFactory.AddSerilog(logger, dispose: true);
        }
    }
}
