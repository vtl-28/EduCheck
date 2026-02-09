using Microsoft.Extensions.DependencyInjection;

namespace EduCheck.Infrastructure.Security;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<ISecurityMonitoringService, SecurityMonitoringService>();
        services.AddSingleton<AttackPatternDetector>();

        return services;
    }
}