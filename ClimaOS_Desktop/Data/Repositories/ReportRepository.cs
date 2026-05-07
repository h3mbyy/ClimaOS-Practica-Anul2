using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data.Repositories;

public class ReportRepository
{
    private readonly MySqlConnectionFactory _factory;

    public ReportRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Report>> SearchAsync(string? query, ReportType? type, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT id, title, type, notes, created_by_user_id, created_at
                        FROM reports WHERE 1=1";
            var cmd = new MySqlCommand();
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (title LIKE @q OR notes LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            if (type.HasValue)
            {
                sql += " AND type = @type";
                cmd.Parameters.AddWithValue("@type", (int)type.Value);
            }
            sql += " ORDER BY created_at DESC LIMIT 500";

            cmd.Connection = conn;
            cmd.CommandText = sql;

            var list = new List<Report>();
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

    public async Task<int> InsertAsync(Report report, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"INSERT INTO reports(title, type, notes, created_by_user_id, created_at)
                  VALUES(@title, @type, @notes, @uid, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@title", report.Title);
            cmd.Parameters.AddWithValue("@type", (int)report.Type);
            cmd.Parameters.AddWithValue("@notes", report.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@uid", (object?)report.CreatedByUserId ?? DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            report.Id = id;
            return id;
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
            await using var cmd = new MySqlCommand("DELETE FROM reports WHERE id = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM reports", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static Report Map(MySqlDataReader r) => new Report
    {
        Id = r.GetInt32("id"),
        Title = r.GetString("title"),
        Type = (ReportType)r.GetInt32("type"),
        Notes = r.GetString("notes"),
        CreatedByUserId = r.IsDBNull(r.GetOrdinal("created_by_user_id"))
            ? null : r.GetInt32("created_by_user_id"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
