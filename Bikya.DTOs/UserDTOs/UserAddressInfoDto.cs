using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.UserDTOs
{
    public class UserAddressInfoDto
    {
        public int Id { get; set; } // Required integer
        public string FullName { get; set; } // Required string

    public string Email { get; set; }
        public string Address { get; set; } // Required string
        public string City { get; set; } // Required string
        public string PostalCode { get; set; } // Required string
        public string PhoneNumber { get; set; } // Required string


   



       



    }
}
