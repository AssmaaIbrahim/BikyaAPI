using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.UserDTOs;
using Bikya.DTOs.DeliveryDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading;

namespace Bikya.Services.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IOrderRepository _orderRepository;
        private readonly IExchangeRequestRepository _exchangeRequestRepository;
        private readonly IProductRepository _productRepository;

        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IExchangeRequestRepository exchangeRequestRepository,
            ILogger<DeliveryService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _exchangeRequestRepository = exchangeRequestRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(DeliveryLoginDto loginDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", 401);
                }

                var isValidPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password);
                if (!isValidPassword)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", 401);
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Delivery"))
                {
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Access denied. Delivery role required.", 403);
                }

                var token = await _jwtService.GenerateAccessTokenAsync(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    UserName = user.UserName ?? "",
                    UserId = user.Id,
                    Roles = userRoles.ToList()
                };

                return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse($"Login failed: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<DeliveryOrderDto>>> GetOrdersForDeliveryAsync()
        {
            try
            {
                var orders = await _orderRepository.GetAllOrdersWithRelationsAsync();
                
                // Include all orders (including completed ones) for delivery history
                var filteredOrders = orders.Where(o => o.Status == OrderStatus.Paid || 
                                                      o.Status == OrderStatus.Shipped || 
                                                      o.Status == OrderStatus.Completed).ToList();

                var deliveryOrders = new List<DeliveryOrderDto>();
                var processedOrderIds = new HashSet<int>();
          
                foreach (var order in filteredOrders)
                {
                    // Skip if already processed as part of an exchange group
                    if (processedOrderIds.Contains(order.Id))
                        continue;

                    var deliveryOrder = new DeliveryOrderDto
                    {
                        Id = order.Id,
                        ProductName = order.Product?.Title ?? "Unknown Product",
                        ProductId = order.ProductId,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status,
                        CreatedAt = order.CreatedAt,
                        PaidAt = order.PaidAt,
                        BuyerInfo = new UserAddressInfoDto
                        {
                            Id=order.BuyerId,
                            FullName=order.ShippingInfo?.RecipientName ?? order.Buyer?.FullName ?? "",
                            Email= order.Buyer?.Email ?? "",
                            Address=order.ShippingInfo?.Address ?? order.Buyer?.Address ?? "",
                            PhoneNumber= order.Buyer?.PhoneNumber ?? "",
                            PostalCode=order.Buyer?.PostalCode ?? "",
                            City=order.Buyer?.City??""
                        },
                        SellerInfo = new UserAddressInfoDto
                        {
                            Id=order.SellerId,
                            FullName=order.ShippingInfo?.RecipientName ?? order.Seller?.FullName ?? "",
                            Email= order.Seller?.Email ?? "",
                            Address=order.ShippingInfo?.Address ?? order.Seller?.Address ?? "",
                            PhoneNumber= order.Seller?.PhoneNumber ?? "",
                            PostalCode=order.Seller?.PostalCode ?? "",
                            City=order.Seller?.City??""
                        },
                        ShippingStatus = order.ShippingInfo?.Status ?? ShippingStatus.Pending,
                        IsSwapOrder = order.IsSwapOrder,
                    
                    };

                    // Check if this is a swap order and find related order
                    if (order.IsSwapOrder)
                    {

                        // Find the related order in the same exchange
                        //var relatedOrder = filteredOrders.FirstOrDefault(o =>
                        //    o.Id != order.Id &&
                        //    o.IsSwapOrder &&
                        //    o.ProductId != order.ProductId &&
                        //    o.CreatedAt.Date == order.CreatedAt.Date && // Same day exchange
                        //    Math.Abs((o.CreatedAt - order.CreatedAt).TotalMinutes) <= 5); // Within 5 minutes
                        var request = await _exchangeRequestRepository.GetByOrderIdAsync(order.Id);
                        if (request != null)
                        {
                            var relatedOrderId = request.OrderForOfferedProductId == order.Id
                                ? request.OrderForRequestedProductId
                                : request.OrderForOfferedProductId;

                            var relatedOrder = await _orderRepository.GetOrderWithShippingInfoAsync(relatedOrderId.Value);


                            if (relatedOrder != null)
                            {
                                deliveryOrder.RelatedOrderId = relatedOrder.Id;
                                deliveryOrder.ExchangeInfo = $"تبادل مع طلب #{relatedOrder.Id} - {relatedOrder.Product?.Title ?? "Unknown Product"}";
                                deliveryOrder.RelatedProductId = relatedOrder.ProductId;
                                deliveryOrder.RelatedProductTitle = relatedOrder.Product.Title;

                                if (order.Status == OrderStatus.Pending || relatedOrder.Status == OrderStatus.Pending)
                                    continue;

                                // Mark both orders as processed
                                processedOrderIds.Add(order.Id);
                                processedOrderIds.Add(relatedOrder.Id);

                                deliveryOrders.Add(deliveryOrder);
                                continue; // Skip adding the individual order
                            }
                        }
                    }


                    deliveryOrders.Add(deliveryOrder);
                    processedOrderIds.Add(order.Id);
                }

                return ApiResponse<List<DeliveryOrderDto>>.SuccessResponse(deliveryOrders, "Orders retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders for delivery");
                return ApiResponse<List<DeliveryOrderDto>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<DeliveryOrderDto>> GetOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    return ApiResponse<DeliveryOrderDto>.ErrorResponse("Order not found", 404);
                }

                var deliveryOrder = new DeliveryOrderDto
                {
                    Id = order.Id,
                    ProductName = order.Product?.Title ?? "Unknown Product",
                    ProductId = order.ProductId,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    CreatedAt = order.CreatedAt,
                    PaidAt = order.PaidAt,
                    BuyerInfo = new UserAddressInfoDto
                    {
                            Id=order.BuyerId,
                            FullName=order.Buyer?.FullName ?? "",
                            Email= order.Buyer?.Email ?? "",
                            Address= order.Buyer?.Address ?? "",
                            PhoneNumber= order.Buyer?.PhoneNumber ?? "",
                            PostalCode=order.Buyer?.PostalCode ?? "",
                            City=order.Buyer?.City??""
                        },
                    SellerInfo = new UserAddressInfoDto
                    {
                            Id=order.SellerId,
                            FullName= order.Seller?.FullName ?? "",
                            Email= order.Seller?.Email ?? "",
                            Address= order.Seller?.Address ?? "",
                            PhoneNumber= order.Seller?.PhoneNumber ?? "",
                            PostalCode=order.Seller?.PostalCode ?? "",
                            City=order.Seller?.City??""
                        },
                    ShippingStatus = order.ShippingInfo?.Status ?? ShippingStatus.Pending,
                   
                    IsSwapOrder = order.IsSwapOrder,
                    
                };
                
                // Check if this is a swap order and find related order
                if (order.IsSwapOrder)
                {
                    // Find the related order in the same exchange
                    var allOrders = await _orderRepository.GetAllOrdersWithRelationsAsync();
                    //var relatedOrder = allOrders.FirstOrDefault(o => 
                    //    o.Id != order.Id && 
                    //    o.IsSwapOrder && 
                    //    o.ProductId != order.ProductId &&
                    //    o.CreatedAt.Date == order.CreatedAt.Date && // Same day exchange
                    //    Math.Abs((o.CreatedAt - order.CreatedAt).TotalMinutes) <= 5); // Within 5 minutes

                    var request = await _exchangeRequestRepository.GetByOrderIdAsync(order.Id);
                    if (request != null)
                    {
                        var relatedOrderId = request.OrderForOfferedProductId == order.Id
                            ? request.OrderForRequestedProductId
                            : request.OrderForOfferedProductId;

                        var relatedOrder = await _orderRepository.GetOrderWithShippingInfoAsync(relatedOrderId.Value);


                        if (relatedOrder != null)
                        {
                            deliveryOrder.RelatedOrderId = relatedOrder.Id;
                            deliveryOrder.ExchangeInfo = $"تبادل مع طلب #{relatedOrder.Id} - {relatedOrder.Product?.Title ?? "Unknown Product"}";
                            deliveryOrder.RelatedProductId = relatedOrder.ProductId;
                            deliveryOrder.RelatedProductTitle = relatedOrder.Product.Title;
                            // Update product name to show it's an exchange
                            //deliveryOrder.ProductName = $"تبادل: {order.Product?.Title ?? "Unknown Product"} ↔ {relatedOrder.Product?.Title ?? "Unknown Product"}";
                        }
                    }
                }

                return ApiResponse<DeliveryOrderDto>.SuccessResponse(deliveryOrder, "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order {OrderId}", orderId);
                return ApiResponse<DeliveryOrderDto>.ErrorResponse($"Failed to retrieve order: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> GetOrderStatusSummaryAsync(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    return ApiResponse<object>.ErrorResponse("Order not found", 404);
                }

                var statusSummary = new
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status,
                    ShippingStatus = order.ShippingInfo?.Status ?? ShippingStatus.Pending,
                    IsSynchronized = IsStatusSynchronized(order.Status, order.ShippingInfo?.Status),
                    LastUpdated = order.CompletedAt ?? order.PaidAt ?? order.CreatedAt,
                    NextAllowedTransitions = GetNextAllowedTransitions(order.Status, order.ShippingInfo?.Status)
                };

                return ApiResponse<object>.SuccessResponse(statusSummary, "Order status summary retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order status summary {OrderId}", orderId);
                return ApiResponse<object>.ErrorResponse($"Failed to retrieve order status summary: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<bool>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateDto)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                // Validate status transition
                if (!IsValidStatusTransition(order.Status, updateDto.Status))
                {
                    return ApiResponse<bool>.ErrorResponse("Invalid status transition", 400);
                }

                order.Status = updateDto.Status;

                // Update ShippingStatus based on OrderStatus change
                if (order.ShippingInfo != null)
                {
                    switch (updateDto.Status)
                    {
                        case OrderStatus.Shipped:
                            // Only update to InTransit if current status is Pending
                            if (order.ShippingInfo.Status == ShippingStatus.Pending)
                            {
                                order.ShippingInfo.Status = ShippingStatus.InTransit;
                            }
                            break;
                        case OrderStatus.Completed:
                            order.CompletedAt = DateTime.UtcNow;
                            order.ShippingInfo.Status = ShippingStatus.Delivered;
                            break;
                        case OrderStatus.Cancelled:
                            order.ShippingInfo.Status = ShippingStatus.Failed;
                            break;
                    }
                }

                // Additional validation: Ensure shipping status is consistent with order status
                if (order.ShippingInfo != null)
                {
                    var expectedShippingStatus = updateDto.Status switch
                    {
                        OrderStatus.Paid => ShippingStatus.Pending,
                        OrderStatus.Shipped => ShippingStatus.InTransit,
                        OrderStatus.Completed => ShippingStatus.Delivered,
                        OrderStatus.Cancelled => ShippingStatus.Failed,
                        _ => order.ShippingInfo.Status
                    };

                    if (order.ShippingInfo.Status != expectedShippingStatus)
                    {
                        order.ShippingInfo.Status = expectedShippingStatus;
                    }
                }

                // Save changes to database
                await _orderRepository.UpdateAsync(order);
                if (updateDto.Status == OrderStatus.Completed)
                {
                    order.Product.Status = ProductStatus.Sold;
                    _productRepository.Update(order.Product);
                    await _productRepository.SaveChangesAsync();
                }


                _logger.LogInformation("Order {OrderId} status updated successfully to {NewStatus}", orderId, updateDto.Status);
                
                // If this is a swap order and it's being completed, also update the related order
                if (order.IsSwapOrder && updateDto.Status == OrderStatus.Completed)
                {
                    await UpdateRelatedSwapOrderStatusAsync(order);
                }
                
                return ApiResponse<bool>.SuccessResponse(true, "Order status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order {OrderId} status", orderId);
                return ApiResponse<bool>.ErrorResponse($"Failed to update order status: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<bool>> UpdateShippingStatusAsync(int orderId, UpdateDeliveryShippingStatusDto updateDto)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                if (order.ShippingInfo == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order has no shipping information", 400);
                }

                // Validate shipping status transition
                if (!IsValidShippingStatusTransition(order.ShippingInfo.Status, updateDto.Status))
                {
                    return ApiResponse<bool>.ErrorResponse("Invalid shipping status transition", 400);
                }

                // Update shipping status
                order.ShippingInfo.Status = updateDto.Status;

                // Update order status based on shipping status if needed
                switch (updateDto.Status)
                {
                    case ShippingStatus.InTransit:
                        if (order.Status == OrderStatus.Paid)
                        {
                            order.Status = OrderStatus.Shipped;
                        }
                        break;
                    case ShippingStatus.Delivered:
                        order.Status = OrderStatus.Completed;
                        order.CompletedAt = DateTime.UtcNow;
                        break;
                    case ShippingStatus.Failed:
                        order.Status = OrderStatus.Cancelled;
                        break;
                }

                // Additional validation: If order is completed but shipping is not delivered
                if (order.Status == OrderStatus.Completed && order.ShippingInfo?.Status != ShippingStatus.Delivered)
                {
                    order.ShippingInfo.Status = ShippingStatus.Delivered;
                }

                // Additional validation: If shipping is delivered but order is not completed
                if (order.ShippingInfo?.Status == ShippingStatus.Delivered && order.Status != OrderStatus.Completed)
                {
                    order.Status = OrderStatus.Completed;
                    order.CompletedAt = DateTime.UtcNow;
                }

                // Save changes to database
                await _orderRepository.UpdateAsync(order);
                
                // If this is a swap order and shipping is delivered, also update the related order
                if (order.IsSwapOrder && updateDto.Status == ShippingStatus.Delivered)
                {
                    await UpdateRelatedSwapOrderStatusAsync(order);
                }
                
                return ApiResponse<bool>.SuccessResponse(true, "Shipping status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shipping status for order {OrderId}", orderId);
                return ApiResponse<bool>.ErrorResponse($"Failed to update shipping status: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<bool>> CreateDeliveryAccountAsync()
        {
            try
            {
                // Check if Delivery role exists
                var deliveryRole = await _roleManager.FindByNameAsync("Delivery");
                if (deliveryRole == null)
                {
                    deliveryRole = new ApplicationRole
                    {
                        Name = "Delivery",
                        Description = "Delivery personnel role"
                    };
                    await _roleManager.CreateAsync(deliveryRole);
                }

                // Check if delivery user already exists
                var existingUser = await _userManager.FindByEmailAsync("delivery@bikya.com");
                if (existingUser != null)
                {
                    return ApiResponse<bool>.SuccessResponse(true, "Delivery account already exists");
                }

                // Create delivery user
                var deliveryUser = new ApplicationUser
                {
                    UserName = "delivery@bikya.com",
                    Email = "delivery@bikya.com",
                    FullName = "Delivery Personnel",
                    PhoneNumber = "1234567890",
                    Address="Unknown",
                    City="UnKnown",
                    PostalCode="UnKnown",
                    IsVerified = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(deliveryUser, "Delivery@123");
                if (!result.Succeeded)
                {
                    return ApiResponse<bool>.ErrorResponse($"Failed to create delivery account: {string.Join(", ", result.Errors.Select(e => e.Description))}", 400);
                }

                // Assign Delivery role
                await _userManager.AddToRoleAsync(deliveryUser, "Delivery");

                return ApiResponse<bool>.SuccessResponse(true, "Delivery account created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create delivery account");
                return ApiResponse<bool>.ErrorResponse($"Failed to create delivery account: {ex.Message}", 500);
            }
        }

        private async Task UpdateRelatedSwapOrderStatusAsync(Order completedOrder)
        {
            try
            {
                // First, try to find related order using exchange request (most reliable method)
                var exchangeRequest = await _exchangeRequestRepository.GetByOrderIdAsync(completedOrder.Id);
                if (exchangeRequest != null)
                {
                    int? relatedOrderId = null;
                    if (exchangeRequest.OrderForOfferedProductId == completedOrder.Id)
                    {
                        relatedOrderId = exchangeRequest.OrderForRequestedProductId;
                    }
                    else if (exchangeRequest.OrderForRequestedProductId == completedOrder.Id)
                    {
                        relatedOrderId = exchangeRequest.OrderForOfferedProductId;
                    }
                    
                    if (relatedOrderId.HasValue)
                    {
                        // Get the related order to verify it exists and is a swap order
                        var relatedOrder = await _orderRepository.GetOrderWithShippingInfoAsync(relatedOrderId.Value);
                        if (relatedOrder != null && relatedOrder.IsSwapOrder)
                        {
                            // Update the related order status to Completed
                            var success = await _orderRepository.UpdateOrderStatusAsync(relatedOrder.Id, OrderStatus.Completed);
                            if (success)
                            {
                                // Update shipping status if available
                                if (relatedOrder.ShippingInfo != null)
                                {
                                    relatedOrder.ShippingInfo.Status = ShippingStatus.Delivered;
                                    await _orderRepository.UpdateAsync(relatedOrder);
                                }
                                
                                await _orderRepository.SaveChangesAsync();
                            }
                        }
                    }
                }
                else
                {
                    // Fallback: try to find related order using heuristic approach
                    var allOrders = await _orderRepository.GetAllAsync();
                    
                    // Look for related order with more flexible criteria
                    var relatedOrder = allOrders.FirstOrDefault(o => 
                        o.Id != completedOrder.Id && 
                        o.IsSwapOrder && 
                        o.ProductId != completedOrder.ProductId &&
                        o.CreatedAt.Date == completedOrder.CreatedAt.Date && // Same day exchange
                        Math.Abs((o.CreatedAt - completedOrder.CreatedAt).TotalMinutes) <= 10); // Within 10 minutes
                    
                    if (relatedOrder != null)
                    {
                        // Update the related order status to Completed
                        var success = await _orderRepository.UpdateOrderStatusAsync(relatedOrder.Id, OrderStatus.Completed);
                        if (success)
                        {
                            // Update shipping status if available
                            var orderToUpdate = await _orderRepository.GetOrderWithShippingInfoAsync(relatedOrder.Id);
                            if (orderToUpdate?.ShippingInfo != null)
                            {
                                orderToUpdate.ShippingInfo.Status = ShippingStatus.Delivered;
                                await _orderRepository.UpdateAsync(orderToUpdate);
                            }
                            
                            await _orderRepository.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update related swap order status for order {OrderId}", completedOrder.Id);
                // Don't throw the exception to avoid affecting the main order update
            }
        }

        public async Task<ApiResponse<bool>> SynchronizeOrderStatusAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Synchronizing order status for order {OrderId}", orderId);

                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                if (order.ShippingInfo == null)
                {
                    _logger.LogWarning("Order {OrderId} has no shipping info", orderId);
                    return ApiResponse<bool>.ErrorResponse("Order has no shipping information", 400);
                }

                var originalOrderStatus = order.Status;
                var originalShippingStatus = order.ShippingInfo.Status;
                var changesMade = false;

                // Synchronize based on order status
                var expectedShippingStatus = order.Status switch
                {
                    OrderStatus.Paid => ShippingStatus.Pending,
                    OrderStatus.Shipped => ShippingStatus.InTransit,
                    OrderStatus.Completed => ShippingStatus.Delivered,
                    OrderStatus.Cancelled => ShippingStatus.Failed,
                    _ => order.ShippingInfo.Status
                };

                if (order.ShippingInfo.Status != expectedShippingStatus)
                {
                    _logger.LogInformation("Correcting shipping status from {Current} to {Expected} for order {OrderId}", 
                        order.ShippingInfo.Status, expectedShippingStatus, orderId);
                    order.ShippingInfo.Status = expectedShippingStatus;
                    changesMade = true;
                }

                // Synchronize based on shipping status
                var expectedOrderStatus = order.ShippingInfo.Status switch
                {
                    ShippingStatus.Pending => OrderStatus.Paid,
                    ShippingStatus.InTransit => OrderStatus.Shipped,
                    ShippingStatus.Delivered => OrderStatus.Completed,
                    ShippingStatus.Failed => OrderStatus.Cancelled,
                    _ => order.Status
                };

                if (order.Status != expectedOrderStatus)
                {
                    _logger.LogInformation("Correcting order status from {Current} to {Expected} for order {OrderId}", 
                        order.Status, expectedOrderStatus, orderId);
                    order.Status = expectedOrderStatus;
                    
                    if (expectedOrderStatus == OrderStatus.Completed)
                    {
                        order.CompletedAt = DateTime.UtcNow;
                    }
                    
                    changesMade = true;
                }

                if (changesMade)
                {
                    await _orderRepository.UpdateAsync(order);
                    _logger.LogInformation("Order {OrderId} status synchronized successfully", orderId);
                    return ApiResponse<bool>.SuccessResponse(true, "Order status synchronized successfully");
                }
                else
                {
                    _logger.LogInformation("Order {OrderId} status is already synchronized", orderId);
                    return ApiResponse<bool>.SuccessResponse(true, "Order status is already synchronized");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synchronize order status for order {OrderId}", orderId);
                return ApiResponse<bool>.ErrorResponse($"Failed to synchronize order status: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> GetAvailableTransitionsAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Getting available transitions for order {OrderId}", orderId);

                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return ApiResponse<object>.ErrorResponse("Order not found", 404);
                }

                var availableTransitions = new
                {
                    OrderId = order.Id,
                    CurrentOrderStatus = order.Status,
                    CurrentShippingStatus = order.ShippingInfo?.Status ?? ShippingStatus.Pending,
                    OrderStatusTransitions = GetOrderStatusTransitions(order.Status),
                    ShippingStatusTransitions = order.ShippingInfo != null ? GetShippingStatusTransitions(order.ShippingInfo.Status) : new string[0],
                    Recommendations = GetTransitionRecommendations(order.Status, order.ShippingInfo?.Status)
                };

                return ApiResponse<object>.SuccessResponse(availableTransitions, "Available transitions retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available transitions for order {OrderId}", orderId);
                return ApiResponse<object>.ErrorResponse($"Failed to get available transitions: {ex.Message}", 500);
            }
        }

        private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            _logger.LogInformation("Validating status transition: {CurrentStatus} -> {NewStatus}", currentStatus, newStatus);
            
            // Define valid transitions
            var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
            {
                { OrderStatus.Pending, new[] { OrderStatus.Paid, OrderStatus.Cancelled } },
                { OrderStatus.Paid, new[] { OrderStatus.Shipped, OrderStatus.Cancelled } },
                { OrderStatus.Shipped, new[] { OrderStatus.Completed, OrderStatus.Cancelled } },
                { OrderStatus.Completed, new OrderStatus[] { } }, // No further transitions allowed
                { OrderStatus.Cancelled, new OrderStatus[] { } }   // No further transitions allowed
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                _logger.LogWarning("Unknown current status: {CurrentStatus}", currentStatus);
                return false;
            }

            var allowedTransitions = validTransitions[currentStatus];
            var isValid = allowedTransitions.Contains(newStatus);
            
            _logger.LogInformation("Status transition validation result: {IsValid}", isValid);
            if (!isValid)
            {
                _logger.LogWarning("Invalid transition from {CurrentStatus} to {NewStatus}. Allowed transitions: {AllowedTransitions}", 
                    currentStatus, newStatus, string.Join(", ", allowedTransitions));
            }
            
            return isValid;
        }

        private bool IsValidShippingStatusTransition(ShippingStatus currentStatus, ShippingStatus newStatus)
        {
            _logger.LogInformation("Validating shipping status transition: {CurrentStatus} -> {NewStatus}", currentStatus, newStatus);
            
            // Define valid shipping status transitions
            var validTransitions = new Dictionary<ShippingStatus, ShippingStatus[]>
            {
                { ShippingStatus.Pending, new[] { ShippingStatus.InTransit, ShippingStatus.Failed, ShippingStatus.Delivered } }, // Allow direct to Delivered
                { ShippingStatus.InTransit, new[] { ShippingStatus.Delivered, ShippingStatus.Failed, ShippingStatus.Pending } }, // Allow going back to pending
                { ShippingStatus.Delivered, new ShippingStatus[] { } }, // No further transitions allowed
                { ShippingStatus.Failed, new[] { ShippingStatus.Pending, ShippingStatus.Delivered } } // Allow retry from failed
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                _logger.LogWarning("Unknown current shipping status: {CurrentStatus}", currentStatus);
                return false;
            }

            var allowedTransitions = validTransitions[currentStatus];
            var isValid = allowedTransitions.Contains(newStatus);
            
            _logger.LogInformation("Shipping status transition validation result: {IsValid}", isValid);
            if (!isValid)
            {
                _logger.LogWarning("Invalid shipping status transition from {CurrentStatus} to {NewStatus}. Allowed transitions: {AllowedTransitions}", 
                    currentStatus, newStatus, string.Join(", ", allowedTransitions));
            }
            
            return isValid;
        }

        private bool IsStatusSynchronized(OrderStatus orderStatus, ShippingStatus? shippingStatus)
        {
            if (shippingStatus == null) return false;
            
            return (orderStatus, shippingStatus) switch
            {
                (OrderStatus.Paid, ShippingStatus.Pending) => true,
                (OrderStatus.Shipped, ShippingStatus.InTransit) => true,
                (OrderStatus.Completed, ShippingStatus.Delivered) => true,
                (OrderStatus.Cancelled, ShippingStatus.Failed) => true,
                _ => false
            };
        }

        private object GetNextAllowedTransitions(OrderStatus orderStatus, ShippingStatus? shippingStatus)
        {
            var orderTransitions = GetOrderStatusTransitions(orderStatus);
            var shippingTransitions = shippingStatus.HasValue ? GetShippingStatusTransitions(shippingStatus.Value) : new string[0];

            return new
            {
                OrderStatus = orderTransitions,
                ShippingStatus = shippingTransitions
            };
        }

        private string[] GetOrderStatusTransitions(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => new[] { "Paid", "Cancelled" },
                OrderStatus.Paid => new[] { "Shipped", "Cancelled" },
                OrderStatus.Shipped => new[] { "Completed", "Cancelled" },
                _ => new string[0]
            };
        }

        private string[] GetShippingStatusTransitions(ShippingStatus status)
        {
            return status switch
            {
                ShippingStatus.Pending => new[] { "InTransit", "Failed" },
                ShippingStatus.InTransit => new[] { "Delivered", "Failed", "Pending" },
                ShippingStatus.Failed => new[] { "Pending" },
                _ => new string[0]
            };
        }

        private object GetTransitionRecommendations(OrderStatus orderStatus, ShippingStatus? shippingStatus)
        {
            var recommendations = new List<string>();

            if (orderStatus == OrderStatus.Completed && shippingStatus == ShippingStatus.Pending)
            {
                recommendations.Add("Order is completed but shipping is pending. Consider updating shipping status to 'Delivered'");
            }
            else if (orderStatus == OrderStatus.Paid && shippingStatus == ShippingStatus.Pending)
            {
                recommendations.Add("Order is paid. Consider updating shipping status to 'InTransit' or 'Delivered'");
            }
            else if (shippingStatus == ShippingStatus.Delivered && orderStatus != OrderStatus.Completed)
            {
                recommendations.Add("Shipping is delivered. Order status should be 'Completed'");
            }
            else if (shippingStatus == ShippingStatus.Failed && orderStatus != OrderStatus.Cancelled)
            {
                recommendations.Add("Shipping failed. Consider cancelling the order");
            }

            return recommendations;
        }
    }
}

