using System.Globalization;
using System.Text.Json;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class WeatherApiService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherSettingsService _settings;

    private const string BaseUrl = "https://api.weatherapi.com/v1";
    private const string CurrentUrl = BaseUrl + "/current.json";
    private const string ForecastUrl = BaseUrl + "/forecast.json";
    private const int ForecastDays = 3;
    private const int HourlyDays = 2;

    public WeatherApiService(WeatherSettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
    }

    public bool HasApiKey => _settings.HasApiKey;

    public async Task<(bool ok, string message)> TestApiKeyAsync(CancellationToken ct = default)
    {
        if (!HasApiKey)
            return (false, "Cheia lipseste.");

        try
        {
            var request = BuildCurrentUrl("Bucharest,RO");
            using var response = await _httpClient.GetAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var detail = TryReadErrorMessage(body);
                var reason = string.IsNullOrWhiteSpace(detail)
                    ? (response.ReasonPhrase ?? "Eroare necunoscuta")
                    : detail;
                return (false, $"HTTP {(int)response.StatusCode}: {reason}");
            }

            if (string.IsNullOrWhiteSpace(body))
                return (false, "Raspuns gol de la serviciu.");

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.TryGetProperty("location", out _))
                    return (true, "Cheia este valida si serviciul raspunde.");
                return (false, "Serviciul raspunde, dar nu a returnat date valide.");
            }
            catch (JsonException)
            {
                return (false, "Raspuns neasteptat (JSON invalid).");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Eroare retea: {ex.Message}");
        }
    }

    private static string? TryReadErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var errorMessage))
                return errorMessage.GetString();

            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
    {
        if (!HasApiKey)
            return GetMockData(city);

        try
        {
            var request = BuildCurrentUrl(city);
            using var response = await _httpClient.GetAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return GetMockData(city);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = document.RootElement;

            var location = root.GetProperty("location");
            var current = root.GetProperty("current");
            var condition = current.GetProperty("condition");
            var isDay = current.TryGetProperty("is_day", out var isDayElement)
                && isDayElement.GetInt32() == 1;
            var lastUpdated = current.TryGetProperty("last_updated", out var updatedElement)
                && DateTime.TryParse(updatedElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedUpdated)
                ? parsedUpdated
                : DateTime.Now;

            return new WeatherInfo
            {
                CityName = location.GetProperty("name").GetString() ?? city,
                CountryCode = location.TryGetProperty("country", out var countryElement)
                    ? countryElement.GetString() ?? string.Empty
                    : string.Empty,
                Temperature = current.GetProperty("temp_c").GetDouble(),
                TempMin = current.GetProperty("temp_c").GetDouble(),
                TempMax = current.GetProperty("temp_c").GetDouble(),
                Humidity = current.GetProperty("humidity").GetInt32(),
                Pressure = (int)Math.Round(current.GetProperty("pressure_mb").GetDouble()),
                WindSpeed = current.GetProperty("wind_kph").GetDouble(),
                Visibility = current.TryGetProperty("vis_km", out var visibilityElement)
                    ? visibilityElement.GetDouble()
                    : 10d,
                Condition = ToTitleCase(condition.GetProperty("text").GetString() ?? "Necunoscut"),
                Icon = condition.GetProperty("icon").GetString() ?? string.Empty,
                WeatherCode = condition.GetProperty("code").GetInt32(),
                IsDay = isDay,
                Latitude = location.GetProperty("lat").GetDouble(),
                Longitude = location.GetProperty("lon").GetDouble(),
                LastUpdated = lastUpdated
            };
        }
        catch
        {
            return GetMockData(city);
        }
    }

    public async Task<List<HourlyForecast>> GetHourlyForecastAsync(string city, CancellationToken ct = default)
    {
        if (!HasApiKey)
            return GetMockHourly();

        try
        {
            var request = BuildForecastUrl(city, HourlyDays);
            using var response = await _httpClient.GetAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return GetMockHourly();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = document.RootElement;

            var location = root.GetProperty("location");
            var localTime = location.TryGetProperty("localtime", out var localTimeElement)
                && DateTime.TryParse(localTimeElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime)
                ? parsedTime
                : DateTime.Now;

            var hours = new List<(DateTime time, JsonElement item)>();
            foreach (var day in root.GetProperty("forecast").GetProperty("forecastday").EnumerateArray())
            {
                if (!day.TryGetProperty("hour", out var hourArray))
                    continue;

                foreach (var hour in hourArray.EnumerateArray())
                {
                    var timeText = hour.GetProperty("time").GetString() ?? string.Empty;
                    if (DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hourTime))
                        hours.Add((hourTime, hour));
                }
            }

            var upcoming = hours
                .Where(h => h.time >= localTime)
                .OrderBy(h => h.time)
                .Take(6)
                .ToList();

            if (upcoming.Count == 0)
            {
                upcoming = hours
                    .OrderBy(h => h.time)
                    .Take(6)
                    .ToList();
            }

            var result = new List<HourlyForecast>();
            foreach (var entry in upcoming)
            {
                var hour = entry.item;
                var condition = hour.GetProperty("condition");
                var code = condition.GetProperty("code").GetInt32();
                var isDay = hour.TryGetProperty("is_day", out var isDayElement)
                    && isDayElement.GetInt32() == 1;

                result.Add(new HourlyForecast
                {
                    Hour = result.Count == 0 ? "Acum" : entry.time.ToString("HH:mm"),
                    Temperature = Math.Round(hour.GetProperty("temp_c").GetDouble()),
                    WeatherEmoji = GetEmojiFromCondition(code, isDay),
                    Summary = ToTitleCase(condition.GetProperty("text").GetString() ?? string.Empty)
                });
            }

            return result.Count > 0 ? result : GetMockHourly();
        }
        catch
        {
            return GetMockHourly();
        }
    }

    public async Task<List<DailyForecast>> GetDailyForecastAsync(string city, CancellationToken ct = default)
    {
        if (!HasApiKey)
            return GetMockDaily();

        try
        {
            var request = BuildForecastUrl(city, ForecastDays);
            using var response = await _httpClient.GetAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return GetMockDaily();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var forecastDays = document.RootElement
                .GetProperty("forecast")
                .GetProperty("forecastday")
                .EnumerateArray()
                .Take(5);

            var result = new List<DailyForecast>();
            var roCulture = new CultureInfo("ro-RO");

            foreach (var day in forecastDays)
            {
                var dayInfo = day.GetProperty("day");
                var condition = dayInfo.GetProperty("condition");
                var code = condition.GetProperty("code").GetInt32();
                var dateText = day.GetProperty("date").GetString() ?? string.Empty;
                var date = DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                    ? parsed
                    : DateTime.Now;

                result.Add(new DailyForecast
                {
                    Day = result.Count == 0 ? "Azi" : roCulture.TextInfo.ToTitleCase(date.ToString("ddd", roCulture)),
                    MinTemperature = dayInfo.GetProperty("mintemp_c").GetDouble(),
                    MaxTemperature = dayInfo.GetProperty("maxtemp_c").GetDouble(),
                    WeatherEmoji = GetEmojiFromCondition(code, isDay: true),
                    Summary = ToTitleCase(condition.GetProperty("text").GetString() ?? string.Empty)
                });
            }

            return result.Count > 0 ? result : GetMockDaily();
        }
        catch
        {
            return GetMockDaily();
        }
    }

    private string BuildCurrentUrl(string city)
        => $"{CurrentUrl}?key={_settings.ApiKey}&q={Uri.EscapeDataString(city)}&lang=ro&aqi=no";

    private string BuildForecastUrl(string city, int days)
    {
        var safeDays = days < 1 ? 1 : days;
        return $"{ForecastUrl}?key={_settings.ApiKey}&q={Uri.EscapeDataString(city)}&days={safeDays}&lang=ro&aqi=no&alerts=no";
    }

    private static string ToTitleCase(string text)
    {
        return CultureInfo.GetCultureInfo("ro-RO").TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    private static string GetEmojiFromCondition(int code, bool isDay)
    {
        if (code == 1000)
            return isDay ? "☀️" : "🌙";
        if (code == 1003)
            return "⛅";
        if (IsThunderCode(code))
            return "⛈️";
        if (IsSnowCode(code))
            return "❄️";
        if (IsRainCode(code))
            return "🌧️";
        if (IsFogCode(code))
            return "🌫️";
        if (IsCloudCode(code))
            return "☁️";
        return "🌤️";
    }

    private static bool IsCloudCode(int code)
        => code is 1006 or 1009;

    private static bool IsRainCode(int code)
        => code is 1063 or 1072 or 1150 or 1153 or 1168 or 1180 or 1183 or 1186 or 1189 or 1192 or 1195 or 1201 or 1240 or 1243 or 1246;

    private static bool IsSnowCode(int code)
        => code is 1066 or 1069 or 1114 or 1117 or 1204 or 1207 or 1210 or 1213 or 1216 or 1219 or 1222 or 1225 or 1249 or 1252 or 1255 or 1258;

    private static bool IsThunderCode(int code)
        => code is 1087 or 1273 or 1276 or 1279 or 1282;

    private static bool IsFogCode(int code)
        => code is 1030 or 1135 or 1147;

    private static List<HourlyForecast> GetMockHourly()
    {
        return
        [
            new() { Hour = "Acum", Temperature = 24, WeatherEmoji = "☀️", Summary = "Cer senin" },
            new() { Hour = "14:00", Temperature = 25, WeatherEmoji = "⛅", Summary = "Câțiva nori" },
            new() { Hour = "17:00", Temperature = 23, WeatherEmoji = "☁️", Summary = "Noros" },
            new() { Hour = "20:00", Temperature = 20, WeatherEmoji = "🌧️", Summary = "Ploaie ușoară" }
        ];
    }

    private static List<DailyForecast> GetMockDaily()
    {
        return
        [
            new() { Day = "Azi", MinTemperature = 18, MaxTemperature = 25, WeatherEmoji = "⛅", Summary = "Parțial noros" },
            new() { Day = "Mie", MinTemperature = 17, MaxTemperature = 22, WeatherEmoji = "🌧️", Summary = "Ploaie" },
            new() { Day = "Joi", MinTemperature = 14, MaxTemperature = 19, WeatherEmoji = "☁️", Summary = "Noros" },
            new() { Day = "Vin", MinTemperature = 16, MaxTemperature = 24, WeatherEmoji = "☀️", Summary = "Cer senin" }
        ];
    }

    private static WeatherInfo GetMockData(string city)
    {
        return new WeatherInfo
        {
            CityName = city,
            CountryCode = "RO",
            Temperature = 22.5,
            TempMin = 18.0,
            TempMax = 25.0,
            Humidity = 55,
            WindSpeed = 14.5,
            Pressure = 1015,
            Visibility = 10,
            Condition = "Parțial noros",
            Icon = "1003",
            WeatherCode = 1003,
            IsDay = true,
            Latitude = 44.4268,
            Longitude = 26.1025,
            LastUpdated = DateTime.Now
        };
    }

}
