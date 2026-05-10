using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using LocationModel = ClimaOS_Desktop.Models.Location;

namespace ClimaOS_Desktop.Services;

public class LocationService
{
    private readonly LocationRepository _repo;

    public LocationService(LocationRepository repo)
    {
        _repo = repo;
    }

    public Task<List<LocationModel>> SearchAsync(string? query, CancellationToken ct = default)
        => _repo.SearchAsync(query, ct);

    public Task<LocationModel?> GetAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public Task<LocationModel?> GetByNameAsync(string name, CancellationToken ct = default)
        => _repo.GetByNameAsync(name, ct);

    public async Task<LocationModel> EnsureAsync(
        string name,
        string country = "",
        double latitude = 0,
        double longitude = 0,
        CancellationToken ct = default)
    {
        var existing = await _repo.GetByNameAsync(name, ct);
        if (existing is not null)
            return existing;

        var loc = new LocationModel
        {
            Name = name.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? "N/A" : country.Trim(),
            Latitude = latitude,
            Longitude = longitude
        };
        return await SaveAsync(loc, ct);
    }

    public async Task<LocationModel> SaveAsync(LocationModel loc, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(loc.Name, "Numele locației"));
        if (loc.Latitude < -90 || loc.Latitude > 90)
            errors.Add("Latitudinea trebuie să fie între -90 și 90.");
        if (loc.Longitude < -180 || loc.Longitude > 180)
            errors.Add("Longitudinea trebuie să fie între -180 și 180.");
        ValidationService.EnsureValid(errors);

        if (loc.Id == 0)
            await _repo.InsertAsync(loc, ct);
        else
            await _repo.UpdateAsync(loc, ct);
        return loc;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
