using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _auth;

    public RegisterPage() : this(ResolveService<AuthService>())
    {
    }

    public RegisterPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
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

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var pass = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

        if (!string.Equals(pass, confirm, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Eroare", "Parolele nu se potrivesc.", "OK");
            return;
        }

        try
        {
            await _auth.RegisterAsync(name, email, pass);
            await DisplayAlertAsync("Cont creat", $"Bine ai venit, {name}!", "OK");
            await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnBackToLoginTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnConfirmCompleted(object? sender, EventArgs e)
    {
        OnCreateAccountClicked(sender, e);
    }
}
