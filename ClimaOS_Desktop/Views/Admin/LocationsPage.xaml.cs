using System.Collections.ObjectModel;
using System.Globalization;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using LocationModel = ClimaOS_Desktop.Models.Location;

namespace ClimaOS_Desktop.Views.Admin;

public partial class LocationsPage : ContentPage
{
    private readonly LocationService _service;
    private readonly SessionStore _session;
    private readonly ObservableCollection<LocationModel> _items = new();

    public LocationsPage()
        : this(
            ResolveService<LocationService>(),
            ResolveService<SessionStore>())
    {
    }

    public LocationsPage(LocationService service, SessionStore session)
    {
        InitializeComponent();
        _service = service;
        _session = session;
        LocationsList.ItemsSource = _items;
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
            var list = await _service.SearchAsync(SearchEntry.Text);
            _items.Clear();
            foreach (var l in list) _items.Add(l);
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
        await LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await ShowEditorAsync(null);
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is LocationModel loc)
            await ShowEditorAsync(loc);
    }

    private async Task ShowEditorAsync(LocationModel? existing)
    {
        var loc = existing ?? new LocationModel();
        var name = await DisplayPromptAsync("Locație", "Nume:", initialValue: loc.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        var country = await DisplayPromptAsync("Locație", "Țară:", initialValue: loc.Country) ?? string.Empty;
        var latStr = await DisplayPromptAsync("Locație", "Latitudine:",
            initialValue: loc.Latitude.ToString(CultureInfo.InvariantCulture));
        if (string.IsNullOrWhiteSpace(latStr)) return;
        var lonStr = await DisplayPromptAsync("Locație", "Longitudine:",
            initialValue: loc.Longitude.ToString(CultureInfo.InvariantCulture));
        if (string.IsNullOrWhiteSpace(lonStr)) return;

        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            await DisplayAlertAsync("Eroare", "Coordonatele nu sunt numere valide.", "OK");
            return;
        }

        try
        {
            loc.Name = name.Trim();
            loc.Country = country.Trim();
            loc.Latitude = lat;
            loc.Longitude = lon;
            await _service.SaveAsync(loc);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not LocationModel loc) return;
        var ok = await DisplayAlertAsync("Ștergere",
            $"Ștergi locația \"{loc.Display}\"?",
            "Da, șterge", "Anulează");
        if (!ok) return;

        try
        {
            await _service.DeleteAsync(loc.Id);
            _items.Remove(loc);
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
