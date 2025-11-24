using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.DashboardDTOs;
using Bikya.Data.Response;
using Bikya.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bikya.Services.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardService> _logger;
        private readonly decimal _platformFeePercentage;
        private readonly decimal _sellerPercentage;

        public DashboardService(
            IOrderRepository orderRepository,
            IConfiguration configuration,
            ILogger<DashboardService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get platform fee percentage from configuration
            _platformFeePercentage = _configuration.GetValue<decimal>("PlatformSettings:PlatformFeePercentage", 0.15m);
            _sellerPercentage = _configuration.GetValue<decimal>("PlatformSettings:SellerPercentage", 0.85m);

            _logger.LogInformation("DashboardService initialized with PlatformFeePercentage: {PlatformFeePercentage}, SellerPercentage: {SellerPercentage}", 
                _platformFeePercentage, _sellerPercentage);
        }

        public async Task<ApiResponse<DashboardStatsDTO>> GetDashboardStatsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching dashboard statistics");

                var totalSales = await _orderRepository.GetTotalSalesAsync();
                var totalOrders = await _orderRepository.GetTotalOrdersAsync();
                var completedOrders = await _orderRepository.GetOrdersCountByStatusAsync(Bikya.Data.Enums.OrderStatus.Completed);
                var pendingOrders = await _orderRepository.GetOrdersCountByStatusAsync(Bikya.Data.Enums.OrderStatus.Pending);

                // Calculate platform profit as 15% of total sales
                var totalPlatformProfit = totalSales * _platformFeePercentage;
                
                // Calculate seller profit as 85% of total sales
                var totalSellerProfit = totalSales * _sellerPercentage;

                // Calculate average order value
                var averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;

                var stats = new DashboardStatsDTO
                {
                    TotalSales = totalSales,
                    TotalPlatformProfit = totalPlatformProfit,
                    TotalSellerProfit = totalSellerProfit,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    PendingOrders = pendingOrders,
                    AverageOrderValue = averageOrderValue,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Dashboard statistics calculated successfully. TotalSales: {TotalSales}, PlatformProfit: {PlatformProfit}, SellerProfit: {SellerProfit}", 
                    totalSales, totalPlatformProfit, totalSellerProfit);

                return ApiResponse<DashboardStatsDTO>.SuccessResponse(stats, "Dashboard statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard statistics");
                return ApiResponse<DashboardStatsDTO>.ErrorResponse("Failed to retrieve dashboard statistics", 500);
            }
        }

        public async Task<ApiResponse<DashboardStatsDTO>> GetDashboardStatsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Fetching dashboard statistics for date range: {StartDate} to {EndDate}", startDate, endDate);

                var totalSales = await _orderRepository.GetTotalSalesByDateRangeAsync(startDate, endDate);
                var totalOrders = await _orderRepository.GetTotalOrdersByDateRangeAsync(startDate, endDate);
                var completedOrders = await _orderRepository.GetOrdersCountByStatusAndDateRangeAsync(Bikya.Data.Enums.OrderStatus.Completed, startDate, endDate);
                var pendingOrders = await _orderRepository.GetOrdersCountByStatusAndDateRangeAsync(Bikya.Data.Enums.OrderStatus.Pending, startDate, endDate);

                // Calculate platform profit as 15% of total sales
                var totalPlatformProfit = totalSales * _platformFeePercentage;
                
                // Calculate seller profit as 85% of total sales
                var totalSellerProfit = totalSales * _sellerPercentage;

                // Calculate average order value
                var averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;

                var stats = new DashboardStatsDTO
                {
                    TotalSales = totalSales,
                    TotalPlatformProfit = totalPlatformProfit,
                    TotalSellerProfit = totalSellerProfit,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    PendingOrders = pendingOrders,
                    AverageOrderValue = averageOrderValue,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Dashboard statistics for date range calculated successfully. TotalSales: {TotalSales}, PlatformProfit: {PlatformProfit}, SellerProfit: {SellerProfit}", 
                    totalSales, totalPlatformProfit, totalSellerProfit);

                return ApiResponse<DashboardStatsDTO>.SuccessResponse(stats, "Dashboard statistics for date range retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard statistics for date range: {StartDate} to {EndDate}", startDate, endDate);
                return ApiResponse<DashboardStatsDTO>.ErrorResponse("Failed to retrieve dashboard statistics for date range", 500);
            }
        }
    }
}
