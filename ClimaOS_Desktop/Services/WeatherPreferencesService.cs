using System.Text.Json;
namespace ClimaOS_Desktop.Services;
public class WeatherPreferencesService
{
    private const string LastLocationKey = "weather.last_location";
    private const string SidebarCollapsedKey = "weather.sidebar.collapsed";
    private const string ApiKeyKey = "weather.openweathermap.api_key";
    public string LastLocation
    {
        get => Preferences.Default.Get(LastLocationKey, "București");
        set => Preferences.Default.Set(LastLocationKey, string.IsNullOrWhiteSpace(value) ? "București" : value.Trim());
    }
    public bool IsSidebarCollapsed
    {
        get => Preferences.Default.Get(SidebarCollapsedKey, false);
        set => Preferences.Default.Set(SidebarCollapsedKey, value);
    }
    public string ApiKey
    {
        get
        {
            var fromPrefs = Preferences.Default.Get(ApiKeyKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(fromPrefs))
                return fromPrefs;
            return Environment.GetEnvironmentVariable("OPENWEATHERMAP_API_KEY") ?? string.Empty;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Preferences.Default.Remove(ApiKeyKey);
            else
                Preferences.Default.Set(ApiKeyKey, value.Trim());
        }
    }
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value);
}
