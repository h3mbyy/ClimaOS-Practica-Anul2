namespace ClimaOS_Desktop.Models;

public class HourlyForecast
{
    public string Hour { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string WeatherEmoji { get; set; } = "☀️";
    public string Summary { get; set; } = string.Empty;
}
