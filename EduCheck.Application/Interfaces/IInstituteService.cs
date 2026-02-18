using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.Interfaces;

public interface IInstituteService
{
    Task<InstituteSearchResponse> SearchInstitutesAsync(InstituteSearchRequest request, Guid? userId = null);
    Task<InstituteDetailResponse> GetInstituteByIdAsync(int id);
}