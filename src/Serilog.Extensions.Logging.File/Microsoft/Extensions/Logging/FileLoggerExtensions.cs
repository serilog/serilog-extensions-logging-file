using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Extensions.Logging.File;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extends <see cref="ILoggerFactory"/> with methods for configuring file logging.
/// </summary>
public static class FileLoggerExtensions
{
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

        var config = configuration.Get<FileLoggingConfiguration>();
        if (string.IsNullOrWhiteSpace(config?.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggerFactory;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);

        return loggerFactory.AddFile(config!.PathFormat!, minimumLevel, levelOverrides, config.Json, config.FileSizeLimitBytes, config.RetainedFileCountLimit, config.OutputTemplate);
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
    /// <param name="outputTemplate">The template used for formatting plain text log output. The default is
    /// "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}"</param>
    /// <returns>A logger factory to allow further configuration.</returns>
    public static ILoggerFactory AddFile(
        this ILoggerFactory loggerFactory,
        string pathFormat,
        LogLevel minimumLevel = LogLevel.Information,
        IDictionary<string, LogLevel>? levelOverrides = null,
        bool isJson = false,
        long? fileSizeLimitBytes = FileLoggingConfiguration.DefaultFileSizeLimitBytes,
        int? retainedFileCountLimit = FileLoggingConfiguration.DefaultRetainedFileCountLimit,
        string outputTemplate = FileLoggingConfiguration.DefaultOutputTemplate)
    {
        var logger = CreateLogger(pathFormat, minimumLevel, levelOverrides, isJson, fileSizeLimitBytes, retainedFileCountLimit, outputTemplate);
        return loggerFactory.AddSerilog(logger, dispose: true);
    }

    /// <summary>
    /// Adds a file logger initialized from the supplied configuration section.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder.</param>
    /// <param name="configuration">A configuration section with file parameters.</param>
    /// <returns>The logging builder to allow further configuration.</returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        if (loggingBuilder == null) throw new ArgumentNullException(nameof(loggingBuilder));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var config = configuration.Get<FileLoggingConfiguration>();
        if (string.IsNullOrWhiteSpace(config?.PathFormat))
        {
            SelfLog.WriteLine("Unable to add the file logger: no PathFormat was present in the configuration");
            return loggingBuilder;
        }

        var minimumLevel = GetMinimumLogLevel(configuration);
        var levelOverrides = GetLevelOverrides(configuration);

        return loggingBuilder.AddFile(config!.PathFormat!, minimumLevel, levelOverrides, config.Json, config.FileSizeLimitBytes, config.RetainedFileCountLimit, config.OutputTemplate);
    }

    /// <summary>
    /// Adds a file logger initialized from the supplied configuration section.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder.</param>
    /// <param name="pathFormat">Filname to write. The filename may include {Date} to specify how the date portion of the
    /// filename is calculated. May include environment variables.</param>
    /// <param name="minimumLevel">The level below which events will be suppressed (the default is <see cref="LogLevel.Information"/>).</param>
    /// <param name="levelOverrides">A dictionary mapping logger name prefixes to minimum logging levels.</param>
    /// <param name="isJson">If true, the log file will be written in JSON format.</param>
    /// <param name="fileSizeLimitBytes">The maximum size, in bytes, to which any single log file will be allowed to grow.
    /// For unrestricted growth, pass null. The default is 1 GB.</param>
    /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained, including the current
    /// log file. For unlimited retention, pass null. The default is 31.</param>
    /// <param name="outputTemplate">The template used for formatting plain text log output. The default is
    /// "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}"</param>
    /// <returns>The logging builder to allow further configuration.</returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder loggingBuilder,
        string pathFormat,
        LogLevel minimumLevel = LogLevel.Information,
        IDictionary<string, LogLevel>? levelOverrides = null,
        bool isJson = false,
        long? fileSizeLimitBytes = FileLoggingConfiguration.DefaultFileSizeLimitBytes,
        int? retainedFileCountLimit = FileLoggingConfiguration.DefaultRetainedFileCountLimit,
        string outputTemplate = FileLoggingConfiguration.DefaultOutputTemplate)
    {
        var logger = CreateLogger(pathFormat, minimumLevel, levelOverrides, isJson, fileSizeLimitBytes, retainedFileCountLimit, outputTemplate);

        return loggingBuilder.AddSerilog(logger, dispose: true);
    }

    static Serilog.Core.Logger CreateLogger(string pathFormat,
        LogLevel minimumLevel,
        IDictionary<string, LogLevel>? levelOverrides,
        bool isJson,
        long? fileSizeLimitBytes,
        int? retainedFileCountLimit,
        string outputTemplate)
    {
        if (pathFormat == null) throw new ArgumentNullException(nameof(pathFormat));

        if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

        var formatter = isJson ?
            (ITextFormatter)new RenderedCompactJsonFormatter() :
            new MessageTemplateTextFormatter(outputTemplate);

        var path = Environment.ExpandEnvironmentVariables(pathFormat);
        if (Path.GetFileNameWithoutExtension(path).EndsWith("{Date}"))
        {
            path = path.Replace("{Date}", "");
        }
        else
        {
            SelfLog.WriteLine("The log file name `{0}` should end with `{Date}`; other formats or rolling strategies are not supported", path);
        }

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelConvert.ToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .WriteTo.Async(w => w.File(
                formatter,
                path,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)));

        if (!isJson)
        {
            configuration.Enrich.With<EventIdEnricher>();
        }

        foreach (var levelOverride in levelOverrides ?? new Dictionary<string, LogLevel>())
        {
            configuration.MinimumLevel.Override(levelOverride.Key, LevelConvert.ToSerilogLevel(levelOverride.Value));
        }

        return configuration.CreateLogger();
    }

    static LogLevel GetMinimumLogLevel(IConfiguration configuration)
    {
        var minimumLevel = LogLevel.Information;
        var defaultLevel = configuration["LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(defaultLevel))
        {
            if (!Enum.TryParse(defaultLevel, out minimumLevel))
            {
                SelfLog.WriteLine("The minimum level setting `{0}` is invalid", defaultLevel);
                minimumLevel = LogLevel.Information;
            }
        }
        return minimumLevel;
    }

    static Dictionary<string, LogLevel> GetLevelOverrides(IConfiguration configuration)
    {
        var levelOverrides = new Dictionary<string, LogLevel>();
        foreach (var overr in configuration.GetSection("LogLevel").GetChildren().Where(cfg => cfg.Key != "Default"))
        {
            if (!Enum.TryParse(overr.Value, out LogLevel value))
            {
                SelfLog.WriteLine("The level override setting `{0}` for `{1}` is invalid", overr.Value, overr.Key);
                continue;
            }

            levelOverrides[overr.Key] = value;
        }

        return levelOverrides;
    }
}