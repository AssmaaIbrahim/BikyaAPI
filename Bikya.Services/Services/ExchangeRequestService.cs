using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.ExchangeRequestDTOs;
using Bikya.Services;
using Bikya.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Bikya.Services.Services
{
    public class ExchangeRequestService : IExchangeRequestService
    {
        private readonly IExchangeRequestRepository _exchangeRequestRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<ExchangeRequestService> _logger;

        public ExchangeRequestService(
            IExchangeRequestRepository exchangeRequestRepository,
            IProductRepository productRepository,
            IWishlistRepository wishlistRepository,
            IOrderRepository orderRepository,
            ILogger<ExchangeRequestService> logger)
        {
            _exchangeRequestRepository = exchangeRequestRepository ?? throw new ArgumentNullException(nameof(exchangeRequestRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _wishlistRepository = wishlistRepository ?? throw new ArgumentNullException(nameof(wishlistRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<ExchangeRequestDTO>> CreateAsync(CreateExchangeRequestDTO dto, int senderUserId)
        {
            try
            {
                var offeredProduct = await _productRepository.GetProductWithImagesByIdAsync(dto.OfferedProductId);
                var requestedProduct = await _productRepository.GetProductWithImagesByIdAsync(dto.RequestedProductId);

                if (offeredProduct == null || requestedProduct == null)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("One or both products not found", 404);

                if (offeredProduct.UserId != senderUserId)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("You can only offer your own product", 403);

                // Check for existing pending request between these products
                var hasPendingRequest = await _exchangeRequestRepository.HasPendingRequestBetweenProductsAsync(
                    dto.OfferedProductId, dto.RequestedProductId);

                if (hasPendingRequest)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("A pending request already exists between these products", 409);

                var request = new ExchangeRequest
                {
                    OfferedProductId = dto.OfferedProductId,
                    RequestedProductId = dto.RequestedProductId,
                    Message = dto.Message
                };

                await _exchangeRequestRepository.AddAsync(request);
                await _exchangeRequestRepository.SaveChangesAsync();

                _logger.LogInformation("Exchange request created successfully by user {UserId} for products {OfferedProductId} and {RequestedProductId}", 
                    senderUserId, dto.OfferedProductId, dto.RequestedProductId);

                return ApiResponse<ExchangeRequestDTO>.SuccessResponse(ToDTO(request), "Exchange request created successfully", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exchange request by user {UserId}", senderUserId);
                return ApiResponse<ExchangeRequestDTO>.ErrorResponse("An error occurred while creating the exchange request", 500);
            }
        }

        public async Task<ApiResponse<ExchangeRequestDTO>> GetByIdAsync(int id)
        {
            try
            {
                var request = await _exchangeRequestRepository.GetByIdWithProductsAsync(id);

                if (request == null)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("Request not found", 404);

                return ApiResponse<ExchangeRequestDTO>.SuccessResponse(ToDTO(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange request with ID {RequestId}", id);
                return ApiResponse<ExchangeRequestDTO>.ErrorResponse("An error occurred while retrieving the exchange request", 500);
            }
        }

        public async Task<ApiResponse<List<ExchangeRequestDTO>>> GetAllAsync()
        {
            try
            {
                var requests = await _exchangeRequestRepository.GetAllWithProductsAsync();
                var requestDTOs = requests.Select(ToDTO).ToList();

                return ApiResponse<List<ExchangeRequestDTO>>.SuccessResponse(requestDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all exchange requests");
                return ApiResponse<List<ExchangeRequestDTO>>.ErrorResponse("An error occurred while retrieving exchange requests", 500);
            }
        }

        public async Task<ApiResponse<List<ExchangeRequestDTO>>> GetSentRequestsAsync(int senderUserId)
        {
            try
            {
                var requests = await _exchangeRequestRepository.GetSentRequestsAsync(senderUserId);
                var requestDTOs = requests.Select(ToDTO).ToList();

                return ApiResponse<List<ExchangeRequestDTO>>.SuccessResponse(requestDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sent requests for user {UserId}", senderUserId);
                return ApiResponse<List<ExchangeRequestDTO>>.ErrorResponse("An error occurred while retrieving sent requests", 500);
            }
        }

        public async Task<ApiResponse<List<ExchangeRequestDTO>>> GetReceivedRequestsAsync(int receiverUserId)
        {
            try
            {
                var requests = await _exchangeRequestRepository.GetReceivedRequestsAsync(receiverUserId);
                // Defensive: filter out any null items just in case
                var safeRequests = requests?.Where(r => r != null).ToList() ?? new List<ExchangeRequest>();
                var requestDTOs = safeRequests.Select(ToDTO).ToList();

                return ApiResponse<List<ExchangeRequestDTO>>.SuccessResponse(requestDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received requests for user {UserId}", receiverUserId);
                // Avoid breaking the UI: return empty list instead of 500
                return ApiResponse<List<ExchangeRequestDTO>>.SuccessResponse(new List<ExchangeRequestDTO>(), "No received requests found");
            }
        }

        public async Task<ApiResponse<ExchangeRequestDTO>> ApproveRequestAsync(int requestId, int currentUserId)
        {
            // Create an explicit scope that allows async flow
            var transaction = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                _logger.LogInformation("Starting transaction for approving exchange request {RequestId}", requestId);
                _logger.LogInformation("Starting approval for exchange request {RequestId} by user {UserId}", requestId, currentUserId);
                
                // Get the exchange request with products
                var exchangeRequest = await _exchangeRequestRepository.GetByIdWithProductsAndUsersAsync(requestId);

                // Validate request
                if (exchangeRequest == null)
                {
                    _logger.LogWarning("Exchange request {RequestId} not found", requestId);
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("Request not found", 404);
                }

                if (exchangeRequest.RequestedProduct?.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} is not authorized to approve request {RequestId}", currentUserId, requestId);
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("You are not authorized to approve this request", 403);
                }

                if (exchangeRequest.Status != ExchangeStatus.Pending)
                {
                    _logger.LogWarning("Exchange request {RequestId} is not in a pending state. Current status: {Status}", 
                        requestId, exchangeRequest.Status);
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("This request is not in a pending state", 400);
                }

                // Validate products
                if (exchangeRequest.OfferedProduct == null || exchangeRequest.RequestedProduct == null)
                {
                    _logger.LogError("One or both products not found in exchange request {RequestId}", requestId);
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("One or both products not found", 404);
                }

                // Mark products as trading
                exchangeRequest.OfferedProduct.Status = ProductStatus.Trading;
                exchangeRequest.RequestedProduct.Status = ProductStatus.Trading;

                await _wishlistRepository.RemoveProductFromAllWishlistsAsync(exchangeRequest.OfferedProduct.Id);
                await _wishlistRepository.RemoveProductFromAllWishlistsAsync(exchangeRequest.RequestedProduct.Id);

                // Update request status
                exchangeRequest.Status = ExchangeStatus.Accepted;
                exchangeRequest.RespondedAt = DateTime.UtcNow;
                
                // Check if orders already exist for this exchange to prevent duplicates
                var existingOrderForOffered = await _orderRepository.GetByProductAndBuyerAsync(
                    exchangeRequest.OfferedProductId, 
                    exchangeRequest.RequestedProduct.UserId ?? 0);
                
                var existingOrderForRequested = await _orderRepository.GetByProductAndBuyerAsync(
                    exchangeRequest.RequestedProductId, 
                    exchangeRequest.OfferedProduct.UserId ?? 0);

                // Only create orders if they don't already exist
                if (existingOrderForOffered == null)
                {
                    _logger.LogInformation("Creating order for offered product {ProductId} in exchange {RequestId}", 
                        exchangeRequest.OfferedProductId, requestId);
                    
                    // Create order for the offered product (being given away)
                    var orderForOffered = new Order
                    {
                        ProductId = exchangeRequest.OfferedProductId,
                        BuyerId = exchangeRequest.RequestedProduct.UserId ?? 0, // Person receiving the offered product
                        SellerId = exchangeRequest.OfferedProduct.UserId ?? 0,  // Person giving away the offered product
                        TotalAmount = 50.0m, // Fixed shipping fee for exchanges
                        PlatformFee = 0,
                        SellerAmount = 50.0m,
                        IsSwapOrder = true,
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _orderRepository.AddAsync(orderForOffered);
                    // Persist to generate Order ID
                    await _orderRepository.SaveChangesAsync();
                    // Assign generated ID to exchange request
                    exchangeRequest.OrderForOfferedProductId = orderForOffered.Id;
                    
                    _logger.LogInformation("Created order {OrderId} for offered product {ProductId}", 
                        orderForOffered.Id, exchangeRequest.OfferedProductId);
                }
                else
                {
                    _logger.LogInformation("Found existing order {OrderId} for offered product {ProductId}", 
                        existingOrderForOffered.Id, exchangeRequest.OfferedProductId);
                    exchangeRequest.OrderForOfferedProductId = existingOrderForOffered.Id;
                }
                
                if (existingOrderForRequested == null)
                {
                    _logger.LogInformation("Creating order for requested product {ProductId} in exchange {RequestId}", 
                        exchangeRequest.RequestedProductId, requestId);
                    
                    // Create order for the requested product (being given away)
                    var orderForRequested = new Order
                    {
                        ProductId = exchangeRequest.RequestedProductId,
                        BuyerId = exchangeRequest.OfferedProduct.UserId ?? 0, // Person receiving the requested product
                        SellerId = exchangeRequest.RequestedProduct.UserId ?? 0, // Person giving away the requested product
                        TotalAmount = 50.0m, // Fixed shipping fee for exchanges
                        PlatformFee = 0,
                        SellerAmount = 50.0m,
                        IsSwapOrder = true,
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _orderRepository.AddAsync(orderForRequested);
                    // Persist to generate Order ID
                    await _orderRepository.SaveChangesAsync();
                    // Assign generated ID to exchange request
                    exchangeRequest.OrderForRequestedProductId = orderForRequested.Id;
                    
                    _logger.LogInformation("Created order {OrderId} for requested product {ProductId}", 
                        orderForRequested.Id, exchangeRequest.RequestedProductId);
                }
                else
                {
                    _logger.LogInformation("Found existing order {OrderId} for requested product {ProductId}", 
                        existingOrderForRequested.Id, exchangeRequest.RequestedProductId);
                    exchangeRequest.OrderForRequestedProductId = existingOrderForRequested.Id;
                }

                // Add status history
                var statusHistory = new ExchangeStatusHistory
                {
                    ExchangeRequestId = exchangeRequest.Id,
                    Status = ExchangeStatus.Accepted,
                    ChangedByUserId = currentUserId.ToString(),
                    ChangedAt = DateTime.UtcNow,
                    Message = "Exchange request approved by admin"
                };
                
                // Add status history to the request's collection
                exchangeRequest.StatusHistory.Add(statusHistory);
                
                // Update exchange request
                exchangeRequest.Status = ExchangeStatus.Accepted;
                exchangeRequest.ProcessedAt = DateTime.UtcNow;
                exchangeRequest.ProcessedBy = currentUserId;
                // Persist all changes (orders, products status, exchange request updates)
                _exchangeRequestRepository.Update(exchangeRequest);
                await _exchangeRequestRepository.SaveChangesAsync();
                
                _logger.LogInformation("Exchange request {RequestId} approved by user {UserId}. Orders created: Offered={OrderForOfferedProductId}, Requested={OrderForRequestedProductId}", 
                    requestId, currentUserId, exchangeRequest.OrderForOfferedProductId, exchangeRequest.OrderForRequestedProductId);

                // Mark transaction as successful
                transaction.Complete();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving exchange request {RequestId} by user {UserId}", requestId, currentUserId);
                return ApiResponse<ExchangeRequestDTO>.ErrorResponse("An error occurred while approving the request", 500);
            }
            finally
            {
                // Ensure the scope is disposed to end the ambient transaction
                transaction.Dispose();
            }

            // Outside of the transaction scope: refresh the request
            var refreshed = await _exchangeRequestRepository.GetByIdWithProductsAsync(requestId);
            if (refreshed == null)
            {
                _logger.LogError("Failed to refresh exchange request {RequestId} after approval", requestId);
                return ApiResponse<ExchangeRequestDTO>.ErrorResponse(
                    "Request approved but failed to retrieve updated details", 500);
            }

            return ApiResponse<ExchangeRequestDTO>.SuccessResponse(
                ToDTO(refreshed),
                "Swap request approved successfully. Two orders have been created for the exchange.");
        }

        public async Task<ApiResponse<ExchangeRequestDTO>> RejectRequestAsync(int requestId, int currentUserId)
        {
            try
            {
                var request = await _exchangeRequestRepository.GetRequestForApprovalAsync(requestId, currentUserId);

                if (request == null)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("Request not found or you are not authorized to reject this request", 404);

                var success = await _exchangeRequestRepository.UpdateStatusAsync(requestId, ExchangeStatus.Rejected);
                if (!success)
                    return ApiResponse<ExchangeRequestDTO>.ErrorResponse("Failed to update request status", 500);

                await _exchangeRequestRepository.SaveChangesAsync();

                // Refresh the request to get updated data
                var updatedRequest = await _exchangeRequestRepository.GetByIdWithProductsAsync(requestId);
                updatedRequest.OfferedProduct.Status = ProductStatus.Available;
                updatedRequest.RequestedProduct.Status = ProductStatus.Available;
                _productRepository.SaveChangesAsync();
                
                _logger.LogInformation("Exchange request {RequestId} rejected by user {UserId}", requestId, currentUserId);
                
                return ApiResponse<ExchangeRequestDTO>.SuccessResponse(ToDTO(updatedRequest!), "Request rejected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting exchange request {RequestId} by user {UserId}", requestId, currentUserId);
                return ApiResponse<ExchangeRequestDTO>.ErrorResponse("An error occurred while rejecting the request", 500);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int requestId, int currentUserId)
        {
            try
            {
                var request = await _exchangeRequestRepository.GetRequestForDeletionAsync(requestId, currentUserId);

                if (request == null)
                    return ApiResponse<bool>.ErrorResponse("Request not found or you are not authorized to delete this request", 404);

                _exchangeRequestRepository.Remove(request);
                await _exchangeRequestRepository.SaveChangesAsync();

                _logger.LogInformation("Exchange request {RequestId} deleted by user {UserId}", requestId, currentUserId);

                return ApiResponse<bool>.SuccessResponse(true, "Request deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exchange request {RequestId} by user {UserId}", requestId, currentUserId);
                return ApiResponse<bool>.ErrorResponse("An error occurred while deleting the request", 500);
            }
        }

        private ExchangeRequestDTO ToDTO(ExchangeRequest request)
        {
            return new ExchangeRequestDTO
            {
                Id = request.Id,
                OfferedProductId = request.OfferedProductId,
                OfferedProductTitle = request.OfferedProduct?.Title ?? string.Empty,
                RequestedProductId = request.RequestedProductId,
                RequestedProductTitle = request.RequestedProduct?.Title ?? string.Empty,
                Status = request.Status.ToString(),
                Message = request.Message,
                RequestedAt = request.RequestedAt,
                OrderForOfferedProductId = request.OrderForOfferedProductId,
                OrderForRequestedProductId = request.OrderForRequestedProductId,
                OfferedProduct = request.OfferedProduct?.ToGetProductDTO(),
                RequestedProduct = request.RequestedProduct?.ToGetProductDTO()
            };
        }
    }
}