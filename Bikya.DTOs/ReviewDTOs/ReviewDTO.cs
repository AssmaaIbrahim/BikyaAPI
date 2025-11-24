using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ReviewDTOs
{
    public class ReviewDTO
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ReviewerId { get; set; }          
        public string BuyerName { get; set; } = "";  

        public int SellerId { get; set; }           
        public string SellerName { get; set; } = "";

        public int OrderId { get; set; }

        //public string ProductName { get; set; } = "";
    }

}
