using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikya.DTOs.ProductDTO;

namespace Bikya.DTOs.ExchangeRequestDTOs
{
    public class ExchangeRequestDTO
    {
        public int Id { get; set; }

        public int OfferedProductId { get; set; }
        public string OfferedProductTitle { get; set; }

        public int RequestedProductId { get; set; }
        public string RequestedProductTitle { get; set; }

        public string? Message { get; set; }

        public string Status { get; set; } 

        public DateTime RequestedAt { get; set; }

        // Newly added: order references created upon approval
        public int? OrderForOfferedProductId { get; set; }
        public int? OrderForRequestedProductId { get; set; }

        // Newly added: full product details with images for UI rendering
        public GetProductDTO? OfferedProduct { get; set; }
        public GetProductDTO? RequestedProduct { get; set; }
    }
}
