using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly BikyaContext _context;

        public ChatMessageRepository(BikyaContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetAllAsync()
        {
            return await _context.ChatMessages.ToListAsync();
        }
    }

}
