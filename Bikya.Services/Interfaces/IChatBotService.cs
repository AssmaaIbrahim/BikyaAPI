using Bikya.Data.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface IChatBotService
    {
        Task<ApiResponse<string>> GetResponseAsync(string userMessage);
    }

}
