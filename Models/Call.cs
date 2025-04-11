namespace testing.Models;

public class Call(string county, string city, double latitude, double longitude, List<ServiceRequest> requests)
{
    public string County { get; private set; } = county;
    public string City { get; private set; } = city;

    public double Latitude { get; set; } = latitude;
    public double Longitude { get; set; } = longitude;
    public List<ServiceRequest> requests { get; set; } = requests;
}
