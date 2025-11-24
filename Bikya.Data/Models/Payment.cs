using Bikya.Data.Enums;
using System;

namespace Bikya.Data.Models
{
    public class Payment
    {
      
        public int Id { get; set; }
        public int UserId { get; set; }
 
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public PaymentGateway Gateway { get; set; }

        public string? StripeSessionId { get; set; } // ãåã ááÑÈØ ãÚ Webhook
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();



    }
} 