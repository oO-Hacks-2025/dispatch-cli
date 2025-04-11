using testing.Models;
namespace testing.Models;

public class GameStatus(string status, TimeSpan runningTime, string seed, double requestCount, int maxActiveCalls, int totalDispatches, int targetDispatches, double distance, double penalty, int httpRequests, int emulatorVersion, string signature, string checksum, List<Errors> errors)
{
    public string Status { get; } = status;
    public TimeSpan RunningTime { get; } = runningTime;
    public string Seed { get; } = seed;
    public double RequestCount { get; } = requestCount;
    public int MaxActiveCalls { get; } = maxActiveCalls;
    public int TotalDispatches { get; } = totalDispatches;
    public int TargetDispatches { get; } = targetDispatches;
    public double Distance { get; } = distance;
    public double Penalty { get; } = penalty;
    public int HttpRequests { get; } = httpRequests;
    public int EmulatorVersion { get; } = emulatorVersion;
    public string Signature { get; } = signature;
    public string Checksum { get; } = checksum;
    public List<Errors> Errors { get; } = errors;
}