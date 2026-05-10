using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Views.Admin;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly DatabaseInitializer _initializer;
    private readonly SessionStore _session;
    private bool _schemaEnsured;

    public LoginPage()
        : this(
            ResolveService<AuthService>(),
            ResolveService<DatabaseInitializer>(),
            ResolveService<SessionStore>())
    {
    }

    public LoginPage(AuthService auth, DatabaseInitializer initializer, SessionStore session)
    {
        InitializeComponent();
        _auth = auth;
        _initializer = initializer;
        _session = session;
        Shell.SetNavBarIsVisible(this, false);
    }

    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            throw new InvalidOperationException(
                $"Nu pot rezolva {typeof(T).Name} înainte ca MauiContext să fie disponibil.");
        return services.GetRequiredService<T>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_schemaEnsured) return;
        try
        {
            await _initializer.EnsureSchemaAsync();
            _schemaEnsured = true;

            var remembered = await _auth.RestoreRememberedSessionAsync();
            if (remembered is not null)
                await NavigateAfterLoginAsync(remembered);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var parola = PasswordEntry.Text ?? string.Empty;

        try
        {
            var user = await _auth.LoginAsync(email ?? string.Empty, parola, RememberMeCheckBox.IsChecked);
            await NavigateAfterLoginAsync(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async Task NavigateAfterLoginAsync(User user)
    {
        var route = user.Role == UserRole.Admin
            ? $"//{nameof(AdminDashboardPage)}"
            : $"//{nameof(DashboardPage)}";
        await Shell.Current.GoToAsync(route);
    }

    private async void OnRegisterTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private void OnPasswordCompleted(object? sender, EventArgs e)
    {
        OnLoginClicked(sender, e);
    }

    private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Google Login", "Autentificarea cu Google va fi disponibilă în curând.", "OK");
    }

    private async void OnResetPasswordTapped(object? sender, EventArgs e)
    {
        var email = await DisplayPromptAsync("Resetare parolă", "Introdu emailul contului:");
        if (string.IsNullOrWhiteSpace(email)) return;

        try
        {
            var temporaryPassword = await _auth.ResetPasswordAsync(email);
            await DisplayAlertAsync("Parolă resetată",
                $"Parola temporară este: {temporaryPassword}\nSchimb-o după autentificare.",
                "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
}
