using System.Text.Json.Serialization;
namespace testing.Models;

/// <summary>
///   Represents the status of the game.
/// </summary>
/// <param name="status"></param>
/// <param name="runningTime"></param>
/// <param name="seed"></param>
/// <param name="requestCount"></param>
/// <param name="maxActiveCalls"></param>
/// <param name="totalDispatches"></param>
/// <param name="targetDispatches"></param>
/// <param name="distance"></param>
/// <param name="penalty"></param>
/// <param name="httpRequests"></param>
/// <param name="emulatorVersion"></param>
/// <param name="signature"></param>
/// <param name="checksum"></param>
/// <param name="error"></param>
public class GameStatus(string status, TimeSpan runningTime, string seed, double requestCount, int maxActiveCalls, int totalDispatches, int targetDispatches, double distance, double penalty, int httpRequests, int emulatorVersion, string signature, string checksum, Error error)
{
    /// <summary>
    ///    The status of the game.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; } = status;


    /// <summary>
    ///    The time the game has been running.
    /// </summary>
    [JsonPropertyName("runningTime")]
    public TimeSpan RunningTime { get; } = runningTime;


    /// <summary>
    ///    The seed used to generate the game.
    /// </summary>
    [JsonPropertyName("seed")]
    public string Seed { get; } = seed;

    /// <summary>
    ///   The number of requests made to the server.
    /// </summary>
    [JsonPropertyName("requestCount")]
    public double RequestCount { get; } = requestCount;

    /// <summary>
    ///   The maximum number of active calls.
    /// </summary>
    [JsonPropertyName("maxActiveCalls")]
    public int MaxActiveCalls { get; } = maxActiveCalls;


    /// <summary>
    ///    The number of dispatches that should be made.
    /// </summary>
    [JsonPropertyName("totalDispatches")]
    public int TotalDispatches { get; } = totalDispatches;

    /// <summary>
    ///  The
    /// </summary>
    [JsonPropertyName("targetDispatches")]
    public int TargetDispatches { get; } = targetDispatches;

    /// <summary>
    ///   The distance traveled by the ambulances.
    /// </summary>
    [JsonPropertyName("distance")]
    public double Distance { get; } = distance;

    /// <summary>
    ///   The penalty incurred by the game.
    /// </summary>
    [JsonPropertyName("penalty")]
    public double Penalty { get; } = penalty;

    /// <summary>
    ///   The number of HTTP requests made to the server.
    /// </summary>
    [JsonPropertyName("httpRequests")]
    public int HttpRequests { get; } = httpRequests;

    /// <summary>
    ///   The version of the emulator.
    /// </summary>
    [JsonPropertyName("emulatorVersion")]
    public int EmulatorVersion { get; } = emulatorVersion;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; } = signature;

    /// <summary>
    ///     The checksum of the game.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string Checksum { get; } = checksum;

    /// <summary>
    ///    The error object containing the number of missed and over dispatched calls.
    /// </summary>
    [JsonPropertyName("errors")]
    public Error Error { get; } = error;
}