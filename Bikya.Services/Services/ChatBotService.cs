using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.Services.Interfaces;
using Bikya.Services.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Bikya.Services.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly IChatBotFaqRepository _faqRepo;
        private readonly IChatMessageRepository _messageRepo;

        public ChatBotService(IChatBotFaqRepository faqRepo, IChatMessageRepository messageRepo)
        {
            _faqRepo = faqRepo;
            _messageRepo = messageRepo;
        }

 
        public async Task<ApiResponse<string>> GetResponseAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return ApiResponse<string>.ErrorResponse("Message cannot be empty", 400);

            var faq = await _faqRepo.GetMatchingFaqAsync(userMessage);

            var reply = faq?.Answer ?? "I'm your assistant! You can ask me about registration, payment, or order tracking.";

            await _messageRepo.AddAsync(new ChatMessage
            {
                UserMessage = userMessage,
                BotReply = reply
            });

            return ApiResponse<string>.SuccessResponse(reply);
        }

        public async Task<string> GetBotResponseAsync(string message)
        {
            var faqs = await _faqRepo.GetAllAsync();

            if (string.IsNullOrWhiteSpace(message))
                return "I'm here to help! Try asking me about registration, orders, or toys.";

            var bestMatch = faqs
                .Select(faq => new
                {
                    Faq = faq,
                    Score = faq.Keyword
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Max(k => StringSimilarity.Similarity(message, k.Trim()))
                })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null && bestMatch.Score >= 0.3)
                return bestMatch.Faq.Answer;

            return "I'm not sure I understood that. Try asking about registration, orders, or toys.";
        }

    }


}
