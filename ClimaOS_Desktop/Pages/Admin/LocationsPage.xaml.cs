using System.Collections.ObjectModel;
using System.Globalization;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class LocationsPage : ContentPage
{
    private readonly LocationService _service;
    private readonly UserService _userService;
    private readonly SessionStore _session;
    private readonly ObservableCollection<Location> _items = new();
    private List<User> _users = new();

    public LocationsPage()
        : this(
            ResolveService<LocationService>(),
            ResolveService<UserService>(),
            ResolveService<SessionStore>())
    {
    }

    public LocationsPage(LocationService service, UserService userService, SessionStore session)
    {
        InitializeComponent();
        _service = service;
        _userService = userService;
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

        await LoadUsersAsync();
        await LoadAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            _users = await _userService.SearchAsync(null, null);
            UserFilterPicker.Items.Clear();
            UserFilterPicker.Items.Add("Toți utilizatorii");
            foreach (var u in _users)
                UserFilterPicker.Items.Add($"{u.Name} ({u.Email})");
            UserFilterPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            int? userId = null;
            if (UserFilterPicker.SelectedIndex > 0
                && UserFilterPicker.SelectedIndex - 1 < _users.Count)
            {
                userId = _users[UserFilterPicker.SelectedIndex - 1].Id;
            }

            var list = await _service.SearchAsync(SearchEntry.Text, userId);
            _items.Clear();
            foreach (var l in list) _items.Add(l);
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
        UserFilterPicker.SelectedIndex = 0;
        await LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await ShowEditorAsync(null);
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is Location loc)
            await ShowEditorAsync(loc);
    }

    private async Task ShowEditorAsync(Location? existing)
    {
        var loc = existing ?? new Location();
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
            await DisplayAlert("Eroare", "Coordonatele nu sunt numere valide.", "OK");
            return;
        }

        try
        {
            loc.Name = name.Trim();
            loc.Country = country.Trim();
            loc.Latitude = lat;
            loc.Longitude = lon;
            if (loc.UserId is null) loc.UserId = _session.CurrentUser?.Id;
            await _service.SaveAsync(loc);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not Location loc) return;
        var ok = await DisplayAlert("Ștergere",
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
}
