using System.Collections.ObjectModel;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaOS_Desktop.Pages.Admin;

public partial class UsersPage : ContentPage
{
    private readonly UserService _service;
    private readonly SessionStore _session;
    private readonly ObservableCollection<User> _items = new();

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

    private async void OnSearchClicked(object sender, EventArgs e) => await LoadAsync();

    private async void OnResetClicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        RolePicker.SelectedIndex = 0;
        await LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Adaugă utilizator", "Nume:");
        if (string.IsNullOrWhiteSpace(name)) return;
        var email = await DisplayPromptAsync("Adaugă utilizator", "Email:");
        if (string.IsNullOrWhiteSpace(email)) return;
        var pass = await DisplayPromptAsync("Adaugă utilizator", "Parolă (min. 6):");
        if (string.IsNullOrWhiteSpace(pass)) return;
        var roleAns = await DisplayActionSheet("Rol utilizator", "Anulează", null, "Utilizator", "Administrator");
        var role = roleAns == "Administrator" ? UserRole.Admin : UserRole.User;

        try
        {
            await _service.CreateAsync(name, email, pass, role);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not User user) return;

        var name = await DisplayPromptAsync("Editare", "Nume:", initialValue: user.Name);
        if (name is null) return;
        var email = await DisplayPromptAsync("Editare", "Email:", initialValue: user.Email);
        if (email is null) return;
        var roleAns = await DisplayActionSheet("Rol", "Anulează", null, "Utilizator", "Administrator");
        if (roleAns is null || roleAns == "Anulează") return;
        var role = roleAns == "Administrator" ? UserRole.Admin : UserRole.User;

        try
        {
            user.Name = name.Trim();
            user.Email = email.Trim();
            user.Role = role;
            await _service.UpdateAsync(user);

            var changePass = await DisplayAlert("Parolă", "Vrei să resetezi parola?", "Da", "Nu");
            if (changePass)
            {
                var newPass = await DisplayPromptAsync("Parolă nouă", "Introdu noua parolă:");
                if (!string.IsNullOrWhiteSpace(newPass))
                    await _service.ChangePasswordAsync(user.Id, newPass);
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not User user) return;
        if (user.Id == _session.CurrentUser?.Id)
        {
            await DisplayAlert("Atenție", "Nu te poți șterge pe tine însuți.", "OK");
            return;
        }
        var ok = await DisplayAlert("Ștergere",
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
}
