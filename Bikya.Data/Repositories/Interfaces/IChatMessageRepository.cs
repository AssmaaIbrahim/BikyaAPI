using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage message);
        Task<List<ChatMessage>> GetAllAsync();
    }

}
