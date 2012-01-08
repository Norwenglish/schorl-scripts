using System;
using System.Collections.Generic;
using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Hera
{
    public partial class Fpsware
    {
        #region Pull
        // _pullBehavior is a class that will hold our logic. 
        private Composite _pullBehavior;
        public override Composite PullBehavior
        {
            get { if (_pullBehavior == null) { Utils.Log("Creating 'Pull' behavior"); _pullBehavior = CreatePullBehavior(); }  return _pullBehavior; }
        }

        private PrioritySelector CreatePullBehavior()
        {
            return new PrioritySelector(

                // If we can't navigate to the target blacklist it.
                new NeedToNavigatePath(new NavigatePath()),

                // Check if the target is suitable for pulling, if not blacklist it
                new NeedToBlacklistPullTarget(new BlacklistPullTarget()),

                // Dismount if mounted. Here to work around a bug in HB
                //new Decorator(ret => Me.Mounted && !Settings.LazyRaider.Contains("always"), new Action(ret => Mount.Dismount())),

                // Check pull timers and blacklist bad pulls where required
                new NeedToCheckPullTimer(new BlacklistPullTarget()),

                // Stealth
                new NeedToStealth(new Stealth()),

                // Premeditation
                new NeedToPremeditation(new Premeditation()),

                // Auto Attack
                //new Decorator(ret => !Self.IsBuffOnMe("Stealth") && Target.IsWithinInteractRange && !Me.IsAutoAttacking, new Action(ret => Utils.AutoAttack(true))),
                //new Decorator(ret => Self.IsBuffOnMe("Stealth") && Me.IsAutoAttacking, new Action(ret => Utils.AutoAttack(false))),

                // Auto Attack During Pull
                new NeedToAutoAttackPull(new AutoAttackPull()),


                // *******************************************************

                // Sap
                new NeedToSap(new Sap()),

                // Get behind sapped target and Backstab/Ambush
                new NeedToGetBehindSap(new GetBehindSap()),

                // Throw
                new NeedToThrow(new Throw()),

                // Shoot
                new NeedToShoot(new Shoot()),

                // Shadowstep
                new NeedToShadowstep(new Shadowstep()),

                // Sprint Pull
                new NeedToSprint(new Sprint()),

                // Distract
                new NeedToDistract(new Distract()),

                // Face Target Pull
                new NeedToFaceTargetPull(new FaceTargetPull()),

                // Pick Pocket
                new NeedToPickPocket(new PickPocket()),

                // Ambush
                new NeedToAmbushOrGarrote(new AmbushOrGarrote()),

                // Cheap Shot
                new NeedToCheapShot(new CheapShot()),

                // Sinister Strike
                new NeedToSinisterStrikePull(new SinisterStrikePull()),

                // Move to target
                new NeedToMoveTo(new MoveTo()),


                // Update ObjectManager
                new Action(ret=>ObjectManager.Update())

                );
        }
        #endregion

        #region Pull Timer / Timeout
        public class NeedToCheckPullTimer : Decorator
        {
            public NeedToCheckPullTimer(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Settings.LazyRaider.Contains("always")) return false;

                return Target.PullTimerExpired;
            }
        }

        public class CheckPullTimer : Action
        {
            protected override RunStatus Run(object context)
            {
                Utils.Log(string.Format("Unable to pull {0}, blacklisting and finding another target.", Me.CurrentTarget.Name), System.Drawing.Color.FromName("Red"));
                Target.BlackList(120);
                Me.ClearTarget();

                return RunStatus.Success;
            }
        }
        #endregion

        #region Combat Timer / Timeout
        public class NeedToCheckCombatTimer : Decorator
        {
            public NeedToCheckCombatTimer(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Utils.IsBattleground) return false;
                if (!Me.GotTarget || Me.CurrentTarget.Dead) return false;
                if (Target.IsElite) return false;
                

                return Target.CombatTimerExpired && Target.IsHealthPercentAbove(98);
            }
        }

        public class CheckCombatTimer : Action
        {
            protected override RunStatus Run(object context)
            {
                Utils.Log(string.Format("Combat with {0} is bugged, blacklisting and finding another target.", Me.CurrentTarget.Name), System.Drawing.Color.FromName("Red"));
                Target.BlackList(60);
                Utils.LagSleep();

                return RunStatus.Success;
            }
        }
        #endregion


        #region Behaviours

        #region Auto Attack During Pull
        public class NeedToAutoAttackPull : Decorator
        {
            public NeedToAutoAttackPull(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (!Me.GotTarget) return false;
                if (!Self.IsBuffOnMe("Stealth") && Target.IsWithinInteractRange && !Me.IsAutoAttacking) return true;
                if (Self.IsBuffOnMe("Stealth") && Me.IsAutoAttacking) { Utils.AutoAttack(false); return false; }

                return false;
            }
        }

        public class AutoAttackPull : Action
        {
            protected override RunStatus Run(object context)
            {
                Utils.AutoAttack(true);

                return RunStatus.Failure;
            }
        }
        #endregion

        #region Navigate Path
        public class NeedToNavigatePath : Decorator
        {
            public NeedToNavigatePath(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (!Me.GotTarget) return false;
                if (Settings.LazyRaider.Contains("always")) return false;
                
                return !Navigator.CanNavigateFully(Me.Location,CT.Location,20);
            }
        }

        public class NavigatePath : Action
        {
            protected override RunStatus Run(object context)
            {
                Utils.Log("Can not navigate to target's location. Blacklisting",Utils.Colour("Red"));
                Target.BlackList(Utils.IsBattleground ? 10 : 30);

                return RunStatus.Success;
            }
        }
        #endregion

        #region Move To
        public class NeedToMoveTo : Decorator
        {
            public NeedToMoveTo(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (Settings.LazyRaider.Contains("always")) return false;

                return Target.IsDistanceMoreThan(Target.InteractRange);
            }
        }

        public class MoveTo : Action
        {
            protected override RunStatus Run(object context)
            {
                

                //if (CT.IsMoving && Target.IsDistanceMoreThan(Target.InteractRange * 0.5))
                if (CT.IsMoving)
                {
                    //Movement.MoveTo(0.0f);
                    float distance = (Self.IsBuffOnMe("Sprint") ? 0.25f : -0.1f);
                    WoWPoint pointBehind = WoWMathHelper.CalculatePointBehind(CT.Location, CT.Rotation, distance);

                    Movement.MoveTo(pointBehind);
                }
                    //else if (!CT.IsMoving && Target.IsDistanceMoreThan(Target.InteractRange))
                else if (!CT.IsMoving)
                {
                    Movement.MoveTo(Target.InteractRange * 0.9f); 
                }
            
                return RunStatus.Failure;
            }
        }
        #endregion

        #region Premeditation
        public class NeedToPremeditation : Decorator
        {
            public NeedToPremeditation(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Premeditation";

                if (Settings.PickPocketOnly.Contains("always")) return false;
                if (!Self.IsBuffOnMe("Stealth") && !Self.IsBuffOnMe("Shadow Dance")) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (!Target.IsHealthPercentAbove(50) && !Target.IsElite) return false;
                if (Spell.IsOnCooldown(dpsSpell)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Premeditation : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Premeditation";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion
    
        #region Stealth
        public class NeedToStealth : Decorator
        {
            public NeedToStealth(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Stealth";
                int stealthDistance = Convert.ToInt16(Settings.StealthDistance);

                if (!Settings.PullSpell.Contains(spellName)) return false;
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (Self.IsBuffOnMe(spellName)) return false;
                if (Me.Level < 11 && Settings.StealthOverride.Contains("don't override")) return false;
                if (Target.IsDistanceMoreThan(stealthDistance)) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class Stealth : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Stealth";
                bool result = Spell.Cast(spellName);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Ambush / Garrote
        public class NeedToAmbushOrGarrote : Decorator
        {
            public NeedToAmbushOrGarrote(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                string spellName = Settings.StealthBehind;  //"Ambush";

                if (Settings.PickPocketOnly.Contains("always")) return false;
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (!Target.IsWithinInteractRange) return false;
                if (!CT.MeIsSafelyBehind) return false;



                return (Spell.CanCast(spellName));
            }
        }

        public class AmbushOrGarrote : Action
        {
            protected override RunStatus Run(object context)
            {
                if (Spell.CanCast("Cold Blood")) {Spell.Cast("Cold Blood");Utils.LagSleep();}

                string spellName = Settings.StealthBehind;
                bool result = Spell.Cast(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Cheap Shot
        public class NeedToCheapShot : Decorator
        {
            public NeedToCheapShot(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Cheap Shot";

                if (Settings.PickPocketOnly.Contains("always")) return false;
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (!Target.IsWithinInteractRange) return false;
                if (CT.MeIsSafelyBehind && Spell.IsKnown("Ambush") && Self.IsBuffOnMe("Stealth")) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class CheapShot : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Cheap Shot";
                bool result = Spell.Cast(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Sprint
        public class NeedToSprint : Decorator
        {
            public NeedToSprint(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Sprint";
                int sprintDistance = Convert.ToInt16(Settings.SprintDistance);

                if (Settings.LazyRaider.Contains("always")) return false;
                //if (!Settings.PullSpell.Contains("Stealth")) return false;
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                //if (!Self.IsBuffOnMe("Stealth")) return false;
                if (Target.IsDistanceLessThan(sprintDistance)) return false;
                
                return (Spell.CanCast(spellName));
            }
        }

        public class Sprint : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Sprint";
                bool result = Spell.Cast(spellName);
                
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Sinister Strike Pull
        public class NeedToSinisterStrikePull : Decorator
        {
            public NeedToSinisterStrikePull(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string spellName = "Sinister Strike";

                if (Settings.PickPocketOnly.Contains("always")) return false;
                if (!Utils.CombatCheckOk(spellName, false)) return false;
                if (!Spell.IsEnoughPower("Sinister Strike")) return false;
                if (!Target.IsWithinInteractRange) return false;
                if (CT.MeIsSafelyBehind && Spell.IsKnown("Ambush") && Self.IsBuffOnMe("Stealth")) return false;
                if (Spell.CanCast("Cheap Shot") && Self.IsBuffOnMe("Stealth")) return false;

                return (Spell.CanCast(spellName));
            }
        }

        public class SinisterStrikePull : Action
        {
            protected override RunStatus Run(object context)
            {
                const string spellName = "Sinister Strike";
                bool result = Spell.Cast(spellName);
                //Utils.LagSleep();
                //bool result = Target.IsDebuffOnTarget(spellName);
                //bool result = Self.IsBuffOnMe(spellName);
                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Pick Pocket
        public class NeedToPickPocket : Decorator
        {
            public NeedToPickPocket(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Pick Pocket";
                if (!CLC.ResultOK(Settings.PickPocket)) return false;
                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (Utils.IsBattleground) return false;
                if (!Timers.Expired("PickPocket", 2500)) return false;
                if (Target.IsDistanceMoreThan(Target.InteractRange)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                // Only pickpocket Humanoids and Undead
                if (CT.CreatureType != WoWCreatureType.Humanoid && CT.CreatureType != WoWCreatureType.Undead) return false;
                

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class PickPocket : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Pick Pocket";
                bool result = Spell.Cast(dpsSpell);
                Timers.Reset("PickPocket");
                System.Threading.Thread.Sleep(2000);

                if (Settings.PickPocketOnly.Contains("always"))
                {
                    Target.BlackList(300);
                    Me.ClearTarget();
                }
                //Utils.LagSleep();
                //bool result = Target.IsDebuffOnTarget(dpsSpell);
                //bool result = Self.IsBuffOnMe(dpsSpell);););

                return RunStatus.Failure;
            }
        }
        #endregion

        #region Distract
        public class NeedToDistract : Decorator
        {
            public NeedToDistract(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Distract";

                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (CT.Combat) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Utils.IsBattleground) return false;
                if (!CT.IsMoving && CT.MeIsSafelyBehind) return false;
                if (Target.IsDistanceMoreThan(Spell.MaxDistance(dpsSpell) - 5)) return false;
                if (!Timers.Expired("Distract",2000)) return false;
                
                //if (Target.IsDistanceLessThan(Target.InteractRange + 3)) return false;
                //if (Target.IsLowLevel) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Distract : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Distract";
                WoWPoint spellLocation = WoWMathHelper.CalculatePointFrom(Me.Location, Me.CurrentTarget.Location, -5);
                Timers.Reset("Distract");

                if (!Navigator.CanNavigateFully(Me.Location, spellLocation, 1)) return RunStatus.Failure;

                bool result = Spell.Cast(dpsSpell, spellLocation);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion
    
        #region Shadowstep
        public class NeedToShadowstep : Decorator
        {
            public NeedToShadowstep(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Shadowstep";

                if (!Settings.PullSpell.Contains("Stealth")) return false;
                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (Target.IsDistanceLessThan(6)) return false;
                if (Target.IsDistanceMoreThan(25)) return false;
                //if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class Shadowstep : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Shadowstep";
                bool result = Spell.Cast(dpsSpell);
                if (Settings.PickPocketOnly.Contains("always")) return RunStatus.Failure;

                Utils.LagSleep();
                //if (Spell.IsKnown("Ambush")) Spell.Cast("Ambush");
                //while (Spell.IsGCD) System.Threading.Thread.Sleep(150);)
                
                if (Spell.CanCast("Ambush")) { Spell.Cast("Ambush");}

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Shoot
        public class NeedToShoot : Decorator
        {
            public NeedToShoot(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Shoot";

                if (!Settings.PullSpell.Contains("Shoot")) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Target.IsDistanceMoreThan(30)) return false;
                if (Target.IsDistanceLessThan(5)) return false;
                if (!Utils.IsInLineOfSight(CT.Location)) return false;

                return ClassHelpers.Rogue.CanShoot;
                //return (Spell.CanCast(dpsSpell));
            }
        }

        public class Shoot : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Shoot";
                if (Me.IsMoving) Movement.StopMoving();
                bool result = Spell.Cast(dpsSpell);
                Utils.LagSleep();
                System.Threading.Thread.Sleep(2000);



                if (Me.LastRedErrorMessage.Contains("Ranged Weapon equipped")) ClassHelpers.Rogue.CanShoot = false;

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Throw
        public class NeedToThrow: Decorator
        {
            public NeedToThrow(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Throw";

                if (!Settings.PullSpell.Contains("Throw")) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;
                if (Target.IsDistanceMoreThan(30)) return false;
                if (Target.IsDistanceLessThan(5)) return false;
                if (!Utils.IsInLineOfSight(CT.Location)) return false;

                return ClassHelpers.Rogue.CanThrow;
                //return (Spell.CanCast(dpsSpell));
            }
        }

        public class Throw : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Throw";

                if (Me.IsMoving) Movement.StopMoving();
                bool result = Spell.Cast(dpsSpell);
                Utils.LagSleep();
                System.Threading.Thread.Sleep(2000);

                if (Me.LastRedErrorMessage.Contains("Thrown equipped")) ClassHelpers.Rogue.CanThrow= false;

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region Sap
        public class NeedToSap : Decorator
        {
            public NeedToSap(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Sap";

                if (Settings.PickPocketOnly.Contains("always")) return false;
                if (!CLC.ResultOK(Settings.Sap)) return false;
                if (!Self.IsBuffOnMe("Stealth")) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                List<WoWUnit> sapTarget = (from o in ObjectManager.ObjectList where o is WoWUnit let p = o.ToUnit() where p.Distance2D < 30 && p.Auras.ContainsKey("Sap") select p).ToList();
                if (sapTarget.Count > 0 )
                {
                    Utils.Log(string.Format("We have a sapped target; {0}", sapTarget[0].Name),Utils.Colour("DarkBlue"));
                    return false;
                }


                List<WoWUnit> sapList = (from o in ObjectManager.ObjectList
                                       where o is WoWUnit
                                       let p = o.ToUnit()
                                       where p.Location.Distance(CT.Location) < 17
                                             && !p.Dead
                                             && (p.CreatureType == WoWCreatureType.Beast || p.CreatureType == WoWCreatureType.Demon || p.CreatureType == WoWCreatureType.Humanoid || p.CreatureType == WoWCreatureType.Dragon)
                                             && p.IsHostile
                                             && p.Attackable
                                             && !p.Combat
                                             && p.Guid != CT.Guid
                                       orderby p.Location.Distance(CT.Location) ascending
                                       select p).ToList();

                if (sapList.Count > 0)
                {
                    Utils.Log(string.Format("Found a target to sap, {0}, {1} yards from our target", sapList[0].Name,sapList[0].Location.Distance(CT.Location)),Utils.Colour("Blue"));
                    ClassHelpers.Rogue.SapTarget = sapList[0];
                    ClassHelpers.Rogue.KillTarget = CT;
                    return true;
                }

                return false;

            }
        }

        public class Sap : Action
        {
            protected override RunStatus Run(object context)
            {
                ClassHelpers.Rogue.SapTarget.Target();
                Utils.LagSleep();
                System.Threading.Thread.Sleep(1000);

                while (!Me.Combat && !Target.IsDebuffOnTarget("Sap") && Self.IsBuffOnMe("Stealth") && ClassHelpers.Rogue.KillTarget.Location.Distance(ClassHelpers.Rogue.SapTarget.Location) < 25)
                {
                    if (!CT.IsMoving) Movement.MoveTo(Target.InteractRange*0.9f);
                    if (CT.IsMoving) Movement.MoveTo(Target.InteractRange * 0.5f);

                    if (Target.IsWithinInteractRange && Spell.CanCast("Sap")) { Target.Face(); Spell.Cast("Sap"); }
                    System.Threading.Thread.Sleep(200);
                }

                ClassHelpers.Rogue.KillTarget.Target();
                Utils.LagSleep();
                System.Threading.Thread.Sleep(500);
                Target.Face();

                return RunStatus.Success;
            }
        }
        #endregion

        #endregion


    }
}