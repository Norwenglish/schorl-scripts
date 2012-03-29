//////////////////////////////////////////////////
//               Raid/Combat.cs                 //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Linq;
using Styx;
using Styx.WoWInternals;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MutaRaidBT.Composites.Context.Raid
{
    class Combat
    {
        static public Composite BuildCombatBehavior()
        {
            return new FlPrioritySelector(
                Helpers.Spells.ToggleAutoAttack(),

                Helpers.Spells.Cast("Redirect", ret => StyxWoW.Me.ComboPoints < StyxWoW.Me.RawComboPoints),


                Helpers.Spells.CastSelf("Blade Flurry", ret => Helpers.Rogue.IsAoeUsable() && !StyxWoW.Me.HasAura("Blade Flurry") &&
                                                               Helpers.Target.mNearbyEnemyUnits.Count(unit => unit.IsWithinMeleeRange) >= 2),

                new Decorator(ret => Helpers.Rogue.IsAoeUsable() && Helpers.Target.mNearbyEnemyUnits.Count(unit => unit.Distance <= 15) < 2 && 
                                     StyxWoW.Me.HasAura("Blade Flurry"),
                    // Ugly. Find a way to cancel auras without Lua.
                    new Action(ret => Lua.DoString("RunMacroText('/cancelaura Blade Flurry');"))
                ),

                Helpers.Spells.CastSelf("Slice and Dice", ret => (StyxWoW.Me.ComboPoints >= 1 || 
                                                                 Helpers.Spells.IsAuraActive(StyxWoW.Me, "Fury of the Destroyer")) && 
                                                                 Helpers.Spells.GetAuraTimeLeft(StyxWoW.Me, "Slice and Dice") < 1),

                Helpers.Spells.Cast("Eviscerate",         ret => (StyxWoW.Me.ComboPoints == 5 && (Helpers.Rogue.mCurrentEnergy >= 65 ||
                                                                 StyxWoW.Me.HasAura("Adrenaline Rush"))) || 
                                                                 Helpers.Spells.IsAuraActive(StyxWoW.Me, "Fury of the Destroyer")),

                Helpers.Spells.CastFocus("Tricks of the Trade", ret => StyxWoW.Me.FocusedUnit.IsFriendly && StyxWoW.Me.FocusedUnit.IsPlayer &&
                                                                       StyxWoW.Me.FocusedUnit.ToPlayer().IsInMyPartyOrRaid),

                new Decorator(ret => Helpers.Rogue.IsCooldownsUsable() && StyxWoW.Me.HasAura("Slice and Dice") &&
                                     (Helpers.Spells.IsAuraActive(StyxWoW.Me, "Moderate Insight") ||
                                     Helpers.Spells.IsAuraActive(StyxWoW.Me, "Deep Insight")) &&
                                     Helpers.Rogue.mCurrentEnergy <= 20,
                    new PrioritySelector(
                        Helpers.Specials.UseSpecialAbilities(),
                        Helpers.Spells.CastSelf("Adrenaline Rush"),
                        Helpers.Spells.CastCooldown("Killing Spree", ret => !StyxWoW.Me.HasAura("Adrenaline Rush"))
                    )
                ),

                Helpers.Spells.Cast("Revealing Strike", ret => StyxWoW.Me.ComboPoints == 4 && 
                                                               !Helpers.Spells.IsAuraActive(StyxWoW.Me.CurrentTarget, "Revealing Strike")),

                Helpers.Spells.Cast("Sinister Strike",  ret => StyxWoW.Me.ComboPoints < 5)
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
