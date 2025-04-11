namespace testing.Models;

public class Availability(string county, string city, double latitude, double longitude, int quantity)
{
    public string County { get; private set; } = county;
    public string City { get; private set; } = city;

    public double Latitude { get; set; } = latitude;
    public double Longitude { get; set; } = longitude;
    public int Quantity { get; set; } = quantity;
}