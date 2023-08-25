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
    public class CompProperties_GeneManipulatorTreatment : CompProperties
    {
        public string treatmentName;
        public string treatmentDescription;
        public string texPath;
        public string key;
        public int cycleDurationTicks;
        public IntRange? xenogerminationTicks;
        public IntRange? genesRegrowingTicks;

        private CachedTexture icon;

        public CachedTexture Icon
        {
            get
            {
                if (icon == null)
                    icon = new CachedTexture(texPath);
                return icon;
            }
        }
    }

    public abstract class CompGeneManipulator_Treatment : ThingComp
    {
        protected Building_GeneManipulator previewBuilding;
        protected Pawn previewPawn;

        public CompProperties_GeneManipulatorTreatment Props => props as CompProperties_GeneManipulatorTreatment;

        // The player just started the treatment. Do initial treatment stuff. Returning true will
        // start a long-term treatment, which will call EndTreatmentCycle when done. Returning
        // false will allow the player to select another treatment immediately, without ejecting
        // the patient.
        public virtual bool DoTreatment(Pawn pawn, out int requiredComplexity)
        {
            requiredComplexity = 0;
            return Props.cycleDurationTicks > 0;
        }

        public virtual void EndTreatmentCycle(Pawn pawn)
        {
            // TODO: Xenogermination coma, genes regrowing
        }

        // Called when the player initially selects this treatment from the list of available
        // treatments.
        public virtual void OnTreatmentPreview(Building_GeneManipulator building, Pawn pawn)
        {
            previewBuilding = building;
            previewPawn = pawn;
        }

        // Called when the player decides against this treatment and goes back to the list of
        // available treatments, or when the player closes the treatment window while previewing
        // this treatment.
        public virtual void OnTreatmentWithdrawn()
        {
            previewBuilding = null;
            previewPawn = null;
        }
           
        // Fill the manipulator's treatment selection UI window. Return true if we should start the
        // treatment.
        public abstract bool DoContents(Rect inRect);

        public virtual bool TreatmentAvailable(Building_GeneManipulator building, Pawn pawn, out string reason)
        {
            reason = null;
            return true;
        }
    }

    public class CompGeneManipulator_Treatment_Dummy : CompGeneManipulator_Treatment
    {
        public override bool DoContents(Rect inRect)
        {
            Widgets.Label(inRect, Props.treatmentDescription);
            return false;
        }
    }
}
