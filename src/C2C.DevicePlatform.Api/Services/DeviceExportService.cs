using System.Collections.Concurrent;
using System.Text;
using C2C.DevicePlatform.Application.Repositories;

namespace C2C.DevicePlatform.Api.Services;

public sealed class DeviceExportService(
    IDeviceCatalogRepository deviceRepository,
    AuditTrailService auditTrailService)
{
    private static readonly ConcurrentDictionary<Guid, DeviceExportJobState> Jobs = new();

    public Task<DeviceExportJobState> StartJobAsync(
        string actor,
        string? environment,
        string? status,
        CancellationToken cancellationToken)
    {
        var job = new DeviceExportJobState
        {
            JobId = Guid.NewGuid(),
            Environment = environment,
            StatusFilter = status,
            Status = "pending",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        Jobs[job.JobId] = job;

        _ = Task.Run(async () =>
        {
            try
            {
                job.Status = "running";
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;

                var devices = await deviceRepository.GetAllAsync(CancellationToken.None);
                var filtered = devices.Where(item =>
                    (string.IsNullOrWhiteSpace(environment) || item.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(status) || item.Status.Equals(status, StringComparison.OrdinalIgnoreCase)));

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("DeviceId,MerchantId,BranchId,RegisterId,Environment,Status,UpdatedAtUtc");

                foreach (var device in filtered)
                {
                    csvBuilder
                        .Append(EscapeCsv(device.DeviceId)).Append(',')
                        .Append(EscapeCsv(device.MerchantId)).Append(',')
                        .Append(EscapeCsv(device.BranchId)).Append(',')
                        .Append(EscapeCsv(device.RegisterId)).Append(',')
                        .Append(EscapeCsv(device.Environment)).Append(',')
                        .Append(EscapeCsv(device.Status)).Append(',')
                        .Append(device.UpdatedAtUtc.UtcDateTime.ToString("O"))
                        .AppendLine();
                }

                job.CsvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                job.Status = "completed";
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;

                await auditTrailService.AppendAsync(
                    actor,
                    "export.completed",
                    "device-export",
                    job.JobId.ToString(),
                    string.IsNullOrWhiteSpace(environment) ? "multi" : environment!,
                    new
                    {
                        status,
                        bytes = job.CsvBytes.Length
                    },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                job.Status = "failed";
                job.Error = ex.Message;
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }, CancellationToken.None);

        return Task.FromResult(job);
    }

    public Task<DeviceExportJobState?> GetJobAsync(Guid jobId)
    {
        Jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public async Task<byte[]?> DownloadAsync(Guid jobId, string actor, CancellationToken cancellationToken)
    {
        var job = await GetJobAsync(jobId);
        if (job is null || !job.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) || job.CsvBytes is null)
        {
            return null;
        }

        await auditTrailService.AppendAsync(
            actor,
            "export.downloaded",
            "device-export",
            jobId.ToString(),
            string.IsNullOrWhiteSpace(job.Environment) ? "multi" : job.Environment!,
            new
            {
                bytes = job.CsvBytes.Length
            },
            cancellationToken);

        return job.CsvBytes;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public sealed class DeviceExportJobState
{
    public Guid JobId { get; init; }
    public string? Environment { get; init; }
    public string? StatusFilter { get; init; }
    public string Status { get; set; } = "pending";
    public string? Error { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public byte[]? CsvBytes { get; set; }
}
