using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace ClimaOS_Desktop.Services;

public enum ExportFormat
{
    Csv,
    Json,
    Excel
}

public class ExportService
{
    private readonly string _basePath;

    public ExportService()
    {
        _basePath = Path.Combine(FileSystem.Current.AppDataDirectory, "exports");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> ExportAsync<T>(
        IEnumerable<T> data,
        IEnumerable<(string Header, Func<T, object?> Selector)> columns,
        ExportFormat format)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ext = format switch
        {
            ExportFormat.Csv => "csv",
            ExportFormat.Json => "json",
            ExportFormat.Excel => "xlsx",
            _ => "txt"
        };
        var fileName = $"export_{timestamp}.{ext}";
        var path = Path.Combine(_basePath, fileName);

        if (format == ExportFormat.Json)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
            return path;
        }

        if (format == ExportFormat.Excel)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Export");
            var cols = columns.ToArray();
            
            for (int i = 0; i < cols.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = cols[i].Header;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < cols.Length; i++)
                {
                    var val = cols[i].Selector(item)?.ToString() ?? string.Empty;
                    worksheet.Cell(rowIndex, i + 1).Value = val;
                }
                rowIndex++;
            }
            
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(path);
            return path;
        }

        // CSV Export
        var sb = new StringBuilder();
        var csvCols = columns.ToArray();
        sb.AppendLine(string.Join(",", csvCols.Select(c => EscapeCsv(c.Header))));
        foreach (var item in data)
        {
            var values = csvCols.Select(c => EscapeCsv(c.Selector(item)));
            sb.AppendLine(string.Join(",", values));
        }
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string EscapeCsv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        var needsQuotes = text.Contains(',') || text.Contains('"') || text.Contains('\n');
        if (text.Contains('"'))
            text = text.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{text}\"" : text;
    }
}
