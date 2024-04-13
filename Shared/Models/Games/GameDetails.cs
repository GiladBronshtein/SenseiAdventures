using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameDetails
    {
        public int ID { get; set; }
        public string GameCode { get; set; }

        [Required(ErrorMessage = "Game Name is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Game Name must be between 3 and 30 characters")]
        [MinLength(3, ErrorMessage = "A game min name can have 3 chars")]
        public string GameName { get; set; }
        public int UserID { get; set; }
        public string GameEndMessage { get; set; }
        public bool IsPublished { get; set; }
        public bool CanPublish { get; set; }
        public string QuestionDescription { get; set; }
        public bool QuestionHasImage { get; set; }
        public string QuestionImageText { get; set; }
        public string QuestionCorrectCategory { get; set; }
        public string QuestionWrongCategory { get; set; }
        public List<GameAnswers> Answers { get; set; }
        public List<GameAnswers> AnswersToDelete { get; set; }
    }
}
