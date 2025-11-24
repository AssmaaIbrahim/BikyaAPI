using System;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StripeUrl { get; set; } = string.Empty;
        public string StripeSessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 