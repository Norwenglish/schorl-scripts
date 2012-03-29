//////////////////////////////////////////////////
//            Raid/Assassination.cs             //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Linq;
using CommonBehaviors.Actions;
using Styx;
using TreeSharp;

namespace MutaRaidBT.Composites.Context.Raid
{
    static class Assassination
    {
        static public Composite BuildCombatBehavior()
        {
            return new FlPrioritySelector(
                Helpers.Spells.ToggleAutoAttack(),

                Helpers.Spells.Cast("Redirect",           ret => StyxWoW.Me.ComboPoints < StyxWoW.Me.RawComboPoints),

                Helpers.Spells.Cast("Envenom",            ret => Helpers.Spells.IsAuraActive(StyxWoW.Me, "Fury of the Destroyer")),

                Helpers.Spells.CastSelf("Slice and Dice", ret => !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Slice and Dice") &&
                                                                 StyxWoW.Me.ComboPoints >= 1),

                Helpers.Spells.Cast("Rupture",            ret => ((Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me.CurrentTarget, "Rupture") < 1 && 
                                                                 Helpers.Rogue.mCurrentEnergy <= 100) ||
                                                                 !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture")) &&
                                                                 StyxWoW.Me.ComboPoints >= 1),

                Helpers.Spells.CastFocus("Tricks of the Trade", ret => StyxWoW.Me.FocusedUnit.IsFriendly && StyxWoW.Me.FocusedUnit.IsPlayer && 
                                                                       StyxWoW.Me.FocusedUnit.ToPlayer().IsInMyPartyOrRaid),

                Helpers.Spells.Cast("Fan of Knives", ret => Helpers.Rogue.IsAoeUsable() && 
                                                            Helpers.Target.mNearbyEnemyUnits.Count(unit => unit.Distance <= 10) > 3),

                new Decorator(ret => Helpers.Rogue.IsCooldownsUsable() &&
                                     Helpers.Spells.IsAuraActive(StyxWoW.Me, "Slice and Dice") &&
                                     Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture"),

                    new PrioritySelector(
                        Helpers.Specials.UseSpecialAbilities(ret => Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Vendetta") ||
                                                                    Helpers.Spells.GetSpellCooldown("Vendetta") > 0),
                        Helpers.Spells.CastCooldown("Vendetta"),

                        new Decorator(ret => Helpers.Spells.CanCast("Vanish") && !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Overkill") &&
                                             Helpers.Rogue.mCurrentEnergy >= 60 && Helpers.Rogue.mCurrentEnergy <= 100 && StyxWoW.Me.ComboPoints != 5,
                            new Sequence(
                                Helpers.Spells.CastSelf("Vanish"),
                                new WaitContinue(1, ret => false, new ActionAlwaysSucceed()),
                                Helpers.Spells.Cast("Garrote")
                                )
                            ),

                        Helpers.Spells.CastSelf("Cold Blood", ret => !StyxWoW.Me.HasAura("Cold Blood") && StyxWoW.Me.ComboPoints == 5 && 
                                                                     Helpers.Rogue.mCurrentEnergy >= 60 && Helpers.Rogue.mCurrentEnergy <= 80)
                    )
                ),

                Helpers.Spells.Cast("Envenom",  ret => ((Helpers.Rogue.mCurrentEnergy >= 90 && StyxWoW.Me.ComboPoints >= 4 && StyxWoW.Me.CurrentTarget.HealthPercent >= 35) ||
                                                       (Helpers.Rogue.mCurrentEnergy >= 90 && StyxWoW.Me.ComboPoints == 5 && StyxWoW.Me.CurrentTarget.HealthPercent < 35) ||
                                                       (Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me, "Slice and Dice") <= 3 && StyxWoW.Me.ComboPoints >= 1)) &&
                                                       (!Helpers.Spells.IsAuraActive(StyxWoW.Me, "Envenom") || Helpers.Rogue.mCurrentEnergy > 100)),

                Helpers.Spells.Cast("Backstab", ret => Helpers.Rogue.IsBehindUnit(StyxWoW.Me.CurrentTarget) && StyxWoW.Me.CurrentTarget.HealthPercent < 35 &&
                                                       ((!Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture") ||
                                                       (StyxWoW.Me.ComboPoints != 5 && Helpers.Rogue.mCurrentEnergy < 110 &&
                                                       (Helpers.Rogue.mCurrentEnergy >= 80 || Helpers.Spells.IsAuraActive(StyxWoW.Me, "Envenom")))))),

                Helpers.Spells.Cast("Mutilate", ret => !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Rupture") ||
                                                       (StyxWoW.Me.ComboPoints < 4 && (Helpers.Rogue.mCurrentEnergy >= 90 ||
                                                       Helpers.Spells.IsAuraActive(StyxWoW.Me, "Envenom"))))

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
