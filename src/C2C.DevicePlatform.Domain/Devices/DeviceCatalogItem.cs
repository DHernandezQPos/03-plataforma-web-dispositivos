namespace C2C.DevicePlatform.Domain.Devices;

public sealed class DeviceCatalogItem
{
    public string DeviceId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string BranchId { get; init; } = string.Empty;
    public string RegisterId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
