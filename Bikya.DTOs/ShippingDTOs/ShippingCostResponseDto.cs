using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ShippingDTOs
{
    public class ShippingCostResponseDto
    {
        public double Cost { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
    }
}
