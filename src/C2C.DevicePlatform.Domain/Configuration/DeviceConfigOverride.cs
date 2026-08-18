namespace C2C.DevicePlatform.Domain.Configuration;

public sealed class DeviceConfigOverride
{
    public Guid OverrideId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string ConfigKey { get; init; } = string.Empty;
    public string ConfigValueJson { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
