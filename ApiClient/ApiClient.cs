using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.Models;

namespace testing.ApiClient;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        string baseUrl = "http://localhost:5000/",
        string userAgent = "Emergency Dispatcher")
    {
        _logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger<ApiClient>();

        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Increase timeout

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Improved retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>(ex =>
                ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
                ex.StatusCode == HttpStatusCode.GatewayTimeout ||
                ex.StatusCode == HttpStatusCode.InternalServerError ||
                ex.StatusCode == HttpStatusCode.BadGateway ||
                ex.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<TimeoutException>()
            .Or<ValidationException>()
            .Or<JsonException>()
            .WaitAndRetryAsync(
                7,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                          $"Retry {retryCount} after {timeSpan.TotalSeconds}s delay. Error: {exception.Message}");
                });
    }

    #region Control Operations
    public async Task<GameStatus?> PostRunReset(string seed, int targetDispatches, int maxActiveCalls)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var query = $"?seed={seed}&targetDispatches={targetDispatches}&maxActiveCalls={maxActiveCalls}";
            var response = await _httpClient.PostAsync(RequestPath.ControlReset + query, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }

    public async Task<GameStatus?> PostRunStop()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsync(RequestPath.ControlStop, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }

    public async Task<GameStatus?> GetRunStatus()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPath.ControlStatus);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }
    #endregion

    #region Call Operations
    public async Task<Call?> GetCallNext()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPath.CallNext);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No calls in queue");
                throw new EmptyQueueException("No calls in queue");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Call>(_jsonOptions);
        });
    }

    public async Task<List<Call>?> GetCallQueue()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPath.CallQueue);
            response.EnsureSuccessStatusCode();
            var calls = await response.Content.ReadFromJsonAsync<List<Call>>(_jsonOptions);
            return calls;
        });
    }
    #endregion

    #region Location Operations
    public async Task<List<City>?> GetLocations()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPath.Locations);
            response.EnsureSuccessStatusCode();
            var cities = await response.Content.ReadFromJsonAsync<List<City>>(_jsonOptions);
            return cities;
        });
    }
    #endregion

    #region Service Operations
    public async Task<List<Availability>?> GetServiceAvailability(ServiceType serviceType)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var endpoint = $"{serviceType}/{RequestPath.Search}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var availability = await response.Content.ReadFromJsonAsync<List<Availability>>(_jsonOptions)
             ?? throw new ValidationException("Server returned null availability data");

            if (availability.Any(a => a.Latitude < -90 || a.Latitude > 90 || a.Longitude < -180 || a.Longitude > 180))
            {
                _logger.LogError($"Invalid coordinates: {JsonSerializer.Serialize(availability, _jsonOptions)}");
                throw new ValidationException("Server returned invalid availability entries");
            }

            if (availability.Any(a => string.IsNullOrWhiteSpace(a.County) || string.IsNullOrWhiteSpace(a.City)))
            {
                _logger.LogError($"Invalid coordinates: {JsonSerializer.Serialize(availability, _jsonOptions)}");
                throw new ValidationException("Server returned invalid availability entries");
            }

            if (availability.Any(a => a.Quantity < 0))
            {
                _logger.LogError($"Invalid coordinates: {JsonSerializer.Serialize(availability, _jsonOptions)}");
                throw new ValidationException("Server returned invalid availability entries");
            }

            return availability;
        });
    }

    public async Task<int> GetServiceAvailabilityByCity(ServiceType serviceType, string county, string city)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var endpoint = $"/{serviceType}/{RequestPath.SearchByCity}?county={WebUtility.UrlEncode(county)}&city={WebUtility.UrlEncode(city)}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ValidationException("Server returned empty response");
            }

            if (int.TryParse(content, out int count))
            {
                if (count < 0)
                {
                    throw new ValidationException($"Invalid count: {content}");
                }
                return count;
            }

            throw new FormatException($"Invalid response format: {content}");
        });
    }

    public async Task<bool> PostServiceDispatch(string sourceCounty, string sourceCity, string targetCounty, string targetCity, int quantity, ServiceType serviceType)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var dispatch = new Dispatch(sourceCounty, sourceCity, targetCounty, targetCity, quantity);
            var endpoint = $"/{serviceType}/{RequestPath.Dispatch}";
            var response = await _httpClient.PostAsJsonAsync(endpoint, dispatch, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return true;
        });
    }
    #endregion
}

public class EmptyQueueException(string message) : Exception(message);
public class ValidationException(string message) : Exception(message);