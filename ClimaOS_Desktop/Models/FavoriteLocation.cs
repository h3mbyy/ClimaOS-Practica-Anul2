using SQLite;

namespace ClimaOS_Desktop.Models;

public class FavoriteLocation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateTime AddedOn { get; set; }
}