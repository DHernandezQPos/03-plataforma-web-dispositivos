using System.Text.Json;
using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Audit;

namespace C2C.DevicePlatform.Api.Services;

public sealed class AuditTrailService(
    IAuditRepository repository,
    SensitiveDataMaskingService maskingService)
{
    public async Task<AuditEntry> AppendAsync(
        string actor,
        string action,
        string entity,
        string entityId,
        string environment,
        object? metadata,
        CancellationToken cancellationToken)
    {
        string? metadataJson = null;
        if (metadata is not null)
        {
            metadataJson = JsonSerializer.Serialize(metadata);
            metadataJson = maskingService.MaskJson(metadataJson);
        }

        return await repository.AppendAsync(
            actor,
            action,
            entity,
            entityId,
            environment,
            metadataJson,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditEntry>> GetByEntityAsync(
        string entity,
        string entityId,
        int page,
        int pageSize,
        string? actionFilter,
        CancellationToken cancellationToken)
    {
        var entries = await repository.GetByEntityAsync(entity, entityId, page, pageSize, actionFilter, cancellationToken);
        return entries
            .Select(entry => new AuditEntry
            {
                AuditId = entry.AuditId,
                Actor = entry.Actor,
                Action = entry.Action,
                Entity = entry.Entity,
                EntityId = entry.EntityId,
                Environment = entry.Environment,
                MetadataJson = string.IsNullOrWhiteSpace(entry.MetadataJson)
                    ? entry.MetadataJson
                    : maskingService.MaskJson(entry.MetadataJson),
                Utc = entry.Utc
            })
            .ToArray();
    }
}
