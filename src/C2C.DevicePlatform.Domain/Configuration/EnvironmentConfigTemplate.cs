namespace C2C.DevicePlatform.Domain.Configuration;

public sealed class EnvironmentConfigTemplate
{
    public Guid ConfigId { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string ConfigKey { get; init; } = string.Empty;
    public string ConfigValueJson { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
