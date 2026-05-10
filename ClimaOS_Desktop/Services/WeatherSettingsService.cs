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

            var legacy = Preferences.Default.Get(LegacyApiKeyPref, string.Empty);
            if (string.IsNullOrWhiteSpace(legacy))
                return string.Empty;

            Preferences.Default.Set(ApiKeyPref, legacy);
            return legacy;
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
