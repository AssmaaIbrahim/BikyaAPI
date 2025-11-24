using Bikya.Data.Response;
using Bikya.DTOs.Orderdto;
using Bikya.DTOs.ShippingDTOs;
using Bikya.Data.Enums;

namespace Bikya.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDTO>> CreateOrderAsync(CreateOrderDTO dto);
        Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int orderId);
        Task<ApiResponse<List<OrderDTO>>> GetAllOrdersAsync();
          Task<ApiResponse<List<OrderReviewDTO>>> GetOrdersNeedingReviewAsync(int userId);
     
            Task<ApiResponse<OrderDTO>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
        
        /// <summary>
        /// Updates the status of an order using DTO
        /// </summary>
        /// <param name="dto">Order status update data</param>
        /// <returns>ApiResponse indicating success or error</returns>
        Task<ApiResponse<bool>> UpdateOrderStatusAsync(UpdateOrderStatusDTO dto);
        Task<ApiResponse<bool>> DeleteOrderAsync(int orderId);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersByUserIdAsync(int userId);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersByProductIdAsync(int productId);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersByStatusAsync(OrderStatus status);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersBySellerIdAsync(int sellerId);
        Task<ApiResponse<List<OrderDTO>>> GetOrdersByBuyerIdAsync(int buyerId);
        
        /// <summary>
        /// Creates exchange orders for a product swap (creates exactly 2 orders)
        /// </summary>
        /// <param name="exchangeRequestId">The exchange request ID</param>
        /// <returns>List of created orders</returns>
        Task<ApiResponse<List<OrderDTO>>> CreateExchangeOrdersAsync(int exchangeRequestId);
        
        Task<ApiResponse<bool>> CancelOrderAsync(int orderId, int buyerId);
        
        /// <summary>
        /// Updates shipping information for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="dto">Shipping information update data</param>
        /// <returns>ApiResponse indicating success or error</returns>
        Task<ApiResponse<bool>> UpdateShippingInfoAsync(int orderId, ShippingInfoDTO dto);
    }
}
