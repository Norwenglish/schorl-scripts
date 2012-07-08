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
using System.Drawing;
using System.Threading;
using System.Diagnostics;

using Levelbot.Actions.Combat;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.POI;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using CommonBehaviors.Actions;
using Action = TreeSharp.Action;
using Sequence = TreeSharp.Sequence;
using Styx.WoWInternals;

using Bobby53;

namespace Styx.Bot.CustomBots
{
    public partial class LazyRaider : BotBase
    {
        private static void IncludeTargetsFilter( List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
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

        private static void GroupIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            WoWPartyMember.GroupRole myRole = GetGroupRoleAssigned(Me);

            if (myRole != WoWPartyMember.GroupRole.Tank)
            {
                foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
                {
                    if (unit.HealthPercent == 100 || !unit.Combat)
                        continue;

                    outgoingUnits.Add(unit);
                }
            }
            else
            {
                foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
                {
                    if (!Me.Combat && (unit.DistanceSqr > 30 * 30 || !unit.InLineOfSpellSight ))
                        continue;

                    outgoingUnits.Add(unit);
                }
            }
        }

        private static void GroupRemoveTargetsFilter(List<WoWObject> units)
        {
            // no, I'm not the tank
            if (GetGroupRoleAssigned(Me) != WoWPartyMember.GroupRole.Tank)
            {
                WoWUnit tank = null;
                var tankInfo = Me.PartyMemberInfos.FirstOrDefault(p => p.HasRole(WoWPartyMember.GroupRole.Tank)) ??
                           Me.PartyMemberInfos.OrderBy(p => p.HealthMax).FirstOrDefault();

                if (tankInfo != null)
                    tank = tankInfo.ToPlayer();

                if (tank == null)
                {
                    // no tank
                    units.RemoveAll(obj =>
                        {
                            var u = obj as WoWUnit;
                            return 
                                   u == null || !obj.IsValid 
                                || !IsValidEnemyTarget( u)
                                || !u.Aggro
                                || !u.InLineOfSpellSight
                                || u.ControllingPlayer != null;
                        });
                }
                else
                {
                    // no tank
                    units.RemoveAll(obj =>
                        {
                            var u = obj as WoWUnit;
                            return
                                u == null || !u.IsValid
                                || !IsValidEnemyTarget(u)
                                || !u.InLineOfSpellSight
                                || u.ControllingPlayer != null;
                        });
                }

                return;
            }

            // yep, I'm the tank
            units.RemoveAll(obj =>
            {
                if (obj == null || !obj.IsValid )
                    return true;

                if (!(obj is WoWUnit))
                    return true;

                // Summon stalker something
                if (obj.Entry == 53488)
                    return true;

                var unit = obj as WoWUnit;
                if ( !unit.CanSelect || !unit.IsAlive || unit.ControllingPlayer != null )
                    return true;

                if ( IsEnemyPlayer(unit))
                    return false;
                    
                if ( unit.IsFriendly 
                    || unit.IsNonCombatPet 
                    || !unit.Attackable 
                    || unit.IsCritter 
                    )
                    return true;

                return false;
            });
        }

        private static void GroupWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            bool ImaTank = GetGroupRoleAssigned(Me) == WoWPartyMember.GroupRole.Tank;
            foreach (var p in units)
            {
                WoWUnit u = p.Object.ToUnit();

                // bad boi.
                p.Score = 100;

                p.Score -= u.Distance;
                if (ImaTank)
                {
                    if (u.IsTargetingMyPartyMember)
                        p.Score += 100;
                }
                else
                {
                    if (Me.Combat)
                    {
                        p.Score += PlayersAttacking(u);
                    }
                    else
                    {
                        p.Score -= u.HealthPercent;
                    }
                }
            }
        }

        private static void PvpIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
            {
                if (unit.DistanceSqr > 40 * 40 || !unit.InLineOfSpellSight)
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
                if (  !IsValidEnemyTarget(unit) || unit.ControllingPlayer != null )
                    return true;

                return false;
            });
        }

        private static void PvpWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            foreach (var p in units)
            {
                WoWUnit u = p.Object.ToUnit();
                p.Score = 100;
                p.Score -= u.HealthPercent;
            }
        }

        private static void SoloIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (var unit in incomingUnits.Select(obj => obj.ToUnit()))
            {
                if (GetGroupRoleAssigned( Me ) != WoWPartyMember.GroupRole.Tank)
                {
                    // Im not a tank
                    if (unit.HealthPercent == 100 || !unit.Combat)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!Me.Combat)
                    {
                        if (unit.DistanceSqr > 30 * 30 || !unit.InLineOfSight)
                        {
                            continue;
                        }
                    }
                }
                outgoingUnits.Add(unit);
            }
        }

        private static void SoloRemoveTargetsFilter(List<WoWObject> units)
        {
            // no, I'm not the tank
            if (GetGroupRoleAssigned(Me) != WoWPartyMember.GroupRole.Tank)
            {
                WoWUnit tank = null;
                var tankInfo = Me.PartyMemberInfos.FirstOrDefault(p => p.HasRole(WoWPartyMember.GroupRole.Tank)) ??
                           Me.PartyMemberInfos.OrderBy(p => p.HealthMax).FirstOrDefault();

                if (tankInfo != null)
                    tank = tankInfo.ToPlayer();

                if (tank == null)
                {
                    units.RemoveAll( obj =>
                           obj.ToUnit() == null
                        || !obj.ToUnit().IsAlive 
                        || !obj.ToUnit().Aggro 
                        || !obj.ToUnit().Attackable 
                        || !obj.ToUnit().InLineOfSpellSight
                        || !StyxWoW.Me.IsSafelyFacing(obj)
                        || obj.ToUnit().IsNonCombatPet
                        || obj.ToUnit().IsCritter 
                        || obj.ToUnit().ControllingPlayer != null 
                        );
                }
            }

            // yep, I'm the tank
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
                if (   unit == null 
                    || unit.Dead 
                    || unit.IsFriendly 
                    || unit.IsNonCombatPet 
                    || !unit.Attackable 
                    || unit.IsCritter 
                    || unit.ControllingPlayer != null
                    )
                    return true;

                return false;
            });
        }

        private static void SoloWeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            foreach (var p in units)
            {
                WoWUnit u = p.Object.ToUnit();

                // bad boi.
                p.Score = 100;

                p.Score -= u.Distance;
                if (GetGroupRoleAssigned(Me) == WoWPartyMember.GroupRole.Tank)
                {
                    if (u.IsTargetingMyPartyMember)
                        p.Score += 100;
                }
                else
                {
                    if (Me.Combat)
                    {
                        p.Score += PlayersAttacking(u);
                    }
                    else
                    {
                        p.Score -= u.HealthPercent;
                    }
                }
            }
        }

        private static int PlayersAttacking(WoWUnit unit)
        {
            return Me.PartyMembers.Count(p => p.CurrentTarget != null && p.CurrentTarget == unit) * 100;
        }

        public static bool IsValidEnemyTarget(WoWUnit u)
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
                && (u.ToPlayer().IsHorde != StyxWoW.Me.IsHorde || (Battlegrounds.IsInsideBattleground && u.ToPlayer().BattlefieldArenaFaction != StyxWoW.Me.BattlefieldArenaFaction ));
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
