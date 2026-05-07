using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class AdminDashboardPage : ContentPage
{
    private readonly UserService _users;
    private readonly LocationService _locations;
    private readonly AlertService _alerts;
    private readonly ReportService _reports;
    private readonly SessionStore _session;
    private readonly AuthService _auth;

    public AdminDashboardPage()
        : this(
            ResolveService<UserService>(),
            ResolveService<LocationService>(),
            ResolveService<AlertService>(),
            ResolveService<ReportService>(),
            ResolveService<SessionStore>(),
            ResolveService<AuthService>())
    {
    }

    public AdminDashboardPage(
        UserService users,
        LocationService locations,
        AlertService alerts,
        ReportService reports,
        SessionStore session,
        AuthService auth)
    {
        InitializeComponent();
        _users = users;
        _locations = locations;
        _alerts = alerts;
        _reports = reports;
        _session = session;
        _auth = auth;
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
    }

    private async Task RefreshStatsAsync()
    {
        try
        {
            var u = await _users.CountAsync();
            var l = await _locations.CountAsync();
            var a = await _alerts.CountAsync();
            var r = await _reports.CountAsync();
            UsersCountLabel.Text = u.ToString();
            LocationsCountLabel.Text = l.ToString();
            AlertsCountLabel.Text = a.ToString();
            ReportsCountLabel.Text = r.ToString();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnUsersClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(UsersPage));

    private async void OnLocationsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(LocationsPage));

    private async void OnAlertsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AlertsPage));

    private async void OnReportsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ReportsPage));

    private async void OnSettingsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SettingsPage));

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        _auth.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
