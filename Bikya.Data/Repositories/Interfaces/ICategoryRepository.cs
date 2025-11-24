using Bikya.Data.Models;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<(List<Category> Categories, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search);
        Task<List<Category>> GetAllAsync(string? search = null);
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetByIdWithProductsAsync(int id);
        Task<Category?> GetByNameWithProductsAsync(string name);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludingIdAsync(string name, int excludeId);
        Task AddAsync(Category category);
        Task AddRangeAsync(List<Category> categories);
        void Update(Category category);
        void Remove(Category category);
        Task SaveChangesAsync();
    }
}