using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class LocationService
{
    private readonly LocationRepository _repo;

    public LocationService(LocationRepository repo)
    {
        _repo = repo;
    }

    public Task<List<Location>> SearchAsync(string? query, int? userId, CancellationToken ct = default)
        => _repo.SearchAsync(query, userId, ct);

    public Task<Location?> GetAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<Location> SaveAsync(Location loc, CancellationToken ct = default)
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
