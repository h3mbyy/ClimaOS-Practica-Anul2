using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class ReportService
{
    private readonly ReportRepository _repo;
    private readonly SessionStore _session;

    public ReportService(ReportRepository repo, SessionStore session)
    {
        _repo = repo;
        _session = session;
    }

    public Task<List<Report>> SearchAsync(string? query, ReportType? type, CancellationToken ct = default)
        => _repo.SearchAsync(query, type, ct);

    public async Task<Report> CreateAsync(string title, ReportType type, string notes, CancellationToken ct = default)
    {
        var errors = ValidationService.ValidateRequired(title, "Titlul raportului");
        ValidationService.EnsureValid(errors);

        var report = new Report
        {
            Title = title.Trim(),
            Type = type,
            Notes = notes ?? string.Empty,
            CreatedByUserId = _session.CurrentUser?.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.InsertAsync(report, ct);
        return report;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
