using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Governance;
using Dapper;
using Npgsql;

namespace C2C.DevicePlatform.Infrastructure.Repositories;

public sealed class SupabaseChangeApprovalRepository(string connectionString) : IChangeApprovalRepository
{
    public async Task<ChangeApproval?> GetPendingAsync(
        string actionType,
        string environment,
        string resourceKey,
        string payloadHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                approval_id as ApprovalId,
                action_type as ActionType,
                environment as Environment,
                resource_key as ResourceKey,
                payload_json as PayloadJson,
                payload_hash as PayloadHash,
                requested_by as RequestedBy,
                approved_by as ApprovedBy,
                status as Status,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from change_approvals
            where action_type = @ActionType
              and environment = @Environment
              and resource_key = @ResourceKey
              and payload_hash = @PayloadHash
              and status = 'pending'
            order by created_at_utc desc
            limit 1;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ChangeApproval>(new CommandDefinition(
            sql,
            new
            {
                ActionType = actionType,
                Environment = environment,
                ResourceKey = resourceKey,
                PayloadHash = payloadHash
            },
            cancellationToken: cancellationToken));
    }

    public async Task<ChangeApproval> CreatePendingAsync(
        string actionType,
        string environment,
        string resourceKey,
        string payloadJson,
        string payloadHash,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into change_approvals
                (action_type, environment, resource_key, payload_json, payload_hash, requested_by, approved_by, status, created_at_utc, updated_at_utc)
            values
                (@ActionType, @Environment, @ResourceKey, @PayloadJson, @PayloadHash, @RequestedBy, null, 'pending', now(), now())
            returning
                approval_id as ApprovalId,
                action_type as ActionType,
                environment as Environment,
                resource_key as ResourceKey,
                payload_json as PayloadJson,
                payload_hash as PayloadHash,
                requested_by as RequestedBy,
                approved_by as ApprovedBy,
                status as Status,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<ChangeApproval>(new CommandDefinition(
            sql,
            new
            {
                ActionType = actionType,
                Environment = environment,
                ResourceKey = resourceKey,
                PayloadJson = payloadJson,
                PayloadHash = payloadHash,
                RequestedBy = requestedBy
            },
            cancellationToken: cancellationToken));
    }

    public async Task<ChangeApproval?> ApproveAsync(
        Guid approvalId,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        const string sql = """
            update change_approvals
            set approved_by = @ApprovedBy,
                status = 'approved',
                updated_at_utc = now()
            where approval_id = @ApprovalId
              and status = 'pending'
              and requested_by <> @ApprovedBy
            returning
                approval_id as ApprovalId,
                action_type as ActionType,
                environment as Environment,
                resource_key as ResourceKey,
                payload_json as PayloadJson,
                payload_hash as PayloadHash,
                requested_by as RequestedBy,
                approved_by as ApprovedBy,
                status as Status,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ChangeApproval>(new CommandDefinition(
            sql,
            new
            {
                ApprovalId = approvalId,
                ApprovedBy = approvedBy
            },
            cancellationToken: cancellationToken));
    }
}
