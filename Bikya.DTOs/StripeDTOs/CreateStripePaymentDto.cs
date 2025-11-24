using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.StripeDTOs
{
    public class CreateStripePaymentDto
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public int OrderId { get; set; }
    }

}
