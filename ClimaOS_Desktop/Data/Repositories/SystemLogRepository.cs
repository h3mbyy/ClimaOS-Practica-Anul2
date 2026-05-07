using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data.Repositories;

public class SystemLogRepository
{
    private readonly MySqlConnectionFactory _factory;

    public SystemLogRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<SystemLog>> SearchAsync(string? query, string? status, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT s.LogId, s.LocationId, s.RequestedBy, s.TemperatureInfo,
                               s.Status, s.ResponseTimeMs, s.LogDate, l.CityName
                        FROM SystemLogs s
                        LEFT JOIN Locations l ON l.LocationId = s.LocationId
                        WHERE 1=1";
            var cmd = new MySqlCommand();

            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (s.RequestedBy LIKE @q OR l.CityName LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "Toate")
            {
                sql += " AND s.Status = @status";
                cmd.Parameters.AddWithValue("@status", status);
            }

            sql += " ORDER BY s.LogDate DESC LIMIT 500";

            cmd.Connection = conn;
            cmd.CommandText = sql;

            var list = new List<SystemLog>();
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

    public async Task<int> InsertAsync(SystemLog log, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"INSERT INTO SystemLogs(LocationId, RequestedBy, TemperatureInfo, Status, ResponseTimeMs, LogDate)
                  VALUES(@lid, @req, @temp, @status, @resp, UTC_TIMESTAMP());
                  SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@lid", (object?)log.LocationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@req", log.RequestedBy ?? string.Empty);
            cmd.Parameters.AddWithValue("@temp", (object?)log.TemperatureInfo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", log.Status);
            cmd.Parameters.AddWithValue("@resp", (object?)log.ResponseTimeMs ?? DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            log.Id = id;
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
            await using var cmd = new MySqlCommand("DELETE FROM SystemLogs WHERE LogId = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM SystemLogs", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static SystemLog Map(DbDataReader r)
    {
        var locationIdOrd = r.GetOrdinal("LocationId");
        var cityOrd = r.GetOrdinal("CityName");
        var tempOrd = r.GetOrdinal("TemperatureInfo");
        var respOrd = r.GetOrdinal("ResponseTimeMs");
        return new SystemLog
        {
            Id = r.GetInt32(r.GetOrdinal("LogId")),
            LocationId = r.IsDBNull(locationIdOrd) ? null : r.GetInt32(locationIdOrd),
            LocationName = r.IsDBNull(cityOrd) ? string.Empty : r.GetString(cityOrd),
            RequestedBy = r.IsDBNull(r.GetOrdinal("RequestedBy")) ? string.Empty : r.GetString(r.GetOrdinal("RequestedBy")),
            TemperatureInfo = r.IsDBNull(tempOrd) ? null : r.GetDouble(tempOrd),
            Status = r.IsDBNull(r.GetOrdinal("Status")) ? "succes" : r.GetString(r.GetOrdinal("Status")),
            ResponseTimeMs = r.IsDBNull(respOrd) ? null : r.GetInt32(respOrd),
            LogDate = r.GetDateTime(r.GetOrdinal("LogDate"))
        };
    }
}
