using System;
using Bikya.Data.Enums;

namespace Bikya.Data.Models
{
    public class ShippingInfo
    {
        public int ShippingId { get; set; }
        public  string RecipientName { get; set; } 
        public  string Address { get; set; } // Required string
        public  string City { get; set; } // Required string
        public  string PostalCode { get; set; } // Required string
        public  string PhoneNumber { get; set; } // Required string
        public ShippingStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public int OrderId { get; set; }
        public  Order? Order { get; set; } // Required navigation property
        public decimal ShippingFee { get; set; }
        public string? ShippingMethod { get; set; }
    }
}