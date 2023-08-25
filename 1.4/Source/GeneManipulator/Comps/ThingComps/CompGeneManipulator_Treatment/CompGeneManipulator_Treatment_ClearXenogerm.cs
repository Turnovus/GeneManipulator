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
    public class CompGeneManipulator_Treatment_ClearXenogerm : CompGeneManipulator_Treatment
    {
        // UI Constants
        public const float HeaderHeight = 28f;
        public const float PostTreatmentBreakdownHeight = 120f;
        public const float BottomButtonHeight = 35f;
        public const float BottomButtonWidth = 80f;
        public const float PostOpHeaderRatio = 0.22f;
        public const float PostOpStatWidth = 450f;
        public const float ScrollbarWidth = 16f;
        public const float GenePadding = 2f;
        public const int GenesPerRow = 6;

        private XenotypeDef previewXenotype;
        private int previewCpx;
        private int previewMet;
        private Vector2 scrollPosition = Vector2.zero;

        public override bool DoContents(Rect inRect)
        {
            bool treatmentConfirmed;
            float bottomButtonX = 0.5f * (inRect.width - BottomButtonWidth);
            float geneSize, geneRows;
            int geneInRow;

            #region PARTITION RECT
            Rect headerRect = new Rect(0, 0, inRect.width, HeaderHeight);
            Rect geneRect = new Rect(0, HeaderHeight, inRect.width, inRect.height - HeaderHeight - PostTreatmentBreakdownHeight - BottomButtonHeight);
            Rect postOpRect = new Rect(0, geneRect.y + geneRect.height, inRect.width, PostTreatmentBreakdownHeight);
            Rect buttonRect = new Rect(bottomButtonX, postOpRect.y + PostTreatmentBreakdownHeight, BottomButtonWidth, BottomButtonHeight);

            Rect postOpHeaderRect = new Rect(0, 0, postOpRect.width, postOpRect.height * PostOpHeaderRatio);
            Rect postOpStatsRect = new Rect(0, postOpHeaderRect.height, postOpRect.width, postOpRect.height * (1f - PostOpHeaderRatio));
            Rect postOpComplexityRect = new Rect(0, postOpHeaderRect.height, postOpRect.width, 0.5f * postOpStatsRect.height);
            Rect postOpMetabolismRect = new Rect(0, postOpComplexityRect.y + postOpComplexityRect.height, postOpRect.width, postOpComplexityRect.height);

            Rect statRowRect = new Rect(0.5f * (postOpRect.width - PostOpStatWidth), 0f, PostOpStatWidth, postOpStatsRect.height * 0.5f);
            Rect statIconRect = new Rect(statRowRect.x, 0f, statRowRect.height, statRowRect.height);
            Rect statNameRect = new Rect(statRowRect.x + statRowRect.height, 0f, statRowRect.width - (2f * statRowRect.height), statRowRect.height);
            Rect statValueRect = new Rect(statNameRect.x + statNameRect.width, 0f, statRowRect.height, statRowRect.height);

            geneSize = (geneRect.width - ScrollbarWidth) / GenesPerRow;
            geneRows = (float)Math.Ceiling((double)(previewPawn.genes.Xenogenes.Count / GenesPerRow));


            Rect geneViewRect = new Rect(0f, 0f, geneRect.width - ScrollbarWidth, geneRows * geneSize);
            #endregion

            Widgets.BeginGroup(inRect);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, "The following genes will be removed:"); // TODO: Translation key

            Widgets.DrawHighlight(geneRect);

            #region DRAW REMOVED GENES
            Widgets.BeginScrollView(geneRect, ref scrollPosition, geneViewRect);
            Rect singleGeneRect = new Rect(0f, 0f, geneSize, geneSize);

            geneInRow = 0;

            foreach (Gene gene in previewPawn.genes.Xenogenes)
            {
                singleGeneRect.x = geneInRow * geneSize;
                GeneUIUtility.DrawGene(gene, singleGeneRect.ContractedBy(GenePadding), GeneType.Xenogene, clickable: false);
                geneInRow += 1;
                if (geneInRow >= GenesPerRow)
                {
                    geneInRow = 0;
                    singleGeneRect.y += geneSize;
                }
            }

            Widgets.EndScrollView();
            #endregion

            #region DRAW POST OP BIOSTATS
            Widgets.BeginGroup(postOpRect);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(postOpHeaderRect, "After the treatment, patient will be a " + previewXenotype.LabelCap); // TODO: Translation key
            Widgets.DrawHighlight(postOpStatsRect);
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.BeginGroup(postOpComplexityRect);
            GUI.DrawTexture(statIconRect, GeneUtility.GCXTex.Texture);
            Widgets.Label(statNameRect, "Genetic complexity: "); // TODO: Translation key
            Widgets.Label(statValueRect, previewCpx.ToString());
            Widgets.EndGroup();

            Widgets.BeginGroup(postOpMetabolismRect);
            GUI.DrawTexture(statIconRect, GeneUtility.METTex.Texture);
            Widgets.Label(statNameRect, "Metabolic efficiency: "); // TODO: Translation key
            Widgets.Label(statValueRect, previewMet.ToString());
            Widgets.EndGroup();

            Widgets.EndGroup();
            #endregion

            treatmentConfirmed = Widgets.ButtonText(buttonRect, "Confirm"); // TODO: Translation key

            Widgets.EndGroup();

            return treatmentConfirmed;
        }

        public override void EndTreatmentCycle(Pawn pawn)
        {
            base.EndTreatmentCycle(pawn);

            pawn.genes.ClearXenogenes();
            pawn.genes.SetXenotypeDirect(FindGermlineTypeFromEndogenes(pawn));
        }

        public override bool TreatmentAvailable(Building_GeneManipulator building, Pawn pawn, out string reason)
        {
            if ((pawn.genes == null || !pawn.genes.Xenogenes.Any()) && pawn.genes?.xenotypeName == null)
            {
                reason = "Patient does not have xenogenes"; // TODO: Translation key
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

        public override void OnTreatmentPreview(Building_GeneManipulator building, Pawn pawn)
        {
            base.OnTreatmentPreview(building, pawn);
            previewXenotype = FindGermlineTypeFromEndogenes(pawn);
            previewCpx = 0;
            previewMet = 0;
            scrollPosition = Vector2.zero;

            foreach (Gene gene in pawn.genes.Endogenes)
            {
                previewCpx += gene.def.biostatCpx;
                previewMet += gene.def.biostatMet;
            }
        }

        public override void OnTreatmentWithdrawn()
        {
            base.OnTreatmentWithdrawn();
            previewXenotype = null;
            previewCpx = 0;
            previewMet = 0;
            scrollPosition = Vector2.zero;
        }

        public static XenotypeDef FindGermlineTypeFromEndogenes(Pawn pawn)
        {
            if (pawn.genes == null)
                return XenotypeDefOf.Baseliner;

            List<GeneDef> endogenes = new List<GeneDef>();
            List<GeneDef> pawnGenes, typeGenes;

            foreach (Gene gene in pawn.genes.Endogenes)
            {
                if (gene.def.passOnDirectly)
                    endogenes.Add(gene.def);
            }

            if (endogenes.Count == 0)
                return XenotypeDefOf.Baseliner;

            foreach (XenotypeDef xeno in DefDatabase<XenotypeDef>.AllDefs)
            {
                if (xeno == XenotypeDefOf.Baseliner || !xeno.inheritable)
                    continue;
                
                pawnGenes = new List<GeneDef>(endogenes);
                typeGenes = new List<GeneDef>(xeno.genes.Where(g => g.passOnDirectly));

                foreach (GeneDef gene in endogenes)
                {
                    if (!typeGenes.Contains(gene))
                        break;

                    pawnGenes.Remove(gene);
                    typeGenes.Remove(gene);
                }

                if (pawnGenes.Count <= 0 && typeGenes.Count <= 0)
                    return xeno;
            }

            return XenotypeDefOf.Baseliner;
        }
    }
}
