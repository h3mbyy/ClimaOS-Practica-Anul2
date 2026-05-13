using System.Security.Cryptography;
using System.Text;
using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Services;

public class AuthService
{
    private const string ResetTokenHashPrefix = "PBKDF2|";
    private const string LegacyResetTokenPrefix = "LEGACY|";

    private readonly UserRepository _users;
    private readonly SessionStore _session;
    private readonly EmailService _email;
    private readonly Data.MySqlConnectionFactory _db;
    private readonly PasswordResetSettings _passwordResetSettings;

    public AuthService(
        UserRepository users,
        SessionStore session,
        EmailService email,
        Data.MySqlConnectionFactory db,
        PasswordResetSettings passwordResetSettings)
    {
        _users = users;
        _session = session;
        _email = email;
        _db = db;
        _passwordResetSettings = passwordResetSettings;
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
            throw new ValidationException("Există deja un cont cu acest email.");

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

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidateEmail(email);
        ValidationService.EnsureValid(errors);

        var normalizedEmail = email.Trim();
        var user = await _users.GetByEmailAsync(normalizedEmail, ct);

        await using var conn = await _db.OpenAsync(ct);
        await CleanupExpiredResetTokensAsync(conn, ct);
        await EnsureResetRequestCooldownAsync(conn, normalizedEmail, ct);

        if (user is null)
        {
            await Task.Delay(250, ct);
            return;
        }

        var code = GenerateResetCode(_passwordResetSettings.MaxCodeLength);
        var codeHash = PasswordHasher.Hash(code);
        var expiration = DateTime.UtcNow.AddMinutes(_passwordResetSettings.CodeLifetimeMinutes);

        await InvalidateActiveResetTokensAsync(conn, user.Email, ct);
        var tokenId = await InsertResetTokenAsync(conn, user.Email, codeHash, expiration, ct);

        try
        {
            await _email.SendResetCodeAsync(user.Email, code, _passwordResetSettings.CodeLifetimeMinutes, ct);
        }
        catch
        {
            await MarkResetTokenUsedAsync(conn, tokenId, ct);
            throw;
        }
    }

    public async Task VerifyResetCodeAsync(string email, string code, CancellationToken ct = default)
    {
        ValidateResetCode(code);

        await using var conn = await _db.OpenAsync(ct);
        await CleanupExpiredResetTokensAsync(conn, ct);

        var token = await GetActiveResetTokenAsync(conn, email.Trim(), ct);
        if (token is null || !IsResetCodeMatch(code.Trim(), token.CodeHash))
            throw new ValidationException("Codul introdus este incorect sau a expirat.");
    }

