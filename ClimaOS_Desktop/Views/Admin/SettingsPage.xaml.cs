using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Views;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views.Admin;

public partial class SettingsPage : ContentPage
{
    private readonly ThemeService _theme;
    private readonly MySqlConnectionFactory _factory;
    private readonly DatabaseInitializer _initializer;
    private readonly AuthService _auth;
    private readonly SessionStore _session;
    private readonly WeatherSettingsService _weatherSettings;
    private readonly WeatherApiService _weatherApi;

    public SettingsPage()
        : this(
            ResolveService<ThemeService>(),
            ResolveService<MySqlConnectionFactory>(),
            ResolveService<DatabaseInitializer>(),
            ResolveService<AuthService>(),
            ResolveService<SessionStore>(),
            ResolveService<WeatherSettingsService>(),
            ResolveService<WeatherApiService>())
    {
    }

    public SettingsPage(
        ThemeService theme,
        MySqlConnectionFactory factory,
        DatabaseInitializer initializer,
        AuthService auth,
        SessionStore session,
        WeatherSettingsService weatherSettings,
        WeatherApiService weatherApi)
    {
        InitializeComponent();
        _theme = theme;
        _factory = factory;
        _initializer = initializer;
        _auth = auth;
        _session = session;
        _weatherSettings = weatherSettings;
        _weatherApi = weatherApi;
        Shell.SetNavBarIsVisible(this, false);
    }

    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MauiContext indisponibil.");
        return services.GetRequiredService<T>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DarkSwitch.IsToggled = _theme.CurrentTheme == AppTheme.Dark;

        var cfg = _factory.CurrentConfig;
        ServerEntry.Text = cfg.Server;
        PortEntry.Text = cfg.Port.ToString();
        DbEntry.Text = cfg.Database;
        UserEntry.Text = cfg.User;
        PasswordEntry.Text = cfg.Password;
        WeatherApiKeyEntry.Text = _weatherSettings.ApiKey;

        UserInfoLabel.Text = _session.CurrentUser is { } u
            ? $"{u.Name} • {u.Email} • {u.RoleDisplay}"
            : "Niciun utilizator autentificat.";
    }

    private void OnDarkToggled(object? sender, ToggledEventArgs e)
    {
        _theme.Set(e.Value ? AppTheme.Dark : AppTheme.Light);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        var cfg = ReadForm();
        if (cfg is null) return;
        try
        {
            var temp = new MySqlConnectionFactory();
            temp.UpdateConfig(cfg);
            var ok = await temp.TestConnectionAsync();
            await DisplayAlertAsync("Test conexiune",
                ok ? "Conexiune reușită." : "Conexiune eșuată.",
                "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnSaveDbClicked(object? sender, EventArgs e)
    {
        var cfg = ReadForm();
        if (cfg is null) return;
        var ok = await DisplayAlertAsync("Salvare configurare",
            "Confirmi salvarea? Aplicația va folosi noua configurare.",
            "Da", "Anulează");
        if (!ok) return;
        try
        {
            _factory.UpdateConfig(cfg);
            await _initializer.EnsureSchemaAsync();
            await DisplayAlertAsync("Salvat", "Configurarea a fost actualizată.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        if (_session.CurrentUser is null)
        {
            await DisplayAlertAsync("Parolă", "Nu există un utilizator autentificat.", "OK");
            return;
        }

        var current = CurrentPasswordEntry.Text ?? string.Empty;
        var next = NewPasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmNewPasswordEntry.Text ?? string.Empty;

        if (!string.Equals(next, confirm, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Eroare", "Parolele noi nu se potrivesc.", "OK");
            return;
        }

        try
        {
            await _auth.ChangePasswordAsync(_session.CurrentUser.Id, current, next);
            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            ConfirmNewPasswordEntry.Text = string.Empty;
            await DisplayAlertAsync("Parolă schimbată", "Parola contului curent a fost actualizată.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnSaveWeatherApiClicked(object? sender, EventArgs e)
    {
        _weatherSettings.ApiKey = WeatherApiKeyEntry.Text ?? string.Empty;
        await DisplayAlertAsync("Cheie salvata", "Cheia WeatherAPI a fost salvata local.", "OK");
    }

    private async void OnTestWeatherApiClicked(object? sender, EventArgs e)
    {
        _weatherSettings.ApiKey = WeatherApiKeyEntry.Text ?? string.Empty;
        var (ok, message) = await _weatherApi.TestApiKeyAsync();
        await DisplayAlertAsync("Test API meteo", message, "OK");
    }

    private DatabaseConfig? ReadForm()
    {
        if (!uint.TryParse(PortEntry.Text, out var port))
        {
            DisplayAlertAsync("Eroare", "Portul nu este un număr valid.", "OK");
            return null;
        }
        return new DatabaseConfig
        {
            Server = (ServerEntry.Text ?? string.Empty).Trim(),
            Port = port,
            Database = (DbEntry.Text ?? string.Empty).Trim(),
            User = (UserEntry.Text ?? string.Empty).Trim(),
            Password = PasswordEntry.Text ?? string.Empty
        };
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    private async void OnUsersClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//UsersPage");
    private async void OnLocationsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//LocationsPage");
    private async void OnAlertsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//AlertsPage");
    private async void OnReportsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//ReportsPage");
    private async void OnFavoritesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//FavoritesPage");
    private async void OnLogsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//LogsPage");
    private async void OnSettingsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//SettingsPage");
}
