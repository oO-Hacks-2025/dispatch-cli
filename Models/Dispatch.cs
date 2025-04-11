using System.Text.Json.Serialization;

namespace testing.Models;
/// <summary>
///  Dispatch class represents a dispatch of ambulances from one location to another.
/// </summary>
/// <param name="sourceCounty"></param>
/// <param name="sourceCity"></param>
/// <param name="targetCounty"></param>
/// <param name="targetCity"></param>
/// <param name="quantity"></param>
public class Dispatch(string sourceCounty, string sourceCity, string targetCounty, string targetCity, int quantity)
{
    /// <summary>
    /// The County where the ambulances are dispatched from.
    /// </summary>
    [JsonPropertyName("sourceCounty")]
    public string SourceCounty { get; set; } = sourceCounty;


    /// <summary>
    /// The City where the ambulances are dispatched from.
    /// </summary>
    [JsonPropertyName("sourceCity")]
    public string SourceCity { get; set; } = sourceCity;

    /// <summary>
    /// The County where the ambulances are dispatched to.
    /// </summary>
    [JsonPropertyName("targetCounty")]
    public string TargetCounty { get; set; } = targetCounty;


    /// <summary>
    /// The City where the ambulances are dispatched to.
    /// </summary>
    [JsonPropertyName("targetCity")]
    public string TargetCity { get; set; } = targetCity;


    /// <summary>
    /// The number of ambulances dispatched.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = quantity;

    public override string ToString()
    {
        return $"{SourceCounty} - {SourceCity} -> {TargetCounty} - {TargetCity} : {Quantity}";
    }
}