namespace ClimaOS_Desktop.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var parola = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(parola))
        {
            await DisplayAlert("Eroare", "Completează email și parolă.", "OK");
            return;
        }

        // Navigăm către Dashboard
        await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
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

