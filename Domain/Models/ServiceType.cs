using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceType
{
    Medical,
    Fire,
    Police,
    Rescue,
    Utility,
}
