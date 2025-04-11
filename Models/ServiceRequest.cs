using System.Text.Json.Serialization;

namespace testing.Models;

public class ServiceRequest(ServiceType serviceType, int quantity)
{
    [JsonPropertyName("Type")]
    public ServiceType ServiceType { get; set; } = (ServiceType)serviceType;
    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; } = quantity;
}