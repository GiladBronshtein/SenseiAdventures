using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameSettingsToUpdate
    {
        public int ID { get; set; }
        public string GameCode { get; set; }
        [Required(ErrorMessage = "Game Name is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Game Name must be between 3 and 30 characters")]
        [MinLength(3, ErrorMessage = "A game min name can have 3 chars")]
        public string GameName { get; set; }
        public string EndingMessage { get; set; }
        public string GameImage { get; set; }
        public bool GameHasImage { get; set; }

    }
}
