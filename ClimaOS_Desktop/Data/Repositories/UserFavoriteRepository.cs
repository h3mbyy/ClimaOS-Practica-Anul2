using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using System.Data.Common;
using MySql.Data.MySqlClient;
namespace ClimaOS_Desktop.Data.Repositories;
public class UserFavoriteRepository
{
    private readonly MySqlConnectionFactory _factory;
    public UserFavoriteRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }
    public async Task<List<UserFavorite>> SearchAsync(string? query, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT f.FavoriteId, f.UserId, f.LocationId, f.AddedAt,
                               u.FullName, u.Email,
                               l.CityName, l.CountryCode
                        FROM UserFavorites f
                        JOIN Users u ON u.UserId = f.UserId
                        JOIN Locations l ON l.LocationId = f.LocationId
                        WHERE 1=1";
            var cmd = new MySqlCommand();
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (u.FullName LIKE @q OR u.Email LIKE @q OR l.CityName LIKE @q OR l.CountryCode LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            sql += " ORDER BY f.AddedAt DESC LIMIT 500";
            cmd.Connection = conn;
            cmd.CommandText = sql;
            var list = new List<UserFavorite>();
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
    public async Task<List<UserFavorite>> SearchForUserAsync(int userId, string? query, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT f.FavoriteId, f.UserId, f.LocationId, f.AddedAt,
                               u.FullName, u.Email,
                               l.CityName, l.CountryCode
                        FROM UserFavorites f
                        JOIN Users u ON u.UserId = f.UserId
                        JOIN Locations l ON l.LocationId = f.LocationId
                        WHERE f.UserId = @uid";
            var cmd = new MySqlCommand();
            cmd.Parameters.AddWithValue("@uid", userId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (l.CityName LIKE @q OR l.CountryCode LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            sql += " ORDER BY f.AddedAt DESC LIMIT 500";
            cmd.Connection = conn;
            cmd.CommandText = sql;
            var list = new List<UserFavorite>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));
            return list;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<UserFavorite?> GetForUserLocationAsync(int userId, int locationId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"SELECT f.FavoriteId, f.UserId, f.LocationId, f.AddedAt,
                         u.FullName, u.Email,
                         l.CityName, l.CountryCode
                  FROM UserFavorites f
                  JOIN Users u ON u.UserId = f.UserId
                  JOIN Locations l ON l.LocationId = f.LocationId
                  WHERE f.UserId = @uid AND f.LocationId = @lid LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@lid", locationId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task<int> InsertAsync(int userId, int locationId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"INSERT INTO UserFavorites(UserId, LocationId, AddedAt)
                  VALUES(@uid, @lid, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@lid", locationId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ValidationException("Favoritul exista deja pentru acest utilizator.");
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
            await using var cmd = new MySqlCommand("DELETE FROM UserFavorites WHERE FavoriteId = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    public async Task DeleteForUserLocationAsync(int userId, int locationId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                "DELETE FROM UserFavorites WHERE UserId = @uid AND LocationId = @lid",
                conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@lid", locationId);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM UserFavorites", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }
    private static UserFavorite Map(DbDataReader r)
    {
        return new UserFavorite
        {
            Id = r.GetInt32(r.GetOrdinal("FavoriteId")),
            UserId = r.GetInt32(r.GetOrdinal("UserId")),
            LocationId = r.GetInt32(r.GetOrdinal("LocationId")),
            AddedAt = r.GetDateTime(r.GetOrdinal("AddedAt")),
            UserName = r.GetString(r.GetOrdinal("FullName")),
            UserEmail = r.GetString(r.GetOrdinal("Email")),
            LocationName = r.GetString(r.GetOrdinal("CityName")),
            CountryCode = r.GetString(r.GetOrdinal("CountryCode"))
        };
    }
}
