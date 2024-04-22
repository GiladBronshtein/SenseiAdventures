using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionsAndAnswers
    {
        public QuestionToAdd Questions { get; set; } = new QuestionToAdd();
        public GameAnswers Answers { get; set; } = new GameAnswers();
    }
}
