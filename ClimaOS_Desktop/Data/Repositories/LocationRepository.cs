using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data.Repositories;

public class LocationRepository
{
    private readonly MySqlConnectionFactory _factory;

    public LocationRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Location>> SearchAsync(string? query, int? userId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT id, user_id, name, country, latitude, longitude, created_at
                        FROM locations WHERE 1=1";
            var cmd = new MySqlCommand();
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (name LIKE @q OR country LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            if (userId.HasValue)
            {
                sql += " AND user_id = @uid";
                cmd.Parameters.AddWithValue("@uid", userId.Value);
            }
            sql += " ORDER BY created_at DESC LIMIT 500";

            cmd.Connection = conn;
            cmd.CommandText = sql;

            var list = new List<Location>();
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

    public async Task<Location?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"SELECT id, user_id, name, country, latitude, longitude, created_at
                  FROM locations WHERE id = @id LIMIT 1",
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

    public async Task<int> InsertAsync(Location loc, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"INSERT INTO locations(user_id, name, country, latitude, longitude, created_at)
                  VALUES(@uid, @name, @country, @lat, @lon, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@uid", (object?)loc.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", loc.Name);
            cmd.Parameters.AddWithValue("@country", loc.Country ?? string.Empty);
            cmd.Parameters.AddWithValue("@lat", loc.Latitude);
            cmd.Parameters.AddWithValue("@lon", loc.Longitude);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            loc.Id = id;
            return id;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    public async Task UpdateAsync(Location loc, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"UPDATE locations
                  SET user_id = @uid, name = @name, country = @country,
                      latitude = @lat, longitude = @lon
                  WHERE id = @id",
                conn);
            cmd.Parameters.AddWithValue("@uid", (object?)loc.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", loc.Name);
            cmd.Parameters.AddWithValue("@country", loc.Country ?? string.Empty);
            cmd.Parameters.AddWithValue("@lat", loc.Latitude);
            cmd.Parameters.AddWithValue("@lon", loc.Longitude);
            cmd.Parameters.AddWithValue("@id", loc.Id);
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
            await using var cmd = new MySqlCommand("DELETE FROM locations WHERE id = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM locations", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static Location Map(MySqlDataReader r) => new Location
    {
        Id = r.GetInt32("id"),
        UserId = r.IsDBNull(r.GetOrdinal("user_id")) ? null : r.GetInt32("user_id"),
        Name = r.GetString("name"),
        Country = r.GetString("country"),
        Latitude = r.GetDouble("latitude"),
        Longitude = r.GetDouble("longitude"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
