using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ShippingDTOs
{
    public class ThirdPartyShippingRequestDto
    {
        public int ShippingId { get; set; }
        public string Provider { get; set; } = "Aramex";
        public string ApiKey { get; set; } = string.Empty; 
    }

}
