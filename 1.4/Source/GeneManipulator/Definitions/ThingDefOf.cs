using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace GeneManipulator.Definitions
{
    [DefOf]
    public static class ThingDefOf
    {
        [DefAlias("Turn_Building_GeneManipulator")]
        public static ThingDef Building_GeneManipulator;

        static ThingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
    }
}
