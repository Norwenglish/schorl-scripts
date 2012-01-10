using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Styx.Combat.CombatRoutine;
using Styx.Logic;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;

namespace Durid.Healing
{
    class HealTargeting
    {
        static readonly SortedList<float, WoWPlayer> HealList = new SortedList<float, WoWPlayer>();

        public static WoWPlayer BestHealTarget { get { return HealList.Values.FirstOrDefault(); } }

        private static float GetClassWeight(WoWClass @class)
        {
            switch (@class)
            {
                case WoWClass.Warrior:
                    return 120f;
                case WoWClass.Paladin:
                    return 110f;
                case WoWClass.DeathKnight:
                    return 120f;
                case WoWClass.Druid:
                    return 110f;
                default:
                    return 100f;
            }
        }

        public static void Pulse()
        {
            HealList.Clear();
            // Firstly, we need all the players in the object manager, so we can heal whoever we want.
            var players = from p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true)
                          where p.IsFriendly && p.IsAlive && !p.IsFlying && !p.OnTaxi
                          select p;

            // Nothing to heal.
            if (players.Count() == 0)
            {
                return;
            }

            foreach (var p in players)
            {
                double weight = 200f;

                // The more health; the lower the weight.
                weight -= p.HealthPercent * 2;

                // Basically; the more weight we give to each class, the more we want to heal them.
                // Tank classes should be preferred over others. Since we need to keep tanks healed first.
                weight += (1.1f * GetClassWeight(p.Class));

                // Little extra weight to the leader.
                if (RaFHelper.Leader == p)
                    weight += 50f;

                // Obviously, we want more weight on LOS players.
                // If they get out of LOS, it's not really our fault. We shouldn't run to move to them!
                if (p.InLineOfSight)
                    weight += 100f;
                else
                    weight -= 100f;

                // Give ourselves less weight than others.
                if (p.IsMe)
                    weight -= 50f;

                // Since it's a sorted list, we don't need to do anything here! Huzzah!
                HealList.Add((float)weight, p);
            }
        }
    }
}
