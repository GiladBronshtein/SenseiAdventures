using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.GPT
{
    public class generatedTrueeFalseQuestions
    {
        public string Question { get; set; }
        public bool Answer { get; set; } // Add this property to denote correct answer
    }
}
