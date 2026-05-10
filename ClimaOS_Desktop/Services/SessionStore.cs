using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class SessionStore
{
    private const string RememberUserIdKey = "climaos.remember.user_id";

    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    public event EventHandler? SessionChanged;

    public int? RememberedUserId
    {
        get
        {
            var id = Preferences.Default.Get(RememberUserIdKey, 0);
            return id > 0 ? id : null;
        }
    }

    public void SignIn(User user, bool remember = false)
    {
        CurrentUser = user;
        if (remember)
            Preferences.Default.Set(RememberUserIdKey, user.Id);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        Preferences.Default.Remove(RememberUserIdKey);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
