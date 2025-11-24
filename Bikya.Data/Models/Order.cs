using Bikya.Data.Enums;

namespace Bikya.Data.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Relationships
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int BuyerId { get; set; }
        public ApplicationUser Buyer { get; set; }

        public int SellerId { get; set; }
        public ApplicationUser Seller { get; set; }

        public ShippingInfo ShippingInfo { get; set; }
        public List<Review>? Reviews { get; set; }

        // Financial Info
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SellerAmount { get; set; }

        // Status Info
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsSwapOrder { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
