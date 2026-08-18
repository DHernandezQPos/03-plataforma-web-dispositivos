namespace C2C.DevicePlatform.Application.Repositories;

public interface IAssignmentTargetRepository
{
    Task<bool> ExistsActiveTargetAsync(
        string merchantId,
        string branchId,
        string registerId,
        string environment,
        CancellationToken cancellationToken);
}
