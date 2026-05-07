using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Models;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data;

public class DatabaseInitializer
{
    private readonly MySqlConnectionFactory _factory;

    public DatabaseInitializer(MySqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _factory.OpenAsync(ct);

            foreach (var statement in SchemaStatements)
            {
                await using var cmd = new MySqlCommand(statement, conn);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await EnsureSeedAdminAsync(conn, ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ErrorHandler.Translate(ex);
        }
    }

    private static async Task EnsureSeedAdminAsync(MySqlConnection conn, CancellationToken ct)
    {
        await using var check = new MySqlCommand(
            "SELECT COUNT(*) FROM users WHERE role = 'admin'",
            conn);
        var count = Convert.ToInt64(await check.ExecuteScalarAsync(ct));
        if (count > 0)
            return;

        var hash = Services.PasswordHasher.Hash("admin1234");
        await using var insert = new MySqlCommand(
            @"INSERT INTO users(name, email, password_hash, role, created_at)
              VALUES(@name, @email, @hash, @role, UTC_TIMESTAMP())",
            conn);
        insert.Parameters.AddWithValue("@name", "Administrator");
        insert.Parameters.AddWithValue("@email", "admin@climaos.local");
        insert.Parameters.AddWithValue("@hash", hash);
        insert.Parameters.AddWithValue("@role", UserRole.Admin.ToDbString());
        await insert.ExecuteNonQueryAsync(ct);
    }

    private static readonly string[] SchemaStatements = new[]
    {
        @"CREATE TABLE IF NOT EXISTS users (
            id INT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(120) NOT NULL,
            email VARCHAR(190) NOT NULL UNIQUE,
            password_hash VARCHAR(255) NOT NULL,
            role VARCHAR(20) NOT NULL DEFAULT 'user',
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS locations (
            id INT AUTO_INCREMENT PRIMARY KEY,
            user_id INT NULL,
            name VARCHAR(120) NOT NULL,
            country VARCHAR(80) NOT NULL DEFAULT '',
            latitude DOUBLE NOT NULL DEFAULT 0,
            longitude DOUBLE NOT NULL DEFAULT 0,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_locations_user_id (user_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS weather_alerts (
            id INT AUTO_INCREMENT PRIMARY KEY,
            location_id INT NULL,
            location_name VARCHAR(120) NOT NULL DEFAULT '',
            title VARCHAR(160) NOT NULL,
            message TEXT NOT NULL,
            severity TINYINT NOT NULL DEFAULT 0,
            starts_at DATETIME NOT NULL,
            ends_at DATETIME NOT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_weather_alerts_location_id (location_id),
            INDEX idx_weather_alerts_severity (severity)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS reports (
            id INT AUTO_INCREMENT PRIMARY KEY,
            title VARCHAR(160) NOT NULL,
            type TINYINT NOT NULL DEFAULT 3,
            notes TEXT NOT NULL,
            created_by_user_id INT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_reports_type (type),
            INDEX idx_reports_user (created_by_user_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
    };
}
