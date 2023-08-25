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
    [StaticConstructorOnStartup]
    public class Dialog_GeneTreatment : Window
    {
        public const float InnerPadding = 5f;
        public const float UpperBarHeight = 35f;
        public const float LowerBarHeight = 35f;
        public const float ButtonWidth = 180f;
        public const float ButtonMargin = 5f;
        public const float WindowWidth = 720f; // 768 is a decent fallback if you break this value
        public const float WindowHeightPercent = 0.85f;
        public const float ScrollbarWidth = 16f;
        public const float TreatmentItemHeight = 160f;
        public const float TreatmentItemDurationHeight = 20f;
        public const float TreatmentIconPadding = 5f;
        public const float TreatmentItemLowerPadding = 5f;
        public const float TreatmentInnerPadding = 1f;
        public const float TreatmentLabelHeightPercent = 0.3f;
        public const float TreatmentSelectButtonWidth = 100f;
        public const float TreatmentSelectButtonHeight = 35f;
        public const float TreatmentDescriptionSideMargin = 8f;
        public const float ComplexityIconSize = 30f;
        public const float ComplexityTextWidth = 30f;
        public const float ComplexityTextMargin = 5f;

        // Scroll position for the initial menu that shows all possible treatment options
        private Vector2 selectionScrollPosition = Vector2.zero;

        private Building_GeneManipulator building;

        private Pawn patient;

        private CompGeneManipulator_Treatment selectedTreatment;

        public override Vector2 InitialSize => new Vector2(WindowWidth, UI.screenHeight * WindowHeightPercent);

        public override bool CausesMessageBackground() => true;

        public Dialog_GeneTreatment(Building_GeneManipulator building, Pawn patient)
        {
            this.building = building;
            this.patient = patient;
            forcePause = true; // Pause the game until the player closes this window
        }

        public override void DoWindowContents(Rect inRect)
        {
            CompGeneManipulator_Treatment newTreatment = null;

            bool closeWindow = false;

            float innerWidth = inRect.width - InnerPadding * 2f;
            float innerHeight = inRect.height - InnerPadding * 2f;

            Rect topRect = new Rect(InnerPadding, InnerPadding, innerWidth, UpperBarHeight);
            Rect innerRect = new Rect(InnerPadding, InnerPadding + UpperBarHeight, innerWidth, innerHeight - UpperBarHeight - LowerBarHeight);
            Rect lowerRect = new Rect(InnerPadding, InnerPadding + innerRect.y + innerRect.height, innerWidth, LowerBarHeight);

            Rect bottomButtonCenter = new Rect(lowerRect.x + lowerRect.width * 0.5f - ButtonWidth * 0.5f, lowerRect.y, ButtonWidth, LowerBarHeight);
            Rect bottomButtonLeft = new Rect(lowerRect.x + lowerRect.width * 0.5f - ButtonMargin * 0.5f - ButtonWidth, lowerRect.y, ButtonWidth, LowerBarHeight);
            Rect bottomButtonRight = new Rect(bottomButtonLeft.x + ButtonMargin + ButtonWidth, lowerRect.y, ButtonWidth, LowerBarHeight);

            Rect complexityIcon = new Rect(lowerRect.x, lowerRect.y + (LowerBarHeight - ComplexityIconSize) * 0.5f, ComplexityIconSize, ComplexityIconSize);
            Rect complexityText = new Rect(complexityIcon.x + complexityIcon.width + ComplexityTextMargin, complexityIcon.y, ComplexityTextWidth, ComplexityIconSize);


            Widgets.BeginGroup(inRect);

            string headerText = selectedTreatment == null ?
                "Select treatment for " + patient.LabelCap : // TODO: Translation key
                selectedTreatment.Props.treatmentName;
            DrawBigLabelSterile(topRect, headerText);

            if (selectedTreatment == null)
            {
                #region DRAW TREATMENT SELECTION MENU
                List<CompGeneManipulator_Treatment> treatments = new List<CompGeneManipulator_Treatment>();
                foreach (ThingComp comp in building.AllComps)
                {
                    if (comp is CompGeneManipulator_Treatment treatmentComp)
                        treatments.Add(treatmentComp);
                }

                newTreatment = DrawAllTreatments(innerRect, treatments, building, patient);
                #endregion
            }
            else
            {
                if (selectedTreatment.DoContents(innerRect)) // Draw call for selected treatment
                {
                    // If the treatment starts a long-term cycle, then do that and close the
                    // treatment selection window. DoTreatment runs either way, which means that we
                    // can have treatments that take effect instantly and then immediately allow
                    // another one to be selected.
                    if (selectedTreatment.DoTreatment(patient, out int complexity))
                    {
                        building.StartTreatment(selectedTreatment, complexity);
                        closeWindow = true;
                    }

                    // Clear the selected treatment and allow it to reset its internal variables.
                    // This also has the effect of returning the player to the treatment selection
                    // screen if a long-term treatment cycle was not started.
                    selectedTreatment.OnTreatmentWithdrawn();
                    selectedTreatment = null;
                }

                // Sanitize font changes made by the selected treatment
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            #region DRAW LOWER BAR

            GUI.DrawTexture(complexityIcon, GeneUtility.GCXTex.Texture);
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(complexityText, building.AvailableComplexity.ToString());
            Text.Anchor = anchor;

            // TODO: Translation keys
            if (selectedTreatment == null)
            {
                closeWindow |= Widgets.ButtonText(bottomButtonCenter, "eject");
            }
            else
            {
                closeWindow |= Widgets.ButtonText(bottomButtonLeft, "eject");
                if (Widgets.ButtonText(bottomButtonRight, "back"))
                {
                    selectedTreatment.OnTreatmentWithdrawn();
                    selectedTreatment = null;
                }
            }

            #endregion

            Widgets.EndGroup(); //inRect

            if (newTreatment != null)
            {
                newTreatment.OnTreatmentPreview(building, patient);
                selectedTreatment = newTreatment;
            }

            if (closeWindow)
                Close();
        }

        public override void Close(bool doCloseSound = true)
        {
            if (selectedTreatment != null)
                selectedTreatment.OnTreatmentWithdrawn();

            if (!building.DoingTreatmentNow)
                building.EjectContents();

            base.Close(doCloseSound);
        }

        private CompGeneManipulator_Treatment DrawAllTreatments(Rect inRect, List<CompGeneManipulator_Treatment> treatments, Building_GeneManipulator building, Pawn pawn)
        {
            CompGeneManipulator_Treatment selectedTreatment = null;
            float y = 0;
            bool other = false;

            if (treatments.NullOrEmpty())
                return null;

            float viewHeight = (TreatmentItemHeight + TreatmentItemLowerPadding) * treatments.Count;
            Rect viewRect = new Rect(0f, 0f, inRect.width - ScrollbarWidth, viewHeight);
            Widgets.BeginScrollView(inRect, ref selectionScrollPosition, viewRect);

            foreach (CompGeneManipulator_Treatment treatment in treatments)
            {
                Rect rect = new Rect(0f, y, viewRect.width, TreatmentItemHeight);

                if (other)
                    Widgets.DrawLightHighlight(rect);

                if (DoTreatmentListing(rect, treatment, building, pawn))
                    selectedTreatment = treatment;

                y += TreatmentItemHeight + TreatmentItemLowerPadding;
                other = !other;
            }

            Widgets.EndScrollView();

            return selectedTreatment;
        }

        private static bool DoTreatmentListing(Rect inRect, CompGeneManipulator_Treatment comp, Building_GeneManipulator building, Pawn pawn)
        {
            float textWidth;
            bool selected = false;
            string lockedReason;

            Rect paddedRect = inRect.ContractedBy(TreatmentInnerPadding);
            float treatmentTextHeight = paddedRect.height - TreatmentItemDurationHeight;

            Widgets.BeginGroup(paddedRect);

            Rect icon = new Rect(0f, 0f, paddedRect.height, paddedRect.height);
            textWidth = paddedRect.width - icon.width - TreatmentSelectButtonWidth - TreatmentIconPadding;
            Rect label = new Rect(icon.width + TreatmentIconPadding, 0f, textWidth, treatmentTextHeight * TreatmentLabelHeightPercent);
            Rect description = new Rect(icon.width + TreatmentIconPadding, label.height, textWidth, treatmentTextHeight * (1f - TreatmentLabelHeightPercent)).ContractedBy(TreatmentDescriptionSideMargin, 0f);
            Rect duration = new Rect(label.x, treatmentTextHeight, textWidth, TreatmentItemDurationHeight);
            Rect button = new Rect(icon.width + textWidth, paddedRect.height * 0.5f - TreatmentSelectButtonHeight * 0.5f, TreatmentSelectButtonWidth, TreatmentSelectButtonHeight);

            GUI.DrawTexture(icon, comp.Props.Icon.Texture);

            DrawBigLabelSterile(label, comp.Props.treatmentName);
            Widgets.Label(description, comp.Props.treatmentDescription);

            int treatmentTicks = comp.Props.cycleDurationTicks;
            string durationString = treatmentTicks > 0 ? treatmentTicks.ToStringTicksToPeriod() : "Instant"; // TODO: Tranlation Key
            Widgets.Label(duration, "Treatment time: " + durationString); // TODO: Translation key

            if (comp.TreatmentAvailable(building, pawn, out lockedReason))
            {
                selected = Widgets.ButtonText(button, "Select"); // TODO: Translation key
            }
            else
            {
                Widgets.Label(button, lockedReason ?? "Unavailable"); //TODO: Translation key
            }

            Widgets.EndGroup();

            return selected;
        }

        private static void DrawBigLabelSterile(Rect inRect, string text)
        {
            TextAnchor anchor = Text.Anchor;
            GameFont font = Text.Font;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, text);

            Text.Font = font;
            Text.Anchor = anchor;
        }
    }
}
