using Bikya.DTOs.ShippingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.Orderdto
{
    public class CreateOrderDTO
    {
        public int ProductId { get; set; }
        public int BuyerId { get; set; }

        public ShippingInfoDTO ShippingInfo { get; set; } 

        public string PaymentMethod { get; set; } = "Cash";
        
        /// <summary>
        /// Indicates whether this order is part of a product swap
        /// </summary>
        public bool IsSwapOrder { get; set; } = false;
    }

}
