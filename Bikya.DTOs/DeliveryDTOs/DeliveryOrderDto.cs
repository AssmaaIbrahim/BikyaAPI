using Bikya.Data.Enums;
using Bikya.DTOs.UserDTOs;

namespace Bikya.DTOs.DeliveryDTOs
{
    public class DeliveryOrderDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int ProductId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        


        //buyer info
        public UserAddressInfoDto BuyerInfo { get; set; }
        public UserAddressInfoDto SellerInfo { get; set; }


        public ShippingStatus ShippingStatus { get; set; }

      

        // Exchange Order Linking
        public bool IsSwapOrder { get; set; }
        public int? RelatedOrderId { get; set; }
        public int? RelatedProductId { get; set; }
        public string? RelatedProductTitle { get; set; }
        public string ExchangeInfo { get; set; } = "";
    }
}

