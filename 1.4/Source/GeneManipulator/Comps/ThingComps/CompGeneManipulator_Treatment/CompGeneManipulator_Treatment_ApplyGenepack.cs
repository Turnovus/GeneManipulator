using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace GeneManipulator
{
    public class CompGeneManipulator_Treatment_ApplyGenepack : CompGeneManipulator_Treatment
    {
        public override bool DoContents(Rect inRect)
        {
            Widgets.Label(inRect, "TODO");
            return false;
        }

        public override bool TreatmentAvailable(Building_GeneManipulator building, Pawn pawn, out string reason)
        {

            if (!building.HasAnyGenepack)
            {
                reason = "No genpacks available"; //TODO: Translation key
                return false;
            }

            int cpx = 0;
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                cpx += gene.def.biostatCpx;
            }
            if (cpx > building.AvailableComplexity)
            {
                reason = "Patient's genes too complex"; // TODO: Translation key
                return false;
            }

            reason = null;
            return true;
        }
    }
}
