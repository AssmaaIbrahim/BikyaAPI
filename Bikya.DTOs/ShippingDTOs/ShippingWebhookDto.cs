using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikya.Data.Models;


namespace Bikya.DTOs.ShippingDTOs
{
    public class ShippingWebhookDto
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public ShippingStatus NewStatus { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Location { get; set; }
    }

}
