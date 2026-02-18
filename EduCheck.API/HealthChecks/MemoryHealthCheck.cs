using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EduCheck.API.HealthChecks;

/// <summary>
/// Health check for memory usage.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private readonly long _thresholdBytes;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
        // Default threshold: 1GB
        _thresholdBytes = 1024L * 1024L * 1024L;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        var data = new Dictionary<string, object>
        {
            { "allocatedMB", allocatedBytes / 1024 / 1024 },
            { "thresholdMB", _thresholdBytes / 1024 / 1024 },
            { "gen0Collections", gen0Collections },
            { "gen1Collections", gen1Collections },
            { "gen2Collections", gen2Collections }
        };

        if (allocatedBytes >= _thresholdBytes)
        {
            _logger.LogWarning(
                "Memory health check: High memory usage {AllocatedMB}MB (threshold: {ThresholdMB}MB)",
                allocatedBytes / 1024 / 1024,
                _thresholdBytes / 1024 / 1024);

            return Task.FromResult(HealthCheckResult.Degraded(
                $"High memory usage: {allocatedBytes / 1024 / 1024}MB",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage: {allocatedBytes / 1024 / 1024}MB",
            data));
    }
}