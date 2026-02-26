using Microsoft.Extensions.Diagnostics.HealthChecks;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduCheck.API.HealthChecks;

/// <summary>
/// Health check for database connectivity and basic operations.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogError("Database health check failed: Cannot connect to database");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            var startTime = DateTime.UtcNow;
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var data = new Dictionary<string, object>
            {
                { "database", _context.Database.GetDbConnection().Database },
                { "server", _context.Database.GetDbConnection().DataSource },
                { "responseTimeMs", duration.TotalMilliseconds }
            };

            if (duration.TotalMilliseconds > 1000)
            {
                _logger.LogWarning("Database health check: High response time {Duration}ms", duration.TotalMilliseconds);
                return HealthCheckResult.Degraded(
                    $"Database responding slowly ({duration.TotalMilliseconds}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}