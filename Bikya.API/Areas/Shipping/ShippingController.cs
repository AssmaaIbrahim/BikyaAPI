using Bikya.DTOs.ShippingDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Areas.Shipping
{
    /// <summary>
    /// Controller for managing shipping operations.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Shipping")]
    [Authorize]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingController(IShippingService shippingService)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
        }

        /// <summary>
        /// Creates a new shipping record.
        /// </summary>
        /// <param name="dto">Shipping creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShippingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shippingService.CreateAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets a shipping record by ID.
        /// </summary>
        /// <param name="id">Shipping ID</param>
        /// <returns>Shipping details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid shipping ID" });

            var result = await _shippingService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets all shipping records (Admin only).
        /// </summary>
        /// <returns>List of all shipping records</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _shippingService.GetAllAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates shipping status (Admin only).
        /// </summary>
        /// <param name="id">Shipping ID</param>
        /// <param name="dto">Status update data</param>
        /// <returns>Status update result</returns>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateShippingStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Invalid shipping ID" });

            var result = await _shippingService.UpdateStatusAsync(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Deletes a shipping record (Admin only).
        /// </summary>
        /// <param name="id">Shipping ID</param>
        /// <returns>Deletion result</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid shipping ID" });

            var result = await _shippingService.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tracks a shipment by tracking number (public access).
        /// </summary>
        /// <param name="trackingNumber">Tracking number</param>
        /// <returns>Tracking information</returns>
        [AllowAnonymous]
        [HttpGet("track/{trackingNumber}")]
        public async Task<IActionResult> Track(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return BadRequest(new { message = "Tracking number is required" });

            var result = await _shippingService.TrackAsync(trackingNumber);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Calculates shipping cost (public access).
        /// </summary>
        /// <param name="dto">Shipping cost calculation data</param>
        /// <returns>Shipping cost information</returns>
        [AllowAnonymous]
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateCost([FromBody] ShippingCostRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shippingService.CalculateCostAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Integrates with a shipping provider (Admin only).
        /// </summary>
        /// <param name="provider">Provider name</param>
        /// <param name="dto">Integration data</param>
        /// <returns>Integration result</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("integrate/{provider}")]
        public async Task<IActionResult> Integrate(string provider, [FromBody] ThirdPartyShippingRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest(new { message = "Provider name is required" });

            dto.Provider = provider;
            var result = await _shippingService.IntegrateWithProviderAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Handles webhook from shipping provider (public access).
        /// </summary>
        /// <param name="provider">Provider name</param>
        /// <param name="dto">Webhook data</param>
        /// <returns>Webhook processing result</returns>
        [AllowAnonymous]
        [HttpPost("webhook/{provider}")]
        public async Task<IActionResult> Webhook(string provider, [FromBody] ShippingWebhookDto dto)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest(new { message = "Provider name is required" });

            var result = await _shippingService.HandleWebhookAsync(provider, dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
