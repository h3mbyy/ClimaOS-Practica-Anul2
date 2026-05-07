using ClimaOS_Desktop.Common;
using ClimaOS_Desktop.Data.Repositories;
using ClimaOS_Desktop.Models;

namespace ClimaOS_Desktop.Services;

public class AlertService
{
    private readonly AlertRepository _repo;

    public AlertService(AlertRepository repo)
    {
        _repo = repo;
    }

    public Task<List<WeatherAlert>> SearchAsync(
        string? query,
        AlertSeverity? minSeverity,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
        => _repo.SearchAsync(query, minSeverity, from, to, ct);

    public Task<WeatherAlert?> GetAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<WeatherAlert> SaveAsync(WeatherAlert alert, CancellationToken ct = default)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationService.ValidateRequired(alert.Title, "Titlul alertei"));
        errors.AddRange(ValidationService.ValidateRequired(alert.Message, "Mesajul alertei"));
        if (alert.EndsAt < alert.StartsAt)
            errors.Add("Data de sfarsit trebuie sa fie dupa data de inceput.");
        ValidationService.EnsureValid(errors);

        if (alert.Id == 0)
            await _repo.InsertAsync(alert, ct);
        else
            await _repo.UpdateAsync(alert, ct);
        return alert;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _repo.CountAsync(ct);
}
