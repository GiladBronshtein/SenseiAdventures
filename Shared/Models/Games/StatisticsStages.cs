using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class StatisticsStages
    {
        public int ID { get; set; }
        public int GameID { get; set; }
        public int StageID { get; set; }
        public string Trophy { get; set; }
        public string StageGrade { get; set; }
        public string StageTime { get; set; }
    }
}
