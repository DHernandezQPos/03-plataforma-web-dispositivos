using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;

namespace C2C.DevicePlatform.Web.Api;

public sealed class DeviceAdminApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    private const string ClientName = "DevicePlatformApi";

    public async Task<IReadOnlyCollection<DeviceRecordDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync("api/devices", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<DeviceRecordDto>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<DeviceRecordDto> RegisterAsync(RegisterDeviceCommand command, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PostAsJsonAsync("api/devices", command, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadRequiredAsync(response, cancellationToken);
    }

    public async Task<DeviceRecordDto?> AssignAsync(string deviceId, AssignDeviceCommand command, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PutAsJsonAsync($"api/devices/{Uri.EscapeDataString(deviceId)}/assign", command, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync(response, cancellationToken);
    }

    public async Task<DeviceRecordDto?> DeactivateAsync(string deviceId, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.DeleteAsync($"api/devices/{Uri.EscapeDataString(deviceId)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync(response, cancellationToken);
    }

    public async Task<DeviceDashboardDto> GetDashboardAsync(string environment, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync(
            $"api/devices/dashboard/{Uri.EscapeDataString(environment)}",
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<DeviceDashboardDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Dashboard API returned an empty response.");
        }

        return payload;
    }

    public async Task<DeviceDetailDto?> GetDeviceDetailAsync(
        string deviceId,
        int page,
        int pageSize,
        string? eventFilter,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var query = string.IsNullOrWhiteSpace(eventFilter)
            ? $"api/devices/{Uri.EscapeDataString(deviceId)}/detail?page={page}&pageSize={pageSize}"
            : $"api/devices/{Uri.EscapeDataString(deviceId)}/detail?page={page}&pageSize={pageSize}&eventFilter={Uri.EscapeDataString(eventFilter)}";

        var response = await client.GetAsync(query, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<DeviceDetailDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device detail API returned an empty response.");
        }

        return payload;
    }

    public async Task<DeviceExportJobDto> StartDeviceExportAsync(
        StartDeviceExportCommand command,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PostAsJsonAsync("api/devices/exports", command, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<DeviceExportJobDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device export start API returned an empty response.");
        }

        return payload;
    }

    public async Task<DeviceExportJobDto?> GetDeviceExportStatusAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync($"api/devices/exports/{jobId:D}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<DeviceExportJobDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device export status API returned an empty response.");
        }

        return payload;
    }

    public async Task<byte[]?> DownloadDeviceExportAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync($"api/devices/exports/{jobId:D}/download", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<DeviceImportResultDto> ImportCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        using var multipartContent = new MultipartFormDataContent();
        using var streamContent = new StreamContent(csvStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipartContent.Add(streamContent, "file", fileName);

        var response = await client.PostAsync("api/devices/import", multipartContent, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<DeviceImportResultDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device import API returned an empty response.");
        }

        return payload;
    }

    public async Task<IReadOnlyCollection<EnvironmentConfigRecordDto>> GetEnvironmentConfigVersionsAsync(
        string environment,
        string configKey,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync(
            $"api/environment-configs/{Uri.EscapeDataString(environment)}/{Uri.EscapeDataString(configKey)}",
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<EnvironmentConfigRecordDto>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<EnvironmentConfigRecordDto> CreateEnvironmentConfigVersionAsync(
        UpsertEnvironmentConfigCommand command,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PostAsJsonAsync("api/environment-configs", command, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<EnvironmentConfigRecordDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Environment config API returned an empty response.");
        }

        return payload;
    }

    public async Task<DeviceConfigOverrideDto> CreateDeviceOverrideAsync(
        UpsertDeviceOverrideCommand command,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PostAsJsonAsync("api/environment-configs/devices/overrides", command, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<DeviceConfigOverrideDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device override API returned an empty response.");
        }

        return payload;
    }

    public async Task<IReadOnlyCollection<DeviceConfigOverrideDto>> GetDeviceOverridesAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync(
            $"api/environment-configs/devices/{Uri.EscapeDataString(deviceId)}/{Uri.EscapeDataString(configKey)}/overrides",
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<DeviceConfigOverrideDto>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<EffectiveDeviceConfigDto?> GetEffectiveConfigAsync(
        string deviceId,
        string configKey,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.GetAsync(
            $"api/environment-configs/devices/{Uri.EscapeDataString(deviceId)}/{Uri.EscapeDataString(configKey)}/effective",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<EffectiveDeviceConfigDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Effective config API returned an empty response.");
        }

        return payload;
    }

    public async Task<EnvironmentConfigRecordDto?> RollbackEnvironmentConfigAsync(
        string environment,
        string configKey,
        int sourceVersion,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync();
        var response = await client.PostAsJsonAsync(
            $"api/environment-configs/{Uri.EscapeDataString(environment)}/{Uri.EscapeDataString(configKey)}/rollback",
            new RollbackEnvironmentConfigCommand(sourceVersion),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<EnvironmentConfigRecordDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Environment config rollback returned an empty response.");
        }

        return payload;
    }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = null;

        var accessToken = string.Empty;
        if (httpContextAccessor.HttpContext is not null)
        {
            accessToken = await httpContextAccessor.HttpContext.GetTokenAsync("access_token") ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    private static async Task<DeviceRecordDto> ReadRequiredAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadFromJsonAsync<DeviceRecordDto>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Device API returned an empty response.");
        }

        return payload;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Device API call failed ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
    }
}

public sealed class DevicePlatformApiOptions
{
    public string BaseUrl { get; set; } = "https://localhost:7279/";
}

public sealed record RegisterDeviceCommand(
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    string Environment,
    string Status);

public sealed record AssignDeviceCommand(
    string MerchantId,
    string BranchId,
    string RegisterId);

public sealed record DeviceRecordDto(
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    string Environment,
    string Status,
    DateTimeOffset UpdatedAtUtc);

public sealed record DeviceImportResultDto(
    int TotalRows,
    int ProcessedRows,
    int ImportedRows,
    int FailedRows,
    IReadOnlyCollection<DeviceImportRowErrorDto> Errors);

public sealed record DeviceImportRowErrorDto(
    int RowNumber,
    string DeviceId,
    string Error);

public sealed record DeviceDashboardDto(
    string Environment,
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int MaintenanceDevices,
    int AlertCount,
    DateTimeOffset? LastActivityUtc);

public sealed record DeviceAssignmentHistoryDto(
    Guid AssignmentId,
    string DeviceId,
    string MerchantId,
    string BranchId,
    string RegisterId,
    bool Active,
    DateTimeOffset AssignedAtUtc);

public sealed record AuditDto(
    long AuditId,
    string Actor,
    string Action,
    string Entity,
    string EntityId,
    string Environment,
    string? MetadataJson,
    DateTimeOffset Utc);

public sealed record DeviceDetailDto(
    DeviceRecordDto Device,
    IReadOnlyCollection<DeviceAssignmentHistoryDto> Assignments,
    IReadOnlyCollection<EffectiveDeviceConfigDto> EffectiveConfigs,
    IReadOnlyCollection<AuditDto> RecentSessions,
    IReadOnlyCollection<AuditDto> RecentTransactions,
    int Page,
    int PageSize,
    string? EventFilter);

public sealed record StartDeviceExportCommand(
    string? Environment,
    string? Status);

public sealed record DeviceExportJobDto(
    Guid JobId,
    string? Environment,
    string? StatusFilter,
    string Status,
    string? Error,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertEnvironmentConfigCommand(
    string Environment,
    string ConfigKey,
    string ConfigValueJson);

public sealed record RollbackEnvironmentConfigCommand(int SourceVersion);

public sealed record UpsertDeviceOverrideCommand(
    string DeviceId,
    string ConfigKey,
    string ConfigValueJson);

public sealed record EnvironmentConfigRecordDto(
    Guid ConfigId,
    string Environment,
    string ConfigKey,
    string ConfigValueJson,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record DeviceConfigOverrideDto(
    Guid OverrideId,
    string DeviceId,
    string ConfigKey,
    string ConfigValueJson,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record EffectiveDeviceConfigDto(
    string DeviceId,
    string Environment,
    string ConfigKey,
    string EffectiveValueJson,
    int TemplateVersion,
    int? OverrideVersion,
    bool HasOverride);