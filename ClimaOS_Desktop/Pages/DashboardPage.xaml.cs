using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Pages.Admin;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly WeatherApiService _weatherApi;
    private readonly AuthService _auth;
    private readonly SessionStore _session;
    private IDispatcherTimer _timer;

    public DashboardPage()
        : this(
            ResolveService<WeatherApiService>(),
            ResolveService<DatabaseService>(),
            ResolveService<AuthService>(),
            ResolveService<SessionStore>())
    {
    }

    public DashboardPage(
        WeatherApiService weatherApi,
        DatabaseService databaseService,
        AuthService auth,
        SessionStore session)
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        _weatherApi = weatherApi;
        _auth = auth;
        _session = session;

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

        await LoadWeatherData("București");
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
                await DisplayAlert("Eroare", "Nu s-au putut prelua datele meteo pentru această locație.", "OK");
                return;
            }

            CurrentDateLabel.Text = DateTime.Now.ToString("dddd, d MMMM yyyy");
            LocationLabel.Text = $"{dateMeteo.CityName}";

            TemperatureLabel.Text = $"{Math.Round(dateMeteo.Temperature)}°C";
            WeatherConditionLabel.Text = string.IsNullOrEmpty(dateMeteo.Condition)
                ? "—"
                : char.ToUpper(dateMeteo.Condition[0]) + dateMeteo.Condition.Substring(1);
            HumidityLabel.Text = $"{dateMeteo.Humidity}%";
            WindLabel.Text = $"{dateMeteo.WindSpeed} km/h";
            PressureLabel.Text = $"{dateMeteo.Pressure} hPa";
            VisibilityLabel.Text = $"{Math.Round(dateMeteo.Visibility, 1)} km";
            LastUpdatedLabel.Text = $"Actualizat la: {dateMeteo.LastUpdated:HH:mm:ss} • Conexiune OpenWeatherMap";
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SearchEntry.Text))
        {
            await LoadWeatherData(SearchEntry.Text.Trim());
            SearchEntry.Text = string.Empty;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!confirm) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
