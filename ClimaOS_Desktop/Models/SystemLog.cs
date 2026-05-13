namespace ClimaOS_Desktop.Models;
public class SystemLog
{
    public int Id { get; set; }
    public int? LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public double? TemperatureInfo { get; set; }
    public string Status { get; set; } = "succes";
    public int? ResponseTimeMs { get; set; }
    public DateTime LogDate { get; set; }
    public string StatusDisplay => Status == "eroare" ? "Eroare" : "Succes";
    public string LogDateDisplay => LogDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
