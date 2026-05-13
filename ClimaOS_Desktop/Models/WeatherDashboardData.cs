namespace ClimaOS_Desktop.Models;
public class WeatherDashboardData
{
    public WeatherInfo Current { get; set; } = new();
    public double UvIndex { get; set; }
    public string UvCategory { get; set; } = string.Empty;
    public List<HourlyForecast> HourlyForecast { get; set; } = [];
    public List<DailyForecast> DailyForecast { get; set; } = [];
}
