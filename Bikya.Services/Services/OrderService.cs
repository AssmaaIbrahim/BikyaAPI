using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.Orderdto;
using Bikya.DTOs.ShippingDTOs;
using Bikya.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bikya.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IShippingServiceRepository _shippingInfoRepository;
        private readonly IExchangeRequestRepository _exchangeRequestRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IWishlistRepository wishlistRepository,
            IShippingServiceRepository shippingInfoRepository,
            IExchangeRequestRepository exchangeRequestRepository,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _wishlistRepository = wishlistRepository ?? throw new ArgumentNullException(nameof(wishlistRepository));
            _shippingInfoRepository = shippingInfoRepository ?? throw new ArgumentNullException(nameof(shippingInfoRepository));
            _exchangeRequestRepository = exchangeRequestRepository ?? throw new ArgumentNullException(nameof(exchangeRequestRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new order for a product and buyer.
        /// </summary>
        /// <param name="dto">Order creation data</param>
        /// <returns>OrderDTO with details or error</returns>
        public async Task<ApiResponse<OrderDTO>> CreateOrderAsync(CreateOrderDTO dto)
        {
            _logger.LogInformation("Starting order creation for product {ProductId} by buyer {BuyerId}", dto.ProductId, dto.BuyerId);
            
            try
            {
                // Validate input
                if (dto == null)
                {
                    _logger.LogError("Order creation failed: DTO is null");
                    return ApiResponse<OrderDTO>.ErrorResponse("Order data is required", 400);
                }

                // Validate shipping info for non-swap orders
                if (dto.ShippingInfo == null && !dto.IsSwapOrder)
                {
                    _logger.LogError("Order creation failed: Shipping info is required for non-swap orders");
                    return ApiResponse<OrderDTO>.ErrorResponse("Shipping information is required", 400);
                }

                // Get product with tracking
                var product = await _productRepository.GetByIdAsync(dto.ProductId);
                if (product == null)
                {
                    _logger.LogError("Order creation failed: Product {ProductId} not found", dto.ProductId);
                    return ApiResponse<OrderDTO>.ErrorResponse("Product not found", 404);
                }

                // Get seller info
                var seller = await _userRepository.FindByIdAsync(product.UserId ?? 0);
                if (seller == null)
                {
                    _logger.LogError("Order creation failed: Seller for product {ProductId} not found", dto.ProductId);
                    return ApiResponse<OrderDTO>.ErrorResponse("Seller not found", 404);
                }

                // Idempotency: if a pending swap order already exists for this product/buyer, reuse it
                var alreadyExistingOrder = await _orderRepository.GetByProductAndBuyerAsync(dto.ProductId, dto.BuyerId);
                if (alreadyExistingOrder != null && alreadyExistingOrder.Status == OrderStatus.Pending && (alreadyExistingOrder.IsSwapOrder || dto.IsSwapOrder || product.Status == Data.Enums.ProductStatus.Trading))
                {
                    _logger.LogInformation("Found existing pending swap order {OrderId} for product {ProductId} and buyer {BuyerId}. Returning existing order instead of creating a new one.", alreadyExistingOrder.Id, dto.ProductId, dto.BuyerId);

                    return ApiResponse<OrderDTO>.SuccessResponse(new OrderDTO
                    {
                        Id = alreadyExistingOrder.Id,
                        ProductId = alreadyExistingOrder.ProductId,
                        ProductTitle = product.Title ?? "Unknown Product",
                        BuyerId = alreadyExistingOrder.BuyerId,
                        BuyerName = seller.FullName ?? "Unknown Buyer",
                        SellerId = alreadyExistingOrder.SellerId,
                        SellerName = seller.FullName ?? "Unknown Seller",
                        TotalAmount = alreadyExistingOrder.TotalAmount,
                        PlatformFee = alreadyExistingOrder.PlatformFee,
                        SellerAmount = alreadyExistingOrder.SellerAmount,
                        Status = alreadyExistingOrder.Status.ToString(),
                        CreatedAt = alreadyExistingOrder.CreatedAt,
                        IsSwapOrder = alreadyExistingOrder.IsSwapOrder,
                        ShippingInfo = alreadyExistingOrder.ShippingInfo != null ? new ShippingInfoDTO
                        {
                            RecipientName = alreadyExistingOrder.ShippingInfo.RecipientName,
                            Address = alreadyExistingOrder.ShippingInfo.Address,
                            City = alreadyExistingOrder.ShippingInfo.City,
                            PostalCode = alreadyExistingOrder.ShippingInfo.PostalCode,
                            PhoneNumber = alreadyExistingOrder.ShippingInfo.PhoneNumber,
                            ShippingFee = alreadyExistingOrder.ShippingInfo.ShippingFee,
                            Status = alreadyExistingOrder.ShippingInfo.Status.ToString()
                        } : null
                    }, "Existing order returned");
                }

                // Check if this is a swap order (no product price, only shipping fee)
                bool isSwapOrder = dto.IsSwapOrder || product.Status == Data.Enums.ProductStatus.Trading;

                // Idempotency for swap orders: ensure only one order per (ProductId, BuyerId) exists
                if (isSwapOrder)
                {
                    var existingOrder = await _orderRepository.GetByProductAndBuyerAsync(dto.ProductId, dto.BuyerId);
                    if (existingOrder != null)
                    {
                        _logger.LogInformation(
                            "Found existing swap order {OrderId} for product {ProductId} and buyer {BuyerId}. Returning existing order.",
                            existingOrder.Id, dto.ProductId, dto.BuyerId);

                        // If shipping info provided, upsert into existing order's shipping info
                        if (dto.ShippingInfo != null)
                        {
                            if (existingOrder.ShippingInfo == null)
                            {
                                existingOrder.ShippingInfo = new ShippingInfo
                                {
                                    RecipientName = dto.ShippingInfo.RecipientName,
                                    Address = dto.ShippingInfo.Address,
                                    City = dto.ShippingInfo.City,
                                    PostalCode = dto.ShippingInfo.PostalCode,
                                    PhoneNumber = dto.ShippingInfo.PhoneNumber,
                                    Status = ShippingStatus.Pending,
                                    ShippingFee = 50.0m,
                                    CreateAt = DateTime.UtcNow,
                                    ShippingMethod = "Standard"
                                };
                            }
                            else
                            {
                                existingOrder.ShippingInfo.RecipientName = dto.ShippingInfo.RecipientName;
                                existingOrder.ShippingInfo.Address = dto.ShippingInfo.Address;
                                existingOrder.ShippingInfo.City = dto.ShippingInfo.City;
                                existingOrder.ShippingInfo.PostalCode = dto.ShippingInfo.PostalCode;
                                existingOrder.ShippingInfo.PhoneNumber = dto.ShippingInfo.PhoneNumber;
                            }

                            _orderRepository.Update(existingOrder);
                            await _orderRepository.SaveChangesAsync();
                        }

                        return ApiResponse<OrderDTO>.SuccessResponse(new OrderDTO
                        {
                            Id = existingOrder.Id,
                            ProductId = product.Id,
                            ProductTitle = product.Title ?? "Unknown Product",
                            BuyerId = existingOrder.BuyerId,
                            BuyerName = existingOrder.Buyer?.FullName ?? (seller.FullName ?? "Unknown Buyer"),
                            SellerId = existingOrder.SellerId,
                            SellerName = seller.FullName ?? "Unknown Seller",
                            TotalAmount = existingOrder.TotalAmount,
                            PlatformFee = existingOrder.PlatformFee,
                            SellerAmount = existingOrder.SellerAmount,
                            Status = existingOrder.Status.ToString(),
                            CreatedAt = existingOrder.CreatedAt,
                            ShippingInfo = new ShippingInfoDTO
                            {
                                RecipientName = existingOrder.ShippingInfo?.RecipientName ?? dto.ShippingInfo?.RecipientName ?? "Swap Recipient",
                                Address = existingOrder.ShippingInfo?.Address ?? dto.ShippingInfo?.Address ?? "Swap Address",
                                City = existingOrder.ShippingInfo?.City ?? dto.ShippingInfo?.City ?? "Unknown",
                                PostalCode = existingOrder.ShippingInfo?.PostalCode ?? dto.ShippingInfo?.PostalCode ?? "00000",
                                PhoneNumber = existingOrder.ShippingInfo?.PhoneNumber ?? dto.ShippingInfo?.PhoneNumber ?? "0000000000",
                                ShippingFee = existingOrder.ShippingInfo?.ShippingFee ?? 50.0m,
                                Status = (existingOrder.ShippingInfo?.Status ?? ShippingStatus.Pending).ToString()
                            }
                        }, "Existing swap order returned");
                    }
                }
                
                // For swap orders, charge only fixed shipping fee of 50 EGP
                // For regular orders, charge product price + shipping fee
                decimal shippingFee = dto.ShippingInfo?.ShippingFee ?? 50.0m;
                
                decimal totalAmount, platformFee, sellerAmount;
                
                if (isSwapOrder)
                {
                    _logger.LogInformation("Processing swap order for product {ProductId}", product.Id);
                    
                    // For swap orders:
                    // - Total amount is just the shipping fee (50 EGP)
                    // - No platform fee
                    // - Seller gets the full shipping fee
                    totalAmount = shippingFee;
                    platformFee = 0;
                    sellerAmount = shippingFee;
                    
                    // Mark the order as a swap order
                    dto.IsSwapOrder = true;
                }
                else
                {
                    _logger.LogInformation("Processing regular order for product {ProductId} with price {Price}", product.Id, product.Price);

                    // For regular orders:
                    // - Total amount is product price + shipping fee
                    // - Platform fee is 5% of product price
                    // - Seller gets 95% of product price + full shipping fee
                    totalAmount = product.Price + shippingFee;
                    platformFee = totalAmount * 0.15m;
                    sellerAmount = totalAmount * 0.85m;
                }

                // Sanitize and clamp fields to comply with DB constraints
                string recipientName = dto.ShippingInfo?.RecipientName ?? "Swap Recipient";
                if (recipientName.Length > 100) recipientName = recipientName.Substring(0, 100);

                string address = dto.ShippingInfo?.Address ?? "Swap Address";
                if (address.Length > 200) address = address.Substring(0, 200);

                string city = dto.ShippingInfo?.City ?? "Unknown";
                if (city.Length > 100) city = city.Substring(0, 100);

                string postalCode = dto.ShippingInfo?.PostalCode ?? "00000";
                if (postalCode.Length > 7) postalCode = postalCode.Substring(0, 7);

                // Sanitize phone to digits only and clamp to 11 to be safe with current DB schema
                string phoneNumberRaw = dto.ShippingInfo?.PhoneNumber ?? "0000000000";
                string phoneNumber = new string(phoneNumberRaw.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(phoneNumber)) phoneNumber = "0000000000";
                if (phoneNumber.Length > 11) phoneNumber = phoneNumber.Substring(0, 11);

                // Create shipping info with all required fields
                var shippingInfo = new ShippingInfo
                {
                    RecipientName = recipientName,
                    Address = address,
                    City = city,
                    PostalCode = postalCode,
                    PhoneNumber = phoneNumber,
                    Status = ShippingStatus.Pending,
                    ShippingFee = shippingFee,
                    CreateAt = DateTime.UtcNow,
                    ShippingMethod = "Standard" // Add default shipping method
                };

                _logger.LogDebug("Creating order with total amount: {TotalAmount}, platform fee: {PlatformFee}, seller amount: {SellerAmount}", 
                    totalAmount, platformFee, sellerAmount);

                var order = new Order
                {
                    ProductId = dto.ProductId,
                    BuyerId = dto.BuyerId,
                    SellerId = seller.Id,
                    TotalAmount = totalAmount,
                    PlatformFee = platformFee,
                    SellerAmount = sellerAmount,
                    IsSwapOrder = isSwapOrder,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ShippingInfo = shippingInfo
                };

                _logger.LogInformation("Saving order to database");
                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();

                product.Status = Data.Enums.ProductStatus.InProcess;
                _productRepository.Update(product);
                await _productRepository.SaveChangesAsync();
              await  _wishlistRepository.RemoveProductFromAllWishlistsAsync(product.Id);

                _logger.LogInformation("Order {OrderId} created successfully for product {ProductId}", order.Id, product.Id);

                return ApiResponse<OrderDTO>.SuccessResponse(new OrderDTO
                {
                    Id = order.Id,
                    ProductId = product.Id,
                    ProductTitle = product.Title ?? "Unknown Product",
                    BuyerId = dto.BuyerId,
                    BuyerName = $"{seller.FullName}".Trim(),
                    SellerId = seller.Id,
                    SellerName = seller.FullName ?? "Unknown Seller",
                    TotalAmount = order.TotalAmount,
                    PlatformFee = order.PlatformFee,
                    SellerAmount = order.SellerAmount,
                    Status = order.Status.ToString(),
                    CreatedAt = order.CreatedAt,
                    ShippingInfo = dto.ShippingInfo ?? new ShippingInfoDTO
                    {
                        RecipientName = shippingInfo.RecipientName,
                        Address = shippingInfo.Address,
                        City = shippingInfo.City,
                        PostalCode = shippingInfo.PostalCode,
                        PhoneNumber = shippingInfo.PhoneNumber,
                        ShippingFee = shippingInfo.ShippingFee,
                        Status = shippingInfo.Status.ToString()
                    }
                }, "Order created successfully");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating order for product {ProductId}", dto?.ProductId);
                var dbMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                // If unique constraint violated for swap orders, fetch existing and return it (idempotency)
                if (dto != null)
                {
                    try
                    {
                        var existing = await _orderRepository.GetByProductAndBuyerAsync(dto.ProductId, dto.BuyerId);
                        if (existing != null && (dto.IsSwapOrder || existing.IsSwapOrder))
                        {
                            // Best-effort shipping info update if provided
                            if (dto.ShippingInfo != null)
                            {
                                if (existing.ShippingInfo == null)
                                {
                                    existing.ShippingInfo = new ShippingInfo
                                    {
                                        RecipientName = dto.ShippingInfo.RecipientName,
                                        Address = dto.ShippingInfo.Address,
                                        City = dto.ShippingInfo.City,
                                        PostalCode = dto.ShippingInfo.PostalCode,
                                        PhoneNumber = dto.ShippingInfo.PhoneNumber,
                                        Status = ShippingStatus.Pending,
                                        ShippingFee = 50.0m,
                                        CreateAt = DateTime.UtcNow,
                                        ShippingMethod = "Standard"
                                    };
                                }
                                else
                                {
                                    existing.ShippingInfo.RecipientName = dto.ShippingInfo.RecipientName;
                                    existing.ShippingInfo.Address = dto.ShippingInfo.Address;
                                    existing.ShippingInfo.City = dto.ShippingInfo.City;
                                    existing.ShippingInfo.PostalCode = dto.ShippingInfo.PostalCode;
                                    existing.ShippingInfo.PhoneNumber = dto.ShippingInfo.PhoneNumber;
                                }

                                _orderRepository.Update(existing);
                                await _orderRepository.SaveChangesAsync();
                            }

                            return ApiResponse<OrderDTO>.SuccessResponse(new OrderDTO
                            {
                                Id = existing.Id,
                                ProductId = existing.ProductId,
                                ProductTitle = "",
                                BuyerId = existing.BuyerId,
                                BuyerName = existing.Buyer?.FullName ?? "",
                                SellerId = existing.SellerId,
                                SellerName = existing.Seller?.FullName ?? "",
                                TotalAmount = existing.TotalAmount,
                                PlatformFee = existing.PlatformFee,
                                SellerAmount = existing.SellerAmount,
                                Status = existing.Status.ToString(),
                                CreatedAt = existing.CreatedAt,
                                ShippingInfo = new ShippingInfoDTO
                                {
                                    RecipientName = existing.ShippingInfo?.RecipientName ?? dto.ShippingInfo?.RecipientName ?? "",
                                    Address = existing.ShippingInfo?.Address ?? dto.ShippingInfo?.Address ?? "",
                                    City = existing.ShippingInfo?.City ?? dto.ShippingInfo?.City ?? "",
                                    PostalCode = existing.ShippingInfo?.PostalCode ?? dto.ShippingInfo?.PostalCode ?? "",
                                    PhoneNumber = existing.ShippingInfo?.PhoneNumber ?? dto.ShippingInfo?.PhoneNumber ?? "",
                                    ShippingFee = existing.ShippingInfo?.ShippingFee ?? 50.0m,
                                    Status = (existing.ShippingInfo?.Status ?? ShippingStatus.Pending).ToString()
                                }
                            }, "Existing swap order returned");
                        }
                    }
                    catch { /* ignore and fallthrough to error response */ }
                }

                return ApiResponse<OrderDTO>.ErrorResponse($"Database error: {dbMessage}", 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating order for product {ProductId}", dto?.ProductId);
                return ApiResponse<OrderDTO>.ErrorResponse("An unexpected error occurred while creating the order. Please try again later.", 500);
            }
        }

        /// <summary>
        /// Creates exchange orders for a product swap (creates exactly 2 orders)
        /// </summary>
        /// <param name="exchangeRequestId">The exchange request ID</param>
        /// <returns>List of created orders</returns>
        public async Task<ApiResponse<List<OrderDTO>>> CreateExchangeOrdersAsync(int exchangeRequestId)
        {
            try
            {
                _logger.LogInformation("Creating exchange orders for exchange request {ExchangeRequestId}", exchangeRequestId);
                
                // Get the exchange request with products
                var exchangeRequest = await _exchangeRequestRepository.GetByIdWithProductsAsync(exchangeRequestId);
                if (exchangeRequest == null)
                {
                    return ApiResponse<List<OrderDTO>>.ErrorResponse("Exchange request not found", 404);
                }
                
                if (exchangeRequest.Status != ExchangeStatus.Accepted)
                {
                    return ApiResponse<List<OrderDTO>>.ErrorResponse("Exchange request must be accepted before creating orders", 400);
                }
                
                // Check if orders already exist
                if (exchangeRequest.OrderForOfferedProductId.HasValue || exchangeRequest.OrderForRequestedProductId.HasValue)
                {
                    return ApiResponse<List<OrderDTO>>.ErrorResponse("Orders already exist for this exchange", 400);
                }
                
                var createdOrders = new List<OrderDTO>();
                
                // Create order for the offered product (being given away)
                var orderForOffered = new Order
                {
                    ProductId = exchangeRequest.OfferedProductId,
                    BuyerId = exchangeRequest.RequestedProduct?.UserId ?? 0, // Person receiving the offered product
                    SellerId = exchangeRequest.OfferedProduct?.UserId ?? 0,  // Person giving away the offered product
                    TotalAmount = 50.0m, // Fixed shipping fee for exchanges
                    PlatformFee = 0,
                    SellerAmount = 50.0m,
                    IsSwapOrder = true,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _orderRepository.AddAsync(orderForOffered);
                
                // Create order for the requested product (being given away)
                var orderForRequested = new Order
                {
                    ProductId = exchangeRequest.RequestedProductId,
                    BuyerId = exchangeRequest.OfferedProduct?.UserId ?? 0, // Person receiving the requested product
                    SellerId = exchangeRequest.RequestedProduct?.UserId ?? 0, // Person giving away the requested product
                    TotalAmount = 50.0m, // Fixed shipping fee for exchanges
                    PlatformFee = 0,
                    SellerAmount = 50.0m,
                    IsSwapOrder = true,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _orderRepository.AddAsync(orderForRequested);
                
                // Update exchange request with order IDs
                exchangeRequest.OrderForOfferedProductId = orderForOffered.Id;
                exchangeRequest.OrderForRequestedProductId = orderForRequested.Id;
                await _exchangeRequestRepository.UpdateAsync(exchangeRequest);
                
                await _orderRepository.SaveChangesAsync();
                
                // Convert to DTOs
                var orderForOfferedDto = new OrderDTO
                {
                    Id = orderForOffered.Id,
                    ProductId = orderForOffered.ProductId,
                    ProductTitle = exchangeRequest.OfferedProduct?.Title ?? "Unknown Product",
                    BuyerId = orderForOffered.BuyerId,
                    BuyerName = exchangeRequest.RequestedProduct?.User?.FullName ?? "Unknown Buyer",
                    SellerId = orderForOffered.SellerId,
                    SellerName = exchangeRequest.OfferedProduct?.User?.FullName ?? "Unknown Seller",
                    TotalAmount = orderForOffered.TotalAmount,
                    PlatformFee = orderForOffered.PlatformFee,
                    SellerAmount = orderForOffered.SellerAmount,
                    Status = orderForOffered.Status.ToString(),
                    CreatedAt = orderForOffered.CreatedAt,
                    IsSwapOrder = true
                };
                
                var orderForRequestedDto = new OrderDTO
                {
                    Id = orderForRequested.Id,
                    ProductId = orderForRequested.ProductId,
                    ProductTitle = exchangeRequest.RequestedProduct?.Title ?? "Unknown Product",
                    BuyerId = orderForRequested.BuyerId,
                    BuyerName = exchangeRequest.OfferedProduct?.User?.FullName ?? "Unknown Buyer",
                    SellerId = orderForRequested.SellerId,
                    SellerName = exchangeRequest.RequestedProduct?.User?.FullName ?? "Unknown Seller",
                    TotalAmount = orderForRequested.TotalAmount,
                    PlatformFee = orderForRequested.PlatformFee,
                    SellerAmount = orderForRequested.SellerAmount,
                    Status = orderForRequested.Status.ToString(),
                    CreatedAt = orderForRequested.CreatedAt,
                    IsSwapOrder = true
                };
                
                createdOrders.Add(orderForOfferedDto);
                createdOrders.Add(orderForRequestedDto);
                
                _logger.LogInformation("Successfully created {Count} exchange orders for exchange request {ExchangeRequestId}", 
                    createdOrders.Count, exchangeRequestId);
                
                return ApiResponse<List<OrderDTO>>.SuccessResponse(createdOrders, 
                    $"Successfully created {createdOrders.Count} exchange orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exchange orders for exchange request {ExchangeRequestId}", exchangeRequestId);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to create exchange orders: {ex.Message}", 500);
            }
        }

        // Add missing methods to implement interface
        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);
                if (order == null)
                {
                    return ApiResponse<OrderDTO>.ErrorResponse("Order not found", 404);
                }

                var orderDto = new OrderDTO
                {
                    Id = order.Id,
                    ProductId = order.ProductId,
                    ProductTitle = order.Product?.Title ?? "Unknown Product",
                    BuyerId = order.BuyerId,
                    BuyerName = order.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = order.SellerId,
                    SellerName = order.Seller?.FullName ?? "Unknown Seller",
                    TotalAmount = order.TotalAmount,
                    PlatformFee = order.PlatformFee,
                    SellerAmount = order.SellerAmount,
                    Status = order.Status.ToString(),
                    CreatedAt = order.CreatedAt,
                    IsSwapOrder = order.IsSwapOrder
                };

                return ApiResponse<OrderDTO>.SuccessResponse(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                return ApiResponse<OrderDTO>.ErrorResponse($"Failed to retrieve order: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _orderRepository.GetAllOrdersWithRelationsAsync();
                var orderDtos = orders.Select(o => new OrderDTO
                {
                    Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                    TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
                }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }
        public async Task<ApiResponse<List<OrderReviewDTO>>> GetOrdersNeedingReviewAsync(int userId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersNeedingReviewAsync(userId);
                var orderDtos = orders.Where(o=>o.Reviews==null||o.Reviews.Count==0) .Select(o => new OrderReviewDTO
                {
                    Id = o.Id,
                 ProductId=o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                 BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Seller",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
            
                    CreatedAt = o.CreatedAt,
                    Status=o.Status,
                    IsSwapOrder = o.IsSwapOrder,


                }).ToList();
           
      


      
        /// <summary>
        /// Indicates whether this order is part of a product swap
        /// </summary>
      
                return ApiResponse<List<OrderReviewDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return ApiResponse<List<OrderReviewDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Updates the status of an order.
        /// </summary>
        public async Task<ApiResponse<OrderDTO>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithAllRelationsAsync(orderId);
                if (order == null)
                {
                    return ApiResponse<OrderDTO>.ErrorResponse("Order not found", 404);
                }

                order.Status = newStatus;
                await _orderRepository.UpdateAsync(order);
                await _orderRepository.SaveChangesAsync();
                
                // If this is a swap order and it's being completed, also update the related order
                if (order.IsSwapOrder && newStatus == OrderStatus.Completed)
                {
                    await UpdateRelatedSwapOrderStatusAsync(order);
                }
                
                // Return the updated order
                return await GetOrderByIdAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId} to {NewStatus}", orderId, newStatus);
                return ApiResponse<OrderDTO>.ErrorResponse($"Failed to update order status: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Updates the status of an order using DTO
        /// </summary>
        /// <param name="dto">Order status update data</param>
        /// <returns>ApiResponse indicating success or error</returns>
        public async Task<ApiResponse<bool>> UpdateOrderStatusAsync(UpdateOrderStatusDTO dto)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(dto.NewStatus, true, out var newStatus))
                {
                    return ApiResponse<bool>.ErrorResponse("Invalid status", 400);
                }

                var order = await _orderRepository.GetOrderWithAllRelationsAsync(dto.OrderId);
                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                order.Status = newStatus;
                await _orderRepository.UpdateAsync(order);
                await _orderRepository.SaveChangesAsync();
                
                // If this is a swap order and it's being completed, also update the related order
                if (order.IsSwapOrder && newStatus == OrderStatus.Completed)
                {
                    await UpdateRelatedSwapOrderStatusAsync(order);
                }
                
                return ApiResponse<bool>.SuccessResponse(true, "Order status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId} to {NewStatus}", dto.OrderId, dto.NewStatus);
                return ApiResponse<bool>.ErrorResponse($"Failed to update order status: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<bool>> DeleteOrderAsync(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                _orderRepository.Remove(order);
            await _orderRepository.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Order deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                return ApiResponse<bool>.ErrorResponse($"Failed to delete order: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersByUserIdAsync(int userId)
        {
            try
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
                var orderDtos = orders.Select(o => new OrderDTO
                {
                    Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    ProductImages = o.Product.Images,
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                    TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder,
                    NeedReview = o.Reviews == null || o.Reviews.Count == 0
                }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersByProductIdAsync(int productId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByProductIdAsync(productId);
                var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
            }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for product {ProductId}", productId);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersByStatusAsync(OrderStatus status)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByStatusAsync(status);
                var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
            }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders with status {Status}", status);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByDateRangeAsync(startDate, endDate);
                var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
            }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders between {StartDate} and {EndDate}", startDate, endDate);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersBySellerIdAsync(int sellerId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersBySellerIdAsync(sellerId);
                var orderDtos = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
            }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for seller {SellerId}", sellerId);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<OrderDTO>>> GetOrdersByBuyerIdAsync(int buyerId)
        {
            try
            {
                var orders = await _orderRepository.GetOrdersByBuyerIdAsync(buyerId);
                var orderDtos = orders.Select(o => new OrderDTO
                {
                    Id = o.Id,
                    ProductId = o.ProductId,
                    ProductTitle = o.Product?.Title ?? "Unknown Product",
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer?.FullName ?? "Unknown Buyer",
                    SellerId = o.SellerId,
                    SellerName = o.Seller?.FullName ?? "Unknown Seller",
                    TotalAmount = o.TotalAmount,
                    PlatformFee = o.PlatformFee,
                    SellerAmount = o.SellerAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    IsSwapOrder = o.IsSwapOrder
                }).ToList();

                return ApiResponse<List<OrderDTO>>.SuccessResponse(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for buyer {BuyerId}", buyerId);
                return ApiResponse<List<OrderDTO>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Cancels an order if the buyer is authorized.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="buyerId">Buyer ID</param>
        /// <returns>ApiResponse indicating success or error</returns>
        public async Task<ApiResponse<bool>> CancelOrderAsync(int orderId, int buyerId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

            var canCancel = await _orderRepository.CanUserCancelOrderAsync(orderId, buyerId);
            if (!canCancel)
                {
                    return ApiResponse<bool>.ErrorResponse("Not authorized or order cannot be canceled", 403);
                }

            var success = await _orderRepository.UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
            if (!success)
                {
                    return ApiResponse<bool>.ErrorResponse("Failed to cancel order", 500);
                }

            await _orderRepository.SaveChangesAsync();
                return ApiResponse<bool>.SuccessResponse(true, "Order canceled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling order {OrderId} for buyer {BuyerId}", orderId, buyerId);
                return ApiResponse<bool>.ErrorResponse($"Failed to cancel order: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Updates shipping information for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="dto">Shipping information update data</param>
        /// <returns>ApiResponse indicating success or error</returns>
        public async Task<ApiResponse<bool>> UpdateShippingInfoAsync(int orderId, ShippingInfoDTO dto)
        {
            try
            {
                var order = await _orderRepository.GetOrderWithShippingInfoAsync(orderId);
                if (order == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Order not found", 404);
                }

                // Upsert shipping info: create if missing (common for swap orders created on approval)
                if (order.ShippingInfo == null)
                {
                    var newShipping = new ShippingInfo
                    {
                        RecipientName = dto.RecipientName ?? order.Buyer?.FullName ?? "Unknown",
                        Address = dto.Address ?? string.Empty,
                        City = dto.City ?? string.Empty,
                        PostalCode = dto.PostalCode ?? string.Empty,
                        PhoneNumber = dto.PhoneNumber ?? string.Empty,
                        Status = ShippingStatus.Pending,
                        ShippingFee = 50.0m,
                        CreateAt = DateTime.UtcNow,
                        ShippingMethod = "Standard",
                        OrderId = order.Id
                    };

                    await _shippingInfoRepository.AddAsync(newShipping);
                    await _shippingInfoRepository.SaveChangesAsync();
                }
                else
                {
                    order.ShippingInfo.RecipientName = dto.RecipientName ?? order.ShippingInfo.RecipientName;
                    order.ShippingInfo.Address = dto.Address ?? order.ShippingInfo.Address;
                    order.ShippingInfo.City = dto.City ?? order.ShippingInfo.City;
                    order.ShippingInfo.PostalCode = dto.PostalCode ?? order.ShippingInfo.PostalCode;
                    order.ShippingInfo.PhoneNumber = dto.PhoneNumber ?? order.ShippingInfo.PhoneNumber;

                    await _shippingInfoRepository.UpdateAsync(order.ShippingInfo);
                    await _shippingInfoRepository.SaveChangesAsync();
                }

                return ApiResponse<bool>.SuccessResponse(true, "Shipping info saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping info for order {OrderId}", orderId);
                return ApiResponse<bool>.ErrorResponse($"Failed to update shipping info: {ex.Message}", 500);
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
                        var relatedOrder = await _orderRepository.GetByIdAsync(relatedOrderId.Value);
                        if (relatedOrder != null && relatedOrder.IsSwapOrder)
                        {
                            // Update the related order status to Completed
                            var success = await _orderRepository.UpdateOrderStatusAsync(relatedOrder.Id, OrderStatus.Completed);
                            if (success)
                            {
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
    }
}