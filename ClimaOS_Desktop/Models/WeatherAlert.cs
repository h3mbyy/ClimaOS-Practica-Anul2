namespace ClimaOS_Desktop.Models;
public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Severe = 2,
    Extreme = 3
}
public class WeatherAlert
{
    public int Id { get; set; }
    public int? LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; } = DateTime.UtcNow.AddHours(6);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SeverityDisplay => Severity switch
    {
        AlertSeverity.Info => "Informare",
        AlertSeverity.Warning => "Avertisment",
        AlertSeverity.Severe => "Sever",
        AlertSeverity.Extreme => "Extrem",
        _ => "Informare"
    };
    public string IntervalDisplay =>
        $"{StartsAt.ToLocalTime():dd.MM HH:mm} - {EndsAt.ToLocalTime():dd.MM HH:mm}";
}
