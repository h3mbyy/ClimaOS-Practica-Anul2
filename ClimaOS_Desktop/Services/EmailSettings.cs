using System.Text.Json;
using Microsoft.Maui.Storage;
namespace ClimaOS_Desktop.Services;
public sealed class EmailSettings
{
    private const string LocalSettingsFileName = "smtp.settings.local.json";
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "ClimaOS";
    public bool EnableSsl { get; init; } = true;
    public bool UseConfiguredSmtp =>
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !IsPlaceholder(Username) &&
        !IsPlaceholder(Password) &&
        !string.IsNullOrWhiteSpace(FromAddress);
    public bool LooksLikePlaceholderConfiguration =>
        IsPlaceholder(Username) ||
        IsPlaceholder(Password) ||
        IsPlaceholder(FromAddress);
    public static EmailSettings LoadFromEnvironment()
    {
        var fileSettings = LoadFromLocalFile();
        return new EmailSettings
        {
            SmtpHost = Read("CLIMAOS_SMTP_HOST", fileSettings?.SmtpHost ?? string.Empty),
            SmtpPort = ReadInt("CLIMAOS_SMTP_PORT", fileSettings?.SmtpPort ?? 587),
            Username = Read("CLIMAOS_SMTP_USERNAME", fileSettings?.Username ?? string.Empty),
            Password = Read("CLIMAOS_SMTP_PASSWORD", fileSettings?.Password ?? string.Empty),
            FromAddress = Read("CLIMAOS_SMTP_FROM", fileSettings?.FromAddress ?? string.Empty),
            FromName = Read("CLIMAOS_SMTP_FROM_NAME", fileSettings?.FromName ?? "ClimaOS"),
            EnableSsl = ReadBool("CLIMAOS_SMTP_SSL", fileSettings?.EnableSsl ?? true)
        };
    }
    private static EmailSettings? LoadFromLocalFile()
    {
        foreach (var path in GetCandidatePaths())
        {
            try
            {
                if (!File.Exists(path))
                    continue;
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<EmailSettings>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (settings is not null)
                    return settings;
            }
            catch
            {
            }
        }
        var packageSettings = LoadFromAppPackage();
        if (packageSettings is not null)
            return packageSettings;
        return null;
    }
    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, LocalSettingsFileName);
        yield return Path.Combine(Environment.CurrentDirectory, LocalSettingsFileName);
        yield return Path.Combine(FileSystem.AppDataDirectory, LocalSettingsFileName);
    }
    private static EmailSettings? LoadFromAppPackage()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(LocalSettingsFileName)
                .GetAwaiter()
                .GetResult();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<EmailSettings>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
    private static string Read(string key, string defaultValue = "")
        => Environment.GetEnvironmentVariable(key)?.Trim() ?? defaultValue;
    private static int ReadInt(string key, int defaultValue)
        => int.TryParse(Read(key), out var parsed) ? parsed : defaultValue;
    private static bool ReadBool(string key, bool defaultValue)
        => bool.TryParse(Read(key), out var parsed) ? parsed : defaultValue;
    private static bool IsPlaceholder(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Contains("example.com") ||
               normalized.Contains("completeaza-aici") ||
               normalized.Contains("parola-aplicatie");
    }
}
