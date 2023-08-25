using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace GeneManipulator
{
    public class WorkGiver_EnterGeneManipulator : WorkGiver_EnterBuilding
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForDef(Definitions.ThingDefOf.Building_GeneManipulator);
    }
}
