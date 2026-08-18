using System.Security.Cryptography;
using System.Text;
using C2C.DevicePlatform.Application.Repositories;

namespace C2C.DevicePlatform.Api.Services;

public sealed class CriticalChangeGuardService(
    IChangeApprovalRepository repository,
    AuditTrailService auditTrailService)
{
    public async Task<CriticalChangeDecision> EvaluateAsync(
        string actionType,
        string environment,
        string resourceKey,
        string payloadJson,
        string actor,
        CancellationToken cancellationToken)
    {
        var payloadHash = ComputeSha256(payloadJson);
        var pending = await repository.GetPendingAsync(
            actionType,
            environment,
            resourceKey,
            payloadHash,
            cancellationToken);

        if (pending is null)
        {
            var created = await repository.CreatePendingAsync(
                actionType,
                environment,
                resourceKey,
                payloadJson,
                payloadHash,
                actor,
                cancellationToken);

            await auditTrailService.AppendAsync(
                actor,
                "approval.pending",
                "critical-change",
                created.ApprovalId.ToString(),
                environment,
                new
                {
                    actionType,
                    resourceKey
                },
                cancellationToken);

            return new CriticalChangeDecision(false, created.ApprovalId, "pending");
        }

        if (pending.RequestedBy.Equals(actor, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Second approval must be completed by a different user.");
        }

        var approved = await repository.ApproveAsync(pending.ApprovalId, actor, cancellationToken);
        if (approved is null)
        {
            throw new InvalidOperationException("Critical change approval could not be completed.");
        }

        await auditTrailService.AppendAsync(
            actor,
            "approval.completed",
            "critical-change",
            approved.ApprovalId.ToString(),
            environment,
            new
            {
                actionType,
                resourceKey,
                requestedBy = pending.RequestedBy
            },
            cancellationToken);

        return new CriticalChangeDecision(true, approved.ApprovalId, "approved");
    }

    private static string ComputeSha256(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

public sealed record CriticalChangeDecision(
    bool IsApproved,
    Guid ApprovalId,
    string Status);
