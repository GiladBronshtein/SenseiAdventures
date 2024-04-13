using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseiAdventures.Shared.Models.Games;
namespace SenseiAdventures.Shared.Models.Users
{
    public class UserWithGames
    {
        public string FirstName { get; set; }
        public List<Games.Games> Games { get; set; }
    }
}
