using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.GPT
{
    public class GeneratedQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public string Answer { get; set; }
    }
}
