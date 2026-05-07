using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views.Admin;

public partial class AdminDashboardPage : ContentPage
{
    private readonly UserService _users;
    private readonly LocationService _locations;
    private readonly AlertService _alerts;
    private readonly ReportService _reports;
    private readonly UserFavoriteService _favorites;
    private readonly SystemLogService _logs;
    private readonly SessionStore _session;
    private readonly AuthService _auth;
    private readonly ExportService _export;

    public AdminDashboardPage()
        : this(
            ResolveService<UserService>(),
            ResolveService<LocationService>(),
            ResolveService<AlertService>(),
            ResolveService<ReportService>(),
            ResolveService<UserFavoriteService>(),
            ResolveService<SystemLogService>(),
            ResolveService<SessionStore>(),
            ResolveService<AuthService>(),
            ResolveService<ExportService>())
    {
    }

    public AdminDashboardPage(
        UserService users,
        LocationService locations,
        AlertService alerts,
        ReportService reports,
        UserFavoriteService favorites,
        SystemLogService logs,
        SessionStore session,
        AuthService auth,
        ExportService export)
    {
        InitializeComponent();
        _users = users;
        _locations = locations;
        _alerts = alerts;
        _reports = reports;
        _favorites = favorites;
        _logs = logs;
        _session = session;
        _auth = auth;
        _export = export;
        Shell.SetNavBarIsVisible(this, false);
    }

    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            throw new InvalidOperationException(
                $"Nu pot rezolva {typeof(T).Name} înainte ca MauiContext să fie disponibil.");
        return services.GetRequiredService<T>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_session.IsAdmin)
        {
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            return;
        }

        WelcomeLabel.Text = $"Bun venit, {_session.CurrentUser?.Name ?? "Administrator"}";
        await RefreshStatsAsync();
        await LoadChartDataAsync();
    }

    private async Task RefreshStatsAsync()
    {
        try
        {
            var u = await _users.CountAsync();
            var l = await _locations.CountAsync();
            var a = await _alerts.CountAsync();
            var r = await _reports.CountAsync();
            var f = await _favorites.CountAsync();
            var g = await _logs.CountAsync();
            UsersCountLabel.Text = u.ToString();
            LocationsCountLabel.Text = l.ToString();
            AlertsCountLabel.Text = a.ToString();
            ReportsCountLabel.Text = r.ToString();
            FavoritesCountLabel.Text = f.ToString();
            LogsCountLabel.Text = g.ToString();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async Task LoadChartDataAsync()
    {
        try
        {
            var logs = await _logs.SearchAsync(null, null);
            var tempLogs = logs.Where(l => l.TemperatureInfo.HasValue)
                               .OrderBy(l => l.LogDate)
                               .TakeLast(20)
                               .ToList();
                               
            if (!tempLogs.Any()) return;

            var temperatures = tempLogs.Select(l => (double)l.TemperatureInfo!.Value).ToArray();
            var labels = tempLogs.Select(l => l.LogDate.ToString("dd/MM HH:mm")).ToArray();

            TemperatureChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = temperatures,
                    Name = "Temperatură",
                    GeometrySize = 10,
                    LineSmoothness = 0.5
                }
            };
            
            TemperatureChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    TextSize = 10
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Eroare grafic: {ex.Message}");
        }
    }
    
    private async void OnExportLogsClicked(object? sender, EventArgs e)
    {
        try
        {
            var logs = await _logs.SearchAsync(null, null);
            var columns = new List<(string Header, Func<ClimaOS_Desktop.Models.SystemLog, object?> Selector)>
            {
                ("ID", x => x.Id),
                ("Data și Ora", x => x.LogDate.ToString("yyyy-MM-dd HH:mm:ss")),
                ("Locație ID", x => x.LocationId),
                ("Locație", x => x.LocationName),
                ("Utilizator", x => x.RequestedBy),
                ("Temperatură (°C)", x => x.TemperatureInfo),
                ("Status", x => x.Status),
                ("Timp Răspuns (ms)", x => x.ResponseTimeMs)
            };

            var path = await _export.ExportAsync(logs, columns, ExportFormat.Excel);
            await DisplayAlert("Export reușit", $"Jurnalele au fost salvate în Excel la locația:\n{path}", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnUsersClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(UsersPage));

    private async void OnLocationsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(LocationsPage));

    private async void OnAlertsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AlertsPage));

    private async void OnReportsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ReportsPage));

    private async void OnFavoritesClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(FavoritesPage));

    private async void OnLogsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(LogsPage));

    private async void OnSettingsClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SettingsPage));

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
