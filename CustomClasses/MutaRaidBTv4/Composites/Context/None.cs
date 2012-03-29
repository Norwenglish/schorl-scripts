//////////////////////////////////////////////////
//                  None.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites.Context
{
    static class None
    {
        static public Composite BuildCombatBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                Level.None.BuildCombatBehavior()
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                Level.None.BuildPullBehavior()
            );
        }
    }
}
