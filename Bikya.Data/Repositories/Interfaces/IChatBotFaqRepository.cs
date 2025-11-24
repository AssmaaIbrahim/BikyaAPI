using Bikya.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IChatBotFaqRepository
    {
        Task<ChatBotFaq?> GetMatchingFaqAsync(string message);
        Task<List<ChatBotFaq>> GetAllAsync();
        Task AddAsync(ChatBotFaq faq);
        Task DeleteAsync(int id);

    }

}
