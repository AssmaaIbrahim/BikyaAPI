using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets overall dashboard statistics including platform profit calculations
        /// </summary>
        /// <returns>Dashboard statistics with total sales, platform profit, and seller profit</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                _logger.LogInformation("Admin requested dashboard statistics");
                var result = await _dashboardService.GetDashboardStatsAsync();
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard statistics");
                return StatusCode(500, new { message = "Internal server error occurred while fetching dashboard statistics" });
            }
        }

        /// <summary>
        /// Gets dashboard statistics for a specific date range
        /// </summary>
        /// <param name="startDate">Start date for the range</param>
        /// <param name="endDate">End date for the range</param>
        /// <returns>Dashboard statistics for the specified date range</returns>
        [HttpGet("stats/range")]
        public async Task<IActionResult> GetDashboardStatsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate >= endDate)
                {
                    return BadRequest(new { message = "Start date must be before end date" });
                }

                _logger.LogInformation("Admin requested dashboard statistics for date range: {StartDate} to {EndDate}", startDate, endDate);
                var result = await _dashboardService.GetDashboardStatsByDateRangeAsync(startDate, endDate);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard statistics for date range: {StartDate} to {EndDate}", startDate, endDate);
                return StatusCode(500, new { message = "Internal server error occurred while fetching dashboard statistics" });
            }
        }
    }
}
