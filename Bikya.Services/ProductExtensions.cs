using Bikya.Data.Models;
using Bikya.DTOs.ProductDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services
{
    public static class ProductExtensions
    {
        public static GetProductDTO ToGetProductDTO(this Product p, HashSet<int>? userWishlistProductIds = null)
        {
            var dto= new GetProductDTO
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                IsForExchange = p.IsForExchange,
                Condition = p.Condition,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Unknown",
                IsApproved = p.IsApproved,
                Status = p.Status,
                UserId = p.UserId,
                UserName = p.User?.FullName ?? "Unknown",
                Images = p.Images?.Select(i => new GetProductImageDTO
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain
                }).ToList() ?? new List<GetProductImageDTO>()
            };
            dto.IsInWishlist = userWishlistProductIds != null && userWishlistProductIds.Contains(p.Id);
            return dto;
        }
    }
}

