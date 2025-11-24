using Bikya.DTOs.ShippingDTOs;
using Bikya.DTOs.ReviewDTOs;
using Bikya.Data.Enums;

namespace Bikya.DTOs.Orderdto
{
    public class OrderDetailsDTO
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;

        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;

        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SellerAmount { get; set; }

        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ShippingDetailsDto? ShippingInfo { get; set; }
        public List<ReviewDTO>? Reviews { get; set; }
    }
}
