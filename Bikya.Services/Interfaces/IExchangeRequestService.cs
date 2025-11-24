using Bikya.DTOs.ExchangeRequestDTOs;
using Bikya.Data.Response;

namespace Bikya.Services.Interfaces
{
    public interface IExchangeRequestService
    {
        
        Task<ApiResponse<ExchangeRequestDTO>> CreateAsync(CreateExchangeRequestDTO dto, int senderUserId);

        Task<ApiResponse<ExchangeRequestDTO>> GetByIdAsync(int id);

        Task<ApiResponse<List<ExchangeRequestDTO>>> GetAllAsync();

        Task<ApiResponse<List<ExchangeRequestDTO>>> GetSentRequestsAsync(int senderUserId);

        Task<ApiResponse<List<ExchangeRequestDTO>>> GetReceivedRequestsAsync(int receiverUserId);


        Task<ApiResponse<ExchangeRequestDTO>> ApproveRequestAsync(int requestId, int currentUserId);

        Task<ApiResponse<ExchangeRequestDTO>> RejectRequestAsync(int requestId, int currentUserId);

   
        Task<ApiResponse<bool>> DeleteAsync(int requestId, int currentUserId);
    }
}
