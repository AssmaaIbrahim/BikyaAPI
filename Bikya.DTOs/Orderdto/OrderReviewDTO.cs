using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.DTOs.ShippingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.Orderdto
{
    public class OrderReviewDTO
    {

            public int Id { get; set; }

            public int ProductId { get; set; }
            public string ProductTitle { get; set; }

            public int BuyerId { get; set; }
            public string BuyerName { get; set; }

            public int SellerId { get; set; }
            public string SellerName { get; set; }

     

            public OrderStatus Status { get; set; }
            public DateTime CreatedAt { get; set; }

            public bool IsSwapOrder { get; set; } = false;
        }
    }


