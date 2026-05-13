using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
namespace ClimaOS_Desktop.Views;
public partial class ResetPasswordPage : ContentPage, IQueryAttributable
{
    private readonly AuthService _auth;
    private string _currentEmail = string.Empty;
    private string _currentCode = string.Empty;
    private bool _isBusy;
    public ResetPasswordPage()
        : this(ResolveService<AuthService>())
    {
    }
    public ResetPasswordPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("email", out var emailValue))
            return;
        var email = Uri.UnescapeDataString(emailValue?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return;
        EmailEntry.Text = email;
        _currentEmail = email;
    }
    private static T ResolveService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            throw new InvalidOperationException($"Nu pot rezolva {typeof(T).Name}.");
        return services.GetRequiredService<T>();
    }
    private async void OnRequestCodeClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;
        var email = EmailEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            await ErrorHandler.ShowAsync(this, new ValidationException("Introduceți adresa de email."));
            return;
        }
        try
        {
            SetBusyState(true, RequestCodeButton);
            await _auth.RequestPasswordResetAsync(email);
            _currentEmail = email;
            _currentCode = string.Empty;
            Step1View.IsVisible = false;
            Step2View.IsVisible = true;
            await DisplayAlertAsync("Cod trimis", "Dacă există un cont pentru acest email, ți-am trimis un cod de resetare.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
        finally
        {
            SetBusyState(false, RequestCodeButton);
        }
    }
    private async void OnVerifyCodeClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;
        var code = CodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            await ErrorHandler.ShowAsync(this, new ValidationException("Introduceți codul primit."));
            return;
        }
        try
        {
            SetBusyState(true, VerifyCodeButton, ResendCodeButton);
            await _auth.VerifyResetCodeAsync(_currentEmail, code);
            _currentCode = code;
            Step2View.IsVisible = false;
            Step3View.IsVisible = true;
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
        finally
        {
            SetBusyState(false, VerifyCodeButton, ResendCodeButton);
        }
    }
    private async void OnResetPasswordClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;
        var newPassword = NewPasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;
        if (newPassword != confirmPassword)
        {
            await ErrorHandler.ShowAsync(this, new ValidationException("Parolele nu coincid."));
            return;
        }
        if (string.IsNullOrWhiteSpace(_currentEmail) || string.IsNullOrWhiteSpace(_currentCode))
        {
            await ErrorHandler.ShowAsync(this, new ValidationException("Reia fluxul de resetare și verifică din nou codul."));
            return;
        }
        try
        {
            SetBusyState(true, ResetPasswordButton);
            await _auth.ResetPasswordWithCodeAsync(_currentEmail, _currentCode, newPassword ?? "");
            await DisplayAlertAsync("Succes", "Parola a fost schimbată cu succes! Te poți autentifica acum.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
        finally
        {
            SetBusyState(false, ResetPasswordButton);
        }
    }
    private async void OnResendCodeClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;
        if (string.IsNullOrWhiteSpace(_currentEmail))
        {
            await ErrorHandler.ShowAsync(this, new ValidationException("Introdu mai întâi emailul pentru care vrei resetarea."));
            Step2View.IsVisible = false;
            Step1View.IsVisible = true;
            return;
        }
        try
        {
            SetBusyState(true, VerifyCodeButton, ResendCodeButton);
            await _auth.RequestPasswordResetAsync(_currentEmail);
            CodeEntry.Text = string.Empty;
            _currentCode = string.Empty;
            await DisplayAlertAsync("Cod retrimis", "Dacă există un cont pentru acest email, am trimis un nou cod de resetare.", "OK");
        }
        catch (Exception ex)
        {
            await ErrorHandler.ShowAsync(this, ex);
        }
        finally
        {
            SetBusyState(false, VerifyCodeButton, ResendCodeButton);
        }
    }
    private async void OnBackToLoginTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private void SetBusyState(bool isBusy, params Button[] buttons)
    {
        _isBusy = isBusy;
        foreach (var button in buttons)
            button.IsEnabled = !isBusy;
    }
}
