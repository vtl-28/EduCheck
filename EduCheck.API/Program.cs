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
using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddTelemetryLogging(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();

builder.Services.AddTelemetry(builder.Configuration);

builder.Services.AddSingleton<BusinessMetrics>();

builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("EduCheckCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:4201"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else if (builder.Environment.IsStaging())
        {
            policy
                .WithOrigins(
                    "http://16.170.145.114:4200"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy
                .WithOrigins(
                    "https://educheck.co.za",
                    "https://www.educheck.co.za"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});


builder.Services.AddSingleton<StartupHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })

    .AddCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" })

    .AddCheck<MemoryHealthCheck>(
        "memory",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready" })

    .AddCheck<StartupHealthCheck>(
        "startup",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })

    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRateLimiter(options =>
{
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

app.UseGlobalExceptionHandler();

app.UseCors("EduCheckCors");
app.UseAuthentication();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

app.UseAuthorization();
app.UseSecurityMonitoring();
app.UseTelemetryEnrichment();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

var startupHealthCheck = app.Services.GetRequiredService<StartupHealthCheck>();
startupHealthCheck.SetReady();

app.MapControllers();

app.Run();
public partial class Program { }