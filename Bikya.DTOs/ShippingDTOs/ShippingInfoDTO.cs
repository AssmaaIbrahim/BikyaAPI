using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ShippingDTOs
{
    public class ShippingInfoDTO
    {
        // Make fields nullable to avoid [ApiController] automatic 400 for missing values,
        // we validate/upsert safely in the service layer.
        public string? RecipientName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal ShippingFee { get; set; } = 50.0m;
        public string Status { get; set; } = "Pending";
        public object? TrackingNumber { get; set; }
    }

}
