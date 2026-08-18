using C2C.DevicePlatform.Api.Contracts;
using C2C.DevicePlatform.Application.Repositories;
using System.Text;

namespace C2C.DevicePlatform.Api.Services;

public sealed class DeviceCatalogService
{
    private static readonly HashSet<string> AllowedEnvironments = ["demo", "qa", "prod"];
    private static readonly HashSet<string> AllowedStatuses = ["active", "inactive", "maintenance"];
    private readonly IDeviceCatalogRepository repository;
    private readonly IAssignmentTargetRepository assignmentTargetRepository;
    private readonly IEnvironmentConfigRepository? environmentConfigRepository;
    private readonly IAuditRepository? auditRepository;
    private readonly SensitiveDataMaskingService? maskingService;

    public DeviceCatalogService(
        IDeviceCatalogRepository repository,
        IAssignmentTargetRepository assignmentTargetRepository)
        : this(repository, assignmentTargetRepository, null, null, null)
    {
    }

    public DeviceCatalogService(
        IDeviceCatalogRepository repository,
        IAssignmentTargetRepository assignmentTargetRepository,
        IEnvironmentConfigRepository? environmentConfigRepository,
        IAuditRepository? auditRepository,
        SensitiveDataMaskingService? maskingService)
    {
        this.repository = repository;
        this.assignmentTargetRepository = assignmentTargetRepository;
        this.environmentConfigRepository = environmentConfigRepository;
        this.auditRepository = auditRepository;
        this.maskingService = maskingService;
    }

    public async Task<IReadOnlyCollection<DeviceRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await repository.GetAllAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<DeviceRecord?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByDeviceIdAsync(deviceId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<DeviceRecord> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.UpsertAsync(
            request.DeviceId,
            request.MerchantId,
            request.BranchId,
            request.RegisterId,
            request.Environment,
            request.Status,
            cancellationToken);

        return Map(entity);
    }

