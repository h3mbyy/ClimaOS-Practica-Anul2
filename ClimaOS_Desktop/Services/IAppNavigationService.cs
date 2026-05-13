namespace ClimaOS_Desktop.Services;

public interface IAppNavigationService
{
    Task NavigateToAsync(string route);
    Task ShowMessageAsync(string title, string message, string cancel = "OK");
}
