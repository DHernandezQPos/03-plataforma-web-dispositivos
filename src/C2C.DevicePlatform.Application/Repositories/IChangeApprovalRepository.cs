using C2C.DevicePlatform.Domain.Governance;

namespace C2C.DevicePlatform.Application.Repositories;

public interface IChangeApprovalRepository
{
    Task<ChangeApproval?> GetPendingAsync(
        string actionType,
        string environment,
        string resourceKey,
        string payloadHash,
        CancellationToken cancellationToken);

    Task<ChangeApproval> CreatePendingAsync(
        string actionType,
        string environment,
        string resourceKey,
        string payloadJson,
        string payloadHash,
        string requestedBy,
        CancellationToken cancellationToken);

    Task<ChangeApproval?> ApproveAsync(
        Guid approvalId,
        string approvedBy,
        CancellationToken cancellationToken);
}
