using Microsoft.Extensions.Logging;
using ClimaOS_Desktop.Views;
using ClimaOS_Desktop.Views.Admin;
using ClimaOS_Desktop.Services;
using ClimaOS_Desktop.ViewModels;
using ClimaOS_Desktop.Data;
using ClimaOS_Desktop.Data.Repositories;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
namespace ClimaOS_Desktop;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseLiveCharts()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("OpenSans-Regular.ttf", "Hanken Grotesk"); 
                fonts.AddFont("OpenSans-Regular.ttf", "MaterialSymbolsOutlined"); 
            });
        builder.Services.AddSingleton<MySqlConnectionFactory>();
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<WeatherPreferencesService>();
        builder.Services.AddSingleton<IConnectivity>(_ => Connectivity.Current);
        builder.Services.AddSingleton(_ => EmailSettings.LoadFromEnvironment());
        builder.Services.AddSingleton(_ => PasswordResetSettings.LoadFromEnvironment());
        builder.Services.AddSingleton<IAppNavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IWeatherService>(sp =>
            sp.GetRequiredService<WeatherApiService>());
        builder.Services.AddSingleton<UserRepository>();
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<AlertRepository>();
        builder.Services.AddSingleton<ReportRepository>();
        builder.Services.AddSingleton<UserFavoriteRepository>();
        builder.Services.AddSingleton<SystemLogRepository>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<AlertService>();
        builder.Services.AddSingleton<ReportService>();
        builder.Services.AddSingleton<UserFavoriteService>();
        builder.Services.AddSingleton<SystemLogService>();
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<WeatherSettingsService>();
        builder.Services.AddSingleton<EmailService>();
        builder.Services.AddSingleton<WeatherApiService>();
        builder.Services.AddTransient<WeatherViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ResetPasswordPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<LocationsPage>();
        builder.Services.AddTransient<AlertsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<SettingsPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        app.Services.GetRequiredService<ThemeService>().ApplyStoredTheme();
        return app;
    }
}
