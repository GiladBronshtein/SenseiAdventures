using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameAnswers
    {
        //public int ID { get; set; }
        public string AnswerDescription { get; set; }
        public string AnswerImage { get; set; }
        public int QuestionID { get; set; }
        public bool HasImage { get; set; }
        public bool IsCorrect { get; set; }
    }
}
