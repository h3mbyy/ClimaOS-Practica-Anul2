namespace ClimaOS_Desktop.Models;

public class DailyForecast
{
    public string Day { get; set; } = string.Empty;
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public string WeatherEmoji { get; set; } = "☀️";
    public string Summary { get; set; } = string.Empty;
    public string RangeDisplay => $"{Math.Round(MinTemperature)}° / {Math.Round(MaxTemperature)}°";
}
