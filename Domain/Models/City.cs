using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class City
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("county")]
    public required string County { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("long")]
    public double Long { get; set; }

    public override string ToString() => $"{Name}, {County}";
}
