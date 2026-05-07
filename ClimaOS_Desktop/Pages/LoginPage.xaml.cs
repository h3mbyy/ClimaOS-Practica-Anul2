using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Pages.Admin;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages;

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
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var parola = PasswordEntry.Text ?? string.Empty;

        try
        {
            var user = await _auth.LoginAsync(email ?? string.Empty, parola);
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

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        OnLoginClicked(sender, e);
    }
}
