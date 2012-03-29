//////////////////////////////////////////////////
//       Battleground/Assassination.cs          //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites.Context.Battleground
{
    static class Assassination
    {
        // For now, just use the same behavior as our level context.

        static public Composite BuildCombatBehavior()
        {
            return new PrioritySelector(
                Helpers.Target.EnsureBestPvPTarget(),
                Level.Assassination.BuildCombatBehavior()
            );
        }

        static public Composite BuildPullBehavior()
        {
            return Level.Assassination.BuildPullBehavior();
        }

        static public Composite BuildBuffBehavior()
        {
            return Level.Assassination.BuildBuffBehavior();
        }
    }
}
