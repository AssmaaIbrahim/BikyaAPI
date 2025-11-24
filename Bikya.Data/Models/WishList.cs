using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Models
{
    public  class WishList
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int UserId { get; set; }

        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
