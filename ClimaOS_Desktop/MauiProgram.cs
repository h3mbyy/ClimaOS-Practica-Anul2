using Microsoft.Extensions.Logging;
using ClimaOS_Desktop.Pages;
using ClimaOS_Desktop.Pages.Admin;
using ClimaOS_Desktop.Services;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Data.Repositories;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace ClimaOS_Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Infrastructură
        builder.Services.AddSingleton<MySqlConnectionFactory>();
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<ThemeService>();

        // Repositories
        builder.Services.AddSingleton<UserRepository>();
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<AlertRepository>();
        builder.Services.AddSingleton<ReportRepository>();

        // Servicii business
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<AlertService>();
        builder.Services.AddSingleton<ReportService>();
        builder.Services.AddSingleton<ExportService>();

        // Servicii existente (păstrăm)
        builder.Services.AddSingleton<WeatherApiService>();
        builder.Services.AddSingleton<DatabaseService>();

        // Pagini publice
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<DashboardPage>();

        // Pagini admin
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<LocationsPage>();
        builder.Services.AddTransient<AlertsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
