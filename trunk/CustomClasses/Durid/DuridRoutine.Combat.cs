using System;
using System.Linq;

using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;

using TreeSharp;

using Action = TreeSharp.Action;

namespace Durid
{
    partial class DuridRoutine
    {
        private Composite _combatBehavior;

        public override Composite CombatBehavior { get { return _combatBehavior ?? (_combatBehavior = CreateCombatBehavior()); } }

        private Composite CreateCombatBehavior()
        {
            return new PrioritySelector(
                // Get around sticking to a dead target. HB will re-target for us.
                new Decorator(
                    ret => Me.CurrentTarget != null && Me.CurrentTarget.Dead,
                    new Action(ret => Me.ClearTarget())),

                new Decorator(ret=>Me.CurrentTarget == null,
                    new ActionLog("Current target is null!!")),

                // Firsly, make sure we're actually facing the damned target.
                new Decorator(
                    // Use a 90 degree cone, instead of 150. This ensures we're always *actually* facing the mob.
                    ret => !Me.IsSafelyFacing(StyxWoW.Me.CurrentTarget, 90f),
                    new Action(ret => StyxWoW.Me.CurrentTarget.Face())),

                new Decorator(
                    ret => Me.Shapeshift != CurrentShapeshift,
                    new Action(ret => EnsureCurrentForm())),

                new Decorator(
                    ret => Me.Shapeshift == ShapeshiftForm.Cat || Me.Shapeshift == ShapeshiftForm.CreatureCat,
                    CreateKittyCombat()),
                new Decorator(
                    ret => Me.Shapeshift == ShapeshiftForm.Moonkin,
                    CreateBoomkinCombat())

                );
        }

        private WoWPoint GetKittyCombatLocation()
        {
            //if (Battlegrounds.IsInsideBattleground)
            //{
            //    return WoWMathHelper.CalculatePointBehind(Me.CurrentTarget.Location, Me.CurrentTarget.Rotation, 1);
            //}
            return Me.CurrentTarget.Location;
        }

        private Composite CreateKittyCombat()
        {
            return new PrioritySelector(
                //new ActionLog("Inside kitty combat!"),

                new Decorator(
                    ret => !Me.IsAutoAttacking,
                    new Action(ret => Me.ToggleAttack())),

                //new ActionLog("AutoAttacking done"),
                CreateCast("Berserk", ret => Me.Fleeing),
                //new ActionLog("Beserk done"),
                CreateCast("Survival Instincts", ret => Me.HealthPercent <= 45),
                CreateSelfBuff("Prowl"),
                CreateCast("Feral Charge (Cat)", ret => Me.CurrentTarget.Distance >= 8 && Me.CurrentTarget.Distance <= 25),
                CreateCast("Skull Bash", ret => Me.CurrentTarget.IsCasting),

                //new ActionLog("Dash/Stampede Check"),
                // Kudos to regecksqt for the dash/stampeding roar logic. Slightly changed for reading purposes.
                new Decorator(
                    ret =>
                    Me.CurrentTarget.Distance > 5 &&
                    !WoWMathHelper.IsFacing(Me.CurrentTarget.Location, Me.CurrentTarget.Rotation, Me.Location, (float)Math.PI) &&
                    Me.CurrentTarget.IsMoving && Me.CurrentTarget.MovementInfo.RunSpeed > Me.MovementInfo.RunSpeed,
                    new PrioritySelector(
                        CreateCast("Dash"),
                        CreateCast("Stampeding Roar (Cat)", ret => Me.CurrentEnergy >= 50))),

                //new ActionLog("Low distance check"),
                new Decorator(
                    ret => Me.CurrentTarget.Distance <= 5,

                    new PrioritySelector(
                        new Decorator(
                            ret => StyxWoW.Me.IsMoving,
                            // We use the player mover, since people can override it. This lets us support their stuff.
                            new Action(ret => Navigator.PlayerMover.MoveStop())),

                        CreateCast("Pounce", ret => Me.HasAura("Prowl")),
                        CreateCast("Barkskin", ret => Adds.Count(u => u.Distance < 5) > 0),
                        CreateCast("Tiger's Fury", ret => Me.CurrentEnergy <= 50),

                        new Decorator(
                            ret => Me.ComboPoints == 5,
                            new PrioritySelector(
                                CreateSelfBuff("Savage Roar", ret => Me.HealthPercent >= 75),
                                CreateCast("Maim", ret => !Me.CurrentTarget.Stunned),
                                CreateCast(
                                    "Rip", ret => !Me.CurrentTarget.HasAura("Rip") || Me.CurrentTarget.GetAuraByName("Rip").CreatorGuid != Me.Guid),
                                CreateCast("Ferocious Bite"))),

                        new Decorator(
                            ret => /*IsBehind(Me.CurrentTarget) &&*/ Me.HasAura("Stampede"),
                            new TreeSharp.Action(a => WoWSpell.FromId(81170).Cast())),

                        new Decorator(
                            ret => !Me.CurrentTarget.HasAura("Mangle") && SpellManager.CanCast("Mangle (Cat)"),
                            new Action(ret => SpellManager.Cast("Mangle (Cat)"))),
                        CreateCast("Rake", ret => !Me.CurrentTarget.HasAura("Rake") || Me.CurrentTarget.GetAuraByName("Rake").CreatorGuid != Me.Guid),
                        CreateCast("Shred", ret => IsBehind(Me.CurrentTarget)),
                        // Don't swipe if we don't have more than 2 people/mobs on us, within range.
                        CreateCast("Swipe (Cat)", ret => Adds.Count(u => u.DistanceSqr <= 5 * 5) >= 2),

                        //new ActionLog("Mangle"),
                        CreateCast("Mangle (Cat)"))),

                //new ActionLog("Far Distance Check"),
                new Decorator(
                    ret => Me.CurrentTarget.Distance > 3f || !Me.CurrentTarget.InLineOfSight,
                    new Action(ret => Navigator.MoveTo(GetKittyCombatLocation()))),

                CreateBuff("Faerie Fire (Feral)"),
                // For absolutely low level usage. We should *never* get to this point.
                CreateCast("Claw")
                //,new ActionLog("End of kitty behavior")
                );

        }

