using Bikya.DTOs.PaymentDTOs;
using Bikya.Data.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentResponseDto>> CreatePaymentAsync(PaymentRequestDto dto);
        Task<IEnumerable<PaymentDto>> GetPaymentsByOrderIdAsync(int orderId);
        Task<IEnumerable<PaymentDto>> GetPaymentsByUserIdAsync(int userId);
        Task<ApiResponse<PaymentStatusDto>> GetPaymentStatusAsync(int paymentId);
        Task<ApiResponse<PaymentSummaryDto>> GetPaymentSummaryAsync(int paymentId);
    }
}
