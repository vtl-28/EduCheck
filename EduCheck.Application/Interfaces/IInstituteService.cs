using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.Interfaces;

public interface IInstituteService
{
    Task<InstituteSearchResponse> SearchInstitutesAsync(InstituteSearchRequest request);
    Task<InstituteDetailResponse> GetInstituteByIdAsync(int id);
}