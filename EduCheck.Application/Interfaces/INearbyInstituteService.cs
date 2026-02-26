using EduCheck.Application.DTOs;

public interface INearbyInstituteService
{
    Task<PaginatedResponse<NearbyInstituteDto>> GetNearbyAsync(
        decimal lat,
        decimal lng,
        decimal radius,
        int page = 1,
        int pageSize = 20);
}