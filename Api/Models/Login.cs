using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Api.Models;

public class LoginRequest(string userName, string password)
{
    [JsonPropertyName("userName")]
    public string UserName { get; private set; } = userName;

    [JsonPropertyName("password")]
    public string Password { get; private set; } = password;
}

public class LoginResponse(string token, string refreshToken)
{
    [JsonPropertyName("Token")]
    public string Token { get; private set; } = token;

    [JsonPropertyName("RefreshToken")]
    public string RefreshToken { get; private set; } = refreshToken;
}
