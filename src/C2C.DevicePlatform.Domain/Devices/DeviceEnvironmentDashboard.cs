namespace C2C.DevicePlatform.Domain.Devices;

public sealed class DeviceEnvironmentDashboard
{
    public string Environment { get; init; } = string.Empty;
    public int TotalDevices { get; init; }
    public int ActiveDevices { get; init; }
    public int InactiveDevices { get; init; }
    public int MaintenanceDevices { get; init; }
    public DateTimeOffset? LastActivityUtc { get; init; }
}
