# Serilog.Extensions.Logging.File&nbsp;[![Build status](https://github.com/serilog/serilog-extensions-logging-file/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/serilog/serilog-extensions-logging-file/actions)&nbsp;[![NuGet Version](http://img.shields.io/nuget/v/Serilog.Extensions.Logging.File.svg?style=flat)](https://www.nuget.org/packages/Serilog.Extensions.Logging.File/)

This package makes it a one-liner - `loggingBuilder.AddFile()` - to configure top-quality file logging for ASP.NET Core apps.

 * Text or JSON file output
 * Files roll over on date; capped file size
 * Request ids and event ids included with each message
 * Log writes are performed asynchronously
 * Files are periodically flushed to disk (required for Azure App Service log collection)
 * Fast, stable, battle-proven logging code courtesy of [Serilog](https://serilog.net)

You can get started quickly with this package, and later migrate to the full Serilog API if you need more sophisticated log file configuration.

### Versioning

This package tracks the version of ASP.NET Core that it targets. If you're using 9.x.x of ASP.NET Core, use 9.x.x of _Serilog.Extensions.Logging.File_, and so on.

> If the version you're using doesn't have a corresponding _Serilog.Extensions.Logging.File_ release, target v3.x of this package.

### Getting started

**1.** Add [the NuGet package](https://nuget.org/packages/serilog.extensions.logging.file) as a dependency of your project either with the package manager or directly to the CSPROJ file:

```xml
<PackageReference Include="Serilog.Extensions.Logging.File" Version="9.0.0" />
```

**2.** In your `Program` class, configure logging on the host builder, and call `AddFile()` on the provided `ILoggingBuilder`:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddFile("Logs/myapp-{Date}.txt");
});

```

**Done!** The framework will inject `ILogger` instances into controllers and other classes:

```csharp
class HomeController : Controller
{
    readonly ILogger<HomeController> _log;

    public HomeController(ILogger<HomeController> log)
    {
        _log = log;
    }

