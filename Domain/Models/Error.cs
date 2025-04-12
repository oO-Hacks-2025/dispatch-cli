using System.Text.Json.Serialization;

namespace EmergencyDispatcher.Domain.Models;

public class Error(int missed, int overDispatched)
{
    [JsonPropertyName("missed")]
    public int Missed { get; set; } = missed;

    [JsonPropertyName("overDispatched")]
    public int OverDispatched { get; set; } = overDispatched;
}
