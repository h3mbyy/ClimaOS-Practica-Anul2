namespace ClimaOS_Desktop.Services;

public class ShellNavigationService : IAppNavigationService
{
    public Task NavigateToAsync(string route)
    {
        if (Shell.Current is null)
            return Task.CompletedTask;

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
    }

    public Task ShowMessageAsync(string title, string message, string cancel = "OK")
    {
        if (Shell.Current is null)
            return Task.CompletedTask;

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, cancel));
    }
}