    public IActionResult Index()
    {
        _log.LogInformation("Hello, world!");
    }
}
```

The events will appear in the log file:

```
2016-10-18T11:14:11.0881912+10:00 0HKVMUG8EMJO9 [INF] Hello, world! (f83bcf75)
```

### File format

By default, the file will be written in plain text. The fields in the log file are:

| Field | Description | Format | Example |
| ----- | ----------- | ------ | ------- |
| **Timestamp** | The time the event occurred. | ISO-8601 with offset | `2016-10-18T11:14:11.0881912+10:00`  |
| **Request id** | Uniquely identifies all messages raised during a single web request. | Alphanumeric | `0HKVMUG8EMJO9` |
| **Level** | The log level assigned to the event. | Three-character code in brackets | `[INF]` |
| **Message** | The log message associated with the event. | Free text | `Hello, world!` |
| **Event id** | Identifies messages generated from the same format string/message template. | 32-bit hexadecimal, in parentheses | `(f83bcf75)` |
| **Exception** | Exception associated with the event. | `Exception.ToString()` format (not shown) | `System.DivideByZeroException: Attempt to divide by zero\r\n\  at...` |

To record events in newline-separated JSON instead, specify `isJson: true` when configuring the logger:

```csharp
loggingBuilder.AddFile("Logs/myapp-{Date}.txt", isJson: true);
```

This will produce a log file with lines like:

```json
{"@t":"2016-06-07T03:44:57.8532799Z","@m":"Hello, world!","@i":"f83bcf75","RequestId":"0HKVMUG8EMJO9"}
```

The JSON document includes all properties associated with the event, not just those present in the message. This makes JSON formatted logs a better choice for offline analysis in many cases.

### Rolling

The filename provided to `AddFile()` should include the `{Date}` placeholder, which will be replaced with the date of the events contained in the file. Filenames use the `yyyyMMdd` date format so that files can be ordered using a lexicographic sort:

```
log-20160631.txt
log-20160701.txt
log-20160702.txt
```

To prevent outages due to disk space exhaustion, each file is capped to 1 GB in size. If the file size is exceeded, events will be dropped until the next roll point.

### Message templates and event ids

The provider supports the templated log messages used by _Microsoft.Extensions.Logging_. By writing events with format strings or [message templates](https://messagetemplates.org), the provider can infer which messages came from the same logging statement.

This means that although the text of two messages may be different, their **event id** fields will match, as shown by the two "view" logging statements below:

```
2016-10-18T11:14:26.2544709+10:00 0HKVMUG8EMJO9 [INF] Running view at "/Views/Home/About.cshtml". (9707eebe)
2016-10-18T11:14:11.0881912+10:00 0HKVMUG8EMJO9 [INF] Hello, world! (f83bcf75)
2016-10-18T11:14:26.2544709+10:00 0HKVMUG8EMJO9 [INF] Running view at "/Views/Home/Index.cshtml". (9707eebe)
```

Each log message describing view rendering is tagged with `(9707eebe)`, while the "hello" log message is given `(f83bcf75)`. This makes it easy to search the log for messages describing the same kind of event.

### Additional configuration

The `AddFile()` method exposes some basic options for controlling the connection and log volume.

| Parameter | Description | Example value |
| --------- | ----------- | ------------- |
| `pathFormat` | Filename to write. The filename may include `{Date}` to specify how the date portion of the filename is calculated. May include environment variables.| `Logs/log-{Date}.txt` |
| `minimumLevel` | The level below which events will be suppressed (the default is `LogLevel.Information`). | `LogLevel.Debug` |
| `levelOverrides` | A dictionary mapping logger name prefixes to minimum logging levels. | |
| `isJson` | If true, the log file will be written in JSON format. | `true` |
| `fileSizeLimitBytes` | The maximum size, in bytes, to which any single log file will be allowed to grow. For unrestricted growth, pass`null`. The default is 1 GiB. | `1024 * 1024 * 1024` |
| `retainedFileCountLimit` | The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass `null`. The default is `31`. | `31` |
| `outputTemplate` | The template used for formatting plain text log output. The default is `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}` | `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} {Properties:j} ({EventId:x8}){NewLine}{Exception}` |

### `appsettings.json` configuration

The file path and other settings can be read from JSON configuration if desired.

In `appsettings.json` add a `"Logging"` property:

```json
{
  "Logging": {
    "PathFormat": "Logs/log-{Date}.txt",
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

And then pass the configuration section to the `AddFile()` method:

```csharp
loggingBuilder.AddFile(hostingContext.Configuration.GetSection("Logging"));
```

In addition to the properties shown above, the `"Logging"` configuration supports:

| Property | Description | Example |
| -------- | ----------- | ------- |
| `Json` | If `true`, the log file will be written in JSON format. | `true` |
| `FileSizeLimitBytes` | The maximum size, in bytes, to which any single log file will be allowed to grow. For unrestricted growth, pass`null`. The default is 1 GiB. | `1024 * 1024 * 1024` |
| `RetainedFileCountLimit` | The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass `null`. The default is `31`. | `31` |
| `OutputTemplate` | The template used for formatting plain text log output. The default is `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}` | `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} {Properties:j} ({EventId:x8}){NewLine}{Exception}` |

### Using the full Serilog API

This package is opinionated, providing the most common/recommended options supported by Serilog. For more sophisticated configuration, using Serilog directly is recommended. See the instructions in [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) to get started.

The following packages are used to provide `loggingBuilder.AddFile()`:

 * [Serilog](https://github.com/serilog/serilog) - the core logging pipeline
 * [Serilog.Formatting.Compact](https://github.com/serilog/serilog-formatting-compact) - JSON event formatting
 * [Serilog.Extensions.Logging](https://github.com/serilog/serilog-extensions-logging) - ASP.NET Core integration
 * [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) - wrapper to perform log writes asynchronously
 * [Serilog.Sinks.RollingFile](https://github.com/serilog/serilog-sinks-rollingfile) - rolling file output

If you decide to switch to the full Serilog API and need help, please drop into the [Gitter channel](https://gitter.im/serilog/serilog) or post your question on [Stack Overflow](http://stackoverflow.com/questions/tagged/serilog).
