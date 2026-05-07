using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Pages;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class SettingsPage : ContentPage
{
    private readonly ThemeService _theme;
    private readonly MySqlConnectionFactory _factory;
    private readonly DatabaseInitializer _initializer;
    private readonly AuthService _auth;
    private readonly SessionStore _session;

    public SettingsPage()
        : this(
            ResolveService<ThemeService>(),
            ResolveService<MySqlConnectionFactory>(),
            ResolveService<DatabaseInitializer>(),
            ResolveService<AuthService>(),
            ResolveService<SessionStore>())
    {
    }

    public SettingsPage(
        ThemeService theme,
        MySqlConnectionFactory factory,
        DatabaseInitializer initializer,
        AuthService auth,
        SessionStore session)
    {
        InitializeComponent();
        _theme = theme;
        _factory = factory;
        _initializer = initializer;
        _auth = auth;
        _session = session;
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

        UserInfoLabel.Text = _session.CurrentUser is { } u
            ? $"{u.Name} • {u.Email} • {u.RoleDisplay}"
            : "Niciun utilizator autentificat.";
    }

    private void OnDarkToggled(object sender, ToggledEventArgs e)
    {
        _theme.Set(e.Value ? AppTheme.Dark : AppTheme.Light);
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnTestClicked(object sender, EventArgs e)
    {
        var cfg = ReadForm();
        if (cfg is null) return;
        try
        {
            var temp = new MySqlConnectionFactory();
            temp.UpdateConfig(cfg);
            var ok = await temp.TestConnectionAsync();
            await DisplayAlert("Test conexiune",
                ok ? "Conexiune reușită." : "Conexiune eșuată.",
                "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnSaveDbClicked(object sender, EventArgs e)
    {
        var cfg = ReadForm();
        if (cfg is null) return;
        var ok = await DisplayAlert("Salvare configurare",
            "Confirmi salvarea? Aplicația va folosi noua configurare.",
            "Da", "Anulează");
        if (!ok) return;
        try
        {
            _factory.UpdateConfig(cfg);
            await _initializer.EnsureSchemaAsync();
            await DisplayAlert("Salvat", "Configurarea a fost actualizată.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private DatabaseConfig? ReadForm()
    {
        if (!uint.TryParse(PortEntry.Text, out var port))
        {
            DisplayAlert("Eroare", "Portul nu este un număr valid.", "OK");
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

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
