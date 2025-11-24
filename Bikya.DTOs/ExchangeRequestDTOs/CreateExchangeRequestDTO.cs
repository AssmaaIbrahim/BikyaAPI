using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ExchangeRequestDTOs
{
    public class CreateExchangeRequestDTO
    {
        [Required]
        public int OfferedProductId { get; set; }

        [Required]
        public int RequestedProductId { get; set; }

        [MaxLength(1000)]
        public string? Message { get; set; }
    }
}
