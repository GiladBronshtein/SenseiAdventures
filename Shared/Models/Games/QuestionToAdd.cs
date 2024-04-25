using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionToAdd
    {
        public int GameID { get; set; }
        public bool HasImage { get; set; }

        [Required(ErrorMessage = "זהו שדה חובה")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "יש להזין שאלת משחק בין 2 ל-50 תווים")]
        public string QuestionDescription { get; set; }
        
        public string QuestionImage { get; set; }
        public int StageID { get; set; }

    }
}
