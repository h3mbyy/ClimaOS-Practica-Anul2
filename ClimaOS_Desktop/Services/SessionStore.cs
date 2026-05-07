using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class SessionStore
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    public event EventHandler? SessionChanged;

    public void SignIn(User user)
    {
        CurrentUser = user;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
