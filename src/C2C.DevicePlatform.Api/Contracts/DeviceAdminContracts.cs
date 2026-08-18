namespace C2C.DevicePlatform.Api.Contracts;

public sealed record RegisterDeviceRequest(
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    string Environment,
    string Status);

public sealed record AssignDeviceRequest(
    string MerchantId,
    string BranchId,
    string RegisterId);

public sealed record DeviceRecord(
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    string Environment,
    string Status,
    DateTimeOffset UpdatedAtUtc);

public sealed record DeviceImportResult(
    int TotalRows,
    int ProcessedRows,
    int ImportedRows,
    int FailedRows,
    IReadOnlyCollection<DeviceImportRowError> Errors);

public sealed record DeviceImportRowError(
    int RowNumber,
    string DeviceId,
    string Error);

public sealed record DeviceDashboardRecord(
    string Environment,
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int MaintenanceDevices,
    int AlertCount,
    DateTimeOffset? LastActivityUtc);

public sealed record DeviceAssignmentHistoryRecord(
    Guid AssignmentId,
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    bool Active,
    DateTimeOffset AssignedAtUtc);

public sealed record AuditRecord(
    long AuditId,
    string Actor,
    string Action,
    string Entity,
    string EntityId,
    string Environment,
    string? MetadataJson,
    DateTimeOffset Utc);

public sealed record DeviceDetailRecord(
    DeviceRecord Device,
    IReadOnlyCollection<DeviceAssignmentHistoryRecord> Assignments,
    IReadOnlyCollection<EffectiveDeviceConfigRecord> EffectiveConfigs,
    IReadOnlyCollection<AuditRecord> RecentSessions,
    IReadOnlyCollection<AuditRecord> RecentTransactions,
    int Page,
    int PageSize,
    string? EventFilter);

public sealed record StartDeviceExportRequest(
    string? Environment,
    string? Status);

public sealed record DeviceExportJobRecord(
    Guid JobId,
    string? Environment,
    string? StatusFilter,
    string Status,
    string? Error,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
