using C2C.DevicePlatform.Api.Services;
using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Audit;
using C2C.DevicePlatform.Domain.Governance;

namespace C2C.DevicePlatform.Tests;

public sealed class CriticalChangeGuardServiceTests
{
    [Fact]
    public async Task EvaluateAsync_FirstRequest_CreatesPendingDecision()
    {
        var repository = new FakeChangeApprovalRepository();
        var auditRepository = new FakeAuditRepository();
        var auditService = new AuditTrailService(auditRepository, new SensitiveDataMaskingService());
        var guard = new CriticalChangeGuardService(repository, auditService);

        var result = await guard.EvaluateAsync(
            "config.publish",
            "demo",
            "payment.rules",
            "{\"timeout\":30}",
            "user-a",
            CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal("pending", result.Status);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task EvaluateAsync_SecondApprover_AllowsChange()
    {
        var repository = new FakeChangeApprovalRepository();
        var auditRepository = new FakeAuditRepository();
        var auditService = new AuditTrailService(auditRepository, new SensitiveDataMaskingService());
        var guard = new CriticalChangeGuardService(repository, auditService);

        await guard.EvaluateAsync(
            "config.override",
            "qa",
            "dev-001:payment.rules",
            "{\"timeout\":45}",
            "user-a",
            CancellationToken.None);

        var result = await guard.EvaluateAsync(
            "config.override",
            "qa",
            "dev-001:payment.rules",
            "{\"timeout\":45}",
            "user-b",
            CancellationToken.None);

        Assert.True(result.IsApproved);
        Assert.Equal("approved", result.Status);
        Assert.Equal("approved", repository.Items[0].Status);
    }

    [Fact]
    public async Task EvaluateAsync_SameActorAsRequester_Throws()
    {
        var repository = new FakeChangeApprovalRepository();
        var auditRepository = new FakeAuditRepository();
        var auditService = new AuditTrailService(auditRepository, new SensitiveDataMaskingService());
        var guard = new CriticalChangeGuardService(repository, auditService);

        await guard.EvaluateAsync(
            "config.rollback",
            "prod",
            "payment.rules",
            "{\"sourceVersion\":1}",
            "user-a",
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.EvaluateAsync(
            "config.rollback",
            "prod",
            "payment.rules",
            "{\"sourceVersion\":1}",
            "user-a",
            CancellationToken.None));

        Assert.Contains("different user", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeChangeApprovalRepository : IChangeApprovalRepository
    {
        public List<ChangeApproval> Items { get; } = [];

        public Task<ChangeApproval?> GetPendingAsync(
            string actionType,
            string environment,
            string resourceKey,
            string payloadHash,
            CancellationToken cancellationToken)
        {
            var item = Items.FirstOrDefault(entry =>
                entry.ActionType == actionType
                && entry.Environment == environment
                && entry.ResourceKey == resourceKey
                && entry.PayloadHash == payloadHash
                && entry.Status == "pending");

            return Task.FromResult(item);
        }

        public Task<ChangeApproval> CreatePendingAsync(
            string actionType,
            string environment,
            string resourceKey,
            string payloadJson,
            string payloadHash,
            string requestedBy,
            CancellationToken cancellationToken)
        {
            var item = new ChangeApproval
            {
                ApprovalId = Guid.NewGuid(),
                ActionType = actionType,
                Environment = environment,
                ResourceKey = resourceKey,
                PayloadJson = payloadJson,
                PayloadHash = payloadHash,
                RequestedBy = requestedBy,
                Status = "pending",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<ChangeApproval?> ApproveAsync(
            Guid approvalId,
            string approvedBy,
            CancellationToken cancellationToken)
        {
            var item = Items.FirstOrDefault(entry => entry.ApprovalId == approvalId && entry.Status == "pending");
            if (item is null || item.RequestedBy.Equals(approvedBy, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ChangeApproval?>(null);
            }

            Items.Remove(item);
            var approved = new ChangeApproval
            {
                ApprovalId = item.ApprovalId,
                ActionType = item.ActionType,
                Environment = item.Environment,
                ResourceKey = item.ResourceKey,
                PayloadJson = item.PayloadJson,
                PayloadHash = item.PayloadHash,
                RequestedBy = item.RequestedBy,
                ApprovedBy = approvedBy,
                Status = "approved",
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            Items.Add(approved);
            return Task.FromResult<ChangeApproval?>(approved);
        }
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task<AuditEntry> AppendAsync(
            string actor,
            string action,
            string entity,
            string entityId,
            string environment,
            string? metadataJson,
            CancellationToken cancellationToken)
        {
            var entry = new AuditEntry
            {
                AuditId = Entries.Count + 1,
                Actor = actor,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Environment = environment,
                MetadataJson = metadataJson,
                Utc = DateTimeOffset.UtcNow
            };

            Entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task<IReadOnlyCollection<AuditEntry>> GetByEntityAsync(
            string entity,
            string entityId,
            int page,
            int pageSize,
            string? actionFilter,
            CancellationToken cancellationToken)
        {
            var result = Entries
                .Where(item => item.Entity == entity && item.EntityId == entityId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<AuditEntry>>(result);
        }
    }
}
