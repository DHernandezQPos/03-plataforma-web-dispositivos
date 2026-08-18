using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Devices;
using Dapper;
using Npgsql;

namespace C2C.DevicePlatform.Infrastructure.Repositories;

public sealed class SupabaseDeviceCatalogRepository(string connectionString) : IDeviceCatalogRepository
{
    public async Task<IReadOnlyCollection<DeviceCatalogItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                environment as Environment,
                status as Status,
                updated_at_utc as UpdatedAtUtc
            from devices
            order by device_id;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<DeviceCatalogItem>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result.ToArray();
    }

    public async Task<DeviceCatalogItem?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                environment as Environment,
                status as Status,
                updated_at_utc as UpdatedAtUtc
            from devices
            where device_id = @DeviceId;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DeviceCatalogItem>(new CommandDefinition(
            sql,
            new { DeviceId = deviceId },
            cancellationToken: cancellationToken));
    }

    public async Task<DeviceCatalogItem> UpsertAsync(
        string deviceId,
        string merchantId,
        string branchId,
        string registerId,
        string environment,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into devices
                (device_id, merchant_id, branch_id, register_id, environment, status, updated_at_utc)
            values
                (@DeviceId, @MerchantId, @BranchId, @RegisterId, @Environment, @Status, now())
            on conflict (device_id) do update
            set merchant_id = excluded.merchant_id,
                branch_id = excluded.branch_id,
                register_id = excluded.register_id,
                environment = excluded.environment,
                status = excluded.status,
                updated_at_utc = now()
            returning
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                environment as Environment,
                status as Status,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<DeviceCatalogItem>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId,
                Environment = environment,
                Status = status
            },
            cancellationToken: cancellationToken));
    }

    public async Task<DeviceCatalogItem?> AssignAsync(
        string deviceId,
        string merchantId,
        string branchId,
        string registerId,
        CancellationToken cancellationToken)
    {
        const string updateDeviceSql = """
            update devices
            set
                merchant_id = @MerchantId,
                branch_id = @BranchId,
                register_id = @RegisterId,
                updated_at_utc = now()
            where device_id = @DeviceId
            returning
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                environment as Environment,
                status as Status,
                updated_at_utc as UpdatedAtUtc;
            """;

        const string deactivatePreviousAssignmentsSql = """
            update device_assignments
            set active = false
            where device_id = @DeviceId
              and active = true;
            """;

        const string insertAssignmentSql = """
            insert into device_assignments
                (device_id, merchant_id, branch_id, register_id, active, assigned_at_utc)
            values
                (@DeviceId, @MerchantId, @BranchId, @RegisterId, true, now());
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var updatedDevice = await connection.QuerySingleOrDefaultAsync<DeviceCatalogItem>(new CommandDefinition(
            updateDeviceSql,
            new
            {
                DeviceId = deviceId,
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (updatedDevice is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            deactivatePreviousAssignmentsSql,
            new
            {
                DeviceId = deviceId
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertAssignmentSql,
            new
            {
                DeviceId = deviceId,
                MerchantId = merchantId,
                BranchId = branchId,
                RegisterId = registerId
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return updatedDevice;
    }

    public async Task<DeviceCatalogItem?> DeactivateAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            update devices
            set
                status = 'inactive',
                updated_at_utc = now()
            where device_id = @DeviceId
            returning
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                environment as Environment,
                status as Status,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DeviceCatalogItem>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId
            },
            cancellationToken: cancellationToken));
    }

    public async Task<DeviceEnvironmentDashboard?> GetEnvironmentDashboardAsync(
        string environment,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                environment as Environment,
                count(*) as TotalDevices,
                count(*) filter (where status = 'active') as ActiveDevices,
                count(*) filter (where status = 'inactive') as InactiveDevices,
                count(*) filter (where status = 'maintenance') as MaintenanceDevices,
                max(updated_at_utc) as LastActivityUtc
            from devices
            where environment = @Environment
            group by environment;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DeviceEnvironmentDashboard>(new CommandDefinition(
            sql,
            new
            {
                Environment = environment
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<DeviceAssignmentHistoryItem>> GetAssignmentHistoryAsync(
        string deviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                assignment_id as AssignmentId,
                device_id as DeviceId,
                merchant_id as MerchantId,
                branch_id as BranchId,
                register_id as RegisterId,
                active as Active,
                assigned_at_utc as AssignedAtUtc
            from device_assignments
            where device_id = @DeviceId
            order by assigned_at_utc desc
            offset @OffsetRows
            limit @PageSize;
            """;

        var offsetRows = (page - 1) * pageSize;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<DeviceAssignmentHistoryItem>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                OffsetRows = offsetRows,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken));

        return result.ToArray();
    }
}
