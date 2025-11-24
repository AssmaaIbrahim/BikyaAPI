using Bikya.Data.Response;
using Bikya.DTOs.ChatbotDTO;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bikya.API.Areas.Chatbot
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatBotController : ControllerBase
    {
        private readonly IChatBotService _chatBotService;

        public ChatBotController(IChatBotService chatBotService)
        {
            _chatBotService = chatBotService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> GetBotResponse([FromBody] ChatRequestDto request)
        {
            var response = await _chatBotService.GetResponseAsync(request.Message);
            return StatusCode(200, response); // أو response.StatusCode لو كنت بتخصص
        }

    }
}
