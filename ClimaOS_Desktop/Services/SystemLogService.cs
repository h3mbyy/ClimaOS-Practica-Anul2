using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class SystemLogService
{
    private readonly SystemLogRepository _repo;

    public SystemLogService(SystemLogRepository repo)
    {
        _repo = repo;
    }

    public Task<List<SystemLog>> SearchAsync(string? query, string? status, CancellationToken ct = default)
        => _repo.SearchAsync(query, status, ct);

    public async Task<SystemLog> CreateAsync(SystemLog log, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(log.RequestedBy, "RequestedBy"));
        if (log.Status != "succes" && log.Status != "eroare")
            errors.Add("Status invalid.");
        ValidationService.EnsureValid(errors);
        await _repo.InsertAsync(log, ct);
        return log;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
