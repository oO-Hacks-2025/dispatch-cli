using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///  Call represents a service call with its location and the number of ambulances requested.
/// </summary>
/// <param name="county"></param>
/// <param name="city"></param>
/// <param name="latitude"></param>
/// <param name="longitude"></param>
/// <param name="requests"></param>
public class Call(string county, string city, double latitude, double longitude, List<ServiceRequest> requests)
{
    /// <summary>
    ///     The county of the call.
    /// </summary>
    [JsonPropertyName("county")]
    public string County { get; private set; } = county;


    /// <summary>
    ///    The city of the call.
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; private set; } = city;

    /// <summary>
    ///    The latitude of the call.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = latitude;


    /// <summary>
    ///  The longitude of the call.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = longitude;


    /// <summary>
    /// The number of ambulances requested.
    /// </summary>
    [JsonPropertyName("requests")]
    public List<ServiceRequest> Requests { get; set; } = requests;
}
