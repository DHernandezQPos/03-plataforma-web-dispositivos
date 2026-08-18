using C2C.DevicePlatform.Application.Repositories;
using Dapper;
using Npgsql;

namespace C2C.DevicePlatform.Infrastructure.Repositories;

public sealed class SupabaseAssignmentTargetRepository(string connectionString) : IAssignmentTargetRepository
{
    public async Task<bool> ExistsActiveTargetAsync(
        string merchantId,
        string branchId,
        string registerId,
        string environment,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select exists (
                select 1
                from organizations o
                join branches b on b.organization_id = o.organization_id
                join registers r on r.branch_id = b.branch_id
                where o.code = @MerchantId
                  and b.code = @BranchId
                  and r.code = @RegisterId
                  and o.environment = @Environment
                  and o.is_active = true
                  and b.is_active = true
                  and r.is_active = true
            );
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new
            {
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId,
                Environment = environment
            },
            cancellationToken: cancellationToken));
    }
}
