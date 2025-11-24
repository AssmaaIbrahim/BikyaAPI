using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.ProductDTO
{
    public class GetProductImageDTO
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; }
    }
}
