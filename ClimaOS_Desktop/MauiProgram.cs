using Microsoft.Extensions.Logging;
using ClimaOS_Desktop.Pages;
using ClimaOS_Desktop.Services;
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

		// Adăugare Servicii (Dependency Injection)
		builder.Services.AddSingleton<WeatherApiService>();
		builder.Services.AddSingleton<DatabaseService>();
		
		// Inregistrare Pagini
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<DashboardPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
