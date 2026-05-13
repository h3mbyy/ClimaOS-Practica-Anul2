using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;
namespace ClimaOS_Desktop.Services;
public class UserService
{
    private readonly UserRepository _repo;
    public UserService(UserRepository repo)
    {
        _repo = repo;
    }
    public Task<List<User>> SearchAsync(string? query, UserRole? role, CancellationToken ct = default)
        => _repo.SearchAsync(query, role, ct);
    public Task<User?> GetAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);
    public async Task<User> CreateAsync(string name, string email, string password, UserRole role, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(name, "Numele"));
        errors.AddRange(ValidationService.ValidateEmail(email));
        errors.AddRange(ValidationService.ValidatePassword(password));
        ValidationService.EnsureValid(errors);
        var user = new User
        {
            Name = name.Trim(),
            Email = email.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.InsertAsync(user, ct);
        return user;
    }
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(user.Name, "Numele"));
        errors.AddRange(ValidationService.ValidateEmail(user.Email));
        ValidationService.EnsureValid(errors);
        await _repo.UpdateAsync(user, ct);
    }
    public async Task ChangePasswordAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidatePassword(newPassword);
        ValidationService.EnsureValid(errors);
        await _repo.UpdatePasswordAsync(userId, PasswordHasher.Hash(newPassword), ct);
    }
    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
