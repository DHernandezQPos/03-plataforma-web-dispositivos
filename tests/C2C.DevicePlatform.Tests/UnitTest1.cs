using C2C.DevicePlatform.Api.Services;
using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Devices;
using System.Text;

namespace C2C.DevicePlatform.Tests;

public sealed class DeviceCatalogServiceTests
{
    [Fact]
    public async Task ImportCsvAsync_ContinuesWhenRowsAreInvalid()
    {
        var repository = new FakeDeviceCatalogRepository();
        var assignmentTargetRepository = new FakeAssignmentTargetRepository();
        var service = new DeviceCatalogService(repository, assignmentTargetRepository);

        const string csv = """
            DeviceId,MerchantId,BranchId,RegisterId,Environment,Status
            dev-001,m-001,b-001,r-001,demo,active
            ,m-002,b-002,r-002,qa,active
            dev-003,m-003,b-003,r-003,invalid,active
            dev-004,m-004,b-004,r-004,prod,
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportCsvAsync(stream, CancellationToken.None);

        Assert.Equal(4, result.ProcessedRows);
        Assert.Equal(2, result.ImportedRows);
        Assert.Equal(2, result.FailedRows);
        Assert.Equal(2, repository.StoredDevices.Count);
        Assert.Contains(result.Errors, error => error.RowNumber == 3 && error.Error.Contains("DeviceId is required", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.RowNumber == 4 && error.Error.Contains("Environment must be demo, qa, or prod", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportCsvAsync_ParsesQuotedValues()
    {
        var repository = new FakeDeviceCatalogRepository();
        var assignmentTargetRepository = new FakeAssignmentTargetRepository();
        var service = new DeviceCatalogService(repository, assignmentTargetRepository);

        const string csv = """
            DeviceId,MerchantId,BranchId,RegisterId,Environment,Status
            "dev,005",m-005,b-005,r-005,demo,active
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportCsvAsync(stream, CancellationToken.None);

        Assert.Equal(1, result.ProcessedRows);
        Assert.Equal(1, result.ImportedRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Single(repository.StoredDevices);
        Assert.Equal("dev,005", repository.StoredDevices[0].DeviceId);
    }

    [Fact]
    public async Task AssignAsync_WhenTargetIsInactive_ThrowsGuidedError()
    {
        var repository = new FakeDeviceCatalogRepository();
        var assignmentTargetRepository = new FakeAssignmentTargetRepository
        {
            ExistsActiveTarget = false
        };

        await repository.UpsertAsync(
            "dev-010",
            "m-010",
            "b-010",
            "r-010",
            "demo",
            "active",
            CancellationToken.None);

        var service = new DeviceCatalogService(repository, assignmentTargetRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignAsync(
            "dev-010",
            new C2C.DevicePlatform.Api.Contracts.AssignDeviceRequest("m-200", "b-200", "r-200"),
            CancellationToken.None));

        Assert.Contains("does not exist or is inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeDeviceCatalogRepository : IDeviceCatalogRepository
    {
        public List<DeviceCatalogItem> StoredDevices { get; } = [];

        public Task<IReadOnlyCollection<DeviceCatalogItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<DeviceCatalogItem>>(StoredDevices);
        }

        public Task<DeviceCatalogItem?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
        {
            var result = StoredDevices.FirstOrDefault(item => item.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(result);
        }

        public Task<DeviceCatalogItem> UpsertAsync(
            string deviceId,
            string merchantId,
            string branchId,
            string registerId,
            string environment,
            string status,
            CancellationToken cancellationToken)
        {
            var existing = StoredDevices.FirstOrDefault(item => item.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                StoredDevices.Remove(existing);
            }

            var updated = new DeviceCatalogItem
            {
                DeviceId = deviceId,
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId,
                Environment = environment,
                Status = status,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            StoredDevices.Add(updated);
            return Task.FromResult(updated);
        }

        public Task<DeviceCatalogItem?> AssignAsync(
            string deviceId,
            string merchantId,
            string branchId,
            string registerId,
            CancellationToken cancellationToken)
        {
            var existing = StoredDevices.FirstOrDefault(item => item.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                return Task.FromResult<DeviceCatalogItem?>(null);
            }

            var updated = new DeviceCatalogItem
            {
                DeviceId = existing.DeviceId,
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId,
                Environment = existing.Environment,
                Status = existing.Status,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            StoredDevices.Remove(existing);
            StoredDevices.Add(updated);
            return Task.FromResult<DeviceCatalogItem?>(updated);
        }

        public Task<DeviceCatalogItem?> DeactivateAsync(string deviceId, CancellationToken cancellationToken)
        {
            var existing = StoredDevices.FirstOrDefault(item => item.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                return Task.FromResult<DeviceCatalogItem?>(null);
            }

            var updated = new DeviceCatalogItem
            {
                DeviceId = existing.DeviceId,
                MerchantId = existing.MerchantId,
                BranchId = existing.BranchId,
                RegisterId = existing.RegisterId,
                Environment = existing.Environment,
                Status = "inactive",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            StoredDevices.Remove(existing);
            StoredDevices.Add(updated);
            return Task.FromResult<DeviceCatalogItem?>(updated);
        }

        public Task<DeviceEnvironmentDashboard?> GetEnvironmentDashboardAsync(string environment, CancellationToken cancellationToken)
        {
            var items = StoredDevices.Where(item => item.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (items.Length == 0)
            {
                return Task.FromResult<DeviceEnvironmentDashboard?>(null);
            }

            var dashboard = new DeviceEnvironmentDashboard
            {
                Environment = environment,
                TotalDevices = items.Length,
                ActiveDevices = items.Count(item => item.Status.Equals("active", StringComparison.OrdinalIgnoreCase)),
                InactiveDevices = items.Count(item => item.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase)),
                MaintenanceDevices = items.Count(item => item.Status.Equals("maintenance", StringComparison.OrdinalIgnoreCase)),
                LastActivityUtc = items.Max(item => item.UpdatedAtUtc)
            };

            return Task.FromResult<DeviceEnvironmentDashboard?>(dashboard);
        }

        public Task<IReadOnlyCollection<DeviceAssignmentHistoryItem>> GetAssignmentHistoryAsync(
            string deviceId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<DeviceAssignmentHistoryItem>>([]);
        }
    }

    private sealed class FakeAssignmentTargetRepository : IAssignmentTargetRepository
    {
        public bool ExistsActiveTarget { get; set; } = true;

        public Task<bool> ExistsActiveTargetAsync(
            string merchantId,
            string branchId,
            string registerId,
            string environment,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistsActiveTarget);
        }
    }
}
