using ClimaOS_Desktop.Common;
using MySql.Data.MySqlClient;

namespace ClimaOS_Desktop.Data;

public class MySqlConnectionFactory
{
    private const string PrefServer = "db.server";
    private const string PrefPort = "db.port";
    private const string PrefDatabase = "db.database";
    private const string PrefUser = "db.user";
    private const string PrefPassword = "db.password";

    private DatabaseConfig _config;

    public MySqlConnectionFactory()
    {
        _config = LoadConfig();
    }

    public DatabaseConfig CurrentConfig => _config;

    public void UpdateConfig(DatabaseConfig config)
    {
        _config = config;
        SaveConfig(config);
    }

    public async Task<MySqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new MySqlConnection(_config.ToConnectionString());
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch (MySqlException ex)
        {
            if (ex.Number == 1049)
            {
                await EnsureDatabaseExistsAsync(ct);
                await connection.DisposeAsync();
                connection = new MySqlConnection(_config.ToConnectionString());
                await connection.OpenAsync(ct);
                return connection;
            }

            connection.Dispose();
            throw new DatabaseException(
                $"Conectare eșuată la {_config.Server}:{_config.Port}/{_config.Database}. {ex.Message}",
                ex);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new MySqlCommand("SELECT 1", conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    private static DatabaseConfig LoadConfig()
    {
        return new DatabaseConfig
        {
            Server = Preferences.Default.Get(PrefServer, "localhost"),
            Port = (uint)Preferences.Default.Get(PrefPort, 3306),
            Database = Preferences.Default.Get(PrefDatabase, "ClimaOS_DB"),
            User = Preferences.Default.Get(PrefUser, "root"),
            Password = Preferences.Default.Get(PrefPassword, "godea1234")
        };
    }

    private static void SaveConfig(DatabaseConfig cfg)
    {
        Preferences.Default.Set(PrefServer, cfg.Server);
        Preferences.Default.Set(PrefPort, (int)cfg.Port);
        Preferences.Default.Set(PrefDatabase, cfg.Database);
        Preferences.Default.Set(PrefUser, cfg.User);
        Preferences.Default.Set(PrefPassword, cfg.Password);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken ct)
    {
        var builder = new MySqlConnectionStringBuilder(_config.ToConnectionString())
        {
            Database = string.Empty
        };

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);

        await using var command = new MySqlCommand(
            $"CREATE DATABASE IF NOT EXISTS `{_config.Database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
            connection);
        await command.ExecuteNonQueryAsync(ct);
    }
}
