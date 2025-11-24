using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Bikya.DTOs.UserDTOs
{
    public class UpdateUserDto
    {
        [StringLength(100, ErrorMessage = "Full name must be less than 100 characters.")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255, ErrorMessage = "Address must be less than 255 characters.")]
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

        [RegularExpression("^(User|Admin)$", ErrorMessage = "Role must be 'User' or 'Admin'.")]
        public string? Role { get; set; }
    }
}
