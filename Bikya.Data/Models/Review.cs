using System;
using System.ComponentModel.DataAnnotations;

namespace Bikya.Data.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; } // Nullable comment

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Reviewer (User who wrote the review)
        public int ReviewerId { get; set; }
        public ApplicationUser? Reviewer { get; set; }

        // Seller (User being reviewed)
        public int SellerId { get; set; }
        public ApplicationUser? Seller { get; set; }

        // Product being reviewed (if needed for reference)
        //public int ProductId { get; set; }
        //public Product? Product { get; set; }

        // Order this review is related to
        public int OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
