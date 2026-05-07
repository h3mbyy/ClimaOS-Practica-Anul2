namespace ClimaOS_Desktop.Data;

public class DatabaseConfig
{
    public string Server { get; set; } = "localhost";
    public uint Port { get; set; } = 3306;
    public string Database { get; set; } = "ClimaOS_DB";
    public string User { get; set; } = "root";
    public string Password { get; set; } = string.Empty;
    public bool SslMode { get; set; } = false;

    public string ToConnectionString()
    {
        var sb = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder
        {
            Server = Server,
            Port = Port,
            Database = Database,
            UserID = User,
            Password = Password,
            SslMode = SslMode
                ? MySql.Data.MySqlClient.MySqlSslMode.Required
                : MySql.Data.MySqlClient.MySqlSslMode.Disabled,
            ConnectionTimeout = 8,
            DefaultCommandTimeout = 30
        };
        return sb.ConnectionString;
    }
}
