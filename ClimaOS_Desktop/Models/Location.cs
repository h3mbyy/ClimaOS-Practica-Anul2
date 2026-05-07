namespace ClimaOS_Desktop.Models;

public class Location
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Coordinates => $"{Latitude:F3}, {Longitude:F3}";
    public string Display => string.IsNullOrWhiteSpace(Country) ? Name : $"{Name}, {Country}";
}
