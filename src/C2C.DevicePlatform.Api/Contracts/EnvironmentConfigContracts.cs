namespace C2C.DevicePlatform.Api.Contracts;

public sealed record UpsertEnvironmentConfigRequest(
    string Environment,
    string ConfigKey,
    string ConfigValueJson);

public sealed record RollbackEnvironmentConfigRequest(int SourceVersion);

public sealed record UpsertDeviceOverrideRequest(
    string DeviceId,
    string ConfigKey,
    string ConfigValueJson);

public sealed record EnvironmentConfigRecord(
    Guid ConfigId,
    string Environment,
    string ConfigKey,
    string ConfigValueJson,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record DeviceConfigOverrideRecord(
    Guid OverrideId,
    string DeviceId,
    string ConfigKey,
    string ConfigValueJson,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record EffectiveDeviceConfigRecord(
    string DeviceId,
    string Environment,
    string ConfigKey,
    string EffectiveValueJson,
    int TemplateVersion,
    int? OverrideVersion,
    bool HasOverride);

public sealed record CriticalChangePendingResponse(
    Guid ApprovalId,
    string Status,
    string Message);
