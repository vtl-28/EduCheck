using EduCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EduCheck.IntegrationTests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("educheck_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Called before any tests run - starts the PostgreSQL container
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        // Get connection string from the running container
        ConnectionString = _postgresContainer.GetConnectionString();

        // Run migrations to create database schema
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
        await SeedRolesAsync(context);

        // Seed test data
        await SeedTestDataAsync(context);

    }

    /// <summary>
    /// Called after all tests finish - stops and removes the container
    /// </summary>
    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a new DbContext connected to the test database
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        context.Roles.AddRange(
            new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Student",
                NormalizedName = "STUDENT"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                NormalizedName = "ADMIN"
            });

        await context.SaveChangesAsync();
    }


    /// <summary>
    /// Seeds initial test data into the database
    /// </summary>
    private async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Clear existing data (in case of re-runs)
        context.Institutes.RemoveRange(context.Institutes);
        await context.SaveChangesAsync();

        // Seed 20 test institutes
        var institutes = TestDataSeeder.CreateTestInstitutes(20);
        await context.Institutes.AddRangeAsync(institutes);

        await context.SaveChangesAsync();
    }
}