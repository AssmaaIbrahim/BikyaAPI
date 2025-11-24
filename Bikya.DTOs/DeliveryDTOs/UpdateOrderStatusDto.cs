using Bikya.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bikya.DTOs.DeliveryDTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }

    public class UpdateDeliveryShippingStatusDto
    {
        [Required]
        public ShippingStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }
}

