using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameAnswers
    {
        public int ID { get; set; }
        public string AnswerDescription { get; set; }
        public bool HasImage { get; set; }
        public string AnswerImageText { get; set; }
        public bool IsCorrect { get; set; }
        //public int GameID { get; set; }
    }
}
