using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Common;

public static class ErrorHandler
{
    public static AppException Translate(Exception ex)
    {
        return ex switch
        {
            AppException app => app,
            MySqlException mysql => new DatabaseException(
                "Nu s-a putut comunica cu baza de date. Verifică conexiunea și încearcă din nou.",
                mysql),
            TimeoutException => new AppException(
                "Operația a durat prea mult și a fost oprită. Încearcă din nou.",
                "Timeout"),
            _ => new AppException(
                "A apărut o eroare neașteptată. Detalii: " + ex.Message,
                "Eroare",
                ex)
        };
    }

    public static async Task ShowAsync(Page page, Exception ex)
    {
        var app = Translate(ex);
        await page.DisplayAlert(app.Title, app.FriendlyMessage, "OK");
    }
}
