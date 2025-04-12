using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///  Call represents a service call with its location and the number of ambulances requested.
/// </summary>
/// <param name="token"></param>
/// <param name="refreshToken"></param>
public class LoginResponse(string token, string refreshToken)
{
    [JsonPropertyName("Token")]
    public string Token { get; private set; } = token;

    [JsonPropertyName("RefreshToken")]
    public string RefreshToken { get; private set; } = refreshToken;
}
