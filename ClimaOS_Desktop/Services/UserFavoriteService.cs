using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;
namespace ClimaOS_Desktop.Services;
public class UserFavoriteService
{
    private readonly UserFavoriteRepository _repo;
    public UserFavoriteService(UserFavoriteRepository repo)
    {
        _repo = repo;
    }
    public Task<List<UserFavorite>> SearchAsync(string? query, CancellationToken ct = default)
        => _repo.SearchAsync(query, ct);
    public Task<List<UserFavorite>> SearchForUserAsync(int userId, string? query = null, CancellationToken ct = default)
        => _repo.SearchForUserAsync(userId, query, ct);
    public Task<UserFavorite?> GetForUserLocationAsync(int userId, int locationId, CancellationToken ct = default)
        => _repo.GetForUserLocationAsync(userId, locationId, ct);
    public async Task<int> AddAsync(int userId, int locationId, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (userId <= 0) errors.Add("Utilizator invalid.");
        if (locationId <= 0) errors.Add("Locatie invalida.");
        ValidationService.EnsureValid(errors);
        return await _repo.InsertAsync(userId, locationId, ct);
    }
    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task DeleteForUserLocationAsync(int userId, int locationId, CancellationToken ct = default)
        => _repo.DeleteForUserLocationAsync(userId, locationId, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
