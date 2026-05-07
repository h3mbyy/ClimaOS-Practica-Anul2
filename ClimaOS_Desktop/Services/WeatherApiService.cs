using System.Text.Json;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class WeatherApiService
{
    private readonly HttpClient _httpClient;
    // TODO: Înlocuiește cu cheia ta reală OpenWeatherMap
    private const string ApiKey = "PLACEHOLDER_API_KEY"; 
    private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

    public WeatherApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
    {
        try
        {
            if (ApiKey == "PLACEHOLDER_API_KEY")
            {
                // Returnează date de test dacă cheia nu este configurată
                return GetMockData(city);
            }

            string url = $"{BaseUrl}?q={city}&appid={ApiKey}&units=metric&lang=ro";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                var main = root.GetProperty("main");
                var wind = root.GetProperty("wind");
                var weather = root.GetProperty("weather")[0];

                return new WeatherInfo
                {
                    CityName = root.GetProperty("name").GetString() ?? city,
                    Temperature = main.GetProperty("temp").GetDouble(),
                    TempMin = main.GetProperty("temp_min").GetDouble(),
                    TempMax = main.GetProperty("temp_max").GetDouble(),
                    Humidity = main.GetProperty("humidity").GetInt32(),
                    Pressure = main.GetProperty("pressure").GetInt32(),
                    WindSpeed = wind.GetProperty("speed").GetDouble(),
                    Visibility = root.TryGetProperty("visibility", out var vis) ? vis.GetDouble() / 1000.0 : 10.0, // in km
                    Condition = weather.GetProperty("description").GetString() ?? "",
                    Icon = weather.GetProperty("icon").GetString() ?? "",
                    LastUpdated = DateTime.Now
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Eroare la preluarea datelor meteo: {ex.Message}");
        }
        
        return null;
    }

    private WeatherInfo GetMockData(string city)
    {
        return new WeatherInfo
        {
            CityName = city,
            Temperature = 22.5,
            TempMin = 18.0,
            TempMax = 25.0,
            Humidity = 55,
            WindSpeed = 14.5,
            Pressure = 1015,
            Visibility = 10,
            Condition = "Parțial noros",
            Icon = "02d",
            LastUpdated = DateTime.Now
        };
    }
}