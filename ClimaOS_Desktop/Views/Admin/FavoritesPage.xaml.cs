using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views.Admin;

public partial class FavoritesPage : ContentPage
{
    private readonly UserFavoriteService _favorites;
    private readonly UserService _users;
    private readonly LocationService _locations;
    private readonly SessionStore _session;
    private readonly ObservableCollection<UserFavorite> _items = new();
    private List<User> _userCache = new();
    private List<ClimaOS_Desktop.Models.Location> _locationCache = new();

    public FavoritesPage()
        : this(
            ResolveService<UserFavoriteService>(),
            ResolveService<UserService>(),
            ResolveService<LocationService>(),
            ResolveService<SessionStore>())
    {
    }

    public FavoritesPage(
        UserFavoriteService favorites,
        UserService users,
        LocationService locations,
        SessionStore session)
    {
        InitializeComponent();
        _favorites = favorites;
        _users = users;
        _locations = locations;
        _session = session;
        FavoritesList.ItemsSource = _items;
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
        await LoadLookupsAsync();
        await LoadAsync();
    }

    private async Task LoadLookupsAsync()
    {
        _userCache = await _users.SearchAsync(null, null);
        _locationCache = await _locations.SearchAsync(null);
    }

    private async Task LoadAsync()
    {
        try
        {
            var list = await _favorites.SearchAsync(SearchEntry.Text);
            _items.Clear();
            foreach (var f in list) _items.Add(f);
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
        var userAns = await DisplayActionSheetAsync("Utilizator", "Anuleaza", null,
            _userCache.Select(u => $"{u.Name} ({u.Email})").ToArray());
        if (string.IsNullOrWhiteSpace(userAns) || userAns == "Anuleaza") return;
        var userIndex = Array.FindIndex(_userCache.ToArray(), u => userAns.Contains(u.Email));
        if (userIndex < 0) return;
        var userId = _userCache[userIndex].Id;

        var locAns = await DisplayActionSheetAsync("Locatie", "Anuleaza", null,
            _locationCache.Select(l => l.Display).ToArray());
        if (string.IsNullOrWhiteSpace(locAns) || locAns == "Anuleaza") return;
        var locIndex = Array.FindIndex(_locationCache.ToArray(), l => l.Display == locAns);
        if (locIndex < 0) return;
        var locationId = _locationCache[locIndex].Id;

        try
        {
            await _favorites.AddAsync(userId, locationId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not UserFavorite f) return;
        var ok = await DisplayAlertAsync("Stergere",
            $"Stergi favorita {f.UserEmail} - {f.LocationDisplay}?",
            "Da, sterge", "Anuleaza");
        if (!ok) return;

        try
        {
            await _favorites.DeleteAsync(f.Id);
            _items.Remove(f);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
}
