using EduCheck.Application.DTOs.Institute;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduCheck.Tests.Services;

public class InstituteServiceTests
{
    private readonly Mock<ILogger<InstituteService>> _loggerMock;

    public InstituteServiceTests()
    {
        _loggerMock = new Mock<ILogger<InstituteService>>();
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedTestInstitutes(ApplicationDbContext context)
    {
        var institutes = new List<Institute>
        {
            new()
            {
                Id = 1,
                InstitutionName = "University of Cape Town",
                AccreditationNumber = "16 UCT 00001",
                AccreditationPeriod = "01 August 2016",
                ProviderType = "Accredited",
                Province = "Western Cape",
                City = "Cape Town",
                PhysicalAddress = "Rondebosch, Cape Town",
                PostalAddress = "Private Bag X3, Rondebosch 7701",
                Telephone = "021 650 9111",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                InstitutionName = "University of Johannesburg",
                AccreditationNumber = "16 UJ 00002",
                AccreditationPeriod = "01 August 2016",
                ProviderType = "Accredited",
                Province = "Gauteng",
                City = "Johannesburg",
                PhysicalAddress = "Auckland Park, Johannesburg",
                PostalAddress = "PO Box 524, Auckland Park 2006",
                Telephone = "011 559 4555",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                InstitutionName = "Stellenbosch University",
                AccreditationNumber = "16 SU 00003",
                AccreditationPeriod = "01 August 2016",
                ProviderType = "Accredited",
                Province = "Western Cape",
                City = "Stellenbosch",
                PhysicalAddress = "Stellenbosch",
                PostalAddress = "Private Bag X1, Matieland 7602",
                Telephone = "021 808 9111",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                InstitutionName = "Fake College",
                AccreditationNumber = "00 FAKE 00000",
                AccreditationPeriod = "01 January 2020",
                ProviderType = "Not Accredited",
                Province = "Gauteng",
                City = "Pretoria",
                PhysicalAddress = "Unknown",
                PostalAddress = "Unknown",
                Telephone = "000 000 0000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 5,
                InstitutionName = "Inactive University",
                AccreditationNumber = "16 IU 00005",
                AccreditationPeriod = "01 August 2016",
                ProviderType = "Accredited",
                Province = "Gauteng",
                City = "Johannesburg",
                PhysicalAddress = "Johannesburg",
                PostalAddress = "PO Box 000",
                Telephone = "011 000 0000",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Institutes.AddRangeAsync(institutes);
        await context.SaveChangesAsync();
    }

    #region SearchInstitutesAsync Tests

    [Fact]
    public async Task SearchInstitutesAsync_ByName_ReturnsMatchingInstitutes()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "University",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Institutes.Should().HaveCount(3);
        result.Data.Institutes.Should().AllSatisfy(i =>
            i.InstitutionName.ToLower().Should().Contain("university"));
    }

    [Fact]
    public async Task SearchInstitutesAsync_ByName_IsCaseInsensitive()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "UNIVERSITY",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

      
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchInstitutesAsync_ByAccreditationNumber_ReturnsExactMatch()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "16 UCT 00001",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(1);
        result.Data.Institutes.First().InstitutionName.Should().Be("University of Cape Town");
        result.Data.Institutes.First().AccreditationNumber.Should().Be("16 UCT 00001");
    }

    [Fact]
    public async Task SearchInstitutesAsync_ByAccreditationNumber_IsCaseInsensitive()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "16 uct 00001",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(1);
        result.Data.Institutes.First().InstitutionName.Should().Be("University of Cape Town");
    }

