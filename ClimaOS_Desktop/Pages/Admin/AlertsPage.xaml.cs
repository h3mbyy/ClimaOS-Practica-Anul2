using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class AlertsPage : ContentPage
{
    private readonly AlertService _service;
    private readonly SessionStore _session;
    private readonly ObservableCollection<WeatherAlert> _items = new();

    public AlertsPage()
        : this(ResolveService<AlertService>(), ResolveService<SessionStore>())
    {
    }

    public AlertsPage(AlertService service, SessionStore session)
    {
        InitializeComponent();
        _service = service;
        _session = session;
        AlertsList.ItemsSource = _items;
        FromPicker.Date = DateTime.Today.AddDays(-7);
        ToPicker.Date = DateTime.Today.AddDays(7);
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
            AlertSeverity? severity = SeverityPicker.SelectedIndex switch
            {
                1 => AlertSeverity.Info,
                2 => AlertSeverity.Warning,
                3 => AlertSeverity.Severe,
                4 => AlertSeverity.Extreme,
                _ => null
            };
            var list = await _service.SearchAsync(SearchEntry.Text, severity, FromPicker.Date, ToPicker.Date.AddDays(1));
            _items.Clear();
            foreach (var a in list) _items.Add(a);
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
        SeverityPicker.SelectedIndex = 0;
        FromPicker.Date = DateTime.Today.AddDays(-7);
        ToPicker.Date = DateTime.Today.AddDays(7);
        await LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object sender, EventArgs e)
        => await ShowEditorAsync(null);

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is WeatherAlert a)
            await ShowEditorAsync(a);
    }

    private async Task ShowEditorAsync(WeatherAlert? existing)
    {
        var alert = existing ?? new WeatherAlert
        {
            StartsAt = DateTime.Now,
            EndsAt = DateTime.Now.AddHours(6)
        };

        var title = await DisplayPromptAsync("Alertă", "Titlu:", initialValue: alert.Title);
        if (string.IsNullOrWhiteSpace(title)) return;
        var msg = await DisplayPromptAsync("Alertă", "Mesaj:", initialValue: alert.Message, maxLength: 500);
        if (string.IsNullOrWhiteSpace(msg)) return;
        var loc = await DisplayPromptAsync("Alertă", "Locație (text):", initialValue: alert.LocationName) ?? string.Empty;
        var sevAns = await DisplayActionSheet("Severitate", "Anulează", null,
            "Informare", "Avertisment", "Sever", "Extrem");
        if (sevAns is null || sevAns == "Anulează") return;

        var hoursStr = await DisplayPromptAsync("Alertă", "Durată (ore):",
            initialValue: ((int)(alert.EndsAt - alert.StartsAt).TotalHours).ToString());
        if (!int.TryParse(hoursStr, out var hours) || hours <= 0) hours = 6;

        try
        {
            alert.Title = title.Trim();
            alert.Message = msg.Trim();
            alert.LocationName = loc.Trim();
            alert.Severity = sevAns switch
            {
                "Avertisment" => AlertSeverity.Warning,
                "Sever" => AlertSeverity.Severe,
                "Extrem" => AlertSeverity.Extreme,
                _ => AlertSeverity.Info
            };
            if (existing is null) alert.StartsAt = DateTime.Now;
            alert.EndsAt = alert.StartsAt.AddHours(hours);

            await _service.SaveAsync(alert);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not WeatherAlert a) return;
        var ok = await DisplayAlert("Ștergere",
            $"Ștergi alerta \"{a.Title}\"?",
            "Da, șterge", "Anulează");
        if (!ok) return;

        try
        {
            await _service.DeleteAsync(a.Id);
            _items.Remove(a);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
}
