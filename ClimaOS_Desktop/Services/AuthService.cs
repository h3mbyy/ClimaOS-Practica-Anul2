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

    public async Task<User> RegisterAsync(string name, string email, string password, bool remember = true, CancellationToken ct = default)
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
        _session.SignIn(user, remember);
        return user;
    }

    public async Task<User> LoginAsync(string email, string password, bool remember = false, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateEmail(email));
        errors.AddRange(ValidationService.ValidatePassword(password));
        ValidationService.EnsureValid(errors);

        var user = await _users.GetByEmailAsync(email.Trim(), ct)
                   ?? throw new AuthException("Email sau parolă incorectă.");

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            throw new AuthException("Email sau parolă incorectă.");

        _session.SignIn(user, remember);
        return user;
    }

    public void Logout() => _session.SignOut();

    public async Task<User?> RestoreRememberedSessionAsync(CancellationToken ct = default)
    {
        if (_session.IsAuthenticated)
            return _session.CurrentUser;

        var rememberedId = _session.RememberedUserId;
        if (rememberedId is null)
            return null;

        var user = await _users.GetByIdAsync(rememberedId.Value, ct);
        if (user is null)
        {
            _session.SignOut();
            return null;
        }

        _session.SignIn(user, remember: true);
        return user;
    }

    public async Task<string> ResetPasswordAsync(string email, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidateEmail(email);
        ValidationService.EnsureValid(errors);

        var user = await _users.GetByEmailAsync(email.Trim(), ct)
                   ?? throw new ValidationException("Nu există un cont cu acest email.");

        var temporaryPassword = $"Clima{Random.Shared.Next(1000, 9999)}!";
        await ChangePasswordAsync(user.Id, temporaryPassword, ct);
        return temporaryPassword;
    }

    public async Task ChangePasswordAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidatePassword(newPassword);
        ValidationService.EnsureValid(errors);
        var newHash = PasswordHasher.Hash(newPassword);
        await _users.UpdatePasswordAsync(userId, newHash, ct);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new AuthException("Utilizatorul curent nu mai există.");

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
            throw new AuthException("Parola curentă este incorectă.");

        await ChangePasswordAsync(userId, newPassword, ct);
    }
}
