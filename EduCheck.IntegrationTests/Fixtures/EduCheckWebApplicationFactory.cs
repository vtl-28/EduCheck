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
    public EduCheckWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _databaseFixture.ConnectionString);

        Environment.SetEnvironmentVariable(
            "JwtSettings__SecretKey",
            "TestSecretKeyThatIsAtLeast32CharactersLongForTesting!");

        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "EduCheck.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "EduCheck.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__RefreshTokenExpirationDays", "7");
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _databaseFixture.ConnectionString);
    }

    public new async Task DisposeAsync()
    {
        await _databaseFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_databaseFixture.ConnectionString);
            });

            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

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

                options.MapInboundClaims = false;
            });
        });
    }


    public ApplicationDbContext CreateDbContext()
    {
        return _databaseFixture.CreateDbContext();
    }
}
