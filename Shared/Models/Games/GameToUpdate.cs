using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseiAdventures.Shared.Models.Games
{
    public class GameToUpdate
    {
        public int ID { get; set; }
        public string GameName { get; set; }
        public string QuestionDescription { get; set; }
        public string GameEndMessage { get; set; }
        public string QuestionCorrectCategory { get; set; }
        public string QuestionWrongCategory { get; set; }
        public string QuestionImageText { get; set; }
        public bool QuestionHasImage { get; set; }
        public List<GameAnswers> Answers { get; set; }
        public List<GameAnswers> AnswersToDelete { get; set; }
    }
}
