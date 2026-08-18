using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Audit;
using Dapper;
using Npgsql;

namespace C2C.DevicePlatform.Infrastructure.Repositories;

public sealed class SupabaseAuditRepository(string connectionString) : IAuditRepository
{
    public async Task<AuditEntry> AppendAsync(
        string actor,
        string action,
        string entity,
        string entityId,
        string environment,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into audit_entries
                (actor, action, entity, entity_id, environment, metadata, utc)
            values
                (@Actor, @Action, @Entity, @EntityId, @Environment, cast(@MetadataJson as jsonb), now())
            returning
                audit_id as AuditId,
                actor as Actor,
                action as Action,
                entity as Entity,
                entity_id as EntityId,
                environment as Environment,
                cast(metadata as text) as MetadataJson,
                utc as Utc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<AuditEntry>(new CommandDefinition(
            sql,
            new
            {
                Actor = actor,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Environment = environment,
                MetadataJson = metadataJson
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<AuditEntry>> GetByEntityAsync(
        string entity,
        string entityId,
        int page,
        int pageSize,
        string? actionFilter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                audit_id as AuditId,
                actor as Actor,
                action as Action,
                entity as Entity,
                entity_id as EntityId,
                environment as Environment,
                cast(metadata as text) as MetadataJson,
                utc as Utc
            from audit_entries
            where entity = @Entity
              and entity_id = @EntityId
              and (@ActionFilter is null or action ilike '%' || @ActionFilter || '%')
            order by utc desc
            offset @OffsetRows
            limit @PageSize;
            """;

        var offsetRows = (page - 1) * pageSize;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<AuditEntry>(new CommandDefinition(
            sql,
            new
            {
                Entity = entity,
                EntityId = entityId,
                ActionFilter = string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
                OffsetRows = offsetRows,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken));

        return result.ToArray();
    }
}
