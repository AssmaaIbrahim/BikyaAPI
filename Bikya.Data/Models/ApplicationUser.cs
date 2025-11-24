using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bikya.Data.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FullName { get; set; } // Required string
        public string? ProfileImageUrl { get; set; } // Nullable string

        public string Address { get; set; } // Required string
        public string City { get; set; } // Required string
        public string PostalCode { get; set; } // Required string
        public string PhoneNumber { get; set; } // Required string
       
        public bool IsVerified { get; set; }
        public ICollection<Product>? Products { get; set; }
        //public Wallet? Wallet { get; set; }
        public ICollection<Review>? ReviewsWritten { get; set; }
        public ICollection<Review>? ReviewsReceived { get; set; }
        public ICollection<Order>? OrdersBought { get; set; }
        public ICollection<Order>? OrdersSold { get; set; }
        public bool IsDeleted { get; set; } = false;
        public double? SellerRating { get; set; }



    }
}