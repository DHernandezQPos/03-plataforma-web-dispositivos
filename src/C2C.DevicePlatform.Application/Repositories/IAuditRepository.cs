using C2C.DevicePlatform.Domain.Audit;

namespace C2C.DevicePlatform.Application.Repositories;

public interface IAuditRepository
{
    Task<AuditEntry> AppendAsync(
        string actor,
        string action,
        string entity,
        string entityId,
        string environment,
        string? metadataJson,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditEntry>> GetByEntityAsync(
        string entity,
        string entityId,
        int page,
        int pageSize,
        string? actionFilter,
        CancellationToken cancellationToken);
}
