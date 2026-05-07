using System.Text.Json;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class WeatherApiService
{
    private readonly HttpClient _httpClient;
    private const string GeocodeUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string WeatherUrl = "https://api.open-meteo.com/v1/forecast";

    public WeatherApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
    {
        try
        {
            // 1. Geocoding: Get Latitude and Longitude for the City
            string geocodeReq = $"{GeocodeUrl}?name={Uri.EscapeDataString(city)}&count=1&language=ro&format=json";
            var geoResponse = await _httpClient.GetAsync(geocodeReq);

            if (!geoResponse.IsSuccessStatusCode) return GetMockData(city);

            var geoJson = await geoResponse.Content.ReadAsStringAsync();
            using var geoDoc = JsonDocument.Parse(geoJson);
            
            if (!geoDoc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return GetMockData(city);
            }

            var location = results[0];
            var lat = location.GetProperty("latitude").GetDouble();
            var lon = location.GetProperty("longitude").GetDouble();
            var realCityName = location.GetProperty("name").GetString() ?? city;

            // 2. Weather: Get Current Weather
            string weatherReq = $"{WeatherUrl}?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,showers,snowfall,weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m&timezone=auto";
            var weatherResponse = await _httpClient.GetAsync(weatherReq);

            if (weatherResponse.IsSuccessStatusCode)
            {
                var weatherJson = await weatherResponse.Content.ReadAsStringAsync();
                using var weatherDoc = JsonDocument.Parse(weatherJson);
                var root = weatherDoc.RootElement;
                
                var current = root.GetProperty("current");

                return new WeatherInfo
                {
                    CityName = realCityName,
                    Temperature = current.GetProperty("temperature_2m").GetDouble(),
                    TempMin = current.GetProperty("temperature_2m").GetDouble() - 2, // Approximation
                    TempMax = current.GetProperty("temperature_2m").GetDouble() + 2, // Approximation
                    Humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                    Pressure = current.GetProperty("surface_pressure").GetInt32(),
                    WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                    Visibility = 10.0, // Open-meteo current doesn't provide visibility easily without hourly data
                    Condition = GetConditionFromCode(current.GetProperty("weather_code").GetInt32()),
                    Icon = "01d", // Mocked icon
                    LastUpdated = DateTime.Now
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Eroare la preluarea datelor meteo: {ex.Message}");
        }
        
        return GetMockData(city);
    }
    
    private string GetConditionFromCode(int code)
    {
        return code switch
        {
            0 => "Cer senin",
            1 or 2 or 3 => "Parțial noros / Noros",
            45 or 48 => "Ceață",
            51 or 53 or 55 => "Burniță",
            61 or 63 or 65 => "Ploaie",
            71 or 73 or 75 => "Ninsoare",
            95 or 96 or 99 => "Furtună",
            _ => "Necunoscut"
        };
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