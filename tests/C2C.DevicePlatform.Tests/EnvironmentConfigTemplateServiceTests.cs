using C2C.DevicePlatform.Api.Contracts;
using C2C.DevicePlatform.Api.Services;
using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Domain.Configuration;

namespace C2C.DevicePlatform.Tests;

public sealed class EnvironmentConfigTemplateServiceTests
{
    [Fact]
    public async Task CreateNextVersionAsync_IncrementsVersionPerEnvironmentAndKey()
    {
        var repository = new FakeEnvironmentConfigRepository();
        var service = new EnvironmentConfigTemplateService(repository);

        var first = await service.CreateNextVersionAsync(
            new UpsertEnvironmentConfigRequest("demo", "payment.rules", "{\"timeout\":30}"),
            CancellationToken.None);

        var second = await service.CreateNextVersionAsync(
            new UpsertEnvironmentConfigRequest("demo", "payment.rules", "{\"timeout\":45}"),
            CancellationToken.None);

        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
    }

    [Fact]
    public async Task RollbackAsync_CreatesNewVersionFromSource()
    {
        var repository = new FakeEnvironmentConfigRepository();
        var service = new EnvironmentConfigTemplateService(repository);

        await service.CreateNextVersionAsync(
            new UpsertEnvironmentConfigRequest("qa", "terminal.config", "{\"mode\":\"safe\"}"),
            CancellationToken.None);

        await service.CreateNextVersionAsync(
            new UpsertEnvironmentConfigRequest("qa", "terminal.config", "{\"mode\":\"fast\"}"),
            CancellationToken.None);

        var rollback = await service.RollbackAsync("qa", "terminal.config", 1, CancellationToken.None);

        Assert.NotNull(rollback);
        Assert.Equal(3, rollback!.Version);
        Assert.Equal("{\"mode\":\"safe\"}", rollback.ConfigValueJson);
    }

    private sealed class FakeEnvironmentConfigRepository : IEnvironmentConfigRepository
    {
        private readonly List<EnvironmentConfigTemplate> _data = [];
        private readonly List<C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride> _overrides = [];

        public Task<IReadOnlyCollection<EnvironmentConfigTemplate>> GetVersionsAsync(
            string environment,
            string configKey,
            CancellationToken cancellationToken)
        {
            var result = _data
                .Where(item => item.Environment == environment && item.ConfigKey == configKey)
                .OrderByDescending(item => item.Version)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EnvironmentConfigTemplate>>(result);
        }

        public Task<EnvironmentConfigTemplate?> GetVersionAsync(
            string environment,
            string configKey,
            int version,
            CancellationToken cancellationToken)
        {
            var result = _data.FirstOrDefault(item =>
                item.Environment == environment
                && item.ConfigKey == configKey
                && item.Version == version);

            return Task.FromResult(result);
        }

        public Task<EnvironmentConfigTemplate> CreateNextVersionAsync(
            string environment,
            string configKey,
            string configValueJson,
            CancellationToken cancellationToken)
        {
            var nextVersion = _data
                .Where(item => item.Environment == environment && item.ConfigKey == configKey)
                .Select(item => item.Version)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var entity = new EnvironmentConfigTemplate
            {
                ConfigId = Guid.NewGuid(),
                Environment = environment,
                ConfigKey = configKey,
                ConfigValueJson = configValueJson,
                Version = nextVersion,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _data.Add(entity);
            return Task.FromResult(entity);
        }

        public async Task<EnvironmentConfigTemplate?> RollbackAsync(
            string environment,
            string configKey,
            int sourceVersion,
            CancellationToken cancellationToken)
        {
            var source = _data.FirstOrDefault(item =>
                item.Environment == environment
                && item.ConfigKey == configKey
                && item.Version == sourceVersion);

            if (source is null)
            {
                return null;
            }

            return await CreateNextVersionAsync(environment, configKey, source.ConfigValueJson, cancellationToken);
        }

        public Task<IReadOnlyCollection<C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride>> GetDeviceOverridesAsync(
            string deviceId,
            string configKey,
            CancellationToken cancellationToken)
        {
            var result = _overrides
                .Where(item => item.DeviceId == deviceId && item.ConfigKey == configKey)
                .OrderByDescending(item => item.Version)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride>>(result);
        }

        public Task<C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride> CreateDeviceOverrideAsync(
            string deviceId,
            string configKey,
            string configValueJson,
            CancellationToken cancellationToken)
        {
            var version = _overrides
                .Where(item => item.DeviceId == deviceId && item.ConfigKey == configKey)
                .Select(item => item.Version)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var entity = new C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride
            {
                OverrideId = Guid.NewGuid(),
                DeviceId = deviceId,
                ConfigKey = configKey,
                ConfigValueJson = configValueJson,
                Version = version,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _overrides.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig?> GetEffectiveConfigAsync(
            string deviceId,
            string configKey,
            CancellationToken cancellationToken)
        {
            var template = _data
                .Where(item => item.ConfigKey == configKey)
                .OrderByDescending(item => item.Version)
                .FirstOrDefault();

            if (template is null)
            {
                return Task.FromResult<C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig?>(null);
            }

            var currentOverride = _overrides
                .Where(item => item.DeviceId == deviceId && item.ConfigKey == configKey)
                .OrderByDescending(item => item.Version)
                .FirstOrDefault();

            var effective = new C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig
            {
                DeviceId = deviceId,
                Environment = template.Environment,
                ConfigKey = configKey,
                EffectiveValueJson = currentOverride?.ConfigValueJson ?? template.ConfigValueJson,
                TemplateVersion = template.Version,
                OverrideVersion = currentOverride?.Version,
                HasOverride = currentOverride is not null
            };

            return Task.FromResult<C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig?>(effective);
        }

        public async Task<IReadOnlyCollection<C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig>> GetEffectiveConfigsAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            var keys = _data.Select(item => item.ConfigKey).Distinct(StringComparer.OrdinalIgnoreCase);
            var list = new List<C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig>();

            foreach (var key in keys)
            {
                var effective = await GetEffectiveConfigAsync(deviceId, key, cancellationToken);
                if (effective is not null)
                {
                    list.Add(effective);
                }
            }

            return list;
        }
    }
}
