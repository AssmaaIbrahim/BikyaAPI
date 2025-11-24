using Bikya.Data.Enums;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikya.Data.Models;


namespace Bikya.DTOs.ShippingDTOs
{
    public class UpdateShippingStatusDto
    {
        public ShippingStatus Status { get; set; }
    }

}
