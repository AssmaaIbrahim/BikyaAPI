using Bikya.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Models
{
    public class Transaction
    {
       
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public string? Description { get; set; }

        public int? RelatedOrderId { get; set; }

        public int? PaymentId { get; set; }

        public Payment? Payment { get; set; }



    }

}
