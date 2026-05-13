using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace ClimaOS_Desktop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        EnsureLocalSmtpSettings();

        try
        {
            var services = activationState?.Context?.Services
                ?? Handler?.MauiContext?.Services;
            var theme = services?.GetService<ThemeService>();
            theme?.ApplyStoredTheme();
        }
        catch
        {
            // Tema este aplicată cu fallback la default dacă DI nu e gata.
        }

        return window;
    }

    private static void EnsureLocalSmtpSettings()
    {
        const string localFileName = "smtp.settings.local.json";
        const string exampleFileName = "smtp.settings.example.json";

        try
        {
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, localFileName);
            if (File.Exists(targetPath))
                return;

            var content = TryReadAppPackageFile(localFileName)
                         ?? TryReadAppPackageFile(exampleFileName);
            if (string.IsNullOrWhiteSpace(content))
                return;

            File.WriteAllText(targetPath, content);
        }
        catch
        {
            // Ignoram erorile; fallback-ul din EmailSettings acopera lipsa fisierului.
        }
    }

    private static string? TryReadAppPackageFile(string fileName)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(fileName)
                .GetAwaiter()
                .GetResult();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }
}
