using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class ServiceRequest(ServiceType serviceType, int quantity)
{
    [JsonPropertyName("Type")]
    public ServiceType ServiceType { get; set; } = serviceType;

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; } = quantity;
}
