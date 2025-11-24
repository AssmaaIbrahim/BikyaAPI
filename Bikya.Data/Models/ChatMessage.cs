using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public string UserMessage { get; set; }

        public string BotReply { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

}
