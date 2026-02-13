using System.Threading.RateLimiting;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Identity;
using EduCheck.Infrastructure.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using EduCheck.API.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using EduCheck.API.Configuration;
using EduCheck.API.Metrics;
using EduCheck.API.Middleware;
using OpenTelemetry.Metrics;
using EduCheck.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Logging Configuration with OpenTelemetry
// ============================================
builder.Logging.AddTelemetryLogging(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();

builder.Services.AddTelemetry(builder.Configuration);

builder.Services.AddSingleton<BusinessMetrics>();

// ============================================
// Health Checks Configuration
// ============================================
builder.Services.AddSingleton<StartupHealthCheck>();
builder.Services.AddHealthChecks()
    // Liveness - Is the app running?
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })

    // Database connectivity
    .AddCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" })

    // Memory check
    .AddCheck<MemoryHealthCheck>(
        "memory",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready" })

    // Startup check
    .AddCheck<StartupHealthCheck>(
        "startup",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })

    // PostgreSQL direct check (backup)
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRateLimiter(options =>
{
    // Favorites rate limiter: 20 requests per minute per user
    options.AddPolicy("favorites", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Global rejection handling
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Success = false,
            Message = "Too many requests. Please try again later.",
            Errors = new[] { "Rate limit exceeded. Maximum 20 requests per minute." }
        }, cancellationToken);
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduCheck API",
        Version = "v1",
        Description = "API for verifying South African educational institution accreditation"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// Security Services
// ============================================
builder.Services.AddSecurityServices();
builder.Services.AddSingleton<SecurityMetrics>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EduCheck API v1");
        options.DocExpansion(DocExpansion.None);
    });
}

//app.UseHttpsRedirection();

app.UseAuthentication();

// Only use rate limiter middleware if it was registered
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

app.UseAuthorization();
// ============================================
// Security Monitoring Middleware
// ============================================
app.UseSecurityMonitoring();
app.UseTelemetryEnrichment();
// ============================================
// Health Check Endpoints
// ============================================

// Liveness probe - Is the app running?
// Used by: Kubernetes liveness probe, load balancer
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Readiness probe - Can it accept traffic?
// Used by: Kubernetes readiness probe, load balancer
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Database health - Is the database connected?
// Used by: Monitoring, debugging
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health check - All checks
// Used by: Detailed monitoring
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Mark application as ready after startup
var startupHealthCheck = app.Services.GetRequiredService<StartupHealthCheck>();
startupHealthCheck.SetReady();

// app.MapPrometheusScrapingEndpoint("/metrics");

app.MapControllers();

app.Run();
// Make Program class accessible to integration tests
public partial class Program { }