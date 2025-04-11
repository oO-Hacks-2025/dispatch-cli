using Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.ApiClient;
using testing.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
ILogger<LocationsCache> cacheLogger = host.Services.GetRequiredService<ILogger<LocationsCache>>();

logger.LogInformation("Starting the HTTP client setup...");

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Add("User-Agent", "object Object agent");

var apiClient = new ApiClient();

logger.LogInformation("HTTP client configured.");

var cache = new LocationsCache(apiClient, cacheLogger);

var cacheGenerated = await cache.GenerateCache();