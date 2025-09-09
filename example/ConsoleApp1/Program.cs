using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddFile("logs/myapp-{Date}.txt");
});

using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var startTime = DateTimeOffset.UtcNow;
logger.LogInformation(1, "Started at {StartTime} and 0x{Hello:X} is hex of 42", startTime, 42);

try
{
    throw new Exception("Boom!");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unexpected critical error starting application");
}

using (logger.BeginScope("Main"))
{
    logger.LogInformation("Waiting for user input");
    var key = Console.Read();
    logger.LogInformation("User pressed {@KeyInfo}", new { Key = key, KeyChar = (char)key });
}

logger.LogInformation("Stopping");

