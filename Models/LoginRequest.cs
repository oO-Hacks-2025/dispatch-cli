using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///  Call represents a service call with its location and the number of ambulances requested.
/// </summary>
/// <param name="username"></param>
/// <param name="password"></param>
public class LoginRequest(string userName, string password)
{
    /// <summary>
    ///     The county of the call.
    /// </summary>
    [JsonPropertyName("userName")]
    public string UserName { get; private set; } = userName;


    /// <summary>
    ///    The city of the call.
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; private set; } = password;
}
