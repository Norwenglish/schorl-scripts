//////////////////////////////////////////////////
//              Raid/Subtlety.cs                //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System;
using CommonBehaviors.Actions;
using Styx;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MutaRaidBT.Composites.Context.Raid
{
    static class Subtlety
    {
        static public Composite BuildCombatBehavior()
        {
            return new FlPrioritySelector(
                Helpers.Spells.ToggleAutoAttack(),

                Helpers.Spells.Cast("Redirect", ret => StyxWoW.Me.ComboPoints < StyxWoW.Me.RawComboPoints),

                new Decorator(ret => StyxWoW.Me.ComboPoints == 5 || Helpers.Spells.IsAuraActive(StyxWoW.Me, "Fury of the Destroyer"),
                    new PrioritySelector(
                        Helpers.Spells.Cast("Rupture",            ret => Helpers.Area.IsCurTargetSpecial() &&
                                                                         Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Find Weakness") &&
                                                                         !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture")),
                        Helpers.Spells.CastSelf("Slice and Dice", ret => Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me, "Slice and Dice") < 3),
                        Helpers.Spells.Cast("Rupture",            ret => Helpers.Area.IsCurTargetSpecial() &&
                                                                         !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture")),
                        Helpers.Spells.CastSelf("Recuperate",     ret => Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me, "Recuperate") < 3),
                        Helpers.Spells.Cast("Eviscerate",         ret => Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Find Weakness") || 
                                                                         Helpers.Spells.IsAuraActive(StyxWoW.Me, "Fury of the Destroyer") ||
                                                                         Helpers.Rogue.mCurrentEnergy >= 60 || (Helpers.Area.IsCurTargetSpecial() && 
                                                                         Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me.CurrentTarget, "Rupture") < 3))
                    )
                ),

                Helpers.Spells.CastFocus("Tricks of the Trade", ret => StyxWoW.Me.FocusedUnit.IsFriendly && StyxWoW.Me.FocusedUnit.IsPlayer &&
                                                                       StyxWoW.Me.FocusedUnit.ToPlayer().IsInMyPartyOrRaid),

                Helpers.Spells.CastCooldown("Premeditation", ret => StyxWoW.Me.ComboPoints <= 3 && (StyxWoW.Me.HasAura("Stealth") || 
                                                                    StyxWoW.Me.HasAura("Shadow Dance") || StyxWoW.Me.HasAura("Vanish"))),

                Helpers.Specials.UseSpecialAbilities(ret => Helpers.Spells.IsAuraActive(StyxWoW.Me, "Shadow Dance") ||
                                                            Helpers.Spells.GetSpellCooldown("Shadow Dance") >= 10),

                new Decorator(ret => StyxWoW.Me.ComboPoints == 0 &&
                                     Helpers.Rogue.mCurrentEnergy >= 50 &&
                                     !(Helpers.Spells.GetSpellCooldown("Premeditation") > 0),
                    new PrioritySelector(
                        new Decorator(ret => Helpers.Spells.CanCast("Shadow Dance"),
                            new Sequence(
                                Helpers.Spells.CastSelf("Shadow Dance"),
                                new WaitContinue(TimeSpan.FromSeconds(0.5), ret => false, new ActionAlwaysSucceed())
                            )
                        ),

                        new Decorator(ret => Helpers.Rogue.IsCooldownsUsable() &&
                                             !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Shadow Dance") &&
                                             Helpers.Spells.GetSpellCooldown("Shadow Dance") > 0 &&
                                             Helpers.Spells.CanCast("Vanish"),
                            new Sequence(
                                Helpers.Spells.CastSelf("Vanish"),
                                new WaitContinue(TimeSpan.FromSeconds(1), ret => false, new ActionAlwaysSucceed()),
                                Helpers.Spells.CastCooldown("Premeditation"),
                                Helpers.Spells.Cast("Ambush", ret => Helpers.Rogue.IsBehindUnit(StyxWoW.Me.CurrentTarget))
                            )
                        ),

                        Helpers.Spells.CastSelf("Preparation", ret => Helpers.Rogue.IsCooldownsUsable() &&
                                                                      Helpers.Spells.GetSpellCooldown("Vanish") > 30)
                    )
                ),

                // CP Builders
                new Decorator(ret => StyxWoW.Me.ComboPoints != 5 && (StyxWoW.Me.ComboPoints < 4 ||
                                     (StyxWoW.Me.ComboPoints == 4 && (Helpers.Rogue.mCurrentEnergy >= 90 ||
                                     Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me.CurrentTarget, "Rupture") < 3 ||
                                     Helpers.Spells.IsAuraActive(StyxWoW.Me, "Shadow Dance")))),
                    new PrioritySelector(
                        Helpers.Spells.Cast("Ambush",     ret => Helpers.Rogue.IsBehindUnit(StyxWoW.Me.CurrentTarget) && 
                                                                 (StyxWoW.Me.HasAura("Stealth") || StyxWoW.Me.HasAura("Vanish") || 
                                                                 StyxWoW.Me.HasAura("Shadow Dance"))),
                        Helpers.Spells.Cast("Hemorrhage", ret => Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me.CurrentTarget, 89775) < 3),
                        Helpers.Spells.Cast("Backstab",   ret => Helpers.Rogue.IsBehindUnit(StyxWoW.Me.CurrentTarget)),
                        Helpers.Spells.Cast("Hemorrhage", ret => !Helpers.Rogue.IsBehindUnit(StyxWoW.Me.CurrentTarget))
                    )
                )
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Action(ret => RunStatus.Failure);
        }

        static public Composite BuildBuffBehavior()
        {
            return new Action(ret => RunStatus.Failure);
        }
    }
}
