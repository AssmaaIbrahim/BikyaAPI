using Bikya.DTOs.DeliveryDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DeliveryLoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _deliveryService.LoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("orders")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetOrdersForDelivery()
        {
            var result = await _deliveryService.GetOrdersForDeliveryAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("orders/{id}")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _deliveryService.GetOrderByIdAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        [HttpGet("orders/{id}/status-summary")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetOrderStatusSummary(int id)
        {
            var result = await _deliveryService.GetOrderStatusSummaryAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        [HttpGet("orders/{id}/available-transitions")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetAvailableTransitions(int id)
        {
            var result = await _deliveryService.GetAvailableTransitionsAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        [HttpPut("orders/{id}/status")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _deliveryService.UpdateOrderStatusAsync(id, updateDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPut("orders/{id}/shipping-status")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> UpdateShippingStatus(int id, [FromBody] UpdateDeliveryShippingStatusDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _deliveryService.UpdateShippingStatusAsync(id, updateDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("orders/{id}/synchronize")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> SynchronizeOrderStatus(int id)
        {
            var result = await _deliveryService.SynchronizeOrderStatusAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("setup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDeliveryAccount()
        {
            var result = await _deliveryService.CreateDeliveryAccountAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
    }
}

