using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.GPT
{
    public class QuestionsFromGPT
    {
        public string information { get; set; }
        public int countQuestions { get; set; }
        public string audienceDescription { get; set; }
    }
}
