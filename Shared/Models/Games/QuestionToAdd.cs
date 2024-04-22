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

        [Required(ErrorMessage = "יש להזין שאלת משחק בין 2 ל-30 תווים")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Game Name must be between 2 and 30 characters")]
        [MinLength(2, ErrorMessage = "A game min name can have 2 chars")]
        [MaxLength(30, ErrorMessage ="A game max name can have 30 chars")]
        public string QuestionDescription { get; set; }
        
        public string QuestionImage { get; set; }
        public int StageID { get; set; }

    }
}
