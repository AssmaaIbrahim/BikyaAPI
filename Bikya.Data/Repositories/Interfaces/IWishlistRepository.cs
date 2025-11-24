using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IWishlistRepository: IGenericRepository<WishList>
    {

        Task CreateAsync(WishList wish, CancellationToken cancellationToken = default);
        Task DeleteAsync(WishList wish, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetUserWishlistProductsAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> CountUserWishlistAsync(int userId, CancellationToken cancellationToken = default);
        Task<WishList?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int userId, int productId);
        Task<HashSet<int>> GetProductIdsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task RemoveProductFromAllWishlistsAsync(int productId, CancellationToken cancellationToken = default);

    }
}


