using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class Availability(
    string county,
    string city,
    double latitude,
    double longitude,
    int quantity
)
{
    [JsonPropertyName("county")]
    public string County { get; private set; } = county;

    [JsonPropertyName("city")]
    public string City { get; private set; } = city;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = latitude;

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = longitude;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = quantity;
}
