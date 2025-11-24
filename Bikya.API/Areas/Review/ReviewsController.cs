using Bikya.Data.Response;
using Bikya.DTOs.ReviewDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Areas.Review
{
    /// <summary>
    /// Controller for managing review operations.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Review")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets all reviews.
        /// </summary>
        /// <returns>List of all reviews</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _service.GetAllAsync();
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets a review by ID.
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <returns>Review details</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid review ID" });

            var response = await _service.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets reviews by order ID.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>List of reviews for the order</returns>
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID" });

            var response = await _service.GetReviewsByOrderIdAsync(orderId);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets reviews by seller ID.
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <returns>List of reviews for the seller</returns>
        [HttpGet("seller/{sellerId:int}")]
        public async Task<IActionResult> GetBySellerId(int sellerId)
        {
            if (sellerId <= 0)
                return BadRequest(new { message = "Invalid seller ID" });

            var response = await _service.GetReviewsBySellerIdAsync(sellerId);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets reviews by reviewer ID.
        /// </summary>
        /// <param name="reviewerId">Reviewer ID</param>
        /// <returns>List of reviews by the reviewer</returns>
        [HttpGet("user/{reviewerId:int}")]
        public async Task<IActionResult> GetByReviewerId(int reviewerId)
        {
            if (reviewerId <= 0)
                return BadRequest(new { message = "Invalid reviewer ID" });

            var response = await _service.GetReviewsByReviewerIdAsync(reviewerId);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Creates a new review.
        /// </summary>
        /// <param name="dto">Review creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] CreateReviewDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _service.AddAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Updates a review.
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <param name="dto">Review update data</param>
        /// <returns>Update result</returns>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Invalid review ID" });

            var response = await _service.UpdateAsync(id, dto);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Deletes a review.
        /// </summary>
        /// <param name="id">Review ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid review ID" });

            var response = await _service.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