    [Fact]
    public async Task SearchInstitutesAsync_WithProvinceFilter_ReturnsFilteredResults()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "University",
            Province = "Western Cape",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

       
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(2);
        result.Data.Institutes.Should().AllSatisfy(i =>
            i.Province.Should().Be("Western Cape"));
    }

    [Fact]
    public async Task SearchInstitutesAsync_WithProvinceFilter_IsCaseInsensitive()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "University",
            Province = "WESTERN CAPE",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchInstitutesAsync_NoResults_ReturnsSuggestions()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "NonExistentInstitute",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Message.Should().Be("No institutes found matching your search");
        result.Data!.Institutes.Should().BeEmpty();
        result.Data.Suggestions.Should().NotBeNull();
        result.Data.Suggestions!.ReportFraud.Should().BeTrue();
        result.Data.Suggestions.FindNearbyInstitutes.Should().BeTrue();
    }

    [Fact]
    public async Task SearchInstitutesAsync_ExcludesInactiveInstitutes()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "Inactive",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchInstitutesAsync_WithPagination_ReturnsCorrectPage()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "University",
            Page = 1,
            PageSize = 2
        };

        
        var result = await service.SearchInstitutesAsync(request);

       
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(2);
        result.Data.Pagination.CurrentPage.Should().Be(1);
        result.Data.Pagination.PageSize.Should().Be(2);
        result.Data.Pagination.TotalCount.Should().Be(3);
        result.Data.Pagination.TotalPages.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchInstitutesAsync_SecondPage_ReturnsRemainingResults()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "University",
            Page = 2,
            PageSize = 2
        };

        
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(1);
        result.Data.Pagination.CurrentPage.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeFalse();
        result.Data.Pagination.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task SearchInstitutesAsync_ReturnsCorrectAccreditationStatus()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "Fake College",
            Page = 1,
            PageSize = 10
        };

       
        var result = await service.SearchInstitutesAsync(request);

        
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(1);
        result.Data.Institutes.First().IsAccredited.Should().BeFalse();
        result.Data.Institutes.First().ProviderType.Should().Be("Not Accredited");
    }

    [Fact]
    public async Task SearchInstitutesAsync_AccreditedInstitute_HasCorrectStatus()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        var request = new InstituteSearchRequest
        {
            Query = "Cape Town",
            Page = 1,
            PageSize = 10
        };

        
        var result = await service.SearchInstitutesAsync(request);

       
        result.Success.Should().BeTrue();
        result.Data!.Institutes.Should().HaveCount(1);
        result.Data.Institutes.First().IsAccredited.Should().BeTrue();
        result.Data.Institutes.First().ProviderType.Should().Be("Accredited");
    }

    #endregion

    #region GetInstituteByIdAsync Tests

    [Fact]
    public async Task GetInstituteByIdAsync_ValidId_ReturnsInstitute()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        
        var result = await service.GetInstituteByIdAsync(1);

        
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Institute found");
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.InstitutionName.Should().Be("University of Cape Town");
        result.Data.AccreditationNumber.Should().Be("16 UCT 00001");
    }

    [Fact]
    public async Task GetInstituteByIdAsync_InvalidId_ReturnsNotFound()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

       
        var result = await service.GetInstituteByIdAsync(999);

        
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Institute not found");
        result.Data.Should().BeNull();
        result.Errors.Should().Contain("No institute found with ID 999");
    }

    [Fact]
    public async Task GetInstituteByIdAsync_InactiveInstitute_ReturnsNotFound()
    {
       
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        
        var result = await service.GetInstituteByIdAsync(5);

        
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Institute not found");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetInstituteByIdAsync_ReturnsAllFields()
    {
        
        await using var context = CreateInMemoryDbContext();
        await SeedTestInstitutes(context);
        var service = new InstituteService(context, _loggerMock.Object);

        
        var result = await service.GetInstituteByIdAsync(1);

       
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.InstitutionName.Should().Be("University of Cape Town");
        result.Data.AccreditationNumber.Should().Be("16 UCT 00001");
        result.Data.AccreditationPeriod.Should().Be("01 August 2016");
        result.Data.ProviderType.Should().Be("Accredited");
        result.Data.Province.Should().Be("Western Cape");
        result.Data.City.Should().Be("Cape Town");
        result.Data.PhysicalAddress.Should().Be("Rondebosch, Cape Town");
        result.Data.PostalAddress.Should().Be("Private Bag X3, Rondebosch 7701");
        result.Data.Telephone.Should().Be("021 650 9111");
        result.Data.IsAccredited.Should().BeTrue();
    }

    #endregion
}