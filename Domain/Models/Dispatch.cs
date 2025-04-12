using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class Dispatch(
    string sourceCounty,
    string sourceCity,
    string targetCounty,
    string targetCity,
    int quantity
)
{
    [JsonPropertyName("sourceCounty")]
    public string SourceCounty { get; set; } = sourceCounty;

    [JsonPropertyName("sourceCity")]
    public string SourceCity { get; set; } = sourceCity;

    [JsonPropertyName("targetCounty")]
    public string TargetCounty { get; set; } = targetCounty;

    [JsonPropertyName("targetCity")]
    public string TargetCity { get; set; } = targetCity;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = quantity;

    public override string ToString()
    {
        return $"{SourceCounty} - {SourceCity} -> {TargetCounty} - {TargetCity} : {Quantity}";
    }
}
