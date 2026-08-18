namespace C2C.DevicePlatform.Domain.Audit;

public sealed class AuditEntry
{
    public long AuditId { get; init; }
    public string Actor { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string? MetadataJson { get; init; }
    public DateTimeOffset Utc { get; init; }
}
