//////////////////////////////////////////////////
//           Level/Assassination.cs             //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System;
using CommonBehaviors.Actions;
using Styx;
using Styx.WoWInternals;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MutaRaidBT.Composites.Context.Level
{
    static class Assassination
    {
        static public Composite BuildCombatBehavior()
        {
            return new PrioritySelector(
                Helpers.Target.EnsureValidTarget(),
                Helpers.Movement.MoveToAndFaceUnit(ret => StyxWoW.Me.CurrentTarget),

                new Decorator(ret => StyxWoW.Me.HealthPercent <= 15 && Helpers.Spells.CanCast("Vanish"),
                    new Sequence(
                        Helpers.Spells.CastSelf("Vanish"),
                        new WaitContinue(2, ret => false, new ActionAlwaysSucceed())
                    )
                ),

                Helpers.Spells.ToggleAutoAttack(),

                Helpers.Specials.UseSpecialAbilities(),

                Helpers.Spells.CastSelf("Evasion",          ret => StyxWoW.Me.HealthPercent <= 35),
                Helpers.Spells.CastSelf("Cloak of Shadows", ret => Helpers.Rogue.IsCloakUsable()),
                Helpers.Spells.CastCooldown("Vendetta",     ret => Helpers.Rogue.IsCooldownsUsable()),
                Helpers.Spells.CastSelf("Cold Blood",       ret => !StyxWoW.Me.HasAura("Cold Blood") && StyxWoW.Me.ComboPoints >= 4 && 
                                                                   Helpers.Rogue.mCurrentEnergy >= 60),

                Helpers.Spells.CastSelf("Recuperate",   ret => !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Recuperate") && StyxWoW.Me.ComboPoints >= 3 && 
                                                               StyxWoW.Me.HealthPercent <= 80),

                new Decorator(ret => Helpers.Rogue.IsInterruptUsable(),
                    new Sequence(
                        Helpers.Spells.Cast("Kick"),
                        new WaitContinue(TimeSpan.FromSeconds(0.5), ret => false, new ActionAlwaysSucceed())
                    )
                ),

                Helpers.Spells.Cast("Kidney Shot",  ret => StyxWoW.Me.ComboPoints >= 3 && (StyxWoW.Me.HealthPercent <= 75 || 
                                                           (Helpers.Rogue.IsInterruptUsable() && Helpers.Spells.GetSpellCooldown("Kick") > 0))),
                Helpers.Spells.Cast("Rupture",      ret => StyxWoW.Me.ComboPoints >= 3 && StyxWoW.Me.CurrentTarget.HealthPercent >= 75 &&
                                                           !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture")),
                Helpers.Spells.Cast("Envenom",      ret => StyxWoW.Me.ComboPoints >= 4),
                Helpers.Spells.Cast("Eviscerate",   ret => StyxWoW.Me.ComboPoints >= 4),

                Helpers.Spells.Cast("Mutilate")
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => StyxWoW.Me.Mounted, 
                    new Action(ret => Lua.DoString("Dismount()"))
                ),

                Helpers.Spells.CastSelf("Stealth", ret => !StyxWoW.Me.HasAura("Stealth")),

                Helpers.Movement.MoveToAndFaceUnit(ret => StyxWoW.Me.CurrentTarget),
                Helpers.Spells.Cast("Cheap Shot",  ret => StyxWoW.Me.HasAura("Stealth")),
                Helpers.Spells.Cast("Mutilate")
            );
        }

        static public Composite BuildBuffBehavior()
        {
            return new Decorator(ret => !StyxWoW.Me.Mounted,
                new PrioritySelector(
                    Helpers.Spells.CastSelf("Recuperate",     ret => !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Recuperate") &&
                                                                     StyxWoW.Me.RawComboPoints >= 1),
                    Helpers.Spells.CastSelf("Slice and Dice", ret => !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Slice and Dice") &&
                                                                     StyxWoW.Me.RawComboPoints >= 1)
                )
            );
        }
    }
}
