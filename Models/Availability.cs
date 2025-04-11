using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///     Availability represents the availability of a service in a specific location.
/// </summary>
/// <param name="county"></param>
/// <param name="city"></param>
/// <param name="latitude"></param>
/// <param name="longitude"></param>
/// <param name="quantity"></param>
public class Availability(string county, string city, double latitude, double longitude, int quantity)
{
    /// <summary>
    /// County of the location.
    /// </summary>
    [JsonPropertyName("county")]
    public string County { get; private set; } = county;

    /// <summary>
    /// City of the location.
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; private set; } = city;

    /// <summary>
    ///  Latitude of the location.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = latitude;

    /// <summary>
    /// The longitude of the location.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = longitude;

    /// <summary>
    /// The quantity of ambulances at the location.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = quantity;
}