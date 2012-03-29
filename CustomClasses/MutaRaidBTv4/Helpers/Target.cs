//////////////////////////////////////////////////
//                Target.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace MutaRaidBT.Helpers
{
    static class Target
    {
        static public IEnumerable<WoWUnit> mNearbyEnemyUnits { get; private set; }

        static public void Pulse()
        {
            mNearbyEnemyUnits = ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                                    .Where(unit =>
                                        !unit.IsFriendly
                                        && unit.IsAlive
                                        && (unit.IsTargetingMeOrPet
                                           || unit.IsTargetingMyPartyMember
                                           || unit.IsTargetingMyRaidMember
                                           || unit.IsPlayer)
                                        && !unit.IsNonCombatPet
                                        && !unit.IsCritter
                                        && unit.Distance <= 40)
                                    .OrderBy(unit => unit.Distance).ToList();
        }

        static public Composite EnsureValidTarget()
        {
            return new Decorator(ret => StyxWoW.Me.CurrentTarget == null || !StyxWoW.Me.CurrentTarget.IsAlive,
                GetNewTarget()
            );
        }

        static public Composite EnsureBestPvPTarget()
        {
            return new Action();
        }

        static private Composite GetNewTarget()
        {
            return new Action(ret =>
                {
                    var botBaseUnit = Targeting.Instance.FirstUnit;

                    if (botBaseUnit != null && botBaseUnit.IsAlive &&
                        !botBaseUnit.IsFriendly)
                    {
                        Logging.Write(Color.Orange, "Changing target to " + botBaseUnit.Name);
                        botBaseUnit.Target();
                    }
                    else
                    {
                        var nextUnit = mNearbyEnemyUnits.FirstOrDefault();

                        if (nextUnit != null)
                        {
                            Logging.Write(Color.Orange, "Changing target to " + nextUnit.Name);
                            nextUnit.Target();
                        }
                    }
                }
            );
        }
    }
}
