namespace ClimaOS_Desktop.Services;
public sealed class PasswordResetSettings
{
    public int CodeLifetimeMinutes { get; init; } = 15;
    public int RequestCooldownSeconds { get; init; } = 60;
    public int MaxCodeLength { get; init; } = 6;
    public static PasswordResetSettings LoadFromEnvironment()
    {
        return new PasswordResetSettings
        {
            CodeLifetimeMinutes = ReadInt("CLIMAOS_RESET_CODE_LIFETIME_MINUTES", 15, minValue: 5, maxValue: 60),
            RequestCooldownSeconds = ReadInt("CLIMAOS_RESET_REQUEST_COOLDOWN_SECONDS", 60, minValue: 15, maxValue: 600),
            MaxCodeLength = 6
        };
    }
    private static string Read(string key)
        => Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;
    private static int ReadInt(string key, int defaultValue, int minValue, int maxValue)
    {
        if (!int.TryParse(Read(key), out var parsed))
            return defaultValue;
        if (parsed < minValue)
            return minValue;
        if (parsed > maxValue)
            return maxValue;
        return parsed;
    }
}
