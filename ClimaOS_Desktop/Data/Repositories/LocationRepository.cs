using ClimaOS_Desktop.Common;
using LocationModel = ClimaOS_Desktop.Models.Location;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data.Repositories;

public class LocationRepository
{
    private readonly MySqlConnectionFactory _factory;

    public LocationRepository(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<LocationModel>> SearchAsync(string? query, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            var sql = @"SELECT LocationId, CityName, CountryCode, Latitude, Longitude
                        FROM Locations WHERE 1=1";
            var cmd = new MySqlCommand();
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND (CityName LIKE @q OR CountryCode LIKE @q)";
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
            }
            sql += " ORDER BY CityName ASC LIMIT 500";

            cmd.Connection = conn;
            cmd.CommandText = sql;

            var list = new List<LocationModel>();
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

    public async Task<LocationModel?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                                @"SELECT LocationId, CityName, CountryCode, Latitude, Longitude
                                    FROM Locations WHERE LocationId = @id LIMIT 1",
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

    public async Task<LocationModel?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"SELECT LocationId, CityName, CountryCode, Latitude, Longitude
                  FROM Locations WHERE LOWER(CityName) = LOWER(@name) LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    public async Task<int> InsertAsync(LocationModel loc, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                                @"INSERT INTO Locations(CityName, CountryCode, Latitude, Longitude)
                  VALUES(@name, @country, @lat, @lon);
                  SELECT LAST_INSERT_ID();",
                conn);
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

    public async Task UpdateAsync(LocationModel loc, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                @"UPDATE Locations
                  SET CityName = @name, CountryCode = @country,
                      Latitude = @lat, Longitude = @lon
                  WHERE LocationId = @id",
                conn);
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
            await using var cmd = new MySqlCommand("DELETE FROM Locations WHERE LocationId = @id", conn);
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
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Locations", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static LocationModel Map(DbDataReader r)
    {
        return new LocationModel
        {
            Id = r.GetInt32(r.GetOrdinal("LocationId")),
            Name = r.GetString(r.GetOrdinal("CityName")),
            Country = r.GetString(r.GetOrdinal("CountryCode")),
            Latitude = r.IsDBNull(r.GetOrdinal("Latitude")) ? 0 : r.GetDouble(r.GetOrdinal("Latitude")),
            Longitude = r.IsDBNull(r.GetOrdinal("Longitude")) ? 0 : r.GetDouble(r.GetOrdinal("Longitude"))
        };
    }
}
