using Bikya.Data.Response;
using Bikya.DTOs.ShippingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface IShippingService
    {
        Task<ApiResponse<List<ShippingDetailsDto>>> GetAllAsync();
        Task<ApiResponse<ShippingDetailsDto>> GetByIdAsync(int id);
        Task<ApiResponse<ShippingDetailsDto>> CreateAsync(CreateShippingDto dto);
        Task<ApiResponse<bool>> UpdateStatusAsync(int id, UpdateShippingStatusDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<TrackShipmentDto>> TrackAsync(string trackingNumber);
        Task<ApiResponse<ShippingCostResponseDto>> CalculateCostAsync(ShippingCostRequestDto dto);
        Task<ApiResponse<bool>> IntegrateWithProviderAsync(ThirdPartyShippingRequestDto dto);
        Task<ApiResponse<bool>> HandleWebhookAsync(string provider, ShippingWebhookDto dto);
    }
}
