using EduCheck.Application.DTOs;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class NearbyInstituteService : INearbyInstituteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NearbyInstituteService> _logger;

    public NearbyInstituteService(
        ApplicationDbContext context,
        ILogger<NearbyInstituteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<NearbyInstituteDto>> GetNearbyAsync(
        decimal lat, decimal lng, decimal radius, int page = 1, int pageSize = 20)
    {
        // All your nearby logic here
        var institutesWithLocation = await _context.Institutes
            .Where(i => i.Latitude != null && i.Longitude != null && i.IsActive)
            .ToListAsync();

        var nearbyInstitutes = institutesWithLocation
            .Select(institute => new
            {
                Institute = institute,
                Distance = DistanceCalculator.CalculateDistance(
                    lat, lng,
                    institute.Latitude!.Value,
                    institute.Longitude!.Value)
            })
            .Where(x => x.Distance <= radius)
            .OrderBy(x => x.Distance)
            .ToList();

        var totalCount = nearbyInstitutes.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paginatedData = nearbyInstitutes
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NearbyInstituteDto
            {
                Id = x.Institute.Id,
                InstitutionName = x.Institute.InstitutionName,
                ProviderType = x.Institute.ProviderType,
                PhysicalAddress = x.Institute.PhysicalAddress,
                City = x.Institute.City,
                Province = x.Institute.Province,
                Latitude = x.Institute.Latitude!.Value,
                Longitude = x.Institute.Longitude!.Value,
                Distance = Math.Round(x.Distance, 1),
                IsAccredited = x.Institute.IsAccredited
            })
            .ToList();

        return new PaginatedResponse<NearbyInstituteDto>
        {
            Data = paginatedData,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }
}