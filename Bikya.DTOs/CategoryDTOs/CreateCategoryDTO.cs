using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.DTOs.CategoryDTOs
{
    public class CreateCategoryDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public IFormFile? Icon { get; set; }
        public string? IconUrl { get; set; } = null;
        public string? parentName { get; set; }
        public int? ParentCategoryId { set; get; }

    }
}
