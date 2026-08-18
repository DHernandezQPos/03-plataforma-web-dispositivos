using C2C.DevicePlatform.Domain.Devices;

namespace C2C.DevicePlatform.Application.Repositories;

public interface IDeviceCatalogRepository
{
    Task<IReadOnlyCollection<DeviceCatalogItem>> GetAllAsync(CancellationToken cancellationToken);

    Task<DeviceCatalogItem?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken);

    Task<DeviceCatalogItem> UpsertAsync(
        string deviceId,
        string merchantId,
        string branchId,
        string registerId,
        string environment,
        string status,
        CancellationToken cancellationToken);

    Task<DeviceCatalogItem?> AssignAsync(
        string deviceId,
        string merchantId,
        string branchId,
        string registerId,
        CancellationToken cancellationToken);

    Task<DeviceCatalogItem?> DeactivateAsync(string deviceId, CancellationToken cancellationToken);

    Task<DeviceEnvironmentDashboard?> GetEnvironmentDashboardAsync(
        string environment,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DeviceAssignmentHistoryItem>> GetAssignmentHistoryAsync(
        string deviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
