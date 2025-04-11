using System.Text.Json.Serialization;

namespace testing.Models;

public class ServiceRequest(int quantity)
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = quantity;
}