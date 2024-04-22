using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameAnswers
    {
        //public int ID { get; set; }

        [Required(ErrorMessage = "יש להזין תשובה בין 2 ל-30 תווים")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Answer description must be between 2 and 30 characters")]
        [MinLength(2, ErrorMessage = "Answer description min name can have 2 chars")]
        [MaxLength(30, ErrorMessage = "Answer description max name can have 30 chars")]
        public string AnswerDescription { get; set; }

        public string AnswerImage { get; set; }
        public int QuestionID { get; set; }
        public bool HasImage { get; set; }
        public bool IsCorrect { get; set; }
    }
}
