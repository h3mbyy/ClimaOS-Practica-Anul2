using ClimaOS_Desktop.Models;
namespace ClimaOS_Desktop.Services;
public interface IWeatherService
{
    bool HasApiKey { get; }
    Task<WeatherDashboardData> GetWeatherAsync(string locationQuery, CancellationToken ct = default);
}