        private string _oldDps = "Wrath";
        private string BoomkinDpsSpell
        {
            get
            {
                if (Me.ActiveAuras.ContainsKey("Eclipse (Solar)"))
                {
                    _oldDps = "Wrath";
                }
                // This doesn't seem to register for whatever reason.
                else if (Me.ActiveAuras.ContainsKey("Eclipse (Lunar)"))//Eclipse (Lunar) => 48518
                {
                    _oldDps = "Starfire";
                }
                return _oldDps;
            }
        }
        private Composite CreateBoomkinCombat()
        {
            return new PrioritySelector(
                // Always auto attack. I don't care how much you hate it, at least if something really bugs out,
                // we can smack them with a stick
                new Decorator(
                    ret => !Me.IsAutoAttacking,
                    new Action(ret => Me.ToggleAttack())),
                new Decorator(
                    ret => Me.CurrentTarget.Distance > 35f || !Me.CurrentTarget.InLineOfSight,
                    new Action(ret => Navigator.MoveTo(Me.CurrentTarget.Location))),
                new Decorator(
                    ret => Me.IsMoving,
                    new Action(ret => Navigator.PlayerMover.MoveStop())),

                new Decorator(
                    ret => Adds.Count > 1 && SpellManager.CanCast("Force of Nature"),
                    new Sequence(
                        new Action(ret => SpellManager.Cast("Force of Nature")),
                        new Action(ret => LegacySpellManager.ClickRemoteLocation(Adds[1].Location)))),

                CreateCast("Solar Beam", ret => Me.CurrentTarget.IsCasting),

                CreateBuff("Cyclone", ret => Adds.Count > 1, ret => Adds[1]),

                CreateCast("Typhoon", ret => Adds.Count > 2 && Battlegrounds.IsInsideBattleground),

                // Always pop starsurge first.
                CreateCast("Starsurge"),
                CreateBuff("Moonfire", ret => !Me.CurrentTarget.HasAura("Sunfire")),
                CreateBuff("Insect Swarm"),

                //CreateCast("Wrath", ret => Me.HasAura("Eclipse (Solar)")),
                //CreateCast("Starfire", ret => Me.HasAura("Eclipse (Lunar)")),



                // Last ditch effort, result to Wrath
                //new ActionLog(ret=>BoomkinDpsSpell),
                CreateCast(ret => BoomkinDpsSpell)
                );
        }

        private void MoveToRange(float range)
        {
            if (Me.CurrentTarget.DistanceSqr > range*range || !Me.CurrentTarget.InLineOfSight)
            {
                Navigator.MoveTo(Me.CurrentTarget.Location);
                return;
            }

            Me.CurrentTarget.Face();
            if(Me.IsMoving)
                Navigator.PlayerMover.MoveStop();
        }
    }
}
