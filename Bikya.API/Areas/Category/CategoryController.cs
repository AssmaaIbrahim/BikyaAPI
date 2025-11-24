using Bikya.Data.Response;
using Bikya.DTOs.CategoryDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Areas.Category
{
    /// <summary>
    /// Controller for managing category operations.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Category")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets all categories with pagination and search.
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="search">Search term</param>
        /// <returns>Paginated list of categories</returns>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 9, [FromQuery] string? search = null)
        {
            var response = await _service.GetPaginatedAsync(page, pageSize, search);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null)
        {
            var response = await _service.GetAllAsync(search);
            return StatusCode(response.StatusCode, response);
        }


        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBulk([FromBody] BulkCreateCategoryDTO request)
        {
            var response = await _service.CreateBulkAsync(request.Categories);
            return StatusCode(response.StatusCode, response);
        }
        /// <summary>
        /// Gets a category by ID.
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid category ID" });

            var response = await _service.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets a category by name.
        /// </summary>
        /// <param name="name">Category name</param>
        /// <returns>Category details</returns>
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Category name is required" });

            var response = await _service.GetByNameAsync(name);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Creates a new category (Admin only).
        /// </summary>
        /// <param name="dto">Category creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromForm] CreateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _service.AddAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Updates a category (Admin only).
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="dto">Category update data</param>
        /// <returns>Update result</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Invalid category ID" });

            var response = await _service.UpdateAsync(id, dto);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Deletes a category (Admin only).
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid category ID" });

            var response = await _service.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
