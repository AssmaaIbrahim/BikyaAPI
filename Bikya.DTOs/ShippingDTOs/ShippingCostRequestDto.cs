using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ShippingDTOs
{
    public class ShippingCostRequestDto
    {
        public double Weight { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Method { get; set; } = "Standard";
    }
}
