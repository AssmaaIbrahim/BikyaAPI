using Bikya.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bikya.Data.Models
{
    public class ExchangeRequest
    {
        public int Id { get; set; }

        // Offered product (the product being offered in exchange)
        public int OfferedProductId { get; set; }
        public Product OfferedProduct { get; set; }

        // Requested product (the product the user wants to get)
        public int RequestedProductId { get; set; }
        public Product RequestedProduct { get; set; }

        // Status tracking
        public ExchangeStatus Status { get; set; } = ExchangeStatus.Pending;
        public string? StatusMessage { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; }

        // Order references for the swap
        public int? OrderForOfferedProductId { get; set; }
        public Order? OrderForOfferedProduct { get; set; }
        
        public int? OrderForRequestedProductId { get; set; }
        public Order? OrderForRequestedProduct { get; set; }

        // Timestamps
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        // Additional information
        [MaxLength(500)]
        public string? Message { get; set; }

        // Navigation properties
        public ICollection<ExchangeStatusHistory> StatusHistory { get; set; } = new List<ExchangeStatusHistory>();
        
        // User who created the request
        public int? UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}