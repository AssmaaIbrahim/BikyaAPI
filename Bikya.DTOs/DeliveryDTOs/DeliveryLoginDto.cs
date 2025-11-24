using System.ComponentModel.DataAnnotations;

namespace Bikya.DTOs.DeliveryDTOs
{
    public class DeliveryLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

