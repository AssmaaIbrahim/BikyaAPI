using System.ComponentModel.DataAnnotations;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من 0")]
        public decimal Amount { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        public string? Description { get; set; }
    }
} 