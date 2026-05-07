namespace ClimaOS_Desktop.Models;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public static class UserRoleExtensions
{
    public static string ToDbString(this UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        _ => "user"
    };

    public static UserRole FromDbString(string? value) => value?.ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        _ => UserRole.User
    };
}
