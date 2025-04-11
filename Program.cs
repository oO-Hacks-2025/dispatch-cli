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
    })
    .Build();

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting the HTTP client setup...");

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Add("User-Agent", "object Object agent");

var apiClient = new ApiClient();

var locations = await apiClient.Locations();
Console.WriteLine("Locations:");
Console.WriteLine(JsonSerializer.Serialize(locations, new JsonSerializerOptions { WriteIndented = true }));

var status = await apiClient.RunReset();
Console.WriteLine("Game reset successfully.");
Console.WriteLine("Game status:");
Console.WriteLine(JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true }));

var nextCall = await apiClient.CallNext();
Console.WriteLine("Next call:");
Console.WriteLine(JsonSerializer.Serialize(nextCall, new JsonSerializerOptions { WriteIndented = true }));


Console.WriteLine("Game status after next call:");
var statusAfterNext = await apiClient.RunStatus();
Console.WriteLine(JsonSerializer.Serialize(statusAfterNext, new JsonSerializerOptions { WriteIndented = true }));


var queue = await apiClient.CallQueue();
Console.WriteLine("Queue:");
Console.WriteLine(JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true }));

var searchFire = await apiClient.Search(ServiceType.Fire);
Console.WriteLine("Search fire:");
Console.WriteLine(JsonSerializer.Serialize(searchFire, new JsonSerializerOptions { WriteIndented = true }));
var searchByCity = await apiClient.SearchByCity(ServiceType.Fire, locations[0]);
Console.WriteLine("Search fire by city:");
Console.WriteLine(JsonSerializer.Serialize(searchByCity, new JsonSerializerOptions { WriteIndented = true }));

var searchPolice = await apiClient.Search(ServiceType.Police);
Console.WriteLine("Search police:");
Console.WriteLine(JsonSerializer.Serialize(searchPolice, new JsonSerializerOptions { WriteIndented = true }));
var searchByCityPolice = await apiClient.SearchByCity(ServiceType.Police, locations[0]);
Console.WriteLine("Search police by city:");
Console.WriteLine(JsonSerializer.Serialize(searchByCityPolice, new JsonSerializerOptions { WriteIndented = true }));

var searchMedical = await apiClient.Search(ServiceType.Medical);
Console.WriteLine("Search medical:");
Console.WriteLine(JsonSerializer.Serialize(searchMedical, new JsonSerializerOptions { WriteIndented = true }));
var searchByCityMedical = await apiClient.SearchByCity(ServiceType.Medical, locations[0]);
Console.WriteLine("Search medical by city:");
Console.WriteLine(JsonSerializer.Serialize(searchByCityMedical, new JsonSerializerOptions { WriteIndented = true }));

var searchRescue = await apiClient.Search(ServiceType.Rescue);
Console.WriteLine("Search rescue:");
Console.WriteLine(JsonSerializer.Serialize(searchRescue, new JsonSerializerOptions { WriteIndented = true }));
var searchByCityRescue = await apiClient.SearchByCity(ServiceType.Rescue, locations[0]);
Console.WriteLine("Search rescue by city:");
Console.WriteLine(JsonSerializer.Serialize(searchByCityRescue, new JsonSerializerOptions { WriteIndented = true }));
var searchUtility = await apiClient.Search(ServiceType.Utility);

Console.WriteLine("Search utility:");
Console.WriteLine(JsonSerializer.Serialize(searchUtility, new JsonSerializerOptions { WriteIndented = true }));
var searchByCityUtility = await apiClient.SearchByCity(ServiceType.Utility, locations[0]);
Console.WriteLine("Search utility by city:");
Console.WriteLine(JsonSerializer.Serialize(searchByCityUtility, new JsonSerializerOptions { WriteIndented = true }));
var searchRescueByCity = await apiClient.SearchByCity(ServiceType.Rescue, locations[0]);

Console.WriteLine("Game status after stopping:");
var statusAfterStop = await apiClient.RunStop();
Console.WriteLine(JsonSerializer.Serialize(statusAfterStop, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine("Game stopped successfully.");
Console.WriteLine("Game status after stopping:");
Console.WriteLine(JsonSerializer.Serialize(statusAfterStop, new JsonSerializerOptions { WriteIndented = true }));

logger.LogInformation("HTTP client configured.");
