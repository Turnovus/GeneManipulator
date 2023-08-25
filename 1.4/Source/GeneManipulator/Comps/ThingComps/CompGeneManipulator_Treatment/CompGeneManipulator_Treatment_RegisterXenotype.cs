using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Verse;
using RimWorld;
using UnityEngine;

namespace GeneManipulator
{
    class CompGeneManipulator_Treatment_RegisterXenotype : CompGeneManipulator_Treatment
    {
        // UI Constants
        public const float NameEntryWidthRatio = 0.7f;
        public const float IconWidthRatio = 0.8f;
        public const float LabelHeight = 22f;
        public const float NameEntryHeight = 25f;
        public const float VerticalMargin = 50f;
        public const float ButtonHeight = 35f;
        public const float ButtonWidth = 80f;
        public const float ScrollbarWidth = 16f;
        public const float IconPadding = 2f;
        public const int IconsPerRow = 10;

        // Copy-pasted from RimWorld.GeneCreationDialogBase
        // Beacause Ludeon hates public fields
        public static readonly Regex ValidNameRegex = new Regex("^[\\p{L}0-9 '\\-]*$");

        // These values copy-pasted from RimWorld.Dialog_SelectXenotype
        public static readonly Color OutlineColorSelected = new Color(1f, 1f, 0.7f, 1f);
        public static readonly Color OutlineColorUnselected = new Color(1f, 1f, 1f, 0.1f);

        // Patient info
        private string xenotypeName;
        private XenotypeIconDef icon;

        private Vector2 scrollPosition;

        public override bool DoContents(Rect inRect)
        {
            float iconSize, iconRows;
            int iconInRow;

            #region RECTS
            Rect nameRect = new Rect(inRect.width * (1f - NameEntryWidthRatio) * 0.5f, VerticalMargin, inRect.width * NameEntryWidthRatio, LabelHeight + NameEntryHeight);
            Rect nameLabel = new Rect(0, 0, nameRect.width, LabelHeight);
            Rect nameEntry = new Rect(0, LabelHeight, nameRect.width, NameEntryHeight);

            float iconRectHeight = inRect.height - ((3f * VerticalMargin) + nameRect.height + ButtonHeight);
            Rect iconRect = new Rect(inRect.width * (1f - IconWidthRatio) * 0.5f, nameRect.y + nameRect.height + VerticalMargin, inRect.width * IconWidthRatio, iconRectHeight);
            Rect iconLabel = new Rect(0, 0, iconRect.width, LabelHeight);
            Rect iconSelect = new Rect(0, LabelHeight, iconRect.width, iconRect.height - LabelHeight);

            Rect buttonRect = new Rect((inRect.width - ButtonWidth) * 0.5f, iconRect.y + iconRect.height + VerticalMargin, ButtonWidth, ButtonHeight);

            iconSize = (iconSelect.width - ScrollbarWidth) / IconsPerRow;
            iconRows = (float)Math.Ceiling((double)(DefDatabase<XenotypeIconDef>.DefCount / IconsPerRow));

            Rect iconView = new Rect(0f, 0f, iconSelect.width - ScrollbarWidth, iconRows * iconSize);
            Rect singleIconRect = new Rect(0f, 0f, iconSize, iconSize);
            Rect iconInnerRect = singleIconRect.ContractedBy(IconPadding);

            if (iconView.height < iconSelect.height)
            {
                iconSelect.x += ScrollbarWidth * 0.5f;
            }
            #endregion

            Text.Anchor = TextAnchor.MiddleCenter;

            #region NAME ENTRY
            Widgets.BeginGroup(nameRect);

            Widgets.Label(nameLabel, "Name xenotype:"); // TODO: Translation key
            xenotypeName = Widgets.TextField(nameEntry, xenotypeName, 40, ValidNameRegex);

            Widgets.EndGroup();
            #endregion

            #region ICON SELECT
            Widgets.BeginGroup(iconRect);

            Widgets.Label(iconLabel, "Choose icon:"); // TODO: Translation key

            iconInRow = 0;
            Widgets.BeginScrollView(iconSelect, ref scrollPosition, iconView);

            foreach (XenotypeIconDef previewIcon in DefDatabase<XenotypeIconDef>.AllDefs)
            {
                singleIconRect.x = iconInRow * iconSize;
                iconInnerRect.x = singleIconRect.x + IconPadding;

                Widgets.DrawHighlight(iconInnerRect);
                if (previewIcon == icon)
                {
                    GUI.color = OutlineColorSelected;
                    Widgets.DrawHighlight(iconInnerRect);
                    Widgets.DrawBox(singleIconRect);
                }
                else
                {
                    GUI.color = OutlineColorUnselected;
                    Widgets.DrawBox(iconInnerRect);
                }
                GUI.color = Color.white;

                if (Widgets.ButtonImage(iconInnerRect, previewIcon.Icon, XenotypeDef.IconColor))
                {
                    icon = previewIcon;
                }

                iconInRow += 1;
                if (iconInRow >= IconsPerRow)
                {
                    iconInRow = 0;
                    singleIconRect.y += iconSize;
                    iconInnerRect.y += iconSize;
                }
            }

            Widgets.EndScrollView();

            Widgets.EndGroup();
            #endregion

            return Widgets.ButtonText(buttonRect, "Apply"); // TODO: Translation key
        }

        public override bool DoTreatment(Pawn pawn, out int requiredComplexity)
        {
            pawn.genes.SetXenotypeDirect(XenotypeDefOf.Baseliner);
            pawn.genes.xenotypeName = xenotypeName;
            pawn.genes.iconDef = icon;
            return base.DoTreatment(pawn, out requiredComplexity);
        }

        public override void OnTreatmentPreview(Building_GeneManipulator building, Pawn pawn)
        {
            base.OnTreatmentPreview(building, pawn);
            xenotypeName = pawn.genes.xenotypeName;
            icon = pawn.genes.iconDef ?? XenotypeIconDefOf.Basic;
            scrollPosition = Vector2.zero;
        }

        public override void OnTreatmentWithdrawn()
        {
            base.OnTreatmentWithdrawn();
            xenotypeName = string.Empty;
            icon = XenotypeIconDefOf.Basic;
            scrollPosition = Vector2.zero;
        }
    }
}
