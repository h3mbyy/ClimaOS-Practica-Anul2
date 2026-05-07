using ClimaOS_Desktop.Pages;
using ClimaOS_Desktop.Pages.Admin;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(UsersPage), typeof(UsersPage));
        Routing.RegisterRoute(nameof(LocationsPage), typeof(LocationsPage));
        Routing.RegisterRoute(nameof(AlertsPage), typeof(AlertsPage));
        Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        var target = args.Target?.Location?.OriginalString ?? string.Empty;

        var session = ResolveSession();
        if (session is null) return;

        if (target.Contains(nameof(LoginPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(RegisterPage), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!session.IsAuthenticated)
        {
            args.Cancel();
            Dispatcher.Dispatch(async () =>
                await GoToAsync($"//{nameof(LoginPage)}"));
            return;
        }

        var requiresAdmin =
            target.Contains(nameof(AdminDashboardPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(UsersPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(LocationsPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(AlertsPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(ReportsPage), StringComparison.OrdinalIgnoreCase) ||
            target.Contains(nameof(SettingsPage), StringComparison.OrdinalIgnoreCase);

        if (requiresAdmin && !session.IsAdmin)
        {
            args.Cancel();
            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Acces refuzat", "Această secțiune este disponibilă doar administratorilor.", "OK");
                await GoToAsync($"//{nameof(DashboardPage)}");
            });
        }
    }

    private static SessionStore? ResolveSession()
    {
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            return services?.GetService<SessionStore>();
        }
        catch
        {
            return null;
        }
    }
}
