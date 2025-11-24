using Bikya.Data.Response;
using Bikya.DTOs.CategoryDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface ICategoryService
    {

        Task<ApiResponse<PaginatedCategoryResponse>> GetPaginatedAsync(int page = 1, int pageSize = 10, string? search = null);
        Task<ApiResponse<List<CategoryDTO>>> GetAllAsync(string? search = null);

        Task<ApiResponse<CategoryDTO>> GetByIdAsync(int id);
        Task<ApiResponse<CategoryDTO>> GetByNameAsync(string name);
        Task<ApiResponse<CategoryDTO>> AddAsync(CreateCategoryDTO dto);
        Task<ApiResponse<CategoryDTO>> UpdateAsync(int id, UpdateCategoryDTO dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<int>> CreateBulkAsync(List<CreateCategoryDTO> dtos);
    }
}
