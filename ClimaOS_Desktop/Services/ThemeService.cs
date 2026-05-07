namespace ClimaOS_Desktop.Services;

public class ThemeService
{
    private const string PrefKey = "app.theme";
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
    public event EventHandler? ThemeChanged;

    public void ApplyStoredTheme()
    {
        var stored = Preferences.Default.Get(PrefKey, "dark");
        CurrentTheme = stored == "light" ? AppTheme.Light : AppTheme.Dark;
        ApplyToApp();
    }

    public void Toggle()
    {
        CurrentTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        Preferences.Default.Set(PrefKey, CurrentTheme == AppTheme.Light ? "light" : "dark");
        ApplyToApp();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Set(AppTheme theme)
    {
        CurrentTheme = theme;
        Preferences.Default.Set(PrefKey, theme == AppTheme.Light ? "light" : "dark");
        ApplyToApp();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyToApp()
    {
        var app = Application.Current;
        if (app is null)
            return;
        app.UserAppTheme = CurrentTheme;
    }
}
