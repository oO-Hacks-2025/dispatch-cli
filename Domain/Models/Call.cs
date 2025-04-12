using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class Call(
    string county,
    string city,
    double latitude,
    double longitude,
    List<ServiceRequest> requests
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

    [JsonPropertyName("requests")]
    public List<ServiceRequest> Requests { get; set; } = requests;
}
