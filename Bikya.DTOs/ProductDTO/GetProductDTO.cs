using Bikya.Data.Enums;
using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ProductDTO
{
    public class GetProductDTO
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsForExchange { get; set; }

        public bool IsApproved { get; set; }
    
        public string Condition { get; set; } // "New", "Used", etc.

        public DateTime CreatedAt { get; set; }
        
        public ProductStatus Status { get; set; }
        
        public int? UserId { get; set; }

        public string UserName { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsInWishlist { get; set; }

        


        public ICollection<GetProductImageDTO> Images { get; set; }

      
    }
}
