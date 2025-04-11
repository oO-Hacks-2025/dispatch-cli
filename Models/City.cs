using System.Text.Json.Serialization;

namespace testing.Models;

/// <summary>
/// Represents a city with its name, county, latitude, and longitude.
/// </summary>
/// <param name="Name"></param>
/// <param name="County"></param>
/// <param name="Lat"></param>
/// <param name="Long"></param>
///
public class City
{
    /// <summary>
    /// The name of the city.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The county of the city.
    /// </summary>
    [JsonPropertyName("county")]
    public required string County { get; set; }

    /// <summary>
    /// The latitude of the city.
    /// </summary>
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    /// <summary>
    /// The longitude of the city.
    /// </summary>
    [JsonPropertyName("long")]
    public double Long { get; set; }

    public override string ToString() => $"{Name}, {County}";
}