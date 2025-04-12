using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class GameStatus(
    string status,
    TimeSpan runningTime,
    string seed,
    double requestCount,
    int maxActiveCalls,
    int totalDispatches,
    int targetDispatches,
    double distance,
    double penalty,
    int httpRequests,
    int emulatorVersion,
    string signature,
    string checksum,
    Error error
)
{
    [JsonPropertyName("status")]
    public string Status { get; } = status;

    [JsonPropertyName("runningTime")]
    public TimeSpan RunningTime { get; } = runningTime;

    [JsonPropertyName("seed")]
    public string Seed { get; } = seed;

    [JsonPropertyName("requestCount")]
    public double RequestCount { get; } = requestCount;

    [JsonPropertyName("maxActiveCalls")]
    public int MaxActiveCalls { get; } = maxActiveCalls;

    [JsonPropertyName("totalDispatches")]
    public int TotalDispatches { get; } = totalDispatches;

    [JsonPropertyName("targetDispatches")]
    public int TargetDispatches { get; } = targetDispatches;

    [JsonPropertyName("distance")]
    public double Distance { get; } = distance;

    [JsonPropertyName("penalty")]
    public double Penalty { get; } = penalty;

    [JsonPropertyName("httpRequests")]
    public int HttpRequests { get; } = httpRequests;

    [JsonPropertyName("emulatorVersion")]
    public int EmulatorVersion { get; } = emulatorVersion;

    [JsonPropertyName("signature")]
    public string Signature { get; } = signature;

    [JsonPropertyName("checksum")]
    public string Checksum { get; } = checksum;

    [JsonPropertyName("errors")]
    public Error Error { get; } = error;
}
