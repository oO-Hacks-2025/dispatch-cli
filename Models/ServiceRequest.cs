using System.Text.Json.Serialization;

namespace testing.Models;

/// <summary>
/// ServiceRequest represents a service request with its type and quantity.
/// </summary>
/// <param name="serviceType"></param>
/// <param name="quantity"></param>
public class ServiceRequest(ServiceType serviceType, int quantity)
{
    /// <summary>
    /// The type of service requested.
    /// </summary>
    [JsonPropertyName("Type")]
    public ServiceType ServiceType { get; set; } = (ServiceType)serviceType;

    /// <summary>
    /// The quantity of service requested.
    /// </summary>
    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; } = quantity;
}