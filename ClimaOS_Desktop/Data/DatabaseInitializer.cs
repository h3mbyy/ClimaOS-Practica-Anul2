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
            await EnsureUsersSchemaAsync(conn, ct);
            await EnsurePasswordResetTokenSchemaAsync(conn, ct);
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
            "SELECT COUNT(*) FROM Users WHERE Role = 'Admin'",
            conn);
        var count = Convert.ToInt64(await check.ExecuteScalarAsync(ct));
        if (count > 0)
        {
            await EnsureAdminPasswordHashAsync(conn, ct);
            return;
        }
        var hash = Services.PasswordHasher.Hash("admin1234");
        await using var insert = new MySqlCommand(
                        @"INSERT INTO Users(FullName, Email, PasswordHash, Role, CreatedAt)
              VALUES(@name, @email, @hash, @role, UTC_TIMESTAMP())",
            conn);
        insert.Parameters.AddWithValue("@name", "Administrator");
        insert.Parameters.AddWithValue("@email", "admin@climaos.local");
        insert.Parameters.AddWithValue("@hash", hash);
        insert.Parameters.AddWithValue("@role", UserRole.Admin.ToDbString());
        await insert.ExecuteNonQueryAsync(ct);
    }
    private static async Task EnsureAdminPasswordHashAsync(MySqlConnection conn, CancellationToken ct)
    {
        await using var select = new MySqlCommand(
            "SELECT UserId FROM Users WHERE Role = 'Admin' AND (PasswordHash IS NULL OR PasswordHash = '' OR PasswordHash NOT LIKE 'PBKDF2|%') LIMIT 1",
            conn);
        var result = await select.ExecuteScalarAsync(ct);
        if (result is null)
            return;
        var userId = Convert.ToInt32(result);
        var hash = Services.PasswordHasher.Hash("admin1234");
        await using var update = new MySqlCommand(
            "UPDATE Users SET PasswordHash = @hash WHERE UserId = @id",
            conn);
        update.Parameters.AddWithValue("@hash", hash);
        update.Parameters.AddWithValue("@id", userId);
        await update.ExecuteNonQueryAsync(ct);
    }
    private static async Task EnsurePasswordResetTokenSchemaAsync(MySqlConnection conn, CancellationToken ct)
    {
        if (!await ColumnExistsAsync(conn, "PasswordResetTokens", "CodeHash", ct))
        {
            await using var addCodeHash = new MySqlCommand(
                "ALTER TABLE PasswordResetTokens ADD COLUMN CodeHash VARCHAR(256) NULL AFTER Email",
                conn);
            await addCodeHash.ExecuteNonQueryAsync(ct);
        }
        if (await ColumnExistsAsync(conn, "PasswordResetTokens", "Code", ct))
        {
            await using var migrateLegacyCodes = new MySqlCommand(
                """
                UPDATE PasswordResetTokens
                SET CodeHash = CONCAT('LEGACY|', Code)
                WHERE (CodeHash IS NULL OR CodeHash = '')
                  AND Code IS NOT NULL
                  AND Code <> ''
                """,
                conn);
            await migrateLegacyCodes.ExecuteNonQueryAsync(ct);
        }
        await EnsureIndexAsync(
            conn,
            "PasswordResetTokens",
            "idx_reset_email_state",
            "CREATE INDEX idx_reset_email_state ON PasswordResetTokens (Email, IsUsed, Expiration)",
            ct);
    }
    private static async Task EnsureUsersSchemaAsync(MySqlConnection conn, CancellationToken ct)
    {
        if (!await ColumnExistsAsync(conn, "Users", "CreatedAt", ct))
        {
            await using var addCreatedAt = new MySqlCommand(
                "ALTER TABLE Users ADD COLUMN CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
                conn);
            await addCreatedAt.ExecuteNonQueryAsync(ct);
        }
    }
    private static async Task<bool> ColumnExistsAsync(MySqlConnection conn, string tableName, string columnName, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName
            """,
            conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);
        cmd.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }
    private static async Task EnsureIndexAsync(
        MySqlConnection conn,
        string tableName,
        string indexName,
        string createSql,
        CancellationToken ct)
    {
        await using var check = new MySqlCommand(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND INDEX_NAME = @indexName
            """,
            conn);
        check.Parameters.AddWithValue("@tableName", tableName);
        check.Parameters.AddWithValue("@indexName", indexName);
        if (Convert.ToInt32(await check.ExecuteScalarAsync(ct)) > 0)
            return;
        await using var create = new MySqlCommand(createSql, conn);
        await create.ExecuteNonQueryAsync(ct);
    }
    private static readonly string[] SchemaStatements = new[]
    {
        @"CREATE TABLE IF NOT EXISTS Users (
            UserId INT AUTO_INCREMENT PRIMARY KEY,
            FullName VARCHAR(100) NOT NULL,
            Email VARCHAR(100) UNIQUE NOT NULL,
            PasswordHash VARCHAR(256) NOT NULL,
            Role VARCHAR(20) NOT NULL DEFAULT 'User',
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT CK_Users_Role CHECK (Role IN ('User', 'Admin'))
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
        @"CREATE TABLE IF NOT EXISTS Locations (
            LocationId INT AUTO_INCREMENT PRIMARY KEY,
            CityName VARCHAR(100) NOT NULL,
            CountryCode VARCHAR(10) NOT NULL DEFAULT 'MD',
            Latitude DECIMAL(9,6) NULL,
            Longitude DECIMAL(9,6) NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
        @"CREATE TABLE IF NOT EXISTS UserFavorites (
            FavoriteId INT AUTO_INCREMENT PRIMARY KEY,
            UserId INT NOT NULL,
            LocationId INT NOT NULL,
            AddedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
            FOREIGN KEY (LocationId) REFERENCES Locations(LocationId) ON DELETE CASCADE,
            CONSTRAINT UQ_User_Location UNIQUE (UserId, LocationId)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
        @"CREATE TABLE IF NOT EXISTS SystemLogs (
            LogId INT AUTO_INCREMENT PRIMARY KEY,
            LocationId INT NULL,
            RequestedBy VARCHAR(100) NULL,
            TemperatureInfo DECIMAL(5,2) NULL,
            Status VARCHAR(20) NULL,
            ResponseTimeMs INT NULL,
            LogDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (LocationId) REFERENCES Locations(LocationId) ON DELETE SET NULL,
            CONSTRAINT CK_SystemLogs_Status CHECK (Status IN ('succes', 'eroare'))
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
            type TINYINT NOT NULL DEFAULT 5,
            notes TEXT NOT NULL,
            created_by_user_id INT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_reports_type (type),
            INDEX idx_reports_user (created_by_user_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
        @"CREATE TABLE IF NOT EXISTS PasswordResetTokens (
            TokenId INT AUTO_INCREMENT PRIMARY KEY,
            Email VARCHAR(100) NOT NULL,
            CodeHash VARCHAR(256) NULL,
            Code VARCHAR(10) NULL,
            Expiration DATETIME NOT NULL,
            IsUsed BOOLEAN NOT NULL DEFAULT 0,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_reset_email (Email)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
    };
}
