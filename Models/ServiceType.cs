using System.Text.Json.Serialization;

namespace testing.Models;

/// <summary>
/// ServiceType represents the type of service requested.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceType
{
    Medical,
    Fire,
    Police,
    Rescue,
    Utility,
}