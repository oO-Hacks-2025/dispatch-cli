namespace testing.Models;

public class Dispatch(string sourceCounty, string sourceCity, string targetCounty, string targetCity, int quantity)
{
    public string SourceCounty { get; set; } = sourceCounty;
    public string SourceCity { get; set; } = sourceCity;
    public string TargetCounty { get; set; } = targetCounty;
    public string TargetCity { get; set; } = targetCity;
    public int Quantity { get; set; } = quantity;

    public override string ToString()
    {
        return $"{SourceCounty} - {SourceCity} -> {TargetCounty} - {TargetCity} : {Quantity}";
    }
}