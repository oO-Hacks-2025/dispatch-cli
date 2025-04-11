namespace testing.Models;

public class City(string name, string country, double latitude, double longitude)
{
    public string Name { get; } = name;
    public string Country { get; } = country;
    public double Latitude { get; } = latitude;
    public double Longitude { get; } = longitude;

    public override string ToString() => $"{Name}, {Country}";
}