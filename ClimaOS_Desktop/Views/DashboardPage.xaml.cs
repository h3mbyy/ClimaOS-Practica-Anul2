using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Views.Admin;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using LocationModel = ClimaOS_Desktop.Models.Location;

namespace ClimaOS_Desktop.Views;

public partial class DashboardPage : ContentPage
{
    private readonly WeatherApiService _weatherApi;
    private readonly AuthService _auth;
    private readonly SessionStore _session;
    private readonly LocationService _locations;
    private readonly UserFavoriteService _favorites;
    private readonly AlertService _alerts;
    private IDispatcherTimer _timer;
    private LocationModel? _currentLocation;
    private bool _missingApiKeyWarned;

    public DashboardPage()
        : this(
            ResolveService<WeatherApiService>(),
            ResolveService<AuthService>(),
            ResolveService<SessionStore>(),
            ResolveService<LocationService>(),
            ResolveService<UserFavoriteService>(),
            ResolveService<AlertService>())
    {
    }

    public DashboardPage(
        WeatherApiService weatherApi,
        AuthService auth,
        SessionStore session,
        LocationService locations,
        UserFavoriteService favorites,
        AlertService alerts)
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _weatherApi = weatherApi;
        _auth = auth;
        _session = session;
        _locations = locations;
        _favorites = favorites;
        _alerts = alerts;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(10);
        _timer.Tick += async (s, e) =>
        {
            var oras = string.IsNullOrWhiteSpace(LocationLabel.Text) ? "București" : LocationLabel.Text.Split(',')[0];
            await LoadWeatherData(oras);
        };
    }

    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException(
                $"Nu pot rezolva {typeof(T).Name} înainte ca MauiContext să fie disponibil.");
        return services.GetRequiredService<T>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _timer.Start();

        if (_session.IsAdmin)
        {
            await Shell.Current.GoToAsync($"//{nameof(AdminDashboardPage)}");
            return;
        }

        if (!_weatherApi.HasApiKey && !_missingApiKeyWarned)
        {
            _missingApiKeyWarned = true;
                await DisplayAlertAsync("API meteo lipsă",
                    "Configureaza cheia WeatherAPI din Settings pentru a vedea vremea in timp real. Pana atunci se folosesc date demo.",
                "OK");
        }

        var favorite = _session.CurrentUser is { } user
            ? (await _favorites.SearchForUserAsync(user.Id)).FirstOrDefault()
            : null;

        await LoadWeatherData(favorite?.LocationName ?? "București");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    private async Task LoadWeatherData(string oras)
    {
        try
        {
            var dateMeteo = await _weatherApi.GetCurrentWeatherAsync(oras);
            if (dateMeteo is null)
            {
                await DisplayAlertAsync("Eroare", "Nu s-au putut prelua datele meteo pentru această locație.", "OK");
                return;
            }

            LocationLabel.Text = $"{dateMeteo.CityName}";
            if (!string.IsNullOrWhiteSpace(dateMeteo.CountryCode))
                LocationLabel.Text = $"{dateMeteo.CityName}, {dateMeteo.CountryCode}";
            TemperatureLabel.Text = $"{Math.Round(dateMeteo.Temperature)} °C";
            RangeLabel.Text = $"L: {Math.Round(dateMeteo.TempMin)}°  H: {Math.Round(dateMeteo.TempMax)}°";
            WeatherConditionLabel.Text = string.IsNullOrEmpty(dateMeteo.Condition)
                ? "Fog"
                : char.ToUpper(dateMeteo.Condition[0]) + dateMeteo.Condition.Substring(1);
            LastUpdatedLabel.Text = $"Actualizat {dateMeteo.LastUpdated:HH:mm}";
            HumidityLabel.Text = $"{dateMeteo.Humidity} %";
            WindLabel.Text = $"{dateMeteo.WindSpeed} km/h";
            PressureLabel.Text = $"{dateMeteo.Pressure} hPa";

            // Populate hourly forecast
            var hourly = await _weatherApi.GetHourlyForecastAsync(oras);
            BindableLayout.SetItemsSource(HourlyForecastLayout, hourly);

            var daily = await _weatherApi.GetDailyForecastAsync(oras);
            BindableLayout.SetItemsSource(DailyForecastLayout, daily);

            _currentLocation = await _locations.EnsureAsync(dateMeteo.CityName, dateMeteo.CountryCode, dateMeteo.Latitude, dateMeteo.Longitude);
            ApplyWeatherTheme(dateMeteo);
            await RefreshFavoriteStateAsync();
            await ShowActiveAlertsAsync(dateMeteo.CityName);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnChangeLocationTapped(object? sender, EventArgs e)
    {
        var result = await DisplayPromptAsync("Schimbă Locația", "Introdu numele orașului:");
        if (!string.IsNullOrWhiteSpace(result))
        {
            await LoadWeatherData(result.Trim());
        }
    }

    private async void OnSearchLocationCompleted(object? sender, EventArgs e)
    {
        var query = SearchLocationEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            var savedMatches = await _locations.SearchAsync(query);
            if (savedMatches.Count > 1)
            {
                var labels = savedMatches.Select(l => l.Display).ToArray();
                var selected = await DisplayActionSheetAsync("Alege stația", "Anulează", null, labels);
                if (string.IsNullOrWhiteSpace(selected) || selected == "Anulează") return;
                var match = savedMatches.First(l => l.Display == selected);
                await LoadWeatherData(match.Name);
                return;
            }

            await LoadWeatherData(savedMatches.FirstOrDefault()?.Name ?? query);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnFavoriteTapped(object? sender, EventArgs e)
    {
        if (_session.CurrentUser is null)
        {
            await DisplayAlertAsync("Favorite", "Autentifică-te pentru a salva favorite.", "OK");
            return;
        }

        if (_currentLocation is null)
        {
            var cityName = (LocationLabel.Text ?? string.Empty).Split(',')[0].Trim();
            _currentLocation = await _locations.EnsureAsync(cityName);
        }

        try
        {
            var existing = await _favorites.GetForUserLocationAsync(_session.CurrentUser.Id, _currentLocation.Id);
            if (existing is null)
            {
                await _favorites.AddAsync(_session.CurrentUser.Id, _currentLocation.Id);
                await DisplayAlertAsync("Favorite", "Locația a fost adăugată la favorite.", "OK");
            }
            else
            {
                await _favorites.DeleteForUserLocationAsync(_session.CurrentUser.Id, _currentLocation.Id);
                await DisplayAlertAsync("Favorite", "Locația a fost eliminată din favorite.", "OK");
            }

            await RefreshFavoriteStateAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async Task RefreshFavoriteStateAsync()
    {
        if (_session.CurrentUser is null || _currentLocation is null)
        {
            FavoriteIcon.Text = "☆";
            FavoriteText.Text = "Adaugă la Favorite";
            return;
        }

        var existing = await _favorites.GetForUserLocationAsync(_session.CurrentUser.Id, _currentLocation.Id);
        FavoriteIcon.Text = existing is null ? "☆" : "★";
        FavoriteText.Text = existing is null ? "Adaugă la Favorite" : "În Favorite";
    }

    private async Task ShowActiveAlertsAsync(string locationName)
    {
        var activeAlerts = await _alerts.GetActiveAsync(locationName);
        var severe = activeAlerts
            .OrderByDescending(a => a.Severity)
            .FirstOrDefault();
        if (severe is null) return;

        await DisplayAlertAsync(severe.Title, severe.Message, "Am înțeles");
    }

    private void ApplyWeatherTheme(ClimaOS_Desktop.Models.WeatherInfo info)
    {
        var code = info.WeatherCode;
        var (top, middle, bottom, accent) = code switch
        {
            1000 when info.IsDay => ("#103D7A", "#1B6CB1", "#89C3FF", "#66F5D06F"),
            1000 => ("#081320", "#102544", "#1B355A", "#444F83CC"),
            _ when IsThunderCode(code) => ("#161A25", "#2C3147", "#46385E", "#66FFB868"),
            _ when IsSnowCode(code) => ("#1B273B", "#38506F", "#9FB7D8", "#66FFFFFF"),
            _ when IsRainCode(code) => ("#1A2236", "#2B4467", "#526C93", "#666DB0FF"),
            _ when IsFogCode(code) => ("#172031", "#2C3C55", "#566A86", "#6680A0C8"),
            _ when IsCloudCode(code) => ("#101D31", "#284666", "#4C6F90", "#5582C4FF"),
            _ => ("#101D31", "#284666", "#4C6F90", "#5582C4FF")
        };

        PrimaryBackgroundLayer.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(top), 0.0f),
                new GradientStop(Color.FromArgb(middle), 0.45f),
                new GradientStop(Color.FromArgb(bottom), 1.0f)
            },
            new Point(0, 0),
            new Point(0, 1));

        SecondaryBackgroundLayer.Background = new RadialGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(accent), 0.0f),
                new GradientStop(Color.FromArgb("#00000000"), 1.0f)
            },
            new Point(0.18, info.IsDay ? 0.2 : 0.35),
            0.95f);
    }

    private static bool IsCloudCode(int code)
        => code is 1003 or 1006 or 1009;

    private static bool IsRainCode(int code)
        => code is 1063 or 1072 or 1150 or 1153 or 1168 or 1180 or 1183 or 1186 or 1189 or 1192 or 1195 or 1201 or 1240 or 1243 or 1246;

    private static bool IsSnowCode(int code)
        => code is 1066 or 1069 or 1114 or 1117 or 1204 or 1207 or 1210 or 1213 or 1216 or 1219 or 1222 or 1225 or 1249 or 1252 or 1255 or 1258;

    private static bool IsThunderCode(int code)
        => code is 1087 or 1273 or 1276 or 1279 or 1282;

    private static bool IsFogCode(int code)
        => code is 1030 or 1135 or 1147;

    private async void OnMapsClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Hărți", "Modulul de hărți interactive va fi disponibil în curând.", "OK");
    }

    private async void OnStationsClicked(object? sender, EventArgs e)
    {
        try
        {
            var stations = await _locations.SearchAsync(null);
            if (stations.Count == 0)
            {
                await DisplayAlertAsync("Stații", "Nu există stații salvate încă.", "OK");
                return;
            }

            var selected = await DisplayActionSheetAsync("Alege o stație", "Anulează", null,
                stations.Select(s => s.Display).ToArray());
            if (string.IsNullOrWhiteSpace(selected) || selected == "Anulează") return;

            var station = stations.First(s => s.Display == selected);
            await LoadWeatherData(station.Name);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnUsersClicked(object? sender, EventArgs e)
    {
        if (_session.IsAdmin)
            await Shell.Current.GoToAsync($"//UsersPage");
        else
            await DisplayAlertAsync("Utilizatori", "Această secțiune este disponibilă doar pentru administratori.", "OK");
    }

    private async void OnUpgradePlanClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Premium", "Upgrade-ul la ClimaOS Premium include hărți detaliate și alerte SMS.", "Mai târziu");
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (_session.IsAdmin)
            await Shell.Current.GoToAsync($"//SettingsPage");
        else
            await DisplayAlertAsync("Cont", _session.CurrentUser is { } user
                ? $"{user.Name}\n{user.Email}"
                : "Nu există o sesiune activă.", "OK");
    }

    private async void OnSupportClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Suport", "Contactează-ne la support@climaos.ro", "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//LoginPage");
    }
}
