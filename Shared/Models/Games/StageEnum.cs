using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class StageEnum
    {
        public enum Stage
        {
            None,
            StageA = 1, //BottleBreak
            StageB = 2, //TargetHit
            StageC = 3, //BazzerHit
            StageD = 4 //SlashingAnswers
        }
    }
}
