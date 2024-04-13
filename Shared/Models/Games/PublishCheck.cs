using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class PublishCheck
    {
        public int ID { get; set; }
        public bool IsPublished { get; set; }
        public bool CanPublish { get; set; }
    }
}
