namespace C2C.DevicePlatform.Domain.Configuration;

public sealed class EffectiveDeviceConfig
{
    public string DeviceId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string ConfigKey { get; init; } = string.Empty;
    public string EffectiveValueJson { get; init; } = string.Empty;
    public int TemplateVersion { get; init; }
    public int? OverrideVersion { get; init; }
    public bool HasOverride { get; init; }
}
