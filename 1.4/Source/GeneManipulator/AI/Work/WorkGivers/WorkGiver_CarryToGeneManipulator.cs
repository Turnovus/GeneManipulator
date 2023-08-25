using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace GeneManipulator
{
    class WorkGiver_CarryToGeneManipulator : WorkGiver_CarryToBuilding
    {
        public override ThingRequest ThingRequest =>
            ThingRequest.ForDef(Definitions.ThingDefOf.Building_GeneManipulator);
    }
}
