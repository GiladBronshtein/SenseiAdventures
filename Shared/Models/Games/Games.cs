using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class Games
    {
        public int ID { get; set; }
        public string GameCode { get; set; }
        public string GameName { get; set; }
        public bool IsPublished { get; set; }
        public bool CanPublish { get; set; }
    }
}
