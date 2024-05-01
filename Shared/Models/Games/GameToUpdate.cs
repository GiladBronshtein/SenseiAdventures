using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameToUpdate
    {
        public int ID { get; set; }
        public string GameCode { get; set; }
        public string GameName { get; set; }
        public string EndingMessage { get; set; }
        public bool GameHasImage { get; set; }
        public string GameImage { get; set; }
        public List<GameAnswers> Answers { get; set; }
        public List<GameAnswers> AnswersToDelete { get; set; }
    }
}
