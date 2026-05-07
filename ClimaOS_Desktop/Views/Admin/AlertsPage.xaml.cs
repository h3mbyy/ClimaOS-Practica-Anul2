using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views.Admin;

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
        Shell.SetNavBarIsVisible(this, false);

        FromPicker.Date = DateTime.Today.AddDays(-7);
        ToPicker.Date = DateTime.Today;
        SeverityPicker.SelectedIndex = 0;
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
            var from = FromPicker.Date ?? DateTime.Today.AddDays(-7);
            var to = (ToPicker.Date ?? DateTime.Today).AddDays(1);

            var list = await _service.SearchAsync(SearchEntry.Text, severity, from, to);
            _items.Clear();
            foreach (var a in list) _items.Add(a);
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
        SeverityPicker.SelectedIndex = 0;
        FromPicker.Date = DateTime.Today.AddDays(-7);
        ToPicker.Date = DateTime.Today;
        await LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object? sender, EventArgs e)
        => await ShowEditorAsync(null);

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is WeatherAlert a)
            await ShowEditorAsync(a);
    }

    private async Task ShowEditorAsync(WeatherAlert? existing)
    {
        var alert = existing ?? new WeatherAlert();
        var title = await DisplayPromptAsync("Alerta", "Titlu:", initialValue: alert.Title);
        if (string.IsNullOrWhiteSpace(title)) return;
        var msg = await DisplayPromptAsync("Alerta", "Mesaj:", initialValue: alert.Message, maxLength: 500);
        if (string.IsNullOrWhiteSpace(msg)) return;
        var loc = await DisplayPromptAsync("Alerta", "Locatie (text):", initialValue: alert.LocationName) ?? string.Empty;
        var sevAns = await DisplayActionSheetAsync("Severitate", "Anuleaza", null,
            "Informare", "Avertisment", "Sever", "Extrem");
        if (sevAns is null || sevAns == "Anuleaza") return;

        var start = await DisplayPromptAsync("Alerta", "Data inceput (yyyy-MM-dd HH:mm):",
            initialValue: alert.StartsAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        if (string.IsNullOrWhiteSpace(start)) return;
        var end = await DisplayPromptAsync("Alerta", "Data sfarsit (yyyy-MM-dd HH:mm):",
            initialValue: alert.EndsAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        if (string.IsNullOrWhiteSpace(end)) return;

        if (!DateTime.TryParse(start, out var startDt) || !DateTime.TryParse(end, out var endDt))
        {
            await DisplayAlertAsync("Eroare", "Date invalide.", "OK");
            return;
        }

        try
        {
            alert.Title = title.Trim();
            alert.Message = msg.Trim();
            alert.LocationName = loc.Trim();
            alert.StartsAt = startDt;
            alert.EndsAt = endDt;
            alert.Severity = sevAns switch
            {
                "Avertisment" => AlertSeverity.Warning,
                "Sever" => AlertSeverity.Severe,
                "Extrem" => AlertSeverity.Extreme,
                _ => AlertSeverity.Info
            };

            await _service.SaveAsync(alert);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not WeatherAlert a) return;
        var ok = await DisplayAlertAsync("Stergere",
            $"Stergi alerta \"{a.Title}\"?",
            "Da, sterge", "Anuleaza");
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
