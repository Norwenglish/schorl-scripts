/*
 * NOTE:    DO NOT POST ANY MODIFIED VERSIONS OF THIS TO THE FORUMS.
 * 
 *          DO NOT UTILIZE ANY PORTION OF THIS PLUGIN WITHOUT
 *          THE PRIOR PERMISSION OF AUTHOR.  PERMITTED USE MUST BE
 *          ACCOMPANIED BY CREDIT/ACKNOWLEDGEMENT TO ORIGINAL AUTHOR.
 * 
 * Author:  Bobby53
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;

using Levelbot.Actions.Combat;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;
using CommonBehaviors.Actions;
using Action = Styx.TreeSharp.Action;
using Sequence = Styx.TreeSharp.Sequence;
using Styx.WoWInternals;

using Bobby53;
using Styx.CommonBot;

namespace Styx.Bot.CustomBots
{
    public partial class LazyRaider : BotBase
    {
        public static IncludeTargetsFilterDelegate includeTargets;
        public static RemoveTargetsFilterDelegate removeTargets;
        public static WeighTargetsDelegate weighTargets;

        private static void TargetFilterSetup()
        {
            TargetFilterClear();

            if (Battlegrounds.IsInsideBattleground)
            {
                includeTargets = PvpIncludeTargetsFilter;
                removeTargets = PvpRemoveTargetsFilter;
                weighTargets = PvpWeighTargetsFilter;
            }
            else if (IsInGroup)
            {
                includeTargets = GroupIncludeTargetsFilter;
                removeTargets = GroupRemoveTargetsFilter;
                weighTargets = GroupWeighTargetsFilter;
            }
            else
            {
                includeTargets = SoloIncludeTargetsFilter;
                removeTargets = SoloRemoveTargetsFilter;
                weighTargets = SoloWeighTargetsFilter;
            }

            Targeting.Instance.IncludeTargetsFilter += includeTargets;
            Targeting.Instance.RemoveTargetsFilter += removeTargets;
            Targeting.Instance.WeighTargetsFilter += weighTargets;
        }

        private static void TargetFilterClear()
        {
            if (includeTargets != null)
                Targeting.Instance.IncludeTargetsFilter -= includeTargets;
            if (removeTargets != null)
                Targeting.Instance.RemoveTargetsFilter -= removeTargets;
            if (weighTargets != null)
                Targeting.Instance.WeighTargetsFilter -= weighTargets;

            includeTargets = null;
            removeTargets = null;
            weighTargets = null;
        }

#if WE_DONT_NEED
        private static void IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            if ( Battlegrounds.IsInsideBattleground )
                PvpIncludeTargetsFilter( incomingUnits, outgoingUnits );
            else if ( !IsInGroup ) 
                SoloIncludeTargetsFilter( incomingUnits, outgoingUnits );
            else 
                GroupIncludeTargetsFilter( incomingUnits, outgoingUnits );

            // Dlog( "IncludeTargetsFilter: ");
        }

        private static void RemoveTargetsFilter(List<WoWObject> units)
        {
            if ( Battlegrounds.IsInsideBattleground )
                PvpRemoveTargetsFilter(units);
            else if ( !IsInGroup ) 
                SoloRemoveTargetsFilter(units);
            else
                GroupRemoveTargetsFilter(units);

            // Dlog("RemoveTargetsFilter: ");
        }

        private static void WeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            if ( Battlegrounds.IsInsideBattleground )
                PvpWeighTargetsFilter(units);
            else if ( !IsInGroup ) 
                SoloWeighTargetsFilter(units);
            else 
                GroupWeighTargetsFilter(units);

            // Dump("WeighTargetsFilter", units);
        }
#endif

        private static void GroupIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            WoWPartyMember.GroupRole myRole = GetGroupRoleAssigned(Me);

            if (myRole != WoWPartyMember.GroupRole.Tank)
            {
                foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
                {
                    if (unit.Combat && unit.GotTarget && unit.CurrentTarget.IsTargetingMyPartyMember )
                        continue;

                    outgoingUnits.Add(unit);
                }
            }
            else
            {
                if (!Me.Combat)
                    return;

                foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
                {
                    if (unit.IsAlive && (unit.DistanceSqr > 30 * 30 || !unit.InLineOfSpellSight ))
                        continue;

                    outgoingUnits.Add(unit);
                }
            }
        }

        private static void GroupRemoveTargetsFilter(List<WoWObject> units)
        {
            units.RemoveAll(obj =>
                {
                    var u = obj as WoWUnit;
                    return 
                            u == null || !obj.IsValid 
                        || !IsEnemy( u)
                        || !u.InLineOfSpellSight
                        || u.ControllingPlayer != null;
                });

            return;
        }

        private static void GroupWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            bool ImaTank = GetGroupRoleAssigned(Me) == WoWPartyMember.GroupRole.Tank;
            foreach (var o in units)
            {
                WoWUnit u = o.Object.ToUnit();

                o.Score = 100;
                o.Score -= u.Distance;
                if (ImaTank)
                {
                    if (u.IsTargetingMyPartyMember)
                        o.Score += 100;
                }
                else if ( u.Combat && u.GotTarget && u.CurrentTarget.IsPlayer )
                {
                    if (Tank.Guid == u.CurrentTarget.Guid)
                        o.Score += 200;
                    else if ( GetGroupRoleAssigned(u.CurrentTarget.ToPlayer()) == WoWPartyMember.GroupRole.Tank )
                        o.Score += 100;
                }
            }
        }

        private static void PvpIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
            {
                if ( unit.IsAlive && (unit.DistanceSqr > 40 * 40 || !unit.InLineOfSpellSight))
                {
                    continue;
                }

                outgoingUnits.Add(unit);
            }
        }

        private static void PvpRemoveTargetsFilter(List<WoWObject> units)
        {
            units.RemoveAll(obj =>
            {
                if (obj == null || !obj.IsValid)
                    return true;

                if (!(obj is WoWUnit))
                    return true;

                var unit = obj.ToUnit();

                if (!unit.IsAlive)
                    return true;

                if (!IsEnemy(unit) || unit.ControllingPlayer != null)
                    return true;

                if (unit.IsPet || unit.CreatedByUnit != null)
                    return true;

                return false;
            });
        }

        private static void PvpWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            WoWPlayer t = Tank.Player;
            foreach (var p in units)
            {
                WoWUnit u = p.Object.ToUnit();
                p.Score = 100 - u.HealthPercent;

                if (t != null && t.CurrentTargetGuid == u.Guid )
                    p.Score += 200;
            }
        }

        private static void SoloIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
            {
                if (!Me.Combat)
                {
                    if (!unit.IsAlive || unit.DistanceSqr > 30 * 30 || !unit.InLineOfSight)
                    {
                        continue;
                    }
                }
                outgoingUnits.Add(unit);
            }
        }

        private static void SoloRemoveTargetsFilter(List<WoWObject> units)
        {
            units.RemoveAll(obj =>
            {
                if (!obj.IsValid)
                    return true;

                if (!(obj is WoWUnit))
                    return true;

                // Summon stalker something
                if (obj.Entry == 53488)
                    return true;

                var unit = obj.ToUnit();
                if (   unit == null || !IsEnemy(unit))
                    return true;

                return false;
            });
        }

        private static void SoloWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            foreach (var p in units)
            {
                WoWUnit u = p.Object.ToUnit();
                p.Score = 100 - u.HealthPercent;
            }
        }

        private static int PlayersAttacking(WoWUnit unit)
        {
            return Me.PartyMembers.Count(p => p.CurrentTarget != null && p.CurrentTarget == unit) * 100;
        }

        public static bool IsEnemy(WoWUnit u)
        {
            return u != null
                && u.CanSelect
                && u.Attackable
                && u.IsAlive
                && (IsEnemyNPC(u) || IsEnemyPlayer(u));
        }

        private static bool IsEnemyNPC(WoWUnit u)
        {
            return !u.IsPlayer
                && (u.IsHostile || (u.IsNeutral && u.Combat && (u.IsTargetingMyPartyMember || u.IsTargetingMyRaidMember || u.IsTargetingMeOrPet )));
        }

        private static bool IsEnemyPlayer(WoWUnit u)
        {
            return u.IsPlayer 
                && (u.ToPlayer().IsHorde != StyxWoW.Me.IsHorde || (Battlegrounds.IsInsideBattleground && !u.ToPlayer().IsInMyPartyOrRaid ));
        }

        private static void Dump(string tag, List<Targeting.TargetPriority> units)
        {
            Dlog("=== TARGET {0} ===", tag);
            foreach (Targeting.TargetPriority pri in units)
            {
                var u = pri.Object as WoWUnit;
                // Dlog("   {0} {1}", pri.Score, u == null ? "-null-" : Safe_UnitName(u));
                Dlog("   {0}{1:F2} {2:F2} {3}", u.Guid == StyxWoW.Me.CurrentTargetGuid ? "*" : " ", pri.Score, u.HealthPercent, u.Name );
            }
        }
    }

}
