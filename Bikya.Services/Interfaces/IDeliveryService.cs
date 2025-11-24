using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.DeliveryDTOs;

namespace Bikya.Services.Interfaces
{
    public interface IDeliveryService
    {
        Task<ApiResponse<AuthResponseDto>> LoginAsync(DeliveryLoginDto loginDto);
        Task<ApiResponse<List<DeliveryOrderDto>>> GetOrdersForDeliveryAsync();
        Task<ApiResponse<DeliveryOrderDto>> GetOrderByIdAsync(int orderId);
        Task<ApiResponse<object>> GetOrderStatusSummaryAsync(int orderId);
        Task<ApiResponse<object>> GetAvailableTransitionsAsync(int orderId);
        Task<ApiResponse<bool>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateDto);
        Task<ApiResponse<bool>> UpdateShippingStatusAsync(int orderId, UpdateDeliveryShippingStatusDto updateDto);
        Task<ApiResponse<bool>> SynchronizeOrderStatusAsync(int orderId);
        Task<ApiResponse<bool>> CreateDeliveryAccountAsync();
    }
}

