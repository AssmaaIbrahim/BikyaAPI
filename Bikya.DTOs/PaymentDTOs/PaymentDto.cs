using Bikya.Data.Enums;
using System;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? StripeUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 