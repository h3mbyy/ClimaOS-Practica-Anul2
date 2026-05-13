namespace ClimaOS_Desktop.Models;
public class UserFavorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LocationId { get; set; }
    public DateTime AddedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string LocationDisplay => string.IsNullOrWhiteSpace(CountryCode)
        ? LocationName
        : $"{LocationName}, {CountryCode}";
    public string AddedAtDisplay => AddedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
