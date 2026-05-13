using System.Globalization;
using System.Text.Json;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class OpenWeatherMapWeatherService : IWeatherService
{
    private const string GeoUrl = "https://api.openweathermap.org/geo/1.0/direct";
    private const string OneCallUrl = "https://api.openweathermap.org/data/3.0/onecall";

    private readonly HttpClient _httpClient;
    private readonly WeatherPreferencesService _preferences;

    public OpenWeatherMapWeatherService(HttpClient httpClient, WeatherPreferencesService preferences)
    {
        _httpClient = httpClient;
        _preferences = preferences;
    }

    public bool HasApiKey => _preferences.HasApiKey;

    public async Task<WeatherDashboardData> GetWeatherAsync(string locationQuery, CancellationToken ct = default)
    {
        if (!HasApiKey)
            return CreateMockDashboard(locationQuery);

        try
        {
            var geocode = await ResolveLocationAsync(locationQuery, ct);
            if (geocode is null)
                return CreateMockDashboard(locationQuery);

            var oneCallUri =
                $"{OneCallUrl}?lat={geocode.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={geocode.Longitude.ToString(CultureInfo.InvariantCulture)}&exclude=minutely,alerts&units=metric&lang=ro&appid={_preferences.ApiKey}";

            using var response = await _httpClient.GetAsync(oneCallUri, ct);
            if (!response.IsSuccessStatusCode)
                return CreateMockDashboard(geocode.Name);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = document.RootElement;

            var current = root.GetProperty("current");
            var currentWeather = current.GetProperty("weather")[0];
            var currentInfo = new WeatherInfo
            {
                CityName = geocode.Name,
                CountryCode = geocode.Country,
                Latitude = geocode.Latitude,
                Longitude = geocode.Longitude,
                Temperature = current.GetProperty("temp").GetDouble(),
                TempMin = current.GetProperty("temp").GetDouble() - 2,
                TempMax = current.GetProperty("temp").GetDouble() + 3,
                Humidity = current.GetProperty("humidity").GetInt32(),
                Pressure = current.GetProperty("pressure").GetInt32(),
                WindSpeed = current.GetProperty("wind_speed").GetDouble() * 3.6d,
                Visibility = current.TryGetProperty("visibility", out var visibilityElement)
                    ? visibilityElement.GetDouble() / 1000d
                    : 10,
                Condition = ToTitleCase(currentWeather.GetProperty("description").GetString() ?? "Stare necunoscută"),
                WeatherCode = currentWeather.GetProperty("id").GetInt32(),
                IsDay = IsDaytime(current, root),
                LastUpdated = DateTimeOffset.FromUnixTimeSeconds(current.GetProperty("dt").GetInt64()).LocalDateTime
            };

            var hourlyForecast = root.GetProperty("hourly")
                .EnumerateArray()
                .Take(8)
                .Select((item, index) =>
                {
                    var weather = item.GetProperty("weather")[0];
                    return new HourlyForecast
                    {
                        Hour = index == 0
                            ? "Acum"
                            : DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).ToLocalTime().ToString("HH:mm"),
                        Temperature = Math.Round(item.GetProperty("temp").GetDouble()),
                        WeatherEmoji = GetIcon(weather.GetProperty("id").GetInt32(), item),
                        Summary = ToTitleCase(weather.GetProperty("description").GetString() ?? string.Empty)
                    };
                })
                .ToList();

            var dailyForecast = root.GetProperty("daily")
                .EnumerateArray()
                .Take(7)
                .Select((item, index) =>
                {
                    var weather = item.GetProperty("weather")[0];
                    var temperature = item.GetProperty("temp");
                    var date = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).ToLocalTime();
                    return new DailyForecast
                    {
                        Day = index == 0
                            ? "Azi"
                            : CultureInfo.GetCultureInfo("ro-RO").TextInfo.ToTitleCase(date.ToString("ddd", CultureInfo.GetCultureInfo("ro-RO"))),
                        MinTemperature = temperature.GetProperty("min").GetDouble(),
                        MaxTemperature = temperature.GetProperty("max").GetDouble(),
                        WeatherEmoji = GetIcon(weather.GetProperty("id").GetInt32(), item),
                        Summary = ToTitleCase(weather.GetProperty("description").GetString() ?? string.Empty)
                    };
                })
                .ToList();

            return new WeatherDashboardData
            {
                Current = currentInfo,
                UvIndex = current.TryGetProperty("uvi", out var uviElement) ? uviElement.GetDouble() : 0,
                UvCategory = GetUvCategory(current.TryGetProperty("uvi", out var uvElement) ? uvElement.GetDouble() : 0),
                HourlyForecast = hourlyForecast,
                DailyForecast = dailyForecast
            };
        }
        catch
        {
            return CreateMockDashboard(locationQuery);
        }
    }

    private async Task<GeoResult?> ResolveLocationAsync(string query, CancellationToken ct)
    {
        var request =
            $"{GeoUrl}?q={Uri.EscapeDataString(query)}&limit=1&appid={_preferences.ApiKey}";

        using var response = await _httpClient.GetAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var result = document.RootElement.EnumerateArray().FirstOrDefault();
        if (result.ValueKind == JsonValueKind.Undefined)
            return null;

        return new GeoResult(
            result.GetProperty("name").GetString() ?? query,
            result.TryGetProperty("country", out var countryElement) ? countryElement.GetString() ?? string.Empty : string.Empty,
            result.GetProperty("lat").GetDouble(),
            result.GetProperty("lon").GetDouble());
    }

    private static bool IsDaytime(JsonElement current, JsonElement root)
    {
        if (!current.TryGetProperty("sunrise", out var sunriseElement) || !current.TryGetProperty("sunset", out var sunsetElement))
            return true;

        var currentTime = current.GetProperty("dt").GetInt64();
        var sunrise = sunriseElement.GetInt64();
        var sunset = sunsetElement.GetInt64();
        return currentTime >= sunrise && currentTime <= sunset;
    }

    private static string GetIcon(int weatherCode, JsonElement context)
    {
        if (weatherCode is >= 200 and < 300) return "⛈";
        if (weatherCode is >= 300 and < 600) return "🌧";
        if (weatherCode is >= 600 and < 700) return "❄";
        if (weatherCode is >= 700 and < 800) return "🌫";
        if (weatherCode == 800)
        {
            var isDay = !context.TryGetProperty("pop", out _) || context.TryGetProperty("sunrise", out _);
            return isDay ? "☀" : "☾";
        }

        return "☁";
    }

    private static string ToTitleCase(string text)
        => CultureInfo.GetCultureInfo("ro-RO").TextInfo.ToTitleCase(text.ToLowerInvariant());

    private static string GetUvCategory(double value)
    {
        if (value < 3) return "Scăzut";
        if (value < 6) return "Moderat";
        if (value < 8) return "Ridicat";
        if (value < 11) return "Foarte ridicat";
        return "Extrem";
    }

    private static WeatherDashboardData CreateMockDashboard(string locationQuery)
    {
        var current = new WeatherInfo
        {
            CityName = string.IsNullOrWhiteSpace(locationQuery) ? "București" : locationQuery,
            CountryCode = "RO",
            Temperature = 21,
            TempMin = 17,
            TempMax = 24,
            Humidity = 58,
            Pressure = 1014,
            WindSpeed = 16,
            Visibility = 10,
            Condition = "Cer variabil",
            WeatherCode = 801,
            IsDay = true,
            Latitude = 44.4268,
            Longitude = 26.1025,
            LastUpdated = DateTime.Now
        };

        return new WeatherDashboardData
        {
            Current = current,
            UvIndex = 4.2,
            UvCategory = "Moderat",
            HourlyForecast =
            [
                new() { Hour = "Acum", Temperature = 21, WeatherEmoji = "☀", Summary = "Senin" },
                new() { Hour = "14:00", Temperature = 22, WeatherEmoji = "⛅", Summary = "Parțial noros" },
                new() { Hour = "17:00", Temperature = 20, WeatherEmoji = "☁", Summary = "Noros" },
                new() { Hour = "20:00", Temperature = 18, WeatherEmoji = "🌧", Summary = "Ploaie slabă" },
                new() { Hour = "23:00", Temperature = 16, WeatherEmoji = "☾", Summary = "Cer senin" }
            ],
            DailyForecast =
            [
                new() { Day = "Azi", MinTemperature = 17, MaxTemperature = 24, WeatherEmoji = "⛅", Summary = "Parțial noros" },
                new() { Day = "Joi", MinTemperature = 16, MaxTemperature = 23, WeatherEmoji = "🌧", Summary = "Averse" },
                new() { Day = "Vin", MinTemperature = 15, MaxTemperature = 21, WeatherEmoji = "☁", Summary = "Nori joși" },
                new() { Day = "Sâm", MinTemperature = 14, MaxTemperature = 22, WeatherEmoji = "☀", Summary = "Însorit" },
                new() { Day = "Dum", MinTemperature = 15, MaxTemperature = 24, WeatherEmoji = "⛅", Summary = "Plăcut" },
                new() { Day = "Lun", MinTemperature = 16, MaxTemperature = 25, WeatherEmoji = "☀", Summary = "Stabil" },
                new() { Day = "Mar", MinTemperature = 17, MaxTemperature = 26, WeatherEmoji = "☀", Summary = "Cald" }
            ]
        };
    }

    private sealed record GeoResult(string Name, string Country, double Latitude, double Longitude);
}
