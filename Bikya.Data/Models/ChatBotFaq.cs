using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Models
{
    public class ChatBotFaq
    {
        public int Id { get; set; }

        public string Keyword { get; set; }      
        public string Answer { get; set; }       
    }

}
