using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GeneManipulator
{
    public class Building_GeneManipulator : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        #region INTERNAL VARS
        private string currentTreatmentKey;
        private int currentTreatmentTicks;
        private int ejectionTicks;
        private int requiredComplexity;

        [Unsaved]
        private CompPowerTrader powerInt;
        #endregion

        #region CONVENIENCE PROPERTIES
        public GeneManipulatorTuning Tuning => def.GetModExtension<GeneManipulatorTuning>();

        public bool HasPower => Power.PowerOn;

        public List<Thing> ConnectedFacilities => this.TryGetComp<CompAffectedByFacilities>().LinkedFacilitiesListForReading;

        public int AvailableComplexity
        {
            get
            {
                int cpx = Tuning.baseComplexity;
                List<Thing> facilites = ConnectedFacilities;
                if (!facilites.NullOrEmpty())
                    foreach(Thing facility in facilites)
                    {
                        CompPowerTrader power = facility.TryGetComp<CompPowerTrader>();
                        if (power == null || power.PowerOn)
                            cpx += (int)facility.GetStatValue(StatDefOf.GeneticComplexityIncrease);
                    }
                return cpx;
            }
        }

        public CompPowerTrader Power
        {
            get
            {
                if (powerInt == null)
                    powerInt = this.TryGetComp<CompPowerTrader>();
                return powerInt;
            }
        }

        public Pawn ContainedPawn => innerContainer.FirstOrDefault() as Pawn;

        public bool DoingTreatmentNow => currentTreatmentKey != null;

        public CompGeneManipulator_Treatment CurrentTreatment
        {
            get
            {
                if (currentTreatmentKey == null)
                    return null;

                foreach (ThingComp comp in AllComps)
                {
                    if (comp is CompGeneManipulator_Treatment treatment && treatment.Props.key == currentTreatmentKey)
                        return treatment;
                }

                return null;
            }
        }

        public bool HasAnyGenepack
        {
            get
            {
                List<Thing> facilities = ConnectedFacilities;
                if (facilities == null)
                    return false;

                foreach (Thing facility in facilities)
                {
                    CompGenepackContainer container = facility.TryGetComp<CompGenepackContainer>();
                    if (container != null && container.PowerOn && !container.ContainedGenepacks.NullOrEmpty())
                        return true;
                }

                return false;
            }
        }
        #endregion

        #region PAWN HOLDER FUNCTIONAL
        public override bool IsContentsSuspended => false;

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            AcceptanceReport canTreat = CanTreatPawn(pawn);
            if (!canTreat)
                return canTreat;

            if (!HasPower)
                return "NoPower".Translate().CapitalizeFirst();

            if (innerContainer.Count > 0)
                return "Occupied".Translate();

            return true;
        }

        public override void TryAcceptPawn(Pawn p)
        {
            if (!CanAcceptPawn(p)) return;

            selectedPawn = p;
            bool selectPawn = p.DeSpawnOrDeselect();

            if (innerContainer.TryAddOrTransfer(p))
            {
                startTick = Find.TickManager.TicksGame;
                DoTreatmentSelection(p);
            }

            if (selectPawn)
                Find.Selector.Select(p, false, false);
        }
        #endregion

        #region PAWN HOLDER GRAPHICAL
        public override Vector3 PawnDrawOffset =>
            IntVec3.West.RotatedBy(Rotation).ToVector3() / def.size.x;

        public float HeldPawnDrawPos_Y => DrawPos.y + Tuning.pawnOffsetY;

        public float HeldPawnBodyAngle =>
            Rotation.Opposite.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public override void Draw()
        {
            base.Draw();
            if (selectedPawn == null || !innerContainer.Contains(selectedPawn))
                return;
            selectedPawn.Drawer.renderer.RenderPawnAt(DrawPos + PawnDrawOffset, neverAimWeapon: true);
        }
        #endregion

        #region STANDARD FUNCTIONALITY
        public override string GetInspectString()
        {
            string str = base.GetInspectString();

            if (!str.NullOrEmpty())
                str += "\n";

            if (ContainedPawn != null)
            {
                str += "Contains " + ContainedPawn.LabelCap; // TODO: Translation key

                CompGeneManipulator_Treatment treatment = CurrentTreatment;
                if (treatment != null)
                {
                    str += "\n" + "Current treatment: " + treatment.Props.treatmentName; // TODO: Translation key
                    str += "\n" + " Time remaining: " + currentTreatmentTicks.ToStringTicksToPeriod(); // TODO: Translation Key
                }
            }
            else if (selectedPawn != null)
            {
                str += "Awaiting " + selectedPawn.LabelCap; // TODO: Translation key
            }
            else
            {
                str += "No patient selected"; // TODO: Translation key
            }

            return str;
        }

        public override void Tick()
        {
            base.Tick();
            innerContainer.ThingOwnerTick();

            if (this.IsHashIntervalTick(250))
                Power.PowerOutput = DoingTreatmentNow ?
                    -PowerComp.Props.PowerConsumption :
                    -PowerComp.Props.idlePowerDraw;

            if (!DoingTreatmentNow)
                return;

            if (!Power.PowerOn)
            {
                ejectionTicks += 1;
                if (ejectionTicks >= Tuning.noPowerTicksToEject)
                    EjectContents();
                return;
            }

            if (currentTreatmentTicks > 0)
                currentTreatmentTicks -= 1;

            if (currentTreatmentTicks > 0)
                return;

            CompGeneManipulator_Treatment treatment = CurrentTreatment;
            if (treatment != null)
                treatment.EndTreatmentCycle(ContainedPawn);
            EjectContents();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }

            #region PAWN NOT LOADED
            if (ContainedPawn == null)
            {
                if (selectedPawn != null)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "CommandCancelLoad".Translate(),
                        defaultDesc = "CommandCancelLoadDesc".Translate(),
                        icon = Tuning.CancelIcon.Texture,
                        action = delegate
                        {
                            EjectContents();
                        },
                        activateSound = SoundDefOf.Designate_Cancel,
                    };
                    yield break;
                }

                yield return new Command_Action()
                {
                    defaultLabel = "InsertPerson".Translate() + "...",
                    defaultDesc = "InsertPersonGeneExtractorDesc".Translate(), // TODO
                    icon = Tuning.InsertIcon.Texture,
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Pawn pawn in Map.mapPawns.AllPawnsSpawned)
                        {
                            if (pawn.genes == null)
                                continue;

                            AcceptanceReport allowed = CanTreatPawn(pawn);
                            string label = pawn.LabelShortCap;

                            if (!allowed.Accepted)
                            {
                                options.Add(new FloatMenuOption(label + ": " + allowed.Reason, null, pawn, Color.white));
                                continue;
                            }

                            options.Add(new FloatMenuOption(label, delegate
                            {
                                SelectPawn(pawn);
                            }, pawn, Color.white));
                        }
                        if (!options.Any())
                        {
                            options.Add(new FloatMenuOption("NoTreatablePawns".Translate(), null));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    },
                    disabled = !HasPower,
                    disabledReason = HasPower ? string.Empty : "NoPower".Translate().CapitalizeFirst().ToString(),
                };
                yield break;
            }
            #endregion

            #region PAWN LOADED
            yield return new Command_Action()
            {
                defaultLabel = "CommandEjectOccupant".Translate(ContainedPawn.LabelShortCap.Named("PAWN")),
                defaultDesc = "CommandEjectOccupant".Translate(ContainedPawn.LabelShortCap.Named("PAWN")), // TODO
                icon = Tuning.CancelIcon.Texture,
                action = delegate
                {
                    EjectContents();
                },
                activateSound = SoundDefOf.Designate_Cancel,
            };

            if (DoingTreatmentNow)
                yield break;

            /*
            foreach (CompGeneManipulator_Treatment treatment in GetComps<CompGeneManipulator_Treatment>())
            {
                yield return treatment.GetGizmo;
            }
            */
            #endregion
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentTreatmentKey, "currentTreatmentKey");
            Scribe_Values.Look(ref currentTreatmentTicks, "currentTreatmentTicks");
            Scribe_Values.Look(ref ejectionTicks, "ejectionTicks");
            Scribe_Values.Look(ref requiredComplexity, "requiredComplexity");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // If the player somehow used the save/load system to glitch a pawn into an inactive
            // manipulator, eject the pawn.
            if (!respawningAfterLoad)
                return;

            EnsurePatientBelongs();
        }
        #endregion

        #region CUSTOM FUNCTIONALITY
        public AcceptanceReport CanTreatPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                return false;

            if (selectedPawn != null && selectedPawn != pawn)
                return false;

            if (!pawn.RaceProps.Humanlike)
                return false;

            if (pawn.health.hediffSet.HasHediff(Tuning.postTreatmentHediff))
                return "GeneManipulatorUserRecovering".Translate();

            return true;
        }

        public void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);

            if (selectedPawn != null && selectedPawn.CurJobDef == JobDefOf.EnterBuilding)
            {
                selectedPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            ResetAll();
        }

        public void ResetAll()
        {
            ResetTreatment();
            ResetContainer();
        }

        public void ResetTreatment()
        {
            currentTreatmentKey = null;
            currentTreatmentTicks = 0;
            ejectionTicks = 0;
            requiredComplexity = 0;
        }

        public void ResetContainer()
        {
            selectedPawn = null;
        }

        public void DoTreatmentSelection(Pawn pawn)
        {
            Find.WindowStack.Add(new Dialog_GeneTreatment(this, pawn));
        }

        public void StartTreatment(CompGeneManipulator_Treatment treatment, int complexity = 0)
        {
            ResetTreatment();
            currentTreatmentKey = treatment.Props.key;
            currentTreatmentTicks = treatment.Props.cycleDurationTicks;
            requiredComplexity = 0;
        }

        private void EnsurePatientBelongs()
        {
            if (ContainedPawn != null && !DoingTreatmentNow)
                EjectContents();
        }
        #endregion

        
    }
}
