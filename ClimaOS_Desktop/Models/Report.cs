namespace ClimaOS_Desktop.Models;

public enum ReportType
{
    Users = 0,
    Locations = 1,
    Alerts = 2,
    Custom = 3
}

public class Report
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReportType Type { get; set; } = ReportType.Custom;
    public string Notes { get; set; } = string.Empty;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string TypeDisplay => Type switch
    {
        ReportType.Users => "Utilizatori",
        ReportType.Locations => "Locații",
        ReportType.Alerts => "Alerte",
        ReportType.Custom => "Personalizat",
        _ => "Personalizat"
    };

    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
