using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikya.Data.Models;

namespace Bikya.DTOs.ShippingDTOs
{
    public class TrackShipmentDto
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public ShippingStatus Status { get; set; }
        public string LastLocation { get; set; } = string.Empty;
        public DateTime? EstimatedArrival { get; set; }
    }

}
