using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using template.Shared.Models.Games;
namespace template.Shared.Models.Users
{
    public class UserWithGames
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public List<Games.Games> Games { get; set; }
    }
}
