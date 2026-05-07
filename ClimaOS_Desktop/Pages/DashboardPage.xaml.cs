using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly WeatherApiService _weatherApi;
    private IDispatcherTimer _timer;

    public DashboardPage()
        : this(ResolveService<WeatherApiService>(), ResolveService<DatabaseService>())
    {
    }
    
    public DashboardPage(WeatherApiService weatherApi, DatabaseService databaseService)
    {
        InitializeComponent();
        
        // Ascunde bara de navigare implicită
        Shell.SetNavBarIsVisible(this, false);

        _weatherApi = weatherApi;

        // Configurăm timer-ul de auto-refresh la fiecare 10 minute
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(10);
        _timer.Tick += async (s, e) => {
            var orasDeCautat = string.IsNullOrWhiteSpace(LocationLabel.Text) ? "București" : LocationLabel.Text.Split(',')[0];
            await LoadWeatherData(orasDeCautat);
        };

    }

    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
        {
            throw new InvalidOperationException($"Nu pot rezolva serviciul {typeof(T).Name} înainte ca MauiContext să fie disponibil.");
        }

        return services.GetRequiredService<T>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _timer.Start();

        // Încărcăm date standard prima dată (ex. București)
        await LoadWeatherData("București");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    private async Task LoadWeatherData(string oras)
    {
        var dateMeteo = await _weatherApi.GetCurrentWeatherAsync(oras);
        
        if (dateMeteo != null)
        {
            // Actualizăm ecranul cu datele meteo aduse
            CurrentDateLabel.Text = DateTime.Now.ToString("dddd, d MMMM yyyy");
            LocationLabel.Text = $"{dateMeteo.CityName}";
            
            TemperatureLabel.Text = $"{Math.Round(dateMeteo.Temperature)}°C";
            WeatherConditionLabel.Text = char.ToUpper(dateMeteo.Condition[0]) + dateMeteo.Condition.Substring(1);
            HumidityLabel.Text = $"{dateMeteo.Humidity}%";
            WindLabel.Text = $"{dateMeteo.WindSpeed} km/h";
            PressureLabel.Text = $"{dateMeteo.Pressure} hPa";
            VisibilityLabel.Text = $"{Math.Round(dateMeteo.Visibility, 1)} km";
            LastUpdatedLabel.Text = $"🔄 Actualizat la: {dateMeteo.LastUpdated.ToString("HH:mm:ss")} • Conexiune OpenWeatherMap";

        }
        else
        {
            await DisplayAlert("Eroare", "Nu s-au putut prelua datele meteo pentru această locație.", "OK");
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
        if (confirm)
        {
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }
    }
}
