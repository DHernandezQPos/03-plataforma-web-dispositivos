namespace C2C.DevicePlatform.Domain.Devices;

public sealed class DeviceAssignmentHistoryItem
{
    public Guid AssignmentId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string BranchId { get; init; } = string.Empty;
    public string RegisterId { get; init; } = string.Empty;
    public bool Active { get; init; }
    public DateTimeOffset AssignedAtUtc { get; init; }
}
