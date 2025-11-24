using Bikya.DTOs.Orderdto;
using Bikya.DTOs.ShippingDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bikya.API.Areas.Order
{
    /// <summary>
    /// Controller for managing order operations.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Order")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        // رسائل الأخطاء الموحدة
        private const string InvalidUserIdMessage = "Invalid user ID";
        private const string InvalidBuyerIdMessage = "Invalid buyer ID";
        private const string InvalidSellerIdMessage = "Invalid seller ID";
        private const string InvalidOrderIdMessage = "Invalid order ID";
        private const string InvalidUserTokenMessage = "Invalid user token";

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="dto">Order creation data</param>
        /// <returns>Order creation result</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Guard: for swap orders, avoid double-submits by checking existing order first
            if (dto.IsSwapOrder && dto.ProductId > 0 && dto.BuyerId > 0)
            {
                // Delegate to service which already has idempotency, but short-circuiting here avoids unnecessary errors
            }

            var result = await _orderService.CreateOrderAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates the status of an order. (مثال تحقق من الصلاحية: فقط الأدمن أو البائع)
        /// </summary>
        /// <param name="dto">Order status update data</param>
        /// <returns>Status update result</returns>
        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // مثال تحقق من الصلاحية (يمكن تخصيصه حسب الحاجة)
            // var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            //     return Unauthorized(new { message = InvalidUserTokenMessage });
            // if (!User.IsInRole("Admin") && !UserIsOrderSeller(userId, dto.OrderId))
            //     return Forbid();

            var result = await _orderService.UpdateOrderStatusAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets orders for a specific user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's orders</returns>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = InvalidUserIdMessage });

            var result = await _orderService.GetOrdersByUserIdAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets orders by buyer ID.
        /// </summary>
        /// <param name="buyerId">Buyer ID</param>
        /// <returns>List of buyer's orders</returns>
        [HttpGet("buyer/{buyerId}")]
        public async Task<IActionResult> GetOrdersByBuyer(int buyerId)
        {
            if (buyerId <= 0)
                return BadRequest(new { message = InvalidBuyerIdMessage });

            var result = await _orderService.GetOrdersByBuyerIdAsync(buyerId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets orders by seller ID.
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <returns>List of seller's orders</returns>
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetOrdersBySeller(int sellerId)
        {
            if (sellerId <= 0)
                return BadRequest(new { message = InvalidSellerIdMessage });

            var result = await _orderService.GetOrdersBySellerIdAsync(sellerId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets all orders (Admin only).
        /// </summary>
        /// <returns>List of all orders</returns>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _orderService.GetAllOrdersAsync();
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("ordersForReview")]
        [Authorize]
        public async Task<IActionResult> GetOrdersForReview()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        var result = await _orderService.GetOrdersNeedingReviewAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets an order by ID.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = InvalidOrderIdMessage });

            var result = await _orderService.GetOrderByIdAsync(orderId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cancels an order.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Cancellation result</returns>
        [HttpDelete("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = InvalidOrderIdMessage });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int buyerId))
                return Unauthorized(new { message = InvalidUserTokenMessage });

            var result = await _orderService.CancelOrderAsync(orderId, buyerId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates shipping information for an order.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="dto">Shipping information</param>
        /// <returns>Shipping update result</returns>
        [HttpPut("{orderId}/shipping")]
        public async Task<IActionResult> UpdateShipping(int orderId, [FromBody] ShippingInfoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (orderId <= 0)
                return BadRequest(new { message = InvalidOrderIdMessage });

            var result = await _orderService.UpdateShippingInfoAsync(orderId, dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
