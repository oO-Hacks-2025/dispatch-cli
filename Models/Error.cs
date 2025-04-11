using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///  Error represents the number of ambulances that were missed or over dispatched.
/// </summary>
/// <param name="missed"></param>
/// <param name="overDispatched"></param>
public class Error(int missed, int overDispatched)
{
    /// <summary>
    /// The number of ambulances that were missed.
    /// </summary>
    [JsonPropertyName("missed")]
    public int Missed { get; set; } = missed;

    /// <summary>
    /// The number of ambulances that were over dispatched.
    /// </summary>
    [JsonPropertyName("overDispatched")]
    public int OverDispatched { get; set; } = overDispatched;
}