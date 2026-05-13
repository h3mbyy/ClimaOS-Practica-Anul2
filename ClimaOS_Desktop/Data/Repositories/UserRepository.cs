using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using System.Data.Common;
using MySql.Data.MySqlClient;
namespace ClimaOS_Desktop.Data.Repositories;
public class UserRepository
{
    private readonly MySqlConnectionFactory _factory;
    public UserRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }
    public async Task<List<User>> SearchAsync(string? query, UserRole? role, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT UserId, FullName, Email, PasswordHash, Role, CreatedAt FROM Users WHERE 1=1";
            var cmd = new MySqlCommand();
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (FullName LIKE @q OR Email LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            if (role.HasValue)
            {
                sql += " AND role = @role";
                cmd.Parameters.AddWithValue("@role", role.Value.ToDbString());
            }
            sql += " ORDER BY created_at DESC LIMIT 500";
            cmd.Connection = conn;
            cmd.CommandText = sql;
            var list = new List<User>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(Map(reader));
            }
            return list;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                "SELECT UserId, FullName, Email, PasswordHash, Role, CreatedAt FROM Users WHERE Email = @email LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("@email", email);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                "SELECT UserId, FullName, Email, PasswordHash, Role, CreatedAt FROM Users WHERE UserId = @id LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<int> InsertAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                                @"INSERT INTO Users(FullName, Email, PasswordHash, Role, CreatedAt)
                  VALUES(@name, @email, @hash, @role, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@role", user.Role.ToDbString());
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            user.Id = id;
            return id;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ValidationException("Există deja un utilizator cu acest email.");
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                                @"UPDATE Users SET FullName = @name, Email = @email, Role = @role
                                    WHERE UserId = @id",
                conn);
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@role", user.Role.ToDbString());
            cmd.Parameters.AddWithValue("@id", user.Id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ValidationException("Există deja un utilizator cu acest email.");
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task UpdatePasswordAsync(int userId, string newHash, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                "UPDATE Users SET PasswordHash = @hash WHERE UserId = @id",
                conn);
            cmd.Parameters.AddWithValue("@hash", newHash);
            cmd.Parameters.AddWithValue("@id", userId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand("DELETE FROM Users WHERE UserId = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Users", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    private static User Map(DbDataReader r)
    {
        var id = r.GetInt32(r.GetOrdinal("UserId"));
        var name = r.GetString(r.GetOrdinal("FullName"));
        var email = r.GetString(r.GetOrdinal("Email"));
        var passwordHash = r.GetString(r.GetOrdinal("PasswordHash"));
        var role = r.GetString(r.GetOrdinal("Role"));
        var createdAt = r.GetDateTime(r.GetOrdinal("CreatedAt"));
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRoleExtensions.FromDbString(role),
            CreatedAt = createdAt
        };
    }
}
