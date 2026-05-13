using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using LocationModel = ClimaOS_Desktop.Models.Location;
namespace ClimaOS_Desktop.Views.Admin;
public partial class ReportsPage : ContentPage
{
    private readonly ReportService _reports;
    private readonly UserService _users;
    private readonly LocationService _locations;
    private readonly AlertService _alerts;
    private readonly UserFavoriteService _favorites;
    private readonly SystemLogService _logs;
    private readonly ExportService _export;
    private readonly SessionStore _session;
    private readonly ObservableCollection<Report> _items = new();
    public ReportsPage()
        : this(
            ResolveService<ReportService>(),
            ResolveService<UserService>(),
            ResolveService<LocationService>(),
            ResolveService<AlertService>(),
            ResolveService<UserFavoriteService>(),
            ResolveService<SystemLogService>(),
            ResolveService<ExportService>(),
            ResolveService<SessionStore>())
    {
    }
    public ReportsPage(
        ReportService reports,
        UserService users,
        LocationService locations,
        AlertService alerts,
        UserFavoriteService favorites,
        SystemLogService logs,
        ExportService export,
        SessionStore session)
    {
        InitializeComponent();
        _reports = reports;
        _users = users;
        _locations = locations;
        _alerts = alerts;
        _favorites = favorites;
        _logs = logs;
        _export = export;
        _session = session;
        ReportsList.ItemsSource = _items;
        Shell.SetNavBarIsVisible(this, false);
        TypePicker.SelectedIndex = 0;
    }
    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MauiContext indisponibil.");
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
        await LoadAsync();
    }
    private async Task LoadAsync()
    {
        try
        {
            ReportType? type = TypePicker.SelectedIndex switch
            {
                1 => ReportType.Users,
                2 => ReportType.Locations,
                3 => ReportType.Alerts,
                4 => ReportType.Favorites,
                5 => ReportType.Logs,
                6 => ReportType.Custom,
                _ => null
            };
            var list = await _reports.SearchAsync(SearchEntry.Text, type);
            _items.Clear();
            foreach (var r in list) _items.Add(r);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnSearchClicked(object? sender, EventArgs e) => await LoadAsync();
    private async void OnResetClicked(object? sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        TypePicker.SelectedIndex = 0;
        await LoadAsync();
    }
    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
    private async void OnAddReportClicked(object? sender, EventArgs e)
    {
        var title = await DisplayPromptAsync("Raport nou", "Titlu:");
        if (string.IsNullOrWhiteSpace(title)) return;
        var typeAns = await DisplayActionSheetAsync("Tip raport", "Anuleaza", null,
            "Utilizatori", "Locatii", "Alerte", "Favorite", "Jurnale", "Personalizat");
        if (typeAns is null || typeAns == "Anuleaza") return;
        var notes = await DisplayPromptAsync("Raport nou", "Note (optional):", maxLength: 500) ?? string.Empty;
        var type = typeAns switch
        {
            "Utilizatori" => ReportType.Users,
            "Locatii" => ReportType.Locations,
            "Alerte" => ReportType.Alerts,
            "Favorite" => ReportType.Favorites,
            "Jurnale" => ReportType.Logs,
            _ => ReportType.Custom
        };
        try
        {
            await _reports.CreateAsync(title, type, notes);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnDeleteReportClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not Report r) return;
        var ok = await DisplayAlertAsync("Stergere",
            $"Stergi raportul \"{r.Title}\"?",
            "Da, sterge", "Anuleaza");
        if (!ok) return;
        try
        {
            await _reports.DeleteAsync(r.Id);
            _items.Remove(r);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async Task<ExportFormat?> PickFormatAsync()
    {
        var ans = await DisplayActionSheetAsync("Format export", "Anuleaza", null, "CSV", "JSON", "Excel");
        return ans switch
        {
            "CSV" => ExportFormat.Csv,
            "JSON" => ExportFormat.Json,
            "Excel" => ExportFormat.Excel,
            _ => null
        };
    }
    private async Task NotifyExportSavedAsync(string path, ReportType type, string title)
    {
        await _reports.CreateAsync(title, type, $"Salvat la: {path}");
        await DisplayAlertAsync("Export reusit", $"Fisier salvat la:\n{path}", "OK");
        await LoadAsync();
    }
    private async void OnExportUsersClicked(object? sender, EventArgs e)
    {
        try
        {
            var data = await _users.SearchAsync(null, null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<User, object?>)[]
                {
                    ("Id", u => u.Id),
                    ("Nume", u => u.Name),
                    ("Email", u => u.Email),
                    ("Rol", u => u.RoleDisplay),
                    ("Creat", u => u.CreatedAtDisplay)
                },
                format.Value);
            await NotifyExportSavedAsync(path, ReportType.Users, "Export utilizatori");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnExportLocationsClicked(object? sender, EventArgs e)
    {
        try
        {
            var data = await _locations.SearchAsync(null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<LocationModel, object?>)[]
                {
                    ("Id", l => l.Id),
                    ("Nume", l => l.Name),
                    ("Tara", l => l.Country),
                    ("Lat", l => l.Latitude),
                    ("Lon", l => l.Longitude)
                },
                format.Value);
            await NotifyExportSavedAsync(path, ReportType.Locations, "Export locatii");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnExportAlertsClicked(object? sender, EventArgs e)
    {
        try
        {
            var data = await _alerts.SearchAsync(null, null, null, null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<WeatherAlert, object?>)[]
                {
                    ("Id", a => a.Id),
                    ("Titlu", a => a.Title),
                    ("Severitate", a => a.SeverityDisplay),
                    ("Locatie", a => a.LocationName),
                    ("Start", a => a.StartsAt),
                    ("Sfarsit", a => a.EndsAt)
                },
                format.Value);
            await NotifyExportSavedAsync(path, ReportType.Alerts, "Export alerte");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnExportFavoritesClicked(object? sender, EventArgs e)
    {
        try
        {
            var data = await _favorites.SearchAsync(null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<UserFavorite, object?>)[]
                {
                    ("Id", f => f.Id),
                    ("Utilizator", f => f.UserEmail),
                    ("Locatie", f => f.LocationDisplay),
                    ("Adaugat", f => f.AddedAtDisplay)
                },
                format.Value);
            await NotifyExportSavedAsync(path, ReportType.Favorites, "Export favorite");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnExportLogsClicked(object? sender, EventArgs e)
    {
        try
        {
            var data = await _logs.SearchAdvancedAsync(null, "Toate", null, null, null, null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<SystemLog, object?>)[]
                {
                    ("Id", l => l.Id),
                    ("Locatie", l => l.LocationName),
                    ("Cine", l => l.RequestedBy),
                    ("Status", l => l.StatusDisplay),
                    ("Raspuns(ms)", l => l.ResponseTimeMs),
                    ("Data", l => l.LogDateDisplay)
                },
                format.Value);
            await NotifyExportSavedAsync(path, ReportType.Logs, "Export jurnale");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnUsersClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//UsersPage");
    private async void OnLocationsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//LocationsPage");
    private async void OnAlertsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//AlertsPage");
    private async void OnReportsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//ReportsPage");
    private async void OnFavoritesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//FavoritesPage");
    private async void OnLogsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//LogsPage");
    private async void OnSettingsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"//SettingsPage");
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        var auth = ResolveService<AuthService>();
        auth.Logout();
        await Shell.Current.GoToAsync($"//LoginPage");
    }
}
