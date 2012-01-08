using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Styx.Helpers;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Hera
{
    public partial class Fpsware
    {

        #region Combat!
        private Composite _combatBehavior;
        public override Composite CombatBehavior
        {
            get { if (_combatBehavior == null) { Utils.Log("Creating 'Combat' behavior"); _combatBehavior = CreateCombatBehavior(); } return _combatBehavior; }
        }
        
         private PrioritySelector CreateCombatBehavior()
         {
             return new PrioritySelector(

                // All spells are checked in the order listed below, a prioritised order
                // Put the most important spells at the top of the list

                // Distance Check
                new NeedToDistanceCheck(new DistanceCheck()),

                // Interact - Face the target
                new NeedToInteract(new Interact()),

                // Backup
                new NeedToBackup(new Backup()),

                
                // Check for aggro during pull
                new NeedToCheckAggroOnPull(new CheckAggroOnPull()),

                // Ignore Runners / Fleeing mobs
                new NeedToIgnoreRunners(new IgnoreRunners()),

                // Retarget
                new NeedToRetarget(new Retarget()),

                // Get behind sap target so we can use Backstab
                new NeedToGetBehindSap(new GetBehindSap()),

                // Abort combat if the target's health is 95% + after 30 seconds of combat
                //new NeedToCheckCombatTimer(new CheckCombatTimer()),

                // Vanish
                new NeedToVanish(new Vanish()),

                // Racial
                new NeedToRacial(new Racial()),

                // Auto attack
                new NeedToAutoAttack(new AutoAttack()),
                                
                // Evasion
                new NeedToEvasion(new Evasion()),

                // Recuperate
                new NeedToRecuperate(new Recuperate()),

                
                
                // ************************************************************************************
                // Important/time sensative spells here
                // These are spells that need to be case asap

                // Kick
                new NeedToKick(new Kick()),

                // Kidney Shot
                new NeedToKidneyShot(new KidneyShot()),

                // Blind
                new NeedToBlind(new Blind()),

                

                // Dismantle
                new NeedToDismantle(new Dismantle()),

                // Gouge + Backstab - Not in Battleground
                new NeedToGougeAndBackstab(new GougeAndBackstab()),

                // Finishing Move - Eviscerate / Expose Armor / Kidney Shot / Rupture
                new NeedToFinishingMove(new FinishingMove()),

                // Rupture
                new NeedToRupture(new Rupture()),

                // Slice And Dice
                new NeedToSliceAndDice(new SliceAndDice()),

                // Gouge - Battleground Only
                new NeedToGouge(new Gouge()),

                // Segregated talent combat rotations
                new Switch<ClassHelpers.Rogue.ClassType>(ret => ClassHelpers.Rogue.ClassSpec,
                                                new SwitchArgument<ClassHelpers.Rogue.ClassType>(AssassinationCombat, ClassHelpers.Rogue.ClassType.Assassination),
                                                new SwitchArgument<ClassHelpers.Rogue.ClassType>(SubtletyCombat, ClassHelpers.Rogue.ClassType.Subtlety),
                                                new SwitchArgument<ClassHelpers.Rogue.ClassType>(CombatCombat, ClassHelpers.Rogue.ClassType.Combat),
                                                new SwitchArgument<ClassHelpers.Rogue.ClassType>(CombatCombat, ClassHelpers.Rogue.ClassType.None))

                );
        }
        #endregion
        
        #region Assassination Combat
        private Composite AssassinationCombat
        {
            get
            {
                return new PrioritySelector(

                    // Vendetta
                    new NeedToVendetta(new Vendetta()),

                    // Mutilate
                    new NeedToMutilate(new Mutilate()),

                    // Sinister Strike
                    new NeedToSinisterStrike(new SinisterStrike())

                    );
            }
        }
        #endregion

        #region Subtlety Combat
        private Composite SubtletyCombat
        {
            get
            {
                return new PrioritySelector(

                    // Shadow Dance
                    new NeedToShadowDance(new ShadowDance()),

                    // Premeditation
                    new NeedToPremeditation(new Premeditation()),

                    // Hemorrhage
                    new NeedToHemorrhage(new Hemorrhage()),

                    // Sinister Strike
                    new NeedToSinisterStrike(new SinisterStrike())

                    );
            }
        }
        #endregion

        #region Combat Combat
        private Composite CombatCombat
        {
            get
            {
                return new PrioritySelector(

                    // Revealing Strike
                    new NeedToRevealingStrike(new RevealingStrike()),

                    // Adrenaline Rush
                    new NeedToAdrenalineRush(new AdrenalineRush()),

                    // Killing Spree
                    new NeedToKillingSpree(new KillingSpree()),

                    // Blade Flurry
                    new NeedToBladeFlurry(new BladeFlurry()),

                    // Sinister Strike
                    new NeedToSinisterStrike(new SinisterStrike())

                    );
            }
        }
        #endregion


        #region Behaviours

        #region Sinister Strike
        public class NeedToSinisterStrike : Decorator
        {
            public NeedToSinisterStrike(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Sinister Strike";
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Spell.IsEnoughPower("Sinister Strike")) return false;
                if (Me.EnergyPercent < Settings.SinisterStrikeEnergy) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class SinisterStrike : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Sinister Strike";
                bool result = Spell.Cast(spellName);
                
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Mutilate
        public class NeedToMutilate: Decorator
        {
            public NeedToMutilate(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Mutilate";

                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Spell.IsEnoughPower(spellName)) return false;
                if (Me.EnergyPercent < Settings.SinisterStrikeEnergy) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class Mutilate : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Mutilate";
                bool result = Spell.Cast(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Herorage
        public class NeedToHemorrhage: Decorator
        {
            public NeedToHemorrhage(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Hemorrhage";
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Spell.IsEnoughPower(spellName)) return false;
                if (Me.EnergyPercent < Settings.SinisterStrikeEnergy) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class Hemorrhage : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Hemorrhage";
                bool result = Spell.Cast(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Kick
        public class NeedToKick : Decorator
        {
            public NeedToKick(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Kick";
                
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Target.IsCasting) return false;
                if (!Timers.Expired("KidneyShot",1500) && Spell.IsKnown("Kidney Shot")) return false;
                if (Target.CanDebuffTarget(spellName)) return false;
                if (Target.IsLowLevel) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class Kick : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Kick";
                bool result = Spell.Cast(spellName);

                Timers.Reset("Kick");
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Evasion
        public class NeedToEvasion : Decorator
        {
            public NeedToEvasion(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Evasion";

                if (Self.IsHealthPercentAbove(Settings.EvasionHealth)) return false;
                if (Spell.IsOnCooldown(spellName) && !Spell.IsOnCooldown("Vanish") && !Self.IsBuffOnMe("Evasion") && Spell.CanCast("Preparation")) { Spell.Cast("Preparation"); while (Spell.IsGCD) Thread.Sleep(150); }
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (Self.IsBuffOnMe("Vanish")) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class Evasion : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Evasion";
                bool result = Spell.Cast(spellName);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Finishing Move
        public class NeedToFinishingMove : Decorator
        {
            public NeedToFinishingMove(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                string spellName = Spell.BestSpell(Settings.FinishingMove);

                /*
                if (Me.ComboPoints <= 0) return false;

                if (spellName.Contains("Kidney Shot +") && Spell.IsOnCooldown("Kidney Shot"))
                {
                    if (spellName.Contains("Envenom")) spellName = "Envenom";
                    if (spellName.Contains("Eviscerate")) spellName = "Eviscerate";
                    if (spellName.Contains("Rupture")) spellName = "Rupture";
                }
                else { spellName = "Kidney Shot"; }
                 */

                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (Target.HealthPercent > Settings.FinishingMoveHealthPercent && !CLC.ResultOK(Settings.FinisherComboPoints)) return false;
                if (Target.HealthPercent < Settings.FinishingMoveHealthPercent && Me.ComboPoints < 3) return false;
                if (!Spell.IsEnoughPower(spellName)) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class FinishingMove : Action
        {
            protected override RunStatus Run(object context)
            {
                string spellName = Spell.BestSpell(Settings.FinishingMove);
                //string spellName = Settings.FinishingMove;

                /*
                if (spellName.Contains("Kidney Shot +") && Spell.IsOnCooldown("Kidney Shot"))
                {
                    if (spellName.Contains("Envenom")) spellName = "Envenom";
                    if (spellName.Contains("Eviscerate")) spellName = "Eviscerate";
                    if (spellName.Contains("Rupture")) spellName = "Rupture";
                }
                else { spellName = "Kidney Shot"; }
                 */

                bool result = Spell.Cast(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Slice And Dice
        public class NeedToSliceAndDice : Decorator
        {
            public NeedToSliceAndDice(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Slice and Dice";

                if (!CLC.ResultOK(Settings.SliceAndDice)) return false;
                if (Target.IsLowLevel) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Self.IsBuffOnMe(dpsSpell)) return false;
                if (Me.ComboPoints > 2) return false;
                if (!Utils.Adds && !Target.IsHealthPercentAbove(35)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class SliceAndDice : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Slice and Dice";
                Spell.Cast(dpsSpell);
                bool result = Self.IsBuffOnMe(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Recuperate
        public class NeedToRecuperate : Decorator
        {
            public NeedToRecuperate(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Recuperate";
                if (Me.ActiveAuras.ContainsKey(dpsSpell)) return false;
                if (Self.IsHealthPercentAbove(Settings.RecuperateHealth)) return false;
                if (!CLC.ResultOK(Settings.RecuperateCombo)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Recuperate : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Recuperate";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Vanish
        public class NeedToVanish : Decorator
        {
            public NeedToVanish(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Vanish";

                if (Self.IsHealthPercentAbove(Settings.VanishHealth)) return false;
                if (Spell.IsOnCooldown(dpsSpell) && !Self.IsBuffOnMe(dpsSpell) && Spell.CanCast("Preparation")) {Spell.Cast("Preparation"); while (Spell.IsGCD) Thread.Sleep(150);}
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Self.IsBuffOnMe(dpsSpell)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Vanish : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Vanish";
                const double distanceFrom = 30;
                WoWPoint pointToGo = Movement.FindSafeLocation(distanceFrom);

                if (Spell.CanCast("Cload of Shadows")) Spell.Cast("Cloak of Shadows");
                Utils.LagSleep(); while (Spell.IsGCD) Thread.Sleep(150);
                
                Spell.Cast(dpsSpell);
                Timers.Reset("Vanish");
                
                WoWMovement.ClickToMove(pointToGo);
                Me.ClearTarget();
                Thread.Sleep(500);

                if (Self.IsBuffOnMe("Vanish"))
                {
                    Me.ClearTarget();
                    Movement.MoveTo(pointToGo);
                    Utils.LagSleep();
                    
                    while (Me.IsMoving) { Thread.Sleep(150); Movement.MoveTo(pointToGo); }
                }

                return RunStatus.Success;
            }
        }
        #endregion

        #region Kidney Shot
        public class NeedToKidneyShot : Decorator
        {
            public NeedToKidneyShot(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Kidney Shot";

                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Target.IsCasting && !Target.IsFleeing) return false;
                if (Target.IsCasting && !Spell.IsOnCooldown("Kick")) return false;
                if (!Timers.Expired("Kick",1500)) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class KidneyShot : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Kidney Shot";
                bool result = Spell.Cast(spellName);
                Timers.Reset("KidneyShot");

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Gouge + Backstab
        
        public class NeedToGougeAndBackstab : Decorator
        {
            public NeedToGougeAndBackstab(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Backstab";

                if (Utils.IsBattleground) return false;
                if (!CLC.ResultOK(Settings.Backstab)) return false;
                if (Utils.Adds) return false;
                if (Me.IsSwimming) return false;
                if (!Spell.IsKnown(dpsSpell)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (!Target.IsHealthPercentAbove(Settings.GougeHealthPercent)) return false;
                if (Me.IsInParty) return false;
                if (!Spell.CanCast("Gouge")) return false;
                
                double energyCost = Spell.PowerCost("Backstab") + (Spell.PowerCost("Gouge")*0.1);
                if (Me.CurrentEnergy < energyCost) return false;

                WoWPoint pointBehind = WoWMathHelper.CalculatePointBehind(CT.Location, Me.CurrentTarget.Rotation, Target.InteractRange - 2);
                if (!Navigator.CanNavigateFully(Me.Location, pointBehind, 1)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class GougeAndBackstab : Action
        {
            protected override RunStatus Run(object context)
            {
                Spell.Cast("Gouge");
                Utils.LagSleep(); Thread.Sleep(250);
                if (!Target.IsDebuffOnTarget("Gouge")) return RunStatus.Failure;

                WoWPoint pointBehind = WoWMathHelper.CalculatePointBehind(CT.Location, Me.CurrentTarget.Rotation,Target.InteractRange - 2);
                Movement.MoveTo(pointBehind);
                Utils.LagSleep();
            
                while (Me.IsMoving) Thread.Sleep(150); Target.Face(); Utils.LagSleep();

                while (Me.CurrentEnergy < Spell.PowerCost("Backstab"))
                {
                    Thread.Sleep(100);
                    if (!Target.IsDebuffOnTarget("Gouge")) break;
                    if (Utils.Adds) break;
                    Utils.Log("-Waiting for enough energy to Backstab...");
                }

                if (Target.IsDistanceMoreThan(Target.InteractRange)) Movement.MoveTo(Target.InteractRange); while (Me.IsMoving) Thread.Sleep(150);
                if (Spell.CanCast("Cold Blood")) { Spell.Cast("Cold Blood"); Utils.LagSleep(); }

                const string dpsSpell = "Backstab";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
         
        #endregion

        #region Get Behind Sap
        public class NeedToGetBehindSap : Decorator
        {
            public NeedToGetBehindSap(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (!Target.IsDebuffOnTarget("Sap")) return false;
                if (Utils.Adds) return false;
                if (!Spell.IsKnown("Backstab")) return false;
                if (Me.IsInParty) return false;
                if (Settings.LazyRaider.Contains("always")) return false;

                return true;
            }
        }

        public class GetBehindSap : Action
        {
            protected override RunStatus Run(object context)
            {
                if (!Target.IsDebuffOnTarget("Sap")) return RunStatus.Failure;

                WoWPoint pointBehind = WoWMathHelper.CalculatePointBehind(CT.Location, Me.CurrentTarget.Rotation, Target.InteractRange - 2);
                Movement.MoveTo(pointBehind);
                Utils.LagSleep();

                while (Me.IsMoving) Thread.Sleep(150); Target.Face(); Utils.LagSleep();

                while (Me.CurrentEnergy < Spell.PowerCost("Backstab"))
                {
                    Thread.Sleep(100);
                    if (!Target.IsDebuffOnTarget("Sap")) break;
                    if (Utils.Adds) break;
                    Utils.Log("-Waiting for enough energy to Backstab...");
                }

                if (Target.IsDistanceMoreThan(Target.InteractRange)) Movement.MoveTo(Target.InteractRange); while (Me.IsMoving) Thread.Sleep(150);
                if (Spell.CanCast("Cold Blood")) { Spell.Cast("Cold Blood"); Utils.LagSleep(); }

                string dpsSpell = "Backstab";
                if (!Spell.CanCast("Backstab"))
                {
                    if (Spell.CanCast("Ambush")) dpsSpell = "Ambush";
                    if (!Spell.CanCast("Ambush")) dpsSpell = "Sinister Strike";
                }


                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Blind
        public class NeedToBlind : Decorator
        {
            public NeedToBlind(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Blind";

                if (!CLC.ResultOK(Settings.BlindAdd)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Target.IsLowLevel) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Blind : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Blind";
                bool result = false;

                List<WoWUnit> hlist =
                    (from o in ObjectManager.ObjectList
                     where o is WoWUnit
                     let p = o.ToUnit()
                     where p.Distance2D < 15
                           && !p.Dead
                           && p.IsTargetingMeOrPet
                           && p.Attackable
                           && p.Guid != CT.Guid
                     orderby p.HealthPercent descending
                     orderby p.Distance2D ascending
                     select p).ToList();

                if (hlist.Count > 0) { result = Spell.Cast(dpsSpell,hlist[0]); Utils.LagSleep(); }

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Blade Flurry
        public class NeedToBladeFlurry : Decorator
        {
            public NeedToBladeFlurry(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Blade Flurry";

                if (Settings.BladeFlurry.Contains("never")) return false;
                if (Settings.BladeFlurry.Contains("only on adds") && !Utils.Adds && Self.IsBuffOnMe(dpsSpell)) { Self.RemoveBuff(dpsSpell); return false; }
                if (Settings.BladeFlurry.Contains("only on adds") && !Utils.Adds) return false;
                
                //if (!CLC.ResultOK(Settings.BladeFlurry)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                
                List<WoWUnit> managedTargets = (from o in ObjectManager.ObjectList where o is WoWUnit let p = o.ToUnit() where p.Distance2D < 15 && (p.Auras.ContainsKey("Sap") || p.Auras.ContainsKey("Blind")) select p).ToList();
                if (managedTargets.Count > 0) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class BladeFlurry : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Blade Flurry";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion
        
        #region Adrenaline Rush
        public class NeedToAdrenalineRush : Decorator
        {
            public NeedToAdrenalineRush(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Adrenaline Rush";

                if (!CLC.ResultOK(Settings.AdrenalineRush)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class AdrenalineRush : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Adrenaline Rush";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Shadow Dance
        public class NeedToShadowDance : Decorator
        {
            public NeedToShadowDance(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Shadow Dance";

                if (!CLC.ResultOK(Settings.ShadowDance)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Self.IsBuffOnMe("Shadow Dance")) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class ShadowDance : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Shadow Dance";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Vendetta
        public class NeedToVendetta : Decorator
        {
            public NeedToVendetta(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Vendetta";

                if (!CLC.ResultOK(Settings.Vendetta)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (!Target.IsHealthPercentAbove(Settings.VendettaHealthPercent) && !Target.IsElite) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Vendetta : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Vendetta";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Killing Spree
        public class NeedToKillingSpree : Decorator
        {
            public NeedToKillingSpree(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Killing Spree";
                if (!CLC.ResultOK(Settings.KillingSpree)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class KillingSpree : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Killing Spree";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Rupture
        public class NeedToRupture : Decorator
        {
            public NeedToRupture(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Rupture";

                if (!CLC.ResultOK(Settings.Rupture)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Target.IsLowLevel) return false;
                //if (!Target.IsHighLevel && !Target.IsElite) return false;
                if (!Target.IsHealthPercentAbove(Settings.RuptureHealthPercent)) return false;
                if (Me.ComboPoints > 2) return false;
                if (Target.IsDebuffOnTarget(dpsSpell)) return false;


                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Rupture : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Rupture";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Dismantle
        public class NeedToDismantle : Decorator
        {
            public NeedToDismantle(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Dismangle";
                if (!Utils.IsBattleground) return false;
                if (Target.IsPlayerCaster) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Dismantle : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Dismangle";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Gouge
        public class NeedToGouge : Decorator
        {
            public NeedToGouge(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Gouge";
                
                if (!Utils.IsBattleground) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (!Target.IsCaster) return false;
                if (!Target.IsCasting) return false;
                 

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Gouge : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Gouge";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion
        
        #region Racial
        public class NeedToRacial : Decorator
        {
            public NeedToRacial(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                string dpsSpell = Settings.RacialSpell;

                if (dpsSpell.Contains("never")) return false;
                if (!CLC.ResultOK(Settings.RacialUseWhen)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Racial : Action
        {
            protected override RunStatus Run(object context)
            {
                string dpsSpell = Settings.RacialSpell;
                bool result = Spell.Cast(dpsSpell);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Distance Check
        public class NeedToDistanceCheck : Decorator
        {
            public NeedToDistanceCheck(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Settings.LazyRaider.Contains("always")) return false;
                if (!Me.GotTarget || Me.CurrentTarget.Dead) return false;
                float distance = !Target.IsFleeing ? Target.InteractRange : Target.InteractRange * 0.1f;

                if (Target.IsDistanceLessThan(distance) && Me.IsMoving) WoWMovement.MoveStop();
                if (Target.IsDistanceMoreThan(distance)) return true;

                return false;
            }
        }

        public class DistanceCheck : Action
        {
            protected override RunStatus Run(object context)
            {
                float distance = !Target.IsFleeing ? Target.InteractRange : Target.InteractRange * 0.1f;
                //float distanceMoveTo = Target.InteractRange;
                Movement.MoveTo(distance);
                //Utils.Log("---- DistanceCheck");

                return RunStatus.Failure;
            }
        }
        #endregion

        #region Interact - Move closer or face the target
        public class NeedToInteract : Decorator
        {
            public NeedToInteract(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (!Me.GotTarget) return false;
                if (Settings.LazyRaider.Contains("always")) return false;

                // May remove this. Added to try and sort out some facing / distance issues
                // =================================================================================
                /*
                if (!Timers.Expired("Interact", 650)) return false;
                if (Target.IsWithinInteractRange && !CT.IsPlayer && Me.IsSafelyFacing(CT.Location))
                {
                    Timers.Reset("Interact");
                    return false;
                }
                 */
                // =================================================================================

                return (Timers.Expired("Interact", 650));
            }
        }

        public class Interact : Action
        {
            protected override RunStatus Run(object context)
            {
                CT.Interact();
                Timers.Reset("Interact");
                //Utils.Log("---- Interact");

                return RunStatus.Success;
            }
        }
        #endregion

        #region Back up if we're too close
        public class NeedToBackup : Decorator
        {
            public NeedToBackup(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                double interactDistance = Target.InteractRange;

                if (!Me.GotTarget || Me.CurrentTarget.Dead) return false;
                if (Target.IsFleeing) return false;
                if (CT.IsMoving) return false;

                if (!Timers.Expired("DistanceCheck", 650)) return false;

                return (Target.IsDistanceLessThan(interactDistance * 0.5));

            }
        }

        public class Backup : Action
        {
            protected override RunStatus Run(object context)
            {
                double interactDistance = Target.InteractRange;

                while (Target.IsDistanceLessThan(interactDistance * 0.7))
                {
                    if (CT.IsMoving) break;
                    WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
                    Thread.Sleep(100);
                }

                WoWMovement.MoveStop();
                Utils.Log("Backup a wee bit, too close to our target", Utils.Colour("Blue"));
                Timers.Reset("DistanceCheck");
                //Utils.Log("---- Backup");

                return RunStatus.Failure;
            }
        }
        #endregion

        #region Retarget
        public class NeedToRetarget : Decorator
        {
            public NeedToRetarget(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Me.GotTarget && CT.IsAlive) return false;
                if (!Me.Combat) return false;

                return true;
            }
        }

        public class Retarget : Action
        {
            protected override RunStatus Run(object context)
            {
                List<WoWUnit> hlist =
                    (from o in ObjectManager.ObjectList
                     where o is WoWUnit
                     let p = o.ToUnit()
                     where p.Distance2D < 40
                           && !p.Dead
                           && (p.IsTargetingMeOrPet || p.Auras.ContainsKey("Blind") || p.Auras.ContainsKey("Sap"))
                           && p.Attackable
                     orderby p.HealthPercent ascending
                     orderby p.Distance2D ascending
                     select p).ToList();

                // Check if we have a sapped target. If so, find another one, leave the sapped target alone for a while.
                if (hlist.Count > 2) { foreach (WoWUnit unit in hlist.Where(unit => !unit.Auras.ContainsKey("Sap"))) { unit.Target(); return RunStatus.Failure; } }

                // No sapped target, kill anything. 
                if (hlist.Count >0) hlist[0].Target();
             
                return RunStatus.Failure;
            }
        }
        #endregion

        #region Ignore Runners
        public class NeedToIgnoreRunners : Decorator
        {
            public NeedToIgnoreRunners(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Settings.IgnoreRunners.Contains("never")) return false;
                if (!Utils.Adds) return false;
                if (!CT.Fleeing) return false;
                if (Me.IsInInstance) return false;

                Utils.Log("Target is feeling, selecting another target...",Utils.Colour("Red"));

                return true;
            }
        }

        public class IgnoreRunners : Action
        {
            protected override RunStatus Run(object context)
            {
                int weight;
                WoWUnit alternateTarget = Utils.BestTarget(out weight);
                
                Utils.Log(string.Format("Attacking {0} for now", alternateTarget.Name), Utils.Colour("Red"));
                alternateTarget.Target();
                return RunStatus.Success;
            }
        }
        #endregion

        #region Revealing Strike
        public class NeedToRevealingStrike : Decorator
        {
            public NeedToRevealingStrike(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Revealing Strike";

                if (!CLC.ResultOK(Settings.RevealingStrike)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Target.IsDebuffOnTarget(dpsSpell)) return false;
                if (Target.IsHealthPercentAbove(Settings.RevealingStrikeHealth)) return false;
                
                return (Spell.CanCast(dpsSpell));
            }
        }

        public class RevealingStrike : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Revealing Strike";
                Spell.Cast(dpsSpell);
                
                Utils.LagSleep();
                bool result = Target.IsDebuffOnTarget(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #endregion

    }
}