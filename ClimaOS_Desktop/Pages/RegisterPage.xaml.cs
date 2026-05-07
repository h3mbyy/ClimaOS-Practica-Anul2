namespace ClimaOS_Desktop.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var pass = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(pass) ||
            string.IsNullOrWhiteSpace(confirm))
        {
            await DisplayAlert("Eroare", "Completează toate câmpurile.", "OK");
            return;
        }

        if (!string.Equals(pass, confirm, StringComparison.Ordinal))
        {
            await DisplayAlert("Eroare", "Parolele nu se potrivesc.", "OK");
            return;
        }

        await DisplayAlert("Cont creat", $"Bine ai venit, {name}!", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackToLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnConfirmCompleted(object sender, EventArgs e)
    {
        OnCreateAccountClicked(sender, e);
    }
}

