using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ReviewDTOs
{
    public class UpdateReviewDTO
    {
        [Required]
        public int Id { get; set; }  

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        [Required]
        public int ReviewerId { get; set; } 

        [Required]
        public int OrderId { get; set; }     
    }

}
