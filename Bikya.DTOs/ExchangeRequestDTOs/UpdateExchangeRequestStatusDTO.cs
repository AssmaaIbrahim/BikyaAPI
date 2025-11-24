using Bikya.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ExchangeRequestDTOs
{
    public class UpdateExchangeRequestStatusDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public ExchangeStatus Status { get; set; } // Pending, Approved, Rejected
    }
}
