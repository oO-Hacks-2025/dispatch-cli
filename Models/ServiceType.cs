using System.Text.Json.Serialization;

namespace testing.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceType
{
    Medical,
    Fire,
    Police,
    Rescue,
    Utility,
}