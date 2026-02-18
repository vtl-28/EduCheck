// using EduCheck.Infrastructure.Data;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.AspNetCore.TestHost;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection.Extensions;

// namespace EduCheck.IntegrationTests.Fixtures;

// public class EduCheckWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
// {
//     private readonly DatabaseFixture _databaseFixture = new();

//     public async Task InitializeAsync()
//     {
//         await _databaseFixture.InitializeAsync();
//     }

//     public new async Task DisposeAsync()
//     {
//         await _databaseFixture.DisposeAsync();
//         await base.DisposeAsync();
//     }

//     protected override void ConfigureWebHost(IWebHostBuilder builder)
//     {
//         // builder.ConfigureAppConfiguration((context, config) =>
//         // {
//         //     // Clear all existing configuration sources
//         //     // This prevents it from looking for User Secrets
//         //      config.Sources.Clear();

//         //     // Add only what we need for testing
//         //     config.AddInMemoryCollection(new Dictionary<string, string?>
//         //     {
//         //         // ✅ Override database connection with Testcontainers connection
//         //         ["ConnectionStrings:DefaultConnection"] = _databaseFixture.ConnectionString,

//         //         // ✅ JWT Settings (use test values)
//         //         ["JwtSettings:SecretKey"] = "TestSecretKeyThatIsAtLeast32CharactersLongForTesting!",
//         //         ["JwtSettings:Issuer"] = "EduCheck.Tests",
//         //         ["JwtSettings:Audience"] = "EduCheck.Tests",
//         //         ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
//         //         ["JwtSettings:RefreshTokenExpirationDays"] = "7",

//         //         // ✅ OAuth (use dummy values - won't be called in tests)
//         //         ["OAuth:Google:ClientId"] = "test-google-client-id",
//         //         ["OAuth:Google:ClientSecret"] = "test-google-client-secret",

//         //         // ✅ Grafana/OpenTelemetry (disable for tests)
//         //         ["Grafana:OtlpEndpoint"] = "http://localhost:4317",
//         //         ["Grafana:ServiceName"] = "educheck-test",
//         //         ["Grafana:ServiceNamespace"] = "educheck-platform",
//         //         ["Grafana:Protocol"] = "http/protobuf",
//         //         ["Grafana:Headers"] = "",
//         //         ["Grafana:Environment"] = "testing",
//         //     });
//         });

//         builder.ConfigureTestServices(services =>
//         {
//             // Remove the real DbContext registration
//             services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
//             services.RemoveAll<ApplicationDbContext>();

//             // Register DbContext pointing to Testcontainers PostgreSQL
//             services.AddDbContext<ApplicationDbContext>(options =>
//             {
//                 options.UseNpgsql(_databaseFixture.ConnectionString);
//             });
//         });

//         builder.UseEnvironment("Testing");
//         builder.UseEnvironment("DefaultConnection");
//     }

//     /// <summary>
//     /// Creates a fresh DbContext for test assertions
//     /// </summary>
//     public ApplicationDbContext CreateDbContext()
//     {
//         return _databaseFixture.CreateDbContext();
//     }
// }
using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace EduCheck.IntegrationTests.Fixtures;

public class EduCheckWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture = new();

    // ✅ Set env vars BEFORE host is built
    public EduCheckWebApplicationFactory()
    {
        // Database
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _databaseFixture.ConnectionString);

        // JWT
        Environment.SetEnvironmentVariable(
            "JwtSettings__SecretKey",
            "TestSecretKeyThatIsAtLeast32CharactersLongForTesting!");

        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "EduCheck.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "EduCheck.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__RefreshTokenExpirationDays", "7");
    }

    // -----------------------------------------
    // Start/stop test database
    // -----------------------------------------
    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();

        // Update connection string AFTER container starts
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _databaseFixture.ConnectionString);
    }

    public new async Task DisposeAsync()
    {
        await _databaseFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    // -----------------------------------------
    // Configure host
    // -----------------------------------------
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real DbContext
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // ✅ Force EF to use Testcontainers DB
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_databaseFixture.ConnectionString);
            });

            // ✅ Replace cache with no-op so tests always read from real database
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

            // ✅ Reconfigure JWT for test environment
            // Without this, ClaimTypes.NameIdentifier is remapped to "sub" by the
            // JWT middleware, causing userId extraction to return null in the
            // InstitutesController, which means history is never recorded
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var testKey = "TestSecretKeyThatIsAtLeast32CharactersLongForTesting!";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testKey));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "EduCheck.Tests",
                    ValidAudience = "EduCheck.Tests",
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                // Prevents ASP.NET Core from remapping ClaimTypes.NameIdentifier
                // to "sub", which breaks User.FindFirst(ClaimTypes.NameIdentifier)
                options.MapInboundClaims = false;
            });
        });
    }

    // -----------------------------------------
    // Helper for assertions
    // -----------------------------------------
    public ApplicationDbContext CreateDbContext()
    {
        return _databaseFixture.CreateDbContext();
    }
}
