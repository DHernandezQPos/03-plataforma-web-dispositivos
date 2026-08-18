namespace C2C.DevicePlatform.Domain.Governance;

public sealed class ChangeApproval
{
    public Guid ApprovalId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string ResourceKey { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public string PayloadHash { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public string? ApprovedBy { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
