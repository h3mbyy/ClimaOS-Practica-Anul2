using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
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
            var sql = @"SELECT id, name, email, password_hash, role, created_at FROM users WHERE 1=1";
            var cmd = new MySqlCommand();

            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (name LIKE @q OR email LIKE @q)";
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
                "SELECT id, name, email, password_hash, role, created_at FROM users WHERE email = @email LIMIT 1",
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
                "SELECT id, name, email, password_hash, role, created_at FROM users WHERE id = @id LIMIT 1",
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
                @"INSERT INTO users(name, email, password_hash, role, created_at)
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
                @"UPDATE users SET name = @name, email = @email, role = @role
                  WHERE id = @id",
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
                "UPDATE users SET password_hash = @hash WHERE id = @id",
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
            await using var cmd = new MySqlCommand("DELETE FROM users WHERE id = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static User Map(MySqlDataReader r) => new User
    {
        Id = r.GetInt32("id"),
        Name = r.GetString("name"),
        Email = r.GetString("email"),
        PasswordHash = r.GetString("password_hash"),
        Role = UserRoleExtensions.FromDbString(r.GetString("role")),
        CreatedAt = r.GetDateTime("created_at")
    };
}
