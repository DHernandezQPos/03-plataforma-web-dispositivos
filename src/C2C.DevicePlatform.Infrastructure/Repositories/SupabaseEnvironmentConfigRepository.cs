using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Configuration;
using Dapper;
using Npgsql;

namespace C2C.DevicePlatform.Infrastructure.Repositories;

public sealed class SupabaseEnvironmentConfigRepository(string connectionString) : IEnvironmentConfigRepository
{
    public async Task<IReadOnlyCollection<EnvironmentConfigTemplate>> GetVersionsAsync(
        string environment,
        string configKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                config_id as ConfigId,
                environment as Environment,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc
            from environment_configs
            where environment = @Environment
              and config_key = @ConfigKey
            order by version desc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<EnvironmentConfigTemplate>(new CommandDefinition(
            sql,
            new
            {
                Environment = environment,
                ConfigKey = configKey
            },
            cancellationToken: cancellationToken));

        return result.ToArray();
    }

    public async Task<EnvironmentConfigTemplate?> GetVersionAsync(
        string environment,
        string configKey,
        int version,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                config_id as ConfigId,
                environment as Environment,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc
            from environment_configs
            where environment = @Environment
              and config_key = @ConfigKey
              and version = @Version;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EnvironmentConfigTemplate>(new CommandDefinition(
            sql,
            new
            {
                Environment = environment,
                ConfigKey = configKey,
                Version = version
            },
            cancellationToken: cancellationToken));
    }

    public async Task<EnvironmentConfigTemplate> CreateNextVersionAsync(
        string environment,
        string configKey,
        string configValueJson,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with next_version as (
                select coalesce(max(version), 0) + 1 as version
                from environment_configs
                where environment = @Environment
                  and config_key = @ConfigKey
            )
            insert into environment_configs
                (environment, config_key, config_value, version, updated_at_utc)
            select
                @Environment,
                @ConfigKey,
                cast(@ConfigValueJson as jsonb),
                next_version.version,
                now()
            from next_version
            returning
                config_id as ConfigId,
                environment as Environment,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<EnvironmentConfigTemplate>(new CommandDefinition(
            sql,
            new
            {
                Environment = environment,
                ConfigKey = configKey,
                ConfigValueJson = configValueJson
            },
            cancellationToken: cancellationToken));
    }

    public async Task<EnvironmentConfigTemplate?> RollbackAsync(
        string environment,
        string configKey,
        int sourceVersion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with source as (
                select config_value
                from environment_configs
                where environment = @Environment
                  and config_key = @ConfigKey
                  and version = @SourceVersion
            ),
            next_version as (
                select coalesce(max(version), 0) + 1 as version
                from environment_configs
                where environment = @Environment
                  and config_key = @ConfigKey
            )
            insert into environment_configs
                (environment, config_key, config_value, version, updated_at_utc)
            select
                @Environment,
                @ConfigKey,
                source.config_value,
                next_version.version,
                now()
            from source
            cross join next_version
            returning
                config_id as ConfigId,
                environment as Environment,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EnvironmentConfigTemplate>(new CommandDefinition(
            sql,
            new
            {
                Environment = environment,
                ConfigKey = configKey,
                SourceVersion = sourceVersion
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<DeviceConfigOverride>> GetDeviceOverridesAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                override_id as OverrideId,
                device_id as DeviceId,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc
            from device_config_overrides
            where device_id = @DeviceId
              and config_key = @ConfigKey
            order by version desc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<DeviceConfigOverride>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                ConfigKey = configKey
            },
            cancellationToken: cancellationToken));

        return result.ToArray();
    }

    public async Task<DeviceConfigOverride> CreateDeviceOverrideAsync(
        string deviceId,
        string configKey,
        string configValueJson,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with next_version as (
                select coalesce(max(version), 0) + 1 as version
                from device_config_overrides
                where device_id = @DeviceId
                  and config_key = @ConfigKey
            )
            insert into device_config_overrides
                (device_id, config_key, config_value, version, updated_at_utc)
            select
                @DeviceId,
                @ConfigKey,
                cast(@ConfigValueJson as jsonb),
                next_version.version,
                now()
            from next_version
            returning
                override_id as OverrideId,
                device_id as DeviceId,
                config_key as ConfigKey,
                cast(config_value as text) as ConfigValueJson,
                version as Version,
                updated_at_utc as UpdatedAtUtc;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<DeviceConfigOverride>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                ConfigKey = configKey,
                ConfigValueJson = configValueJson
            },
            cancellationToken: cancellationToken));
    }

    public async Task<EffectiveDeviceConfig?> GetEffectiveConfigAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with template as (
                select
                    e.config_value,
                    e.version
                from environment_configs e
                where e.environment = (
                    select d.environment
                    from devices d
                    where d.device_id = @DeviceId
                )
                  and e.config_key = @ConfigKey
                order by e.version desc
                limit 1
            ),
            override_version as (
                select
                    o.config_value,
                    o.version
                from device_config_overrides o
                where o.device_id = @DeviceId
                  and o.config_key = @ConfigKey
                order by o.version desc
                limit 1
            )
            select
                d.device_id as DeviceId,
                d.environment as Environment,
                @ConfigKey as ConfigKey,
                cast(coalesce(o.config_value, t.config_value) as text) as EffectiveValueJson,
                t.version as TemplateVersion,
                o.version as OverrideVersion,
                (o.version is not null) as HasOverride
            from devices d
            join template t on true
            left join override_version o on true
            where d.device_id = @DeviceId;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EffectiveDeviceConfig>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                ConfigKey = configKey
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<EffectiveDeviceConfig>> GetEffectiveConfigsAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with latest_template as (
                select distinct on (e.config_key)
                    e.config_key,
                    e.config_value,
                    e.version
                from environment_configs e
                where e.environment = (
                    select d.environment
                    from devices d
                    where d.device_id = @DeviceId
                )
                order by e.config_key, e.version desc
            ),
            latest_override as (
                select distinct on (o.config_key)
                    o.config_key,
                    o.config_value,
                    o.version
                from device_config_overrides o
                where o.device_id = @DeviceId
                order by o.config_key, o.version desc
            )
            select
                d.device_id as DeviceId,
                d.environment as Environment,
                t.config_key as ConfigKey,
                cast(coalesce(o.config_value, t.config_value) as text) as EffectiveValueJson,
                t.version as TemplateVersion,
                o.version as OverrideVersion,
                (o.version is not null) as HasOverride
            from devices d
            join latest_template t on true
            left join latest_override o
                on o.config_key = t.config_key
            where d.device_id = @DeviceId
            order by t.config_key;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryAsync<EffectiveDeviceConfig>(new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId
            },
            cancellationToken: cancellationToken));

        return result.ToArray();
    }
}
