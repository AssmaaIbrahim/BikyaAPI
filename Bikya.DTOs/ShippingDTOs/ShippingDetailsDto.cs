using Bikya.Data.Enums;
﻿using Bikya.Data.Enums;
using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ShippingDTOs
{
    public class ShippingDetailsDto
    {
        public int ShippingId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public ShippingStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public int OrderId { get; set; }
    }
}
