using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Services.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;





namespace Bikya.Data.Repositories
{
    public class ChatBotFaqRepository : IChatBotFaqRepository
    {
        private readonly BikyaContext _context;

        public ChatBotFaqRepository(BikyaContext context)
        {
            _context = context;
        }


        public async Task<List<ChatBotFaq>> GetAllAsync()
        {
            return await _context.ChatBotFaqs.ToListAsync();
        }

        public async Task AddAsync(ChatBotFaq faq)
        {
            await _context.ChatBotFaqs.AddAsync(faq);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var faq = await _context.ChatBotFaqs.FindAsync(id);
            if (faq != null)
            {
                _context.ChatBotFaqs.Remove(faq);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<ChatBotFaq?> GetMatchingFaqAsync(string message)
        {
            var faqs = await _context.ChatBotFaqs.ToListAsync();

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

            return bestMatch != null && bestMatch.Score >= 0.3
                ? bestMatch.Faq
                : null;
        }

    }

}
