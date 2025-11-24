using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public AdminController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpPost("setup-delivery")]
        public async Task<IActionResult> SetupDeliverySystem()
        {
            var result = await _deliveryService.CreateDeliveryAccountAsync();
            
            if (result.Success)
            {
                return Ok(new { 
                    message = "Delivery system setup completed successfully",
                    credentials = new {
                        email = "delivery@bikya.com",
                        password = "Delivery@123",
                        note = "Please change the password after first login"
                    }
                });
            }
            
            return BadRequest(result);
        }
    }
}

