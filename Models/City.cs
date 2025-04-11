using System.Text.Json.Serialization;

namespace testing.Models;

public class City
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("county")]
    public string County { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("long")]
    public double Long { get; set; }

    public override string ToString() => $"{Name}, {County}";
}