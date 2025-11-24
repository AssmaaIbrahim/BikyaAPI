
using Bikya.Data.Models;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IProductImageRepository : IGenericRepository<ProductImage>
    {
        Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<ProductImage?> GetImageByIdWithProductAsync(int id, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProductImage productImage, CancellationToken cancellationToken = default);
        Task DeleteAsync(ProductImage productImage, CancellationToken cancellationToken = default);
        Task DeleteImagesByProductIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<bool> UserOwnsImageAsync(int imageId, int userId, CancellationToken cancellationToken = default);
    }
}