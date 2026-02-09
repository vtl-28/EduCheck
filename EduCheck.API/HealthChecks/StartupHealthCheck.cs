using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EduCheck.API.HealthChecks;

/// <summary>
/// Health check that indicates whether the application has completed startup.
/// Used for Kubernetes startup probes.
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady = false;
    private readonly ILogger<StartupHealthCheck> _logger;

    public StartupHealthCheck(ILogger<StartupHealthCheck> logger)
    {
        _logger = logger;
    }

    public void SetReady()
    {
        _isReady = true;
        _logger.LogInformation("Application startup completed - ready to accept traffic");
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_isReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Application has completed startup"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Application is still starting up"));
    }
}