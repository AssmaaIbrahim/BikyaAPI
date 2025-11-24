using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.ShippingDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class ShippingService : IShippingService
    {
        private readonly IShippingServiceRepository _shippingRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShippingService(IShippingServiceRepository shippingRepository, IHttpContextAccessor httpContextAccessor)
        {
            _shippingRepository = shippingRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<List<ShippingDetailsDto>>> GetAllAsync()
        {
            var shippingInfos = await _shippingRepository.GetAllWithOrderByAsync();

            var data = shippingInfos.Select(s => new ShippingDetailsDto
            {
                ShippingId = s.ShippingId,
                RecipientName = s.RecipientName,
                Address = s.Address,
                City = s.City,
                PostalCode = s.PostalCode,
                PhoneNumber = s.PhoneNumber,
                Status = s.Status,
                CreateAt = s.CreateAt,
                OrderId = s.OrderId
            }).ToList();

            return ApiResponse<List<ShippingDetailsDto>>.SuccessResponse(data);
        }

        public async Task<ApiResponse<ShippingDetailsDto>> GetByIdAsync(int id)
        {
            var shipping = await _shippingRepository.GetByIdAsync(id);

            if (shipping == null)
                return ApiResponse<ShippingDetailsDto>.ErrorResponse("Shipping not found", 404);

            var shippingDetails = new ShippingDetailsDto
            {
                ShippingId = shipping.ShippingId,
                RecipientName = shipping.RecipientName,
                Address = shipping.Address,
                City = shipping.City,
                PostalCode = shipping.PostalCode,
                PhoneNumber = shipping.PhoneNumber,
                Status = shipping.Status,
                CreateAt = shipping.CreateAt,
                OrderId = shipping.OrderId
            };

            return ApiResponse<ShippingDetailsDto>.SuccessResponse(shippingDetails);
        }

        public async Task<ApiResponse<ShippingDetailsDto>> CreateAsync(CreateShippingDto dto)
        {
            // 1. Check if order exists and get order details
            var order = await _shippingRepository.GetOrderForShippingAsync(dto.OrderId);
            if (order == null)
                return ApiResponse<ShippingDetailsDto>.ErrorResponse("Order not found", 404);

            // 2. Extract UserId from Claims
            var userIdStr = _httpContextAccessor?.HttpContext?.User?.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return ApiResponse<ShippingDetailsDto>.ErrorResponse("Unauthorized", 401);

            // 3. Validate order ownership
            var isAuthorized = await _shippingRepository.ValidateOrderOwnershipAsync(dto.OrderId, userId);
            if (!isAuthorized)
                return ApiResponse<ShippingDetailsDto>.ErrorResponse("You are not allowed to create a shipment for this order", 403);

            // 4. Check if shipping already exists for this order
            var hasExistingShipping = await _shippingRepository.HasExistingShippingAsync(dto.OrderId);
            if (hasExistingShipping)
                return ApiResponse<ShippingDetailsDto>.ErrorResponse("Shipping already exists for this order", 409);

            // 5. Create shipping
            var shipping = new ShippingInfo
            {
                RecipientName = dto.RecipientName,
                Address = dto.Address,
                City = dto.City,
                PostalCode = dto.PostalCode,
                PhoneNumber = dto.PhoneNumber,
                OrderId = dto.OrderId
            };

            await _shippingRepository.AddAsync(shipping);
            await _shippingRepository.SaveChangesAsync();

            var result = new ShippingDetailsDto
            {
                ShippingId = shipping.ShippingId,
                RecipientName = shipping.RecipientName,
                Address = shipping.Address,
                City = shipping.City,
                PostalCode = shipping.PostalCode,
                PhoneNumber = shipping.PhoneNumber,
                Status = shipping.Status,
                CreateAt = shipping.CreateAt,
                OrderId = shipping.OrderId
            };

            return ApiResponse<ShippingDetailsDto>.SuccessResponse(result, "Shipping created successfully", 201);
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(int id, UpdateShippingStatusDto dto)
        {
            var success = await _shippingRepository.UpdateShippingStatusAsync(id, dto.Status);
            if (!success)
                return ApiResponse<bool>.ErrorResponse("Shipping not found", 404);

            await _shippingRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Shipping status updated");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var shipping = await _shippingRepository.GetByIdAsync(id);
            if (shipping == null)
                return ApiResponse<bool>.ErrorResponse("Shipping not found", 404);

            _shippingRepository.Remove(shipping);
            await _shippingRepository.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Shipping deleted");
        }

        public async Task<ApiResponse<TrackShipmentDto>> TrackAsync(string trackingNumber)
        {
            var shipping = await _shippingRepository.GetByTrackingNumberAsync(trackingNumber);

            if (shipping == null)
                return ApiResponse<TrackShipmentDto>.ErrorResponse("Tracking number not found", 404);

            var dto = new TrackShipmentDto
            {
                TrackingNumber = trackingNumber,
                Status = shipping.Status,
                LastLocation = "Warehouse", // Can be enhanced with a separate location tracking system
                EstimatedArrival = DateTime.UtcNow.AddDays(3)
            };

            return ApiResponse<TrackShipmentDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<ShippingCostResponseDto>> CalculateCostAsync(ShippingCostRequestDto dto)
        {
            // Business logic for cost calculation
            double ratePerKg = dto.Method == "Express" ? 20.0 : 10.0;
            double cost = dto.Weight * ratePerKg;

            var result = new ShippingCostResponseDto
            {
                Cost = cost,
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(dto.Method == "Express" ? 1 : 4)
            };

            return ApiResponse<ShippingCostResponseDto>.SuccessResponse(result);
        }

        public async Task<ApiResponse<bool>> IntegrateWithProviderAsync(ThirdPartyShippingRequestDto dto)
        {
            // Integration logic with third-party shipping providers
            if (dto.Provider.ToLower() == "aramex")
            {
                // Simulate sending request to external API
                return ApiResponse<bool>.SuccessResponse(true, "Integrated with Aramex");
            }

            return ApiResponse<bool>.ErrorResponse("Provider not supported", 400);
        }

        public async Task<ApiResponse<bool>> HandleWebhookAsync(string provider, ShippingWebhookDto dto)
        {
            var shipping = await _shippingRepository.GetByTrackingNumberAsync(dto.TrackingNumber);

            if (shipping == null)
                return ApiResponse<bool>.ErrorResponse("Shipping not found for webhook", 404);

            var success = await _shippingRepository.UpdateShippingStatusAsync(shipping.ShippingId, dto.NewStatus);
            if (!success)
                return ApiResponse<bool>.ErrorResponse("Failed to update shipping status", 500);

            await _shippingRepository.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Shipping status updated via webhook");
        }
    }
}