    public async Task ResetPasswordWithCodeAsync(string email, string code, string newPassword, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidatePassword(newPassword);
        ValidationService.EnsureValid(errors);
        ValidateResetCode(code);

        var normalizedEmail = email.Trim();

        await using var conn = await _db.OpenAsync(ct);
        await CleanupExpiredResetTokensAsync(conn, ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var token = await GetActiveResetTokenAsync(conn, normalizedEmail, ct, (MySqlTransaction)tx);
        if (token is null || !IsResetCodeMatch(code.Trim(), token.CodeHash))
            throw new ValidationException("Codul introdus este incorect sau a expirat.");

        var newHash = PasswordHasher.Hash(newPassword);

        await using var updateUser = new MySqlCommand(
            "UPDATE Users SET PasswordHash = @hash WHERE Email = @email",
            conn,
            (MySqlTransaction)tx);
        updateUser.Parameters.AddWithValue("@hash", newHash);
        updateUser.Parameters.AddWithValue("@email", normalizedEmail);

        var updatedUsers = await updateUser.ExecuteNonQueryAsync(ct);
        if (updatedUsers == 0)
            throw new ValidationException("Nu s-a găsit utilizatorul.");

        await MarkResetTokenUsedAsync(conn, token.TokenId, ct, (MySqlTransaction)tx);
        await InvalidateActiveResetTokensAsync(conn, normalizedEmail, ct, (MySqlTransaction)tx, token.TokenId);

        await tx.CommitAsync(ct);
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

    private async Task CleanupExpiredResetTokensAsync(MySqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(
            "DELETE FROM PasswordResetTokens WHERE Expiration <= UTC_TIMESTAMP()",
            conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task EnsureResetRequestCooldownAsync(MySqlConnection conn, string email, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(
            "SELECT MAX(CreatedAt) FROM PasswordResetTokens WHERE Email = @email",
            conn);
        cmd.Parameters.AddWithValue("@email", email);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result == DBNull.Value)
            return;

        var lastRequestAt = Convert.ToDateTime(result);
        var secondsSinceLastRequest = (DateTime.UtcNow - DateTime.SpecifyKind(lastRequestAt, DateTimeKind.Utc)).TotalSeconds;
        if (secondsSinceLastRequest < _passwordResetSettings.RequestCooldownSeconds)
        {
            var waitSeconds = Math.Max(1, _passwordResetSettings.RequestCooldownSeconds - (int)secondsSinceLastRequest);
            throw new ValidationException($"A fost trimis deja un cod recent. Încearcă din nou peste aproximativ {waitSeconds} secunde.");
        }
    }

    private async Task InvalidateActiveResetTokensAsync(
        MySqlConnection conn,
        string email,
        CancellationToken ct,
        MySqlTransaction? tx = null,
        int? exceptTokenId = null)
    {
        var sql = "UPDATE PasswordResetTokens SET IsUsed = 1 WHERE Email = @email AND IsUsed = 0";
        if (exceptTokenId.HasValue)
            sql += " AND TokenId <> @exceptTokenId";

        await using var cmd = new MySqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@email", email);
        if (exceptTokenId.HasValue)
            cmd.Parameters.AddWithValue("@exceptTokenId", exceptTokenId.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<int> InsertResetTokenAsync(
        MySqlConnection conn,
        string email,
        string codeHash,
        DateTime expiration,
        CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(
            """
            INSERT INTO PasswordResetTokens (Email, CodeHash, Expiration, IsUsed, CreatedAt)
            VALUES (@email, @codeHash, @expiration, 0, UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """,
            conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@codeHash", codeHash);
        cmd.Parameters.AddWithValue("@expiration", expiration);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private async Task<PasswordResetTokenRecord?> GetActiveResetTokenAsync(
        MySqlConnection conn,
        string email,
        CancellationToken ct,
        MySqlTransaction? tx = null)
    {
        await using var cmd = new MySqlCommand(
            """
            SELECT TokenId, CodeHash
            FROM PasswordResetTokens
            WHERE Email = @email AND IsUsed = 0 AND Expiration > UTC_TIMESTAMP()
            ORDER BY CreatedAt DESC
            LIMIT 1
            """,
            conn,
            tx);
        cmd.Parameters.AddWithValue("@email", email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var tokenIdOrdinal = reader.GetOrdinal("TokenId");
        var codeHashOrdinal = reader.GetOrdinal("CodeHash");

        return new PasswordResetTokenRecord(
            reader.GetInt32(tokenIdOrdinal),
            reader.GetString(codeHashOrdinal));
    }

    private async Task MarkResetTokenUsedAsync(
        MySqlConnection conn,
        int tokenId,
        CancellationToken ct,
        MySqlTransaction? tx = null)
    {
        await using var cmd = new MySqlCommand(
            "UPDATE PasswordResetTokens SET IsUsed = 1 WHERE TokenId = @tokenId",
            conn,
            tx);
        cmd.Parameters.AddWithValue("@tokenId", tokenId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private void ValidateResetCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("Introduceți codul primit.");

        var trimmed = code.Trim();
        if (trimmed.Length != _passwordResetSettings.MaxCodeLength || trimmed.Any(c => !char.IsDigit(c)))
            throw new ValidationException($"Codul trebuie să conțină exact {_passwordResetSettings.MaxCodeLength} cifre.");
    }

    private static string GenerateResetCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));

        return new string(chars);
    }

    private static bool IsResetCodeMatch(string code, string storedValue)
    {
        if (storedValue.StartsWith(ResetTokenHashPrefix, StringComparison.Ordinal))
            return PasswordHasher.Verify(code, storedValue);

        if (storedValue.StartsWith(LegacyResetTokenPrefix, StringComparison.Ordinal))
            return ConstantTimeEquals(code, storedValue[LegacyResetTokenPrefix.Length..]);

        return ConstantTimeEquals(code, storedValue);
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record PasswordResetTokenRecord(int TokenId, string CodeHash);
}
