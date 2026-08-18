using C2C.DevicePlatform.Domain.Configuration;

namespace C2C.DevicePlatform.Application.Repositories;

public interface IEnvironmentConfigRepository
{
    Task<IReadOnlyCollection<EnvironmentConfigTemplate>> GetVersionsAsync(
        string environment,
        string configKey,
        CancellationToken cancellationToken);

    Task<EnvironmentConfigTemplate?> GetVersionAsync(
        string environment,
        string configKey,
        int version,
        CancellationToken cancellationToken);

    Task<EnvironmentConfigTemplate> CreateNextVersionAsync(
        string environment,
        string configKey,
        string configValueJson,
        CancellationToken cancellationToken);

    Task<EnvironmentConfigTemplate?> RollbackAsync(
        string environment,
        string configKey,
        int sourceVersion,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DeviceConfigOverride>> GetDeviceOverridesAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken);

    Task<DeviceConfigOverride> CreateDeviceOverrideAsync(
        string deviceId,
        string configKey,
        string configValueJson,
        CancellationToken cancellationToken);

    Task<EffectiveDeviceConfig?> GetEffectiveConfigAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EffectiveDeviceConfig>> GetEffectiveConfigsAsync(
        string deviceId,
        CancellationToken cancellationToken);
}
