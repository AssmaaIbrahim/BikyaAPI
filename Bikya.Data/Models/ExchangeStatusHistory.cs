using Bikya.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bikya.Data.Models
{
    public class ExchangeStatusHistory
    {
        public int Id { get; set; }
        public int ExchangeRequestId { get; set; }
        public ExchangeRequest ExchangeRequest { get; set; }
        public ExchangeStatus Status { get; set; }
        public string? Message { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? ChangedByUserId { get; set; }
    }
}
