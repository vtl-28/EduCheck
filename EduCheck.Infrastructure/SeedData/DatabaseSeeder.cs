using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EduCheck.Infrastructure.SeedData;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedInstitutesAsync();
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var roles = new[] { UserRole.Student.ToString(), UserRole.Admin.ToString() };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };

                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {Role}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role already exists: {Role}", roleName);
            }
        }
    }

    private async Task SeedInstitutesAsync()
    {
        if (await _context.Institutes.AnyAsync())
        {
            _logger.LogInformation("Institutes table already has data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding institutes data...");

        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var jsonFilePath = Path.Combine(baseDirectory, "SeedData", "institutes.json");

            if (!File.Exists(jsonFilePath))
            {
                _logger.LogWarning("Seed file not found at: {Path}. Skipping seed.", jsonFilePath);
                _logger.LogWarning("To seed data, add institutes.json to the SeedData folder.");
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            var instituteDtos = JsonSerializer.Deserialize<List<InstituteJsonDto>>(jsonContent);

            if (instituteDtos == null || instituteDtos.Count == 0)
            {
                _logger.LogWarning("No institutes found in seed file.");
                return;
            }

            _logger.LogInformation("Found {Count} institutes in file.", instituteDtos.Count);

            var duplicates = instituteDtos
                .GroupBy(x => x.AccreditationNumber.Trim())
                .Where(g => g.Count() > 1)
                .Select(g => new { AccreditationNumber = g.Key, Count = g.Count() })
                .ToList();

            if (duplicates.Any())
            {
                _logger.LogWarning("Found {Count} duplicate accreditation numbers:", duplicates.Count);
                foreach (var dup in duplicates.Take(10))
                {
                    _logger.LogWarning("  - '{AccreditationNumber}' appears {Count} times",
                        dup.AccreditationNumber, dup.Count);
                }
                if (duplicates.Count > 10)
                {
                    _logger.LogWarning("  ... and {Count} more duplicates", duplicates.Count - 10);
                }
            }

            var uniqueInstitutes = instituteDtos
                .GroupBy(x => x.AccreditationNumber.Trim())
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("After removing duplicates: {Count} unique institutes.", uniqueInstitutes.Count);

            var institutes = uniqueInstitutes.Select(dto => new Institute
            {
                InstitutionName = dto.InstitutionName.Trim(),
                AccreditationNumber = dto.AccreditationNumber.Trim(),
                AccreditationPeriod = dto.AccreditationPeriod?.Trim(),
                ProviderType = dto.ProviderType?.Trim(),
                PostalAddress = dto.PostalAddress?.Trim(),
                PhysicalAddress = dto.PhysicalAddress?.Trim(),
                Telephone = dto.Telephone?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await _context.Institutes.AddRangeAsync(institutes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} institutes.", institutes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding institutes data.");
            throw;
        }
    }
}