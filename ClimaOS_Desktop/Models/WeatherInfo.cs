namespace ClimaOS_Desktop.Models;
public class WeatherInfo
{
    public string CityName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int Pressure { get; set; }
    public double Visibility { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int WeatherCode { get; set; }
    public bool IsDay { get; set; } = true;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime LastUpdated { get; set; }
}
