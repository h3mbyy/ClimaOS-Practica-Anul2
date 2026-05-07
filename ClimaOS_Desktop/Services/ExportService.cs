using System.Globalization;
using System.Text;
using System.Text.Json;
using ClimaOS_Desktop.Common;

namespace ClimaOS_Desktop.Services;

public enum ExportFormat
{
    Csv,
    Json
}

public class ExportService
{
    public string ToCsv<T>(IEnumerable<T> items, IReadOnlyList<(string Header, Func<T, object?> Selector)> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => Escape(c.Header))));
        foreach (var item in items)
        {
            var values = columns.Select(c => Escape(Format(c.Selector(item))));
            sb.AppendLine(string.Join(",", values));
        }
        return sb.ToString();
    }

    public string ToJson<T>(IEnumerable<T> items)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(items, options);
    }

    public async Task<string> ExportAsync<T>(
        IEnumerable<T> items,
        IReadOnlyList<(string Header, Func<T, object?> Selector)> columns,
        ExportFormat format,
        string fileBaseName,
        CancellationToken ct = default)
    {
        try
        {
            var dir = FileSystem.AppDataDirectory;
            var ext = format == ExportFormat.Csv ? "csv" : "json";
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(dir, $"{fileBaseName}_{stamp}.{ext}");
            var content = format == ExportFormat.Csv ? ToCsv(items, columns) : ToJson(items);
            await File.WriteAllTextAsync(path, content, ct);
            return path;
        }
        catch (Exception ex)
        {
            throw new AppException(
                "Nu s-a putut salva exportul: " + ex.Message,
                "Export",
                ex);
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            double d => d.ToString("0.######", CultureInfo.InvariantCulture),
            float f => f.ToString("0.######", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.######", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? string.Empty
        };
    }
}
