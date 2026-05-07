using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

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
}
