//////////////////////////////////////////////////
//                 Focus.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using Styx;
using Styx.WoWInternals.WoWObjects;

namespace MutaRaidBT.Helpers
{
    static class Focus
    {
        static public WoWUnit mFocusTarget { get; private set; }

        static public void Pulse()
        {
            mFocusTarget = GetFocusTarget();
        }

        static private WoWUnit GetFocusTarget()
        {
            WoWUnit curFocus = StyxWoW.Me.FocusedUnit;

            if (curFocus != null && curFocus.InLineOfSpellSight && curFocus.IsAlive)
            {
                return curFocus;
            }

            return null;
        }
    }
}
