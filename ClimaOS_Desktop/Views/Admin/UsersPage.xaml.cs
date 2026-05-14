using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
namespace ClimaOS_Desktop.Views.Admin;
public partial class UsersPage : ContentPage
{
    private readonly UserService _service;
    private readonly SessionStore _session;
    private readonly ObservableCollection<User> _items = new();
    private User? _editingUser;
    public UsersPage()
        : this(ResolveService<UserService>(), ResolveService<SessionStore>())
    {
    }
    public UsersPage(UserService service, SessionStore session)
    {
        InitializeComponent();
        _service = service;
        _session = session;
        UsersList.ItemsSource = _items;
        Shell.SetNavBarIsVisible(this, false);
        EditorRolePicker.SelectedIndex = 0;
        ResetEditor();
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
            var query = SearchEntry.Text;
            UserRole? role = RolePicker.SelectedIndex switch
            {
                1 => UserRole.Admin,
                2 => UserRole.User,
                _ => null
            };
            var list = await _service.SearchAsync(query, role);
            _items.Clear();
            foreach (var u in list) _items.Add(u);
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
        RolePicker.SelectedIndex = 0;
        await LoadAsync();
    }
    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
    private async void OnAddClicked(object? sender, EventArgs e)
    {
        ResetEditor();
    }
    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not User user) return;
        _editingUser = user;
        EditorTitleLabel.Text = "Editare utilizator";
        EditorSubtitleLabel.Text = "Actualizează datele și rolul direct în MySQL.";
        EditorNameEntry.Text = user.Name;
        EditorEmailEntry.Text = user.Email;
        EditorRolePicker.SelectedIndex = user.Role == UserRole.Admin ? 1 : 0;
        EditorPasswordEntry.Text = string.Empty;
        PasswordSection.IsVisible = false;
        ResetPasswordSection.IsVisible = true;
        SaveUserButton.Text = "Actualizează";
    }
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not User user) return;
        if (user.Id == _session.CurrentUser?.Id)
        {
            await DisplayAlertAsync("Atenție", "Nu te poți șterge pe tine însuți.", "OK");
            return;
        }
        var ok = await DisplayAlertAsync("Ștergere",
            $"Confirmi ștergerea utilizatorului {user.Email}?",
            "Da, șterge", "Anulează");
        if (!ok) return;
        try
        {
            await _service.DeleteAsync(user.Id);
            _items.Remove(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnSaveUserClicked(object? sender, EventArgs e)
    {
        var name = EditorNameEntry.Text?.Trim() ?? string.Empty;
        var email = EditorEmailEntry.Text?.Trim() ?? string.Empty;
        var role = EditorRolePicker.SelectedIndex == 1 ? UserRole.Admin : UserRole.User;
        var password = EditorPasswordEntry.Text ?? string.Empty;
        try
        {
            if (_editingUser is null)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync("Parolă necesară", "Introdu o parolă pentru utilizatorul nou.", "OK");
                    return;
                }
                await _service.CreateAsync(name, email, password, role);
            }
            else
            {
                _editingUser.Name = name;
                _editingUser.Email = email;
                _editingUser.Role = role;
                await _service.UpdateAsync(_editingUser);
            }
            ResetEditor();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private async void OnResetPasswordClicked(object? sender, EventArgs e)
    {
        if (_editingUser is null) return;
        var newPass = await DisplayPromptAsync("Resetare parolă", "Introdu o parolă nouă:");
        if (string.IsNullOrWhiteSpace(newPass)) return;
        try
        {
            await _service.ChangePasswordAsync(_editingUser.Id, newPass);
            await DisplayAlertAsync("Parolă actualizată", "Parola utilizatorului a fost schimbată.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }
    private void OnClearEditorClicked(object? sender, EventArgs e)
    {
        ResetEditor();
    }
    private void ResetEditor()
    {
        _editingUser = null;
        EditorTitleLabel.Text = "Utilizator nou";
        EditorSubtitleLabel.Text = "Completează datele și salvează direct în baza de date.";
        EditorNameEntry.Text = string.Empty;
        EditorEmailEntry.Text = string.Empty;
        EditorPasswordEntry.Text = string.Empty;
        EditorRolePicker.SelectedIndex = 0;
        PasswordSection.IsVisible = true;
        ResetPasswordSection.IsVisible = false;
        SaveUserButton.Text = "Salvează";
    }
    private async void OnUsersClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(UsersPage));
    private async void OnLocationsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(LocationsPage));
    private async void OnAlertsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AlertsPage));
    private async void OnReportsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ReportsPage));
    private async void OnFavoritesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(FavoritesPage));
    private async void OnLogsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(LogsPage));
    private async void OnSettingsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(SettingsPage));
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("Deconectare", "Ești sigur că vrei să te deconectezi?", "Da", "Nu");
        if (!ok) return;
        var auth = ResolveService<AuthService>();
        auth.Logout();
        await Shell.Current.GoToAsync($"//LoginPage");
    }
}
