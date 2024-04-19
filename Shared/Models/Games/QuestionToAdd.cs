using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionToAdd
    {
        public int GameID { get; set; }
        public bool HasImage { get; set; }
        public string QuestionDescription { get; set; }
        public string QuestionImage { get; set; }
        public int StageID { get; set; }
        //public List<GameAnswers> Answers { get; set; }
    }
}
