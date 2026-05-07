using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data.Repositories;

public class AlertRepository
{
    private readonly MySqlConnectionFactory _factory;

    public AlertRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<WeatherAlert>> SearchAsync(
        string? query,
        AlertSeverity? minSeverity,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT id, location_id, location_name, title, message, severity,
                              starts_at, ends_at, created_at
                        FROM weather_alerts WHERE 1=1";
            var cmd = new MySqlCommand();

            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (title LIKE @q OR message LIKE @q OR location_name LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            if (minSeverity.HasValue)
            {
                sql += " AND severity >= @sev";
                cmd.Parameters.AddWithValue("@sev", (int)minSeverity.Value);
            }
            if (from.HasValue)
            {
                sql += " AND ends_at >= @from";
                cmd.Parameters.AddWithValue("@from", from.Value);
            }
            if (to.HasValue)
            {
                sql += " AND starts_at <= @to";
                cmd.Parameters.AddWithValue("@to", to.Value);
            }

            sql += " ORDER BY starts_at DESC LIMIT 500";

            cmd.Connection = conn;
            cmd.CommandText = sql;

            var list = new List<WeatherAlert>();
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

    public async Task<WeatherAlert?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"SELECT id, location_id, location_name, title, message, severity,
                         starts_at, ends_at, created_at
                  FROM weather_alerts WHERE id = @id LIMIT 1",
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

    public async Task<int> InsertAsync(WeatherAlert alert, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"INSERT INTO weather_alerts
                  (location_id, location_name, title, message, severity, starts_at, ends_at, created_at)
                  VALUES (@lid, @lname, @title, @msg, @sev, @start, @end, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@lid", (object?)alert.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lname", alert.LocationName ?? string.Empty);
            cmd.Parameters.AddWithValue("@title", alert.Title);
            cmd.Parameters.AddWithValue("@msg", alert.Message);
            cmd.Parameters.AddWithValue("@sev", (int)alert.Severity);
            cmd.Parameters.AddWithValue("@start", alert.StartsAt);
            cmd.Parameters.AddWithValue("@end", alert.EndsAt);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            alert.Id = id;
            return id;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    public async Task UpdateAsync(WeatherAlert alert, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"UPDATE weather_alerts
                  SET location_id = @lid, location_name = @lname, title = @title,
                      message = @msg, severity = @sev,
                      starts_at = @start, ends_at = @end
                  WHERE id = @id",
                conn);
            cmd.Parameters.AddWithValue("@lid", (object?)alert.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lname", alert.LocationName ?? string.Empty);
            cmd.Parameters.AddWithValue("@title", alert.Title);
            cmd.Parameters.AddWithValue("@msg", alert.Message);
            cmd.Parameters.AddWithValue("@sev", (int)alert.Severity);
            cmd.Parameters.AddWithValue("@start", alert.StartsAt);
            cmd.Parameters.AddWithValue("@end", alert.EndsAt);
            cmd.Parameters.AddWithValue("@id", alert.Id);
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
            await using var cmd = new MySqlCommand("DELETE FROM weather_alerts WHERE id = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM weather_alerts", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static WeatherAlert Map(MySqlDataReader r) => new WeatherAlert
    {
        Id = r.GetInt32("id"),
        LocationId = r.IsDBNull(r.GetOrdinal("location_id")) ? null : r.GetInt32("location_id"),
        LocationName = r.GetString("location_name"),
        Title = r.GetString("title"),
        Message = r.GetString("message"),
        Severity = (AlertSeverity)r.GetInt32("severity"),
        StartsAt = r.GetDateTime("starts_at"),
        EndsAt = r.GetDateTime("ends_at"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
