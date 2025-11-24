using Bikya.DTOs.DashboardDTOs;
using Bikya.Data.Response;

namespace Bikya.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardStatsDTO>> GetDashboardStatsAsync();
        Task<ApiResponse<DashboardStatsDTO>> GetDashboardStatsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
