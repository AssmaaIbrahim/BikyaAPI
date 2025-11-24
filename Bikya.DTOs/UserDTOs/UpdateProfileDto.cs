using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Bikya.DTOs.UserDTOs
{
    public class UpdateProfileDto
    {
        [StringLength(100, ErrorMessage = "Full name must be less than 100 characters.")]
        public string? FullName { get; set; }

        [StringLength(255, ErrorMessage = "Address must be less than 255 characters.")]

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City must be less than 100 characters.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(20, ErrorMessage = "Postal code must be less than 20 characters.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain exactly 11 digits.")]
        public string PhoneNumber { get; set; } = string.Empty;


        [StringLength(500, ErrorMessage = "Profile image URL must be less than 500 characters.")]
        [Url(ErrorMessage = "Invalid profile image URL.")]
        public string? ProfileImageUrl { get; set; }


    }
}
