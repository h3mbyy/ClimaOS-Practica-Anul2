using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class AuthService
{
    private readonly UserRepository _users;
    private readonly SessionStore _session;

    public AuthService(UserRepository users, SessionStore session)
    {
        _users = users;
        _session = session;
    }

    public async Task<User> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(name, "Numele"));
        errors.AddRange(ValidationService.ValidateEmail(email));
        errors.AddRange(ValidationService.ValidatePassword(password));
        ValidationService.EnsureValid(errors);

        var existing = await _users.GetByEmailAsync(email.Trim(), ct);
        if (existing is not null)
        {
            throw new ValidationException("Există deja un cont cu acest email.");
        }

        var user = new User
        {
            Name = name.Trim(),
            Email = email.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        await _users.InsertAsync(user, ct);
        _session.SignIn(user);
        return user;
    }

    public async Task<User> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateEmail(email));
        errors.AddRange(ValidationService.ValidatePassword(password));
        ValidationService.EnsureValid(errors);

        var user = await _users.GetByEmailAsync(email.Trim(), ct)
                   ?? throw new AuthException("Email sau parolă incorectă.");

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            throw new AuthException("Email sau parolă incorectă.");

        _session.SignIn(user);
        return user;
    }

    public void Logout() => _session.SignOut();

    public async Task ChangePasswordAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidatePassword(newPassword);
        ValidationService.EnsureValid(errors);
        var newHash = PasswordHasher.Hash(newPassword);
        await _users.UpdatePasswordAsync(userId, newHash, ct);
    }
}
