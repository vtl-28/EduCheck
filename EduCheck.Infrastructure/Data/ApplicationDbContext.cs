using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduCheck.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Institute> Institutes => Set<Institute>();
    public DbSet<InstituteSearchHistory> InstituteSearchHistory => Set<InstituteSearchHistory>();
    public DbSet<FavoriteInstitute> FavoriteInstitutes => Set<FavoriteInstitute>();
    public DbSet<FraudReport> FraudReports => Set<FraudReport>();
    public DbSet<FraudReportAction> FraudReportActions => Set<FraudReportAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        ConfigureIdentityTables(modelBuilder);
        ConfigureApplicationUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureStudent(modelBuilder);
        ConfigureAdmin(modelBuilder);
        ConfigureInstitute(modelBuilder);
        ConfigureInstituteSearchHistory(modelBuilder);
        ConfigureFavoriteInstitute(modelBuilder);
        ConfigureFraudReport(modelBuilder);
        ConfigureFraudReportAction(modelBuilder);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        // Rename Identity tables to lowercase with underscores (PostgreSQL convention)
        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
    }

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);

            // One-to-one with Student
            entity.HasOne(e => e.Student)
                .WithOne(s => s.User)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one with Admin
            entity.HasOne(e => e.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false);

            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);

            // Relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureStudent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Province)
                .HasMaxLength(50);

            entity.Property(e => e.City)
                .HasMaxLength(100);

            entity.Property(e => e.Latitude)
                .HasPrecision(10, 8);

            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }

    private static void ConfigureAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("admins");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Department)
                .HasMaxLength(100);

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50);

            entity.Property(e => e.AdminLevel)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AdminLevel.Standard);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.AdminLevel);
        });
    }

    private static void ConfigureInstitute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Institute>(entity =>
        {
            entity.ToTable("institutes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.InstitutionName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.AccreditationNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.AccreditationPeriod)
                .HasMaxLength(100);

            entity.Property(e => e.ProviderType)
                .HasMaxLength(50);

            entity.Property(e => e.Telephone)
                .HasMaxLength(50);

            entity.Property(e => e.Province)
                .HasMaxLength(50);

            entity.Property(e => e.City)
                .HasMaxLength(100);

            entity.Property(e => e.Latitude)
                .HasPrecision(10, 8);

            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.InstitutionName);
            entity.HasIndex(e => e.AccreditationNumber).IsUnique();
            entity.HasIndex(e => e.ProviderType);
            entity.HasIndex(e => e.Province);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureInstituteSearchHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstituteSearchHistory>(entity =>
        {
            entity.ToTable("institute_search_history");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.SearchedAt)
                .HasDefaultValueSql("NOW()");

            // Unique constraint: one entry per student-institute pair
            entity.HasIndex(e => new { e.StudentId, e.InstituteId }).IsUnique();

            // Index for user history queries
            entity.HasIndex(e => new { e.StudentId, e.SearchedAt });

            // Index for cleanup job
            entity.HasIndex(e => e.SearchedAt);

            // Relationships
            entity.HasOne(e => e.Student)
                .WithMany(s => s.SearchHistory)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.SearchHistory)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureFavoriteInstitute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FavoriteInstitute>(entity =>
        {
            entity.ToTable("favorite_institutes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Unique constraint: one favorite per student-institute pair
            entity.HasIndex(e => new { e.StudentId, e.InstituteId }).IsUnique();

            // Index for user favorites list
            entity.HasIndex(e => new { e.StudentId, e.CreatedAt });

            // Relationships
            entity.HasOne(e => e.Student)
                .WithMany(s => s.Favorites)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.Favorites)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureFraudReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FraudReport>(entity =>
        {
            entity.ToTable("fraud_reports");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReportedInstituteName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ReportedInstitutePhone)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(FraudReportStatus.Submitted);

            entity.Property(e => e.Severity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(FraudSeverity.Medium);

            entity.Property(e => e.IsAnonymous)
                .HasDefaultValue(false);

            entity.Property(e => e.EvidenceUrls)
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.StudentId);

            // Relationships
            entity.HasOne(e => e.Student)
                .WithMany(s => s.FraudReports)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.FraudReports)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureFraudReportAction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FraudReportAction>(entity =>
        {
            entity.ToTable("fraud_report_actions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActionType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.PreviousStatus)
                .HasMaxLength(50);

            entity.Property(e => e.NewStatus)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => new { e.FraudReportId, e.CreatedAt });
            entity.HasIndex(e => e.AdminId);

            // Relationships
            entity.HasOne(e => e.FraudReport)
                .WithMany(r => r.Actions)
                .HasForeignKey(e => e.FraudReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Admin)
                .WithMany(a => a.Actions)
                .HasForeignKey(e => e.AdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // Override SaveChanges to auto-update UpdatedAt
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case ApplicationUser user:
                    user.UpdatedAt = DateTime.UtcNow;
                    break;
                case Student student:
                    student.UpdatedAt = DateTime.UtcNow;
                    break;
                case Admin admin:
                    admin.UpdatedAt = DateTime.UtcNow;
                    break;
                case Institute institute:
                    institute.UpdatedAt = DateTime.UtcNow;
                    break;
                case FraudReport report:
                    report.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}