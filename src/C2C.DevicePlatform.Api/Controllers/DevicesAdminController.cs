using C2C.DevicePlatform.Api.Contracts;
using C2C.DevicePlatform.Api.Security;
using C2C.DevicePlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C2C.DevicePlatform.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesAdminController(
    DeviceCatalogService deviceCatalogService,
    DeviceExportService exportService,
    AuditTrailService auditTrailService,
    UserEnvironmentScopeService scopeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeviceRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DeviceRecord>>> GetAll(CancellationToken cancellationToken)
    {
        var allowedEnvironments = scopeService.GetAllowedEnvironments(User);
        var devices = await deviceCatalogService.GetAllAsync(cancellationToken);

        var scoped = devices
            .Where(item => allowedEnvironments.Contains(item.Environment, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        return Ok(scoped);
    }

    [HttpGet("{deviceId}")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(DeviceRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceRecord>> GetByDeviceId(string deviceId, CancellationToken cancellationToken)
    {
        var result = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, result.Environment))
        {
            return Forbid();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceRecord>> Register([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId)
            || string.IsNullOrWhiteSpace(request.MerchantId)
            || string.IsNullOrWhiteSpace(request.Environment))
        {
            return BadRequest("DeviceId, MerchantId and Environment are required.");
        }

        if (!scopeService.CanAccessEnvironment(User, request.Environment))
        {
            return Forbid();
        }

        var result = await deviceCatalogService.RegisterAsync(request, cancellationToken);
        var actor = scopeService.GetActor(User);

        await auditTrailService.AppendAsync(
            actor,
            "device.register",
            "device",
            result.DeviceId,
            result.Environment,
            new
            {
                result.MerchantId,
                result.BranchId,
                result.RegisterId,
                result.Status
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("import")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceImportResult>> ImportCsv(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A non-empty CSV file is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await deviceCatalogService.ImportCsvAsync(stream, cancellationToken);

        await auditTrailService.AppendAsync(
            scopeService.GetActor(User),
            "device.import",
            "device-import",
            file.FileName,
            "multi",
            new
            {
                result.TotalRows,
                result.ImportedRows,
                result.FailedRows
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{deviceId}/assign")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceRecord>> Assign(string deviceId, [FromBody] AssignDeviceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantId)
            || string.IsNullOrWhiteSpace(request.BranchId)
            || string.IsNullOrWhiteSpace(request.RegisterId))
        {
            return BadRequest("MerchantId, BranchId and RegisterId are required.");
        }

        var existing = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, existing.Environment))
        {
            return Forbid();
        }

        try
        {
            var result = await deviceCatalogService.AssignAsync(deviceId, request, cancellationToken);
            if (result is null)
            {
                return NotFound();
            }

            await auditTrailService.AppendAsync(
                scopeService.GetActor(User),
                "device.assign",
                "device",
                result.DeviceId,
                result.Environment,
                new
                {
                    result.MerchantId,
                    result.BranchId,
                    result.RegisterId
                },
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{deviceId}")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceRecord>> Deactivate(string deviceId, CancellationToken cancellationToken)
    {
        var existing = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, existing.Environment))
        {
            return Forbid();
        }

        var result = await deviceCatalogService.DeactivateAsync(deviceId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        await auditTrailService.AppendAsync(
            scopeService.GetActor(User),
            "device.deactivate",
            "device",
            result.DeviceId,
            result.Environment,
            new { result.Status },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("dashboard/{environment}")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(DeviceDashboardRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceDashboardRecord>> GetDashboard(
        string environment,
        CancellationToken cancellationToken)
    {
        if (!scopeService.CanAccessEnvironment(User, environment))
        {
            return Forbid();
        }

        try
        {
            var result = await deviceCatalogService.GetDashboardAsync(environment, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{deviceId}/detail")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(DeviceDetailRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceDetailRecord>> GetDetail(
        string deviceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? eventFilter = null,
        CancellationToken cancellationToken = default)
    {
        var current = await deviceCatalogService.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (current is null)
        {
            return NotFound();
        }

        if (!scopeService.CanAccessEnvironment(User, current.Environment))
        {
            return Forbid();
        }

        try
        {
            var result = await deviceCatalogService.GetDetailAsync(deviceId, page, pageSize, eventFilter, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("exports")]
    [Authorize(Policy = PolicyNames.DeviceManage)]
    [ProducesResponseType(typeof(DeviceExportJobRecord), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceExportJobRecord>> StartExport(
        [FromBody] StartDeviceExportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Environment))
        {
            return BadRequest("Environment is required for scoped export.");
        }

        var environment = request.Environment.Trim().ToLowerInvariant();
        if (!scopeService.CanAccessEnvironment(User, environment))
        {
            return Forbid();
        }

        var actor = scopeService.GetActor(User);
        var job = await exportService.StartJobAsync(actor, environment, request.Status, cancellationToken);
        return Accepted(Map(job));
    }

    [HttpGet("exports/{jobId:guid}")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(DeviceExportJobRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceExportJobRecord>> GetExportStatus(Guid jobId)
    {
        var job = await exportService.GetJobAsync(jobId);
        if (job is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(job.Environment)
            && !scopeService.CanAccessEnvironment(User, job.Environment))
        {
            return Forbid();
        }

        return Ok(Map(job));
    }

    [HttpGet("exports/{jobId:guid}/download")]
    [Authorize(Policy = PolicyNames.SupportReadOnly)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadExport(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await exportService.GetJobAsync(jobId);
        if (job is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(job.Environment)
            && !scopeService.CanAccessEnvironment(User, job.Environment))
        {
            return Forbid();
        }

        var content = await exportService.DownloadAsync(jobId, scopeService.GetActor(User), cancellationToken);
        if (content is null)
        {
            return NotFound();
        }

        var filename = $"devices-export-{jobId:N}.csv";
        return File(content, "text/csv", filename);
    }

    private static DeviceExportJobRecord Map(DeviceExportJobState job)
    {
        return new DeviceExportJobRecord(
            job.JobId,
            job.Environment,
            job.StatusFilter,
            job.Status,
            job.Error,
            job.CreatedAtUtc,
            job.UpdatedAtUtc);
    }
}
