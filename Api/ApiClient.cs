using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EmergencyDispatcher.Api.Models;
using EmergencyDispatcher.Cache;
using EmergencyDispatcher.Domain.Models;
using Polly;
using Polly.Retry;

namespace EmergencyDispatcher.Api;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncRetryPolicy _authRetryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiClient> _logger;
    private readonly TokenCache _tokenCache;

    public ApiClient(
        TokenCache tokenCache,
        ILogger<ApiClient> logger,
        string baseUrl,
        string userAgent
    )
    {
        _tokenCache = tokenCache;
        _logger = logger;

        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System
                .Text
                .Json
                .Serialization
                .JsonIgnoreCondition
                .WhenWritingNull,
        };

        _retryPolicy = CreateRetryPolicy();
        _authRetryPolicy = CreateAuthRetryPolicy();
    }

    #region Policy Configuration
    private AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>(ex =>
                ex.StatusCode == HttpStatusCode.ServiceUnavailable
                || ex.StatusCode == HttpStatusCode.GatewayTimeout
                || ex.StatusCode == HttpStatusCode.InternalServerError
                || ex.StatusCode == HttpStatusCode.BadGateway
                || ex.StatusCode == HttpStatusCode.TooManyRequests
            )
            .Or<TimeoutException>()
            .Or<ValidationException>()
            .Or<JsonException>()
            .WaitAndRetryAsync(
                7,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        $"Retry {retryCount} after {timeSpan.TotalSeconds}s delay. Error: {exception.Message}"
                    );
                }
            );
    }

    private AsyncRetryPolicy CreateAuthRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.Unauthorized)
            .RetryAsync(
                5,
                async (exception, retryCount) =>
                {
                    _logger.LogWarning(
                        $"Authentication failed: {exception.Message} Attempting token refresh."
                    );

                    try
                    {
                        var refreshToken = _tokenCache.GetRefreshToken();

                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            var newTokens = await PostRefreshToken(refreshToken);
                            _tokenCache.SetToken(newTokens.Token);
                            _tokenCache.SetRefreshToken(newTokens.RefreshToken);

                            _logger.LogInformation("Token refreshed successfully.");
                        }
                        else
                        {
                            _logger.LogWarning("No refresh token available.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Token refresh failed: {ex.Message}");
                    }
                }
            );
    }
    #endregion

    private async Task<T> ExecuteWithAuth<T>(Func<Task<T>> action)
    {
        return await _authRetryPolicy.ExecuteAsync(async () =>
        {
            SetBearerJwtHeader(_tokenCache.GetToken());
            return await _retryPolicy.ExecuteAsync(action);
        });
    }

    #region Control Operations
    public async Task<GameStatus?> PostRunReset(
        string seed,
        int targetDispatches,
        int maxActiveCalls
    )
    {
        return await ExecuteWithAuth(async () =>
        {
            var query =
                $"?seed={WebUtility.UrlEncode(seed)}&targetDispatches={targetDispatches}&maxActiveCalls={maxActiveCalls}";
            var response = await _httpClient.PostAsync(RequestPaths.ControlReset + query, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }

    public async Task<GameStatus?> PostRunStop()
    {
        return await ExecuteWithAuth(async () =>
        {
            var response = await _httpClient.PostAsync(RequestPaths.ControlStop, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }

    public async Task<GameStatus?> GetRunStatus()
    {
        return await ExecuteWithAuth(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPaths.ControlStatus);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStatus>(_jsonOptions);
        });
    }
    #endregion

    #region Call Operations
    public async Task<Call?> GetCallNext()
    {
        return await ExecuteWithAuth(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPaths.CallNext);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No calls in queue");
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Call>(_jsonOptions);
        });
    }

    public async Task<List<Call>?> GetCallQueue()
    {
        return await ExecuteWithAuth(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPaths.CallQueue);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Call>>(_jsonOptions);
        });
    }
    #endregion

    #region Location Operations
    public async Task<List<City>?> GetLocations()
    {
        return await ExecuteWithAuth(async () =>
        {
            var response = await _httpClient.GetAsync(RequestPaths.Locations);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<City>>(_jsonOptions);
        });
    }
    #endregion

    #region Service Operations
    public async Task<List<Availability>?> GetServiceAvailability(ServiceType serviceType)
    {
        return await ExecuteWithAuth(async () =>
        {
            var endpoint = $"{serviceType}/{RequestPaths.Search}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var availability =
                await response.Content.ReadFromJsonAsync<List<Availability>>(_jsonOptions)
                ?? throw new ValidationException("Server returned null availability data");

            ValidateAvailabilityData(availability);

            return availability;
        });
    }

    private void ValidateAvailabilityData(List<Availability> availability)
    {
        foreach (var item in availability)
        {
            if (
                item.Latitude < -90
                || item.Latitude > 90
                || item.Longitude < -180
                || item.Longitude > 180
            )
            {
                _logger.LogError(
                    $"Invalid coordinates: {JsonSerializer.Serialize(item, _jsonOptions)}"
                );
                throw new ValidationException("Server returned invalid coordinates");
            }

            if (string.IsNullOrWhiteSpace(item.County) || string.IsNullOrWhiteSpace(item.City))
            {
                _logger.LogError(
                    $"Missing location data: {JsonSerializer.Serialize(item, _jsonOptions)}"
                );
                throw new ValidationException("Server returned incomplete location data");
            }

            if (item.Quantity < 0)
            {
                _logger.LogError(
                    $"Invalid quantity: {JsonSerializer.Serialize(item, _jsonOptions)}"
                );
                throw new ValidationException("Server returned negative quantity value");
            }
        }
    }

    public async Task<int> GetServiceAvailabilityByCity(
        ServiceType serviceType,
        string county,
        string city
    )
    {
        return await ExecuteWithAuth(async () =>
        {
            if (string.IsNullOrWhiteSpace(county) || string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("County and city parameters cannot be empty");
            }

            var endpoint =
                $"{serviceType}/{RequestPaths.SearchByCity}?county={WebUtility.UrlEncode(county)}&city={WebUtility.UrlEncode(city)}";
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

    public async Task<bool> PostServiceDispatch(
        string sourceCounty,
        string sourceCity,
        string targetCounty,
        string targetCity,
        int quantity,
        ServiceType serviceType
    )
    {
        return await ExecuteWithAuth(async () =>
        {
            if (
                string.IsNullOrWhiteSpace(sourceCounty)
                || string.IsNullOrWhiteSpace(sourceCity)
                || string.IsNullOrWhiteSpace(targetCounty)
                || string.IsNullOrWhiteSpace(targetCity)
            )
            {
                throw new ArgumentException("County and city parameters cannot be empty");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero");
            }

            var dispatch = new Dispatch(
                sourceCounty,
                sourceCity,
                targetCounty,
                targetCity,
                quantity
            );
            var endpoint = $"{serviceType}/{RequestPaths.Dispatch}";

            var response = await _httpClient.PostAsJsonAsync(endpoint, dispatch, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return true;
        });
    }
    #endregion

    #region Authentication Operations
    public async Task<LoginResponse> PostLogin(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Username and password cannot be empty");
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var loginRequest = new LoginRequest(userName, password);
            var response = await _httpClient.PostAsJsonAsync(
                RequestPaths.Login,
                loginRequest,
                _jsonOptions
            );
            response.EnsureSuccessStatusCode();

            var loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions)
                ?? throw new ValidationException("Server returned null login response");

            return loginResponse;
        });
    }

    public async Task<LoginResponse> PostRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token cannot be empty");
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                RequestPaths.RefreshToken
            );
            requestMessage.Headers.TryAddWithoutValidation("refresh_token", refreshToken);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions)
                ?? throw new ValidationException("Server returned null login response");
        });
    }

    private void SetBearerJwtHeader(string token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );
        }
    }
    #endregion
}
