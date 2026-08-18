using C2C.DevicePlatform.Api.Contracts;
using C2C.DevicePlatform.Application.Repositories;
using System.Text.RegularExpressions;

namespace C2C.DevicePlatform.Api.Services;

public sealed class EnvironmentConfigTemplateService
{
    private static readonly Regex SafeKeyRegex = new("^[a-zA-Z0-9_.:-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedEnvironments = ["demo", "qa", "prod"];
    private readonly IEnvironmentConfigRepository repository;
    private readonly IDeviceCatalogRepository? deviceCatalogRepository;
    private readonly SensitiveDataMaskingService? maskingService;

    public EnvironmentConfigTemplateService(IEnvironmentConfigRepository repository)
        : this(repository, null, null)
    {
    }

    public EnvironmentConfigTemplateService(
        IEnvironmentConfigRepository repository,
        IDeviceCatalogRepository? deviceCatalogRepository,
        SensitiveDataMaskingService? maskingService)
    {
        this.repository = repository;
        this.deviceCatalogRepository = deviceCatalogRepository;
        this.maskingService = maskingService;
    }

    public async Task<IReadOnlyCollection<EnvironmentConfigRecord>> GetVersionsAsync(
        string environment,
        string configKey,
        CancellationToken cancellationToken)
    {
        ValidateEnvironmentAndKey(environment, configKey);
        var result = await repository.GetVersionsAsync(environment, configKey, cancellationToken);
        return result.Select(Map).ToArray();
    }

    public async Task<EnvironmentConfigRecord?> GetVersionAsync(
        string environment,
        string configKey,
        int version,
        CancellationToken cancellationToken)
    {
        ValidateEnvironmentAndKey(environment, configKey);
        if (version <= 0)
        {
            throw new InvalidOperationException("Version must be greater than 0.");
        }

        var result = await repository.GetVersionAsync(environment, configKey, version, cancellationToken);
        return result is null ? null : Map(result);
    }

    public async Task<EnvironmentConfigRecord> CreateNextVersionAsync(
        UpsertEnvironmentConfigRequest request,
        CancellationToken cancellationToken)
    {
        ValidateEnvironmentAndKey(request.Environment, request.ConfigKey);
        ValidateConfigJson(request.ConfigValueJson);

        var result = await repository.CreateNextVersionAsync(
            request.Environment,
            request.ConfigKey,
            request.ConfigValueJson,
            cancellationToken);

        return Map(result);
    }

    public async Task<EnvironmentConfigRecord?> RollbackAsync(
        string environment,
        string configKey,
        int sourceVersion,
        CancellationToken cancellationToken)
    {
        ValidateEnvironmentAndKey(environment, configKey);
        if (sourceVersion <= 0)
        {
            throw new InvalidOperationException("SourceVersion must be greater than 0.");
        }

        var result = await repository.RollbackAsync(environment, configKey, sourceVersion, cancellationToken);
        return result is null ? null : Map(result);
    }

    public async Task<IReadOnlyCollection<DeviceConfigOverrideRecord>> GetDeviceOverridesAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        ValidateDeviceAndKey(deviceId, configKey);
        var result = await repository.GetDeviceOverridesAsync(deviceId, configKey, cancellationToken);
        return result.Select(Map).ToArray();
    }

    public async Task<DeviceConfigOverrideRecord> CreateDeviceOverrideAsync(
        UpsertDeviceOverrideRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDeviceAndKey(request.DeviceId, request.ConfigKey);
        ValidateConfigJson(request.ConfigValueJson);

        if (deviceCatalogRepository is not null)
        {
            var device = await deviceCatalogRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
            if (device is null)
            {
                throw new InvalidOperationException("Device was not found.");
            }
        }

        var result = await repository.CreateDeviceOverrideAsync(
            request.DeviceId,
            request.ConfigKey,
            request.ConfigValueJson,
            cancellationToken);

        return Map(result);
    }

    public async Task<EffectiveDeviceConfigRecord?> GetEffectiveConfigAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        ValidateDeviceAndKey(deviceId, configKey);
        var result = await repository.GetEffectiveConfigAsync(deviceId, configKey, cancellationToken);
        return result is null ? null : Map(result);
    }

    public async Task<IReadOnlyCollection<EffectiveDeviceConfigRecord>> GetEffectiveConfigsAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        ValidateDeviceId(deviceId);
        var result = await repository.GetEffectiveConfigsAsync(deviceId, cancellationToken);
        return result.Select(Map).ToArray();
    }

    private static void ValidateEnvironmentAndKey(string environment, string configKey)
    {
        if (!AllowedEnvironments.Contains(environment.Trim().ToLowerInvariant()))
        {
            throw new InvalidOperationException("Environment must be demo, qa, or prod.");
        }

        ValidateConfigKey(configKey);
    }

    private static void ValidateConfigJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("ConfigValueJson is required.");
        }

        if (value.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ConfigValueJson contains blocked script content.");
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(value);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new InvalidOperationException("ConfigValueJson must be valid JSON.");
        }
    }

    private static void ValidateConfigKey(string configKey)
    {
        if (string.IsNullOrWhiteSpace(configKey))
        {
            throw new InvalidOperationException("ConfigKey is required.");
        }

        if (!SafeKeyRegex.IsMatch(configKey.Trim()))
        {
            throw new InvalidOperationException("ConfigKey contains invalid characters.");
        }
    }

    private static void ValidateDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new InvalidOperationException("DeviceId is required.");
        }
    }

    private static void ValidateDeviceAndKey(string deviceId, string configKey)
    {
        ValidateDeviceId(deviceId);
        ValidateConfigKey(configKey);
    }

    private EnvironmentConfigRecord Map(C2C.DevicePlatform.Domain.Configuration.EnvironmentConfigTemplate entity)
    {
        return new EnvironmentConfigRecord(
            entity.ConfigId,
            entity.Environment,
            entity.ConfigKey,
            Mask(entity.ConfigValueJson),
            entity.Version,
            entity.UpdatedAtUtc);
    }

    private DeviceConfigOverrideRecord Map(C2C.DevicePlatform.Domain.Configuration.DeviceConfigOverride entity)
    {
        return new DeviceConfigOverrideRecord(
            entity.OverrideId,
            entity.DeviceId,
            entity.ConfigKey,
            Mask(entity.ConfigValueJson),
            entity.Version,
            entity.UpdatedAtUtc);
    }

    private EffectiveDeviceConfigRecord Map(C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig entity)
    {
        return new EffectiveDeviceConfigRecord(
            entity.DeviceId,
            entity.Environment,
            entity.ConfigKey,
            Mask(entity.EffectiveValueJson),
            entity.TemplateVersion,
            entity.OverrideVersion,
            entity.HasOverride);
    }

    private string Mask(string json)
    {
        if (maskingService is null)
        {
            return json;
        }

        return maskingService.MaskJson(json);
    }
}
