using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ProductDTO
{
    public class UpdatProductImage
    {
        [Required]
        public IFormFile Image { get; set; }
  
    }
}