    public async Task<DeviceRecord?> AssignAsync(string deviceId, AssignDeviceRequest request, CancellationToken cancellationToken)
    {
        var device = await repository.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        var existsActiveTarget = await assignmentTargetRepository.ExistsActiveTargetAsync(
            request.MerchantId,
            request.BranchId,
            request.RegisterId,
            device.Environment,
            cancellationToken);

        if (!existsActiveTarget)
        {
            throw new InvalidOperationException(
                "Assignment target does not exist or is inactive for the specified environment.");
        }

        var entity = await repository.AssignAsync(
            deviceId,
            request.MerchantId,
            request.BranchId,
            request.RegisterId,
            cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<DeviceRecord?> DeactivateAsync(string deviceId, CancellationToken cancellationToken)
    {
        var entity = await repository.DeactivateAsync(deviceId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<DeviceDashboardRecord> GetDashboardAsync(string environment, CancellationToken cancellationToken)
    {
        if (!AllowedEnvironments.Contains(environment.Trim().ToLowerInvariant()))
        {
            throw new InvalidOperationException("Environment must be demo, qa, or prod.");
        }

        var dashboard = await repository.GetEnvironmentDashboardAsync(environment, cancellationToken);
        if (dashboard is null)
        {
            return new DeviceDashboardRecord(environment, 0, 0, 0, 0, 0, null);
        }

        var alertCount = dashboard.InactiveDevices + dashboard.MaintenanceDevices;
        return new DeviceDashboardRecord(
            dashboard.Environment,
            dashboard.TotalDevices,
            dashboard.ActiveDevices,
            dashboard.InactiveDevices,
            dashboard.MaintenanceDevices,
            alertCount,
            dashboard.LastActivityUtc);
    }

    public async Task<DeviceDetailRecord?> GetDetailAsync(
        string deviceId,
        int page,
        int pageSize,
        string? eventFilter,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new InvalidOperationException("DeviceId is required.");
        }

        if (page <= 0)
        {
            throw new InvalidOperationException("Page must be greater than 0.");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            throw new InvalidOperationException("PageSize must be between 1 and 200.");
        }

        var device = await repository.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        var assignments = await repository.GetAssignmentHistoryAsync(deviceId, page, pageSize, cancellationToken);
        var assignmentRecords = assignments
            .Select(Map)
            .ToArray();

        var effectiveConfigRecords = Array.Empty<EffectiveDeviceConfigRecord>();
        if (environmentConfigRepository is not null)
        {
            var effectiveConfigs = await environmentConfigRepository.GetEffectiveConfigsAsync(deviceId, cancellationToken);
            effectiveConfigRecords = effectiveConfigs
                .Select(Map)
                .ToArray();
        }

        var sessionRecords = Array.Empty<AuditRecord>();
        var transactionRecords = Array.Empty<AuditRecord>();
        if (auditRepository is not null)
        {
            var events = await auditRepository.GetByEntityAsync("device", deviceId, page, pageSize, eventFilter, cancellationToken);
            var mapped = events.Select(Map).ToArray();

            sessionRecords = mapped
                .Where(item => item.Action.Contains("session", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            transactionRecords = mapped
                .Where(item =>
                    item.Action.Contains("transaction", StringComparison.OrdinalIgnoreCase)
                    || item.Action.Contains("payment", StringComparison.OrdinalIgnoreCase)
                    || item.Action.Contains("tx", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return new DeviceDetailRecord(
            Map(device),
            assignmentRecords,
            effectiveConfigRecords,
            sessionRecords,
            transactionRecords,
            page,
            pageSize,
            eventFilter);
    }

    public async Task<DeviceImportResult> ImportCsvAsync(Stream csvStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var errors = new List<DeviceImportRowError>();
        var totalRows = 0;
        var processedRows = 0;
        var importedRows = 0;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            totalRows++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = ParseCsvLine(line);
            if (totalRows == 1 && IsHeaderRow(columns))
            {
                continue;
            }

            processedRows++;
            if (columns.Count < 6)
            {
                errors.Add(new DeviceImportRowError(totalRows, string.Empty, "Row must contain 6 columns: DeviceId, MerchantId, BranchId, RegisterId, Environment, Status."));
                continue;
            }

            var deviceId = columns[0].Trim();
            var merchantId = columns[1].Trim();
            var branchId = columns[2].Trim();
            var registerId = columns[3].Trim();
            var environment = columns[4].Trim().ToLowerInvariant();
            var status = string.IsNullOrWhiteSpace(columns[5])
                ? "active"
                : columns[5].Trim().ToLowerInvariant();

            var rowError = ValidateCsvRow(deviceId, merchantId, branchId, registerId, environment, status);
            if (rowError is not null)
            {
                errors.Add(new DeviceImportRowError(totalRows, deviceId, rowError));
                continue;
            }

            try
            {
                await RegisterAsync(
                    new RegisterDeviceRequest(deviceId, merchantId, branchId, registerId, environment, status),
                    cancellationToken);

                importedRows++;
            }
            catch (Exception ex)
            {
                errors.Add(new DeviceImportRowError(totalRows, deviceId, ex.Message));
            }
        }

        return new DeviceImportResult(
            totalRows,
            processedRows,
            importedRows,
            errors.Count,
            errors);
    }

    private static DeviceRecord Map(C2C.DevicePlatform.Domain.Devices.DeviceCatalogItem entity)
    {
        return new DeviceRecord(
            entity.DeviceId,
            entity.MerchantId,
            entity.BranchId,
            entity.RegisterId,
            entity.Environment,
            entity.Status,
            entity.UpdatedAtUtc);
    }

    private static DeviceAssignmentHistoryRecord Map(C2C.DevicePlatform.Domain.Devices.DeviceAssignmentHistoryItem entity)
    {
        return new DeviceAssignmentHistoryRecord(
            entity.AssignmentId,
            entity.DeviceId,
            entity.MerchantId,
            entity.BranchId,
            entity.RegisterId,
            entity.Active,
            entity.AssignedAtUtc);
    }

    private EffectiveDeviceConfigRecord Map(C2C.DevicePlatform.Domain.Configuration.EffectiveDeviceConfig entity)
    {
        return new EffectiveDeviceConfigRecord(
            entity.DeviceId,
            entity.Environment,
            entity.ConfigKey,
            MaskJson(entity.EffectiveValueJson),
            entity.TemplateVersion,
            entity.OverrideVersion,
            entity.HasOverride);
    }

    private AuditRecord Map(C2C.DevicePlatform.Domain.Audit.AuditEntry entity)
    {
        return new AuditRecord(
            entity.AuditId,
            entity.Actor,
            entity.Action,
            entity.Entity,
            entity.EntityId,
            entity.Environment,
            string.IsNullOrWhiteSpace(entity.MetadataJson)
                ? entity.MetadataJson
                : MaskJson(entity.MetadataJson),
            entity.Utc);
    }

    private string MaskJson(string json)
    {
        if (maskingService is null)
        {
            return json;
        }

        return maskingService.MaskJson(json);
    }

    private static string? ValidateCsvRow(
        string deviceId,
        string merchantId,
        string branchId,
        string registerId,
        string environment,
        string status)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "DeviceId is required.";
        }

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return "MerchantId is required.";
        }

        if (string.IsNullOrWhiteSpace(branchId))
        {
            return "BranchId is required.";
        }

        if (string.IsNullOrWhiteSpace(registerId))
        {
            return "RegisterId is required.";
        }

        if (!AllowedEnvironments.Contains(environment))
        {
            return "Environment must be demo, qa, or prod.";
        }

        if (!AllowedStatuses.Contains(status))
        {
            return "Status must be active, inactive, or maintenance.";
        }

        return null;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> columns)
    {
        return columns.Count >= 6
            && columns[0].Trim().Equals("deviceid", StringComparison.OrdinalIgnoreCase)
            && columns[1].Trim().Equals("merchantid", StringComparison.OrdinalIgnoreCase)
            && columns[2].Trim().Equals("branchid", StringComparison.OrdinalIgnoreCase)
            && columns[3].Trim().Equals("registerid", StringComparison.OrdinalIgnoreCase)
            && columns[4].Trim().Equals("environment", StringComparison.OrdinalIgnoreCase)
            && columns[5].Trim().Equals("status", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    builder.Append('"');
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                result.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(character);
        }

        result.Add(builder.ToString());
        return result;
    }
}
