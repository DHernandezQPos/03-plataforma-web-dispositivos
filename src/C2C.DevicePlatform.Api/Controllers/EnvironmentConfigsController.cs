using C2C.DevicePlatform.Api.Contracts;
using C2C.DevicePlatform.Api.Security;
using C2C.DevicePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace C2C.DevicePlatform.Api.Controllers;

[ApiController]
[Route("api/environment-configs")]
public sealed class EnvironmentConfigsController(
    EnvironmentConfigTemplateService service,
    DeviceCatalogService deviceCatalogService,
    CriticalChangeGuardService criticalChangeGuardService,
    AuditTrailService auditTrailService,
    UserEnvironmentScopeService scopeService) : ControllerBase
{
    [HttpGet("{environment}/{configKey}")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(IReadOnlyCollection<EnvironmentConfigRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<EnvironmentConfigRecord>>> GetVersions(
        string environment,
        string configKey,
        CancellationToken cancellationToken)
    {
        if (!scopeService.CanAccessEnvironment(User, environment))
        {
            return Forbid();
        }

        try
        {
            var result = await service.GetVersionsAsync(environment, configKey, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{environment}/{configKey}/{version:int}")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(EnvironmentConfigRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnvironmentConfigRecord>> GetVersion(
        string environment,
        string configKey,
        int version,
        CancellationToken cancellationToken)
    {
        if (!scopeService.CanAccessEnvironment(User, environment))
        {
            return Forbid();
        }

        try
        {
            var result = await service.GetVersionAsync(environment, configKey, version, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(EnvironmentConfigRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CriticalChangePendingResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnvironmentConfigRecord>> Upsert(
        [FromBody] UpsertEnvironmentConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!scopeService.CanAccessEnvironment(User, request.Environment))
        {
            return Forbid();
        }

        try
        {
            var actor = scopeService.GetActor(User);
            var decision = await criticalChangeGuardService.EvaluateAsync(
                "config.publish",
                request.Environment,
                request.ConfigKey,
                request.ConfigValueJson,
                actor,
                cancellationToken);

            if (!decision.IsApproved)
            {
                return Conflict(new CriticalChangePendingResponse(
                    decision.ApprovalId,
                    decision.Status,
                    "Critical change requires a second approver."));
            }

            var result = await service.CreateNextVersionAsync(request, cancellationToken);

            await auditTrailService.AppendAsync(
                actor,
                "config.publish",
                "environment-config",
                $"{result.Environment}:{result.ConfigKey}:v{result.Version}",
                result.Environment,
                new
                {
                    result.ConfigKey,
                    result.Version
                },
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{environment}/{configKey}/rollback")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(EnvironmentConfigRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CriticalChangePendingResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnvironmentConfigRecord>> Rollback(
        string environment,
        string configKey,
        [FromBody] RollbackEnvironmentConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!scopeService.CanAccessEnvironment(User, environment))
        {
            return Forbid();
        }

        try
        {
            var actor = scopeService.GetActor(User);
            var decision = await criticalChangeGuardService.EvaluateAsync(
                "config.rollback",
                environment,
                configKey,
                JsonSerializer.Serialize(new { request.SourceVersion }),
                actor,
                cancellationToken);

            if (!decision.IsApproved)
            {
                return Conflict(new CriticalChangePendingResponse(
                    decision.ApprovalId,
                    decision.Status,
                    "Rollback requires a second approver."));
            }

            var result = await service.RollbackAsync(environment, configKey, request.SourceVersion, cancellationToken);
            if (result is not null)
            {
                await auditTrailService.AppendAsync(
                    actor,
                    "config.rollback",
                    "environment-config",
                    $"{result.Environment}:{result.ConfigKey}:v{result.Version}",
                    result.Environment,
                    new
                    {
                        request.SourceVersion,
                        result.Version
                    },
                    cancellationToken);
            }

            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("devices/{deviceId}/{configKey}/overrides")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeviceConfigOverrideRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<DeviceConfigOverrideRecord>>> GetDeviceOverrides(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        var device = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, device.Environment))
        {
            return Forbid();
        }

        try
        {
            var result = await service.GetDeviceOverridesAsync(deviceId, configKey, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("devices/{deviceId}/{configKey}/effective")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(EffectiveDeviceConfigRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EffectiveDeviceConfigRecord>> GetEffectiveConfig(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        var device = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, device.Environment))
        {
            return Forbid();
        }

        try
        {
            var result = await service.GetEffectiveConfigAsync(deviceId, configKey, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("devices/overrides")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceConfigOverrideRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CriticalChangePendingResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceConfigOverrideRecord>> UpsertDeviceOverride(
        [FromBody] UpsertDeviceOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var device = await deviceCatalogService.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (device is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, device.Environment))
        {
            return Forbid();
        }

        try
        {
            var actor = scopeService.GetActor(User);
            var decision = await criticalChangeGuardService.EvaluateAsync(
                "config.override",
                device.Environment,
                $"{request.DeviceId}:{request.ConfigKey}",
                request.ConfigValueJson,
                actor,
                cancellationToken);

            if (!decision.IsApproved)
            {
                return Conflict(new CriticalChangePendingResponse(
                    decision.ApprovalId,
                    decision.Status,
                    "Device override requires a second approver."));
            }

            var result = await service.CreateDeviceOverrideAsync(request, cancellationToken);

            await auditTrailService.AppendAsync(
                actor,
                "config.override",
                "device-config-override",
                $"{request.DeviceId}:{request.ConfigKey}:v{result.Version}",
                device.Environment,
                new
                {
                    request.DeviceId,
                    request.ConfigKey,
                    result.Version
                },
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
