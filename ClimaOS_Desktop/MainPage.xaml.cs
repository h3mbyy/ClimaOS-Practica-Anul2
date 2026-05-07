namespace ClimaOS_Desktop;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        // Preluăm ce a scris utilizatorul în căsuțe
        string email = EmailEntry.Text;
        string parola = PasswordEntry.Text;

        // Momentan punem un mesaj de test. Mai târziu vom conecta DatabaseHelper aici!
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(parola))
        {
            DisplayAlert("Eroare", "Te rugăm să completezi ambele câmpuri!", "OK");
        }
        else
        {
            DisplayAlert("Succes", $"Se încearcă autentificarea pentru: {email}", "OK");
        }
    }

    private void OnRegisterTapped(object sender, EventArgs e)
    {
        DisplayAlert("Creează cont", "Aici vom face trecerea la pagina de Înregistrare.", "OK");
    }
}