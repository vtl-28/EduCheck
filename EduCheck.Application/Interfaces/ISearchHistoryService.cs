using EduCheck.Application.DTOs.SearchHistory;

namespace EduCheck.Application.Interfaces;

public interface ISearchHistoryService
{
    Task<SearchHistoryResponse> GetUserSearchHistoryAsync(Guid userId, int page = 1, int pageSize = 10);
    Task RecordSearchAsync(Guid userId, int instituteId);
    Task<DeleteSearchHistoryResponse> DeleteSearchHistoryEntryAsync(Guid userId, int historyId);
    Task<DeleteSearchHistoryResponse> ClearUserSearchHistoryAsync(Guid userId);
}