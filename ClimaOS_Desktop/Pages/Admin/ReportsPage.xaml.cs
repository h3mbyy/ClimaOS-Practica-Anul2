using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class ReportsPage : ContentPage
{
    private readonly ReportService _reports;
    private readonly ExportService _export;
    private readonly UserService _users;
    private readonly LocationService _locations;
    private readonly AlertService _alerts;
    private readonly SessionStore _session;

    private readonly ObservableCollection<Report> _items = new();

    public ReportsPage()
        : this(
            ResolveService<ReportService>(),
            ResolveService<ExportService>(),
            ResolveService<UserService>(),
            ResolveService<LocationService>(),
            ResolveService<AlertService>(),
            ResolveService<SessionStore>())
    {
    }

    public ReportsPage(
        ReportService reports,
        ExportService export,
        UserService users,
        LocationService locations,
        AlertService alerts,
        SessionStore session)
    {
        InitializeComponent();
        _reports = reports;
        _export = export;
        _users = users;
        _locations = locations;
        _alerts = alerts;
        _session = session;
        ReportsList.ItemsSource = _items;
        Shell.SetNavBarIsVisible(this, false);
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
                4 => ReportType.Custom,
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

    private async void OnSearchClicked(object sender, EventArgs e) => await LoadAsync();

    private async void OnResetClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        TypePicker.SelectedIndex = 0;
        await LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddReportClicked(object sender, EventArgs e)
    {
        var title = await DisplayPromptAsync("Raport nou", "Titlu:");
        if (string.IsNullOrWhiteSpace(title)) return;
        var typeAns = await DisplayActionSheet("Tip raport", "Anulează", null,
            "Utilizatori", "Locații", "Alerte", "Personalizat");
        if (typeAns is null || typeAns == "Anulează") return;
        var notes = await DisplayPromptAsync("Raport nou", "Note (opțional):", maxLength: 500) ?? string.Empty;

        var type = typeAns switch
        {
            "Utilizatori" => ReportType.Users,
            "Locații" => ReportType.Locations,
            "Alerte" => ReportType.Alerts,
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

    private async void OnDeleteReportClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not Report r) return;
        var ok = await DisplayAlert("Ștergere",
            $"Ștergi raportul \"{r.Title}\"?",
            "Da, șterge", "Anulează");
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

    private async void OnExportUsersClicked(object sender, EventArgs e)
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
                    ("CreatLa", u => u.CreatedAt)
                },
                format.Value, "users");
            await NotifyExportSavedAsync(path, ReportType.Users, $"Export utilizatori ({data.Count})");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnExportLocationsClicked(object sender, EventArgs e)
    {
        try
        {
            var data = await _locations.SearchAsync(null, null);
            var format = await PickFormatAsync();
            if (format is null) return;
            var path = await _export.ExportAsync(data,
                new (string, Func<Location, object?>)[]
                {
                    ("Id", l => l.Id),
                    ("UserId", l => l.UserId),
                    ("Nume", l => l.Name),
                    ("Tara", l => l.Country),
                    ("Latitudine", l => l.Latitude),
                    ("Longitudine", l => l.Longitude),
                    ("CreatLa", l => l.CreatedAt)
                },
                format.Value, "locations");
            await NotifyExportSavedAsync(path, ReportType.Locations, $"Export locații ({data.Count})");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnExportAlertsClicked(object sender, EventArgs e)
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
                    ("Mesaj", a => a.Message),
                    ("Severitate", a => a.SeverityDisplay),
                    ("Locatie", a => a.LocationName),
                    ("Start", a => a.StartsAt),
                    ("Sfarsit", a => a.EndsAt)
                },
                format.Value, "alerts");
            await NotifyExportSavedAsync(path, ReportType.Alerts, $"Export alerte ({data.Count})");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async Task<ExportFormat?> PickFormatAsync()
    {
        var ans = await DisplayActionSheet("Format export", "Anulează", null, "CSV", "JSON");
        return ans switch
        {
            "CSV" => ExportFormat.Csv,
            "JSON" => ExportFormat.Json,
            _ => null
        };
    }

    private async Task NotifyExportSavedAsync(string path, ReportType type, string title)
    {
        await _reports.CreateAsync(title, type, $"Salvat la: {path}");
        await DisplayAlert("Export reușit", $"Fișier salvat la:\n{path}", "OK");
        await LoadAsync();
    }
}
