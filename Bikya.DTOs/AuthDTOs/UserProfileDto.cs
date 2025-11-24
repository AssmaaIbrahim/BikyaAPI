using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.AuthDTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public string Address { get; set; } // Required string
        public string City { get; set; } // Required string
        public string PostalCode { get; set; } // Required string
        public string PhoneNumber { get; set; } // Required string
        public bool IsLocked { get; set; } // Required string


        public string? ProfileImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public double? SellerRating { get; set; } 

        public IList<string>? Roles { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
