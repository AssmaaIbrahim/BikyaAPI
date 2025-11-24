using System;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentStatusDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StripeSessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 