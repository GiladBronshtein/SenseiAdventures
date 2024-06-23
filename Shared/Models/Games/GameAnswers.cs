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
        public int ID { get; set; }

        [Required(ErrorMessage = "זהו שדה חובה")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "יש להזין תשובה בין 2 ל-30 תווים")]
        public string AnswerDescription { get; set; }
        public string AnswerImage { get; set; }
        public int QuestionID { get; set; }
        public bool HasImage { get; set; }
        public bool IsCorrect { get; set; }
    }
}
