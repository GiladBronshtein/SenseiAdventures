using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.GPT
{
    public class GPTRequest
    {
        public string model { get; set; }
        public List<Message> messages { get; set; } = new List<Message>();
        public int max_tokens { get; set; }
    }
}
