using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Views.Admin;

public partial class LogsPage : ContentPage
{
    private readonly SystemLogService _logs;
    private readonly SessionStore _session;
    private readonly ObservableCollection<SystemLog> _items = new();

    public LogsPage()
        : this(ResolveService<SystemLogService>(), ResolveService<SessionStore>())
    {
    }

    public LogsPage(SystemLogService logs, SessionStore session)
    {
        InitializeComponent();
        _logs = logs;
        _session = session;
        LogsList.ItemsSource = _items;
        Shell.SetNavBarIsVisible(this, false);
        StatusPicker.SelectedIndex = 0;
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
            var status = StatusPicker.SelectedIndex switch
            {
                1 => "succes",
                2 => "eroare",
                _ => "Toate"
            };
            var list = await _logs.SearchAsync(SearchEntry.Text, status);
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
        StatusPicker.SelectedIndex = 0;
        await LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        var who = await DisplayPromptAsync("Jurnal", "Solicitat de:");
        if (string.IsNullOrWhiteSpace(who)) return;
        var status = await DisplayActionSheetAsync("Status", "Anuleaza", null, "succes", "eroare");
        if (status is null || status == "Anuleaza") return;
        var timeStr = await DisplayPromptAsync("Jurnal", "Timp raspuns (ms):", keyboard: Keyboard.Numeric);
        int? time = null;
        if (!string.IsNullOrWhiteSpace(timeStr) && int.TryParse(timeStr, out var t))
            time = t;

        var log = new SystemLog
        {
            RequestedBy = who.Trim(),
            Status = status,
            ResponseTimeMs = time
        };

        try
        {
            await _logs.CreateAsync(log);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not SystemLog l) return;
        var ok = await DisplayAlertAsync("Stergere",
            "Stergi acest jurnal?",
            "Da, sterge", "Anuleaza");
        if (!ok) return;

        try
        {
            await _logs.DeleteAsync(l.Id);
            _items.Remove(l);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
}
