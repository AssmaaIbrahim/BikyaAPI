using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.CategoryDTOs
{
    public class CategoryDTO
    {

        public int Id { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? ParentCategoryId { get; set; }

        public string? ParentName { get; set; }
        public DateTime CreatedAt { get; set; }


    }
}
