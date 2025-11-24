using Bikya.DTOs.ProductDTO;
using Bikya.DTOs.ReviewDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.UserDTOs
{
    public class PublicUserProfileDto
    {
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Stats
        public int? ProductCount { get; set; }
        public int? SalesCount { get; set; }
        public double? AverageRating { get; set; }

        public List<ReviewDTO>? Reviews { get; set; }
        public List<GetProductDTO>? ProductsForSale { get; set; }
    }

}
