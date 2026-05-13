using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using ClimaOS_Desktop.Views;
using ClimaOS_Desktop.Views.Admin;
using LocationModel = ClimaOS_Desktop.Models.Location;
namespace ClimaOS_Desktop.ViewModels;
public partial class WeatherViewModel : ObservableObject
{
    private readonly IWeatherService _weatherService;
    private readonly ThemeService _themeService;
    private readonly WeatherPreferencesService _preferences;
    private readonly IAppNavigationService _navigation;
    private readonly IConnectivity _connectivity;
    private readonly LocationService _locationService;
    private readonly UserFavoriteService _favoriteService;
    private readonly SessionStore _session;
    private readonly AuthService _auth;
    private LocationModel? _currentLocation;
    public WeatherViewModel(
        IWeatherService weatherService,
        ThemeService themeService,
        WeatherPreferencesService preferences,
        IAppNavigationService navigation,
        IConnectivity connectivity,
        LocationService locationService,
        UserFavoriteService favoriteService,
        SessionStore session,
        AuthService auth)
    {
        _weatherService = weatherService;
        _themeService = themeService;
        _preferences = preferences;
        _navigation = navigation;
        _connectivity = connectivity;
        _locationService = locationService;
        _favoriteService = favoriteService;
        _session = session;
        _auth = auth;
        PrimaryNavigation = new ObservableCollection<NavItemViewModel>
        {
            new NavItemViewModel("forecast", "Prognoză", "☁"),
            new NavItemViewModel("maps", "Hartă", "🗺"),
            new NavItemViewModel("stations", "Stații", "⌖"),
            new NavItemViewModel("users", "Utilizatori", "👥")
        };
        FooterNavigation = new ObservableCollection<NavItemViewModel>
        {
            new NavItemViewModel("settings", "Setări", "⚙"),
            new NavItemViewModel("theme", "Tema întunecată", "◐"),
            new NavItemViewModel("support", "Suport", "❓"),
            new NavItemViewModel("logout", "Deconectare", "⇥") { IsDestructive = true }
        };
        connectivity.ConnectivityChanged += (_, _) =>
        {
            IsOffline = _connectivity.NetworkAccess != NetworkAccess.Internet;
        };
        IsOffline = _connectivity.NetworkAccess != NetworkAccess.Internet;
        SearchQuery = _preferences.LastLocation;
        IsSidebarCollapsed = _preferences.IsSidebarCollapsed;
        SelectNavigation("forecast");
    }
    public ObservableCollection<NavItemViewModel> PrimaryNavigation { get; }
    public ObservableCollection<NavItemViewModel> FooterNavigation { get; }
    public ObservableCollection<HourlyForecast> HourlyForecast { get; } = new();
    public ObservableCollection<DailyForecast> DailyForecast { get; } = new();
    [ObservableProperty]
    private string searchQuery = "București";
    [ObservableProperty]
    private string locationName = "București, RO";
    [ObservableProperty]
    private string temperatureText = "21°";
    [ObservableProperty]
    private string conditionText = "Cer variabil";
    [ObservableProperty]
    private string rangeText = "L: 17°  H: 24°";
    [ObservableProperty]
    private string lastUpdatedText = "Actualizat acum";
    [ObservableProperty]
    private string humidityText = "58%";
    [ObservableProperty]
    private string uvIndexText = "4.2";
    [ObservableProperty]
    private string uvCategoryText = "Moderat";
    [ObservableProperty]
    private string windText = "16 km/h";
    [ObservableProperty]
    private string pressureText = "1014 hPa";
    [ObservableProperty]
    private bool isFavorite;
    [ObservableProperty]
    private bool isBusy;
    [ObservableProperty]
    private bool isOffline;
    [ObservableProperty]
    private bool isDesktop;
    [ObservableProperty]
    private bool isTablet;
    [ObservableProperty]
    private bool isMobile;
    [ObservableProperty]
    private bool isSidebarCollapsed;
    public bool ShowDesktopNavigation => !IsMobile;
    public bool ShowMobileNavigation => IsMobile;
    public bool ShowDesktopCards => !IsMobile;
    public bool ShowMobileCards => IsMobile;
    public bool ShowSidebarBrand => !IsSidebarCollapsed;
    public bool IsAuthenticated => _session.IsAuthenticated;
    public string FavoriteIcon => IsFavorite ? "★" : "☆";
    public string ThemeLabel => _themeService.CurrentTheme == AppTheme.Dark ? "Tema întunecată" : "Tema luminoasă";
    public string SidebarToggleIcon => IsSidebarCollapsed ? "☰" : "✕";
    public double SidebarWidth => IsSidebarCollapsed ? 92 : 280;
    public string UpgradeButtonText => IsSidebarCollapsed ? "Pro" : "Treci la Pro";
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;
        await SearchAsync(SearchQuery);
    }
    [RelayCommand]
    private async Task SearchAsync(string? query = null)
    {
        if (IsBusy)
            return;
        var safeQuery = string.IsNullOrWhiteSpace(query) ? SearchQuery : query.Trim();
        if (string.IsNullOrWhiteSpace(safeQuery))
            return;
        if (IsOffline)
        {
            await _navigation.ShowMessageAsync("Fără conexiune", "Aplicația este offline. Se afișează ultimele date disponibile sau demo.");
        }
        try
        {
            IsBusy = true;
            var dashboard = await _weatherService.GetWeatherAsync(safeQuery);
            ApplyDashboard(dashboard);
            _preferences.LastLocation = dashboard.Current.CityName;
            SearchQuery = dashboard.Current.CityName;
            _currentLocation = await _locationService.EnsureAsync(
                dashboard.Current.CityName,
                dashboard.Current.CountryCode,
                dashboard.Current.Latitude,
                dashboard.Current.Longitude);
            await RefreshFavoriteStateAsync();
        }
        catch (Exception ex)
        {
            await _navigation.ShowMessageAsync("Weather", $"Nu am putut actualiza vremea: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task UseMyLocationAsync()
    {
        try
        {
            var deviceLocation = await Geolocation.Default.GetLastKnownLocationAsync();
            if (deviceLocation is null)
            {
                await SearchAsync(SearchQuery);
                return;
            }
            var placemark = (await Geocoding.Default.GetPlacemarksAsync(deviceLocation.Latitude, deviceLocation.Longitude)).FirstOrDefault();
            var label = placemark?.Locality ?? placemark?.SubAdminArea ?? placemark?.CountryName ?? SearchQuery;
            await SearchAsync(label);
        }
        catch
        {
            await SearchAsync(SearchQuery);
        }
    }
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (_session.CurrentUser is null || _currentLocation is null)
        {
            await _navigation.ShowMessageAsync("Favorite", "Autentifică-te și selectează o locație pentru a o salva la favorite.");
            return;
        }
        var existing = await _favoriteService.GetForUserLocationAsync(_session.CurrentUser.Id, _currentLocation.Id);
        if (existing is null)
        {
            await _favoriteService.AddAsync(_session.CurrentUser.Id, _currentLocation.Id);
            IsFavorite = true;
        }
        else
        {
            await _favoriteService.DeleteAsync(existing.Id);
            IsFavorite = false;
        }
        OnPropertyChanged(nameof(FavoriteIcon));
    }
    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.Toggle();
        OnPropertyChanged(nameof(ThemeLabel));
    }
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        _preferences.IsSidebarCollapsed = IsSidebarCollapsed;
        OnPropertyChanged(nameof(SidebarToggleIcon));
        OnPropertyChanged(nameof(ShowSidebarBrand));
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(UpgradeButtonText));
    }
    [RelayCommand]
    private async Task NavigateAsync(NavItemViewModel? item)
    {
        if (item is null)
            return;
        SelectNavigation(item.Key);
        switch (item.Key)
        {
            case "forecast":
            case "maps":
            case "stations":
                break;
            case "users":
                if (_session.IsAdmin)
                    await _navigation.NavigateToAsync(nameof(UsersPage));
                else
                    await _navigation.ShowMessageAsync("Users", "Secțiunea Users este disponibilă pentru administratori.");
                break;
            case "settings":
                if (_session.IsAdmin)
                    await _navigation.NavigateToAsync(nameof(SettingsPage));
                else
                    await _navigation.ShowMessageAsync("Settings", "Setările avansate sunt disponibile în contul de administrator.");
                break;
            case "theme":
                ToggleTheme();
                break;
            case "support":
                await _navigation.ShowMessageAsync("Suport", "Pentru ajutor, contactează echipa ClimaOS.");
                break;
            case "logout":
                _auth.Logout();
                await _navigation.NavigateToAsync($"//{nameof(LoginPage)}");
                break;
        }
    }
    [RelayCommand]
    private Task UpgradePlanAsync()
        => _navigation.ShowMessageAsync("ClimaOS Pro", "Planul Pro va include hărți avansate, alerte și prognoză extinsă.");
    public void UpdateAdaptiveLayout(double width)
    {
        var idiom = DeviceInfo.Current.Idiom;
        IsMobile = idiom == DeviceIdiom.Phone || width < 720;
        IsTablet = !IsMobile && (idiom == DeviceIdiom.Tablet || width < 1100);
        IsDesktop = !IsMobile && !IsTablet;
        OnPropertyChanged(nameof(ShowDesktopNavigation));
        OnPropertyChanged(nameof(ShowMobileNavigation));
        OnPropertyChanged(nameof(ShowDesktopCards));
        OnPropertyChanged(nameof(ShowMobileCards));
    }
    private void ApplyDashboard(WeatherDashboardData dashboard)
    {
        LocationName = string.IsNullOrWhiteSpace(dashboard.Current.CountryCode)
            ? dashboard.Current.CityName
            : $"{dashboard.Current.CityName}, {dashboard.Current.CountryCode}";
        TemperatureText = $"{Math.Round(dashboard.Current.Temperature)}°";
        ConditionText = dashboard.Current.Condition;
        RangeText = $"Min: {Math.Round(dashboard.Current.TempMin)}°  Max: {Math.Round(dashboard.Current.TempMax)}°";
        LastUpdatedText = $"Actualizat {dashboard.Current.LastUpdated:HH:mm}";
        HumidityText = $"{dashboard.Current.Humidity}%";
        UvIndexText = dashboard.UvIndex.ToString("0.0");
        UvCategoryText = dashboard.UvCategory;
        WindText = $"{Math.Round(dashboard.Current.WindSpeed)} km/h";
        PressureText = $"{dashboard.Current.Pressure} hPa";
        HourlyForecast.Clear();
        foreach (var hourly in dashboard.HourlyForecast)
            HourlyForecast.Add(hourly);
        DailyForecast.Clear();
        foreach (var daily in dashboard.DailyForecast)
            DailyForecast.Add(daily);
    }
    private async Task RefreshFavoriteStateAsync()
    {
        if (_session.CurrentUser is null || _currentLocation is null)
        {
            IsFavorite = false;
            OnPropertyChanged(nameof(FavoriteIcon));
            return;
        }
        IsFavorite = await _favoriteService.GetForUserLocationAsync(_session.CurrentUser.Id, _currentLocation.Id) is not null;
        OnPropertyChanged(nameof(FavoriteIcon));
    }
    private void SelectNavigation(string key)
    {
        foreach (var item in PrimaryNavigation)
            item.IsSelected = item.Key == key;
        foreach (var item in FooterNavigation)
            item.IsSelected = item.Key == key;
    }
}
