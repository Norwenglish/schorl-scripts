//////////////////////////////////////////////////
//               Level/None.cs                  //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using Styx;
using TreeSharp;

namespace MutaRaidBT.Composites.Context.Level
{
    static class None
    {
        static public Composite BuildCombatBehavior()
        {
            return new PrioritySelector(
                Helpers.Target.EnsureValidTarget(),
                Helpers.Movement.MoveToAndFaceUnit(ret => StyxWoW.Me.CurrentTarget),

                Helpers.Spells.CastSelf("Evasion", ret => StyxWoW.Me.HealthPercent <= 35),

                Helpers.Spells.Cast("Eviscerate", ret => StyxWoW.Me.ComboPoints == 5 || StyxWoW.Me.CurrentTarget.HealthPercent <= 60),
                Helpers.Spells.Cast("Sinister Strike")
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new PrioritySelector(
                Helpers.Movement.MoveToAndFaceUnit(ret => StyxWoW.Me.CurrentTarget),
                Helpers.Spells.Cast("Sinister Strike")
            );
        }
    }
}
