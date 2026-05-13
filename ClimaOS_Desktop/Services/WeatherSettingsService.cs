namespace ClimaOS_Desktop.Services;
public class WeatherSettingsService
{
    private const string ApiKeyPref = "weather.weatherapi.api_key";
    private const string LegacyApiKeyPref = "weather.openweathermap.api_key";
    public string ApiKey
    {
        get
        {
            var key = Preferences.Default.Get(ApiKeyPref, string.Empty);
            if (!string.IsNullOrWhiteSpace(key))
                return key;
            return Environment.GetEnvironmentVariable("CLIMAOS_WEATHERAPI_KEY")
                   ?? Environment.GetEnvironmentVariable("WEATHERAPI_API_KEY")
                   ?? string.Empty;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Preferences.Default.Remove(ApiKeyPref);
                Preferences.Default.Remove(LegacyApiKeyPref);
            }
            else
                Preferences.Default.Set(ApiKeyPref, value.Trim());
        }
    }
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
