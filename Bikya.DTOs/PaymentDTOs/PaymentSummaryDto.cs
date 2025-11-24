using System;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentSummaryDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }
} 