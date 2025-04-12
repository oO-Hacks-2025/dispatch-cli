using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.Models;

namespace testing.ApiClient;

public class ApiClient : HttpClient
{
    public ApiClient(string baseUrl = "http://localhost:5000/", string userAgent = "object Object agent")
    {
        BaseAddress = new Uri(baseUrl);
        DefaultRequestHeaders.Accept.Clear();
        DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        DefaultRequestHeaders.Add("User-Agent", userAgent);
    }

    #region Control
    public async Task<GameStatus> RunReset(string seed = "default", int targetDispatches = 10_000, int maxActiveCalls = 100)
    {
        var httpContent = new StringContent(JsonSerializer.Serialize(new { seed, targetDispatches, maxActiveCalls }));
        var result = await PostAsync(RequestPath.ControlReset, httpContent);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to reset the game. Status code: {result.StatusCode}");
        }
        var stream = await result.Content.ReadAsStreamAsync();
        var statusStream = await JsonSerializer.DeserializeAsync<GameStatus>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return statusStream;
    }

    public async Task<GameStatus> RunStop()
    {
        var result = await PostAsync(RequestPath.ControlStop, null);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to reset the game. Status code: {result.StatusCode}");
        }
        var stream = await result.Content.ReadAsStreamAsync();
        var statusStream = await JsonSerializer.DeserializeAsync<GameStatus>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return statusStream;
    }

    public async Task<GameStatus> RunStatus()
    {
        var stream = await GetStreamAsync(RequestPath.ControlStatus);
        var statusStream = await JsonSerializer.DeserializeAsync<GameStatus>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return statusStream;
    }
    #endregion


    #region Calls
    public async Task<Call> CallNext()
    {
        var stream = await GetStreamAsync(RequestPath.CallNext);
        var result = await JsonSerializer.DeserializeAsync<Call>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return result;
    }

    public async Task<List<Call>> CallQueue()
    {
        var stream = await GetStreamAsync(RequestPath.CallQueue);
        var result = await JsonSerializer.DeserializeAsync<List<Call>>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return result;
    }
    #endregion


    #region Locations
    public async Task<List<City>> Locations()
    {
        var stream = await GetStreamAsync(RequestPath.Locations);
        var result = await JsonSerializer.DeserializeAsync<List<City>>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return result;
    }
    #endregion

    #region Medical
    public async Task<List<Availability>> Search(ServiceType serviceType)
    {
        var stream = await GetStreamAsync($"{serviceType}/{RequestPath.Search}");
        var result = await JsonSerializer.DeserializeAsync<List<Availability>>(stream) ?? throw new Exception("Failed to deserialize the response.");
        return result;
    }

    public async Task<int> SearchByCity(ServiceType serviceType, string city, string county)
    {
        var query = new Dictionary<string, string>
        {
            { "county", county },
            { "city", city }
        };
        var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
        var stream = await GetStreamAsync($"{serviceType}/{RequestPath.SearchByCity}?{queryString}");
        var count = await JsonSerializer.DeserializeAsync<int>(stream);
        return count;
    }

    public async Task<string> Dispatch(string sourceCounty, string sourceCity, string targetCounty, string targetCity, int quantity, ServiceType serviceType)
    {
        var httpContent = new StringContent(JsonSerializer.Serialize(new Dispatch(sourceCounty, sourceCity, targetCounty, targetCity, quantity)));
        var result = await PostAsync($"/{serviceType}/{RequestPath.Dispatch}", httpContent);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to dispatch the game. Status code: {result.StatusCode}");
        }
        return result.Content.ToString() ?? throw new Exception("Failed to get the response content.");
    }
    #endregion
}