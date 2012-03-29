//////////////////////////////////////////////////
//                 Area.cs                      //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Drawing;
using Styx;
using Styx.Helpers;
using Styx.Logic;

namespace MutaRaidBT.Helpers
{
    static class Area
    {
        static public Enumeration.LocationContext mLocation { get; private set; }

        static public void Pulse()
        {
            Enumeration.LocationContext curLocation = !Settings.Mode.mOverrideContext ? GetCurrentLocation() : Settings.Mode.mLocationSettings;

            if (mLocation != curLocation)
            {
                mLocation = curLocation;

                Logging.Write(Color.Orange, "");
                Logging.Write(Color.Orange, "Your current context is {0}.", mLocation);
                Logging.Write(Color.Orange, "");
            }
        }

        static public bool IsCurTargetSpecial()
        {
            switch (mLocation)
            {
                case Enumeration.LocationContext.Raid:

                    return StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.WorldBoss ||
                           (StyxWoW.Me.CurrentTarget.Level == 88 &&
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.Elite);

                case Enumeration.LocationContext.HeroicDungeon:

                    return StyxWoW.Me.CurrentTarget.Level >= 87 && 
                           (StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.Elite ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.Rare ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.RareElite ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.WorldBoss);

                case Enumeration.LocationContext.Battleground:

                    return StyxWoW.Me.CurrentTarget.IsPlayer;

                default:

                    return StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.Elite ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.Rare ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.RareElite ||
                           StyxWoW.Me.CurrentTarget.CreatureRank == WoWUnitClassificationType.WorldBoss;
            }
        }

        static private Enumeration.LocationContext GetCurrentLocation()
        {
            if (Battlegrounds.IsInsideBattleground)
            {
                return Enumeration.LocationContext.Battleground;
            }

            if(StyxWoW.Me.IsInRaid)
            {
                return Enumeration.LocationContext.Raid;
            }

            if (StyxWoW.Me.IsInInstance && StyxWoW.Me.Level == 85)
            {
                return Enumeration.LocationContext.HeroicDungeon;
            }

            if (StyxWoW.Me.IsInInstance)
            {
                return Enumeration.LocationContext.Dungeon;
            }

            return Enumeration.LocationContext.World;
        }
    }
}
