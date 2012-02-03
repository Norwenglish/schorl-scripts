/*
 * NOTE:    DO NOT POST ANY MODIFIED VERSIONS OF THIS TO THE FORUMS.
 * 
 *          DO NOT UTILIZE ANY PORTION OF THIS COMBAT CLASS WITHOUT
 *          THE PRIOR PERMISSION OF AUTHOR.  PERMITTED USE MUST BE
 *          ACCOMPANIED BY CREDIT/ACKNOWLEDGEMENT TO ORIGINAL AUTHOR.
 * 
 * ShamWOW Shaman CC 
 * 
 * Author:  Bobby53
 * 
 * See the ShamWOW.chm file for Help
 *
 */
#pragma warning disable 642

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bobby53
{
    partial class Shaman
    {
        static WoWUnit unitToSaveHeal = null;       // key unit in trouble that needs immediate heal (ignore normal heal priority)

        private bool HealRaid()
        {

            bool wasSpellCast = false;
            int healPct = hsm.NeedHeal;

            // following line is here solely to make sure we wait on the GCD since
            // ... prior checks may pause only if we were casting.  This prevents 
            // ... misleading results from WoWSpell.Cooldown later in .CastHeal
            WaitForCurrentCastOrGCD();

            WoWUnit p = ChooseHealTarget(healPct, SpellRange.NoCheck);
            if (p != null)
                wasSpellCast = hsm.CastHeal(p);

            // check and earth shield tank if needed
            if (IsRAFandTANK() && GroupTank.IsAlive && typeShaman == ShamanType.Resto)
            {
                bool inCombat = _me.Combat || GroupTank.Combat;
                if (GetAuraStackCount(GroupTank, "Earth Shield") < (inCombat ? 1 : 3))
                {
                    MoveToHealTarget(GroupTank, 35);
                    if (_me.IsUnitInRange(GroupTank, 39))
                        wasSpellCast = wasSpellCast || Safe_CastSpell(GroupTank, "Earth Shield");
                }
            }

            return wasSpellCast;
        }

        private bool DispelRaid()
        {
            bool WasHealCast = false;

            bool knowCleanseSpirit = SpellManager.HasSpell("Cleanse Spirit");
            bool canCleanCurse = knowCleanseSpirit;
            bool canCleanMagic = knowCleanseSpirit && _hasTalentImprovedCleanseSpirit;
            bool knowStoneform = SpellManager.HasSpell("Stoneform");

            WoWUnit player = (from p in HealMembers
                                where
                                    p.Distance <= 38
                                    // && !Blacklist.Contains(p)
                                    && !(from dbf in p.Debuffs where _hashCleanseBlacklist.Contains(dbf.Value.SpellId) select dbf.Value).Any()
                                    && (from dbf in p.Debuffs
                                        where
                                            (dbf.Value.Spell.DispelType == WoWDispelType.Curse && canCleanCurse)
                                            || (dbf.Value.Spell.DispelType == WoWDispelType.Magic && canCleanMagic)
                                            || (p.IsMe &&
                                                    (
                                                        (dbf.Value.Spell.DispelType == WoWDispelType.Magic && _hasGlyphOfShamanisticRage)
                                                     || (dbf.Value.Spell.DispelType == WoWDispelType.Poison && knowStoneform)
                                                     || (dbf.Value.Spell.DispelType == WoWDispelType.Disease && knowStoneform)
                                                    )
                                               )
                                        select dbf.Value
                                        ).Any()
                                select p
                            ).FirstOrDefault();

            if (player != null)
            {
                WasHealCast = CleanseIfNeeded(player);
            }

            return WasHealCast;
        }

        private bool HealMyself()
        {
            return HealMyself(GetSelfHealThreshhold());
        }

        private bool HealMyself(int threshhold)
        {
            if (_me.HealthPercent >= threshhold)
                return false;

            if (IsHealer())
            {
                Log("^Heal Target: {0}[{1}] at {2:F1}%", Safe_UnitName(_me), _me.Level, _me.HealthPercent);
            }

            // non-combat heal... do what we can to try and top-off
            if (_me.Combat)
            {
                if ( _me.HealthPercent < cfg.EmergencyHealthPercent || IsFightStressful())
                    Warstomp();         

                if (_me.ManaPercent <= cfg.EmergencyManaPercent && _me.HealthPercent > cfg.NeedHealHealthPercent)
                    UseManaPotionIfAvailable();

                if (_hasGlyphOfStoneClaw && _me.HealthPercent < cfg.NeedHealHealthPercent)
                {
                    if (!TotemExist(TotemId.EARTH_ELEMENTAL_TOTEM) && !TotemExist(TotemId.STONECLAW_TOTEM))
                    {
                        if (TotemCast(TotemId.STONECLAW_TOTEM))
                        {
                            Log("^Shaman Bubble: casting Stoneclaw Totem w/ Glyph");
                        }
                    }
                }

                if (_me.HealthPercent < cfg.TrinkAtHealth )
                {
                    if ( UseItem( CheckForItem(5512)))
                        ;
                    else
                        UseHealthPotionIfAvailable();
                }

                if (_me.HealthPercent >= threshhold)
                    return true;
            }

            double checkHealth = _me.HealthPercent;
            double checkMana = _me.ManaPercent;
            bool WasHealCast = false;

            if (!_me.IsAlive)
            {
                Dlog("HealPlayer: I am dead, too late for heals now...");
                return WasHealCast;
            }

            Safe_StopMoving( "for self heal");

            // wait for any current spell cast... this is overly cautious...
            // if in danger of dying, fastest heal possible
            if (_me.Combat && _me.HealthPercent < cfg.EmergencyHealthPercent)
            {
/*
                if (SpellManager.HasSpell("Tidal Force"))
                    Safe_CastSpell("Tidal Force", SpellRange.NoCheck, SpellWait.Complete);
*/
                if (SpellManager.HasSpell("Nature's Swiftness"))          
                {
                    if (!Safe_CastSpell(_me, "Nature's Swiftness"))
                        Dlog("Nature's Swiftness on cooldown, cannot Oh S@#$ heal");
                    else
                    {
                        if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave"))
                            WasHealCast = Safe_CastSpell(_me, "Greater Healing Wave");
                        if (!WasHealCast && SpellManager.HasSpell("Healing Surge"))
                            WasHealCast = Safe_CastSpell(_me, "Healing Surge");
                        if (!WasHealCast && SpellManager.HasSpell("Healing Wave"))
                            WasHealCast = Safe_CastSpell(_me, "Healing Wave");

                        if (WasHealCast)
                            Slog("Big Heals - clicked the Oh S@#$ button!");
                        else
                            Slog("Attempted Oh S@#$ heal but couldn't cast Healing Wave");
                    }
                }
            }

            if (!_me.Combat)
            {
                if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave"))
                    WasHealCast = Safe_CastSpell(_me, "Greater Healing Wave");
                if (!WasHealCast && SpellManager.HasSpell("Healing Wave") && _hasTalentMaelstromWeapon && IsAuraPresent(_me, "Maelstrom Weapon"))
                    WasHealCast = Safe_CastSpell(_me, "Healing Wave");
                if (!WasHealCast && SpellManager.HasSpell("Healing Surge"))
                    WasHealCast = Safe_CastSpell(_me, "Healing Surge");
            }
            else
            {
                if (!WasHealCast && SpellManager.HasSpell("Riptide"))
                    WasHealCast = Safe_CastSpell(_me, "Riptide");
                if (_hasTalentMaelstromWeapon && 3 <= GetAuraStackCount(_me, "Maelstrom Weapon"))
                {
                    if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave"))
                    {
                        Dlog("HealMyself:  GHW selected because of Maelstrom Weapon");
                        WasHealCast = Safe_CastSpell(_me, "Greater Healing Wave");
                    }
                    if (!WasHealCast && SpellManager.HasSpell("Healing Wave") )
                    {
                        Dlog("HealMyself:  HW selected because of Maelstrom Weapon");
                        WasHealCast = Safe_CastSpell(_me, "Healing Wave");
                    }
                }
                if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave") && IsAuraPresent(_me, "Tidal Waves"))
                {
                    Dlog("HealMyself:  GHW selected because of Tidal Waves");
                    WasHealCast = Safe_CastSpell(_me, "Greater Healing Wave");
                }
                if (!WasHealCast && SpellManager.HasSpell("Healing Surge") && (_me.CurrentHealth < 40 || !SpellManager.HasSpell("Greater Healing Wave")))
                {
                    Dlog("HealMyself:  HS selected because of low-health or GHW not trained");
                    WasHealCast = Safe_CastSpell(_me, "Healing Surge");
                }
                if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave"))
                {
                    Dlog("HealMyself:  GHW selected by default");
                    WasHealCast = Safe_CastSpell(_me, "Greater Healing Wave");
                }
            }

            if (!WasHealCast)
            {
                if (!SpellManager.HasSpell("Healing Wave"))
                    Slog("No healing spells trained, you need potions or first-aid to heal", checkHealth, checkMana);
                else
                {
                    WasHealCast = Safe_CastSpell(_me, "Healing Wave");

                    // at this point no healing worked, so issue a heal failed message
                    if (!WasHealCast)
                        Slog("Casting of heal prevented: Health={0:F0}% Mana={1:F0}%", checkHealth, checkMana);
                }
            }

            if (WasHealCast)
            {
                Dlog("^Heal begun @ health:{0:F2}% mana:{1:F2}%", checkHealth, checkMana);
                WaitForCurrentHeal(_me, threshhold);
            }

            return WasHealCast;
        }

        private static bool WillChainHealHop(WoWUnit healTarget)
        {
            Stopwatch timer = new Stopwatch();
            double threshhold = hsm.NeedHeal;
            timer.Start();

            if (healTarget == null)
                return false;

#if CHAIN_HEAL_LOOKS_FOR_ONE
            WoWUnit player = null;
            try
            {
                player = ObjectManager.GetObjectsOfType<WoWUnit>(false).Find(
                            p => p != null
                                && p.IsPlayer && !p.IsPet
                                && p != healTarget
                                && p.IsAlive
                                && p.HealthPercent < threshhold
                                && healTarget.Location.Distance(p.Location) <= 12
                            );
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in WillChainHealHop()");
                Logging.WriteException(e);
            }

            if ( player == null)
                Dlog("WillChainHealHop(): took {0} ms and found no players in range", timer.ElapsedMilliseconds);
            else
                Dlog("WillChainHealHop(): took {0} ms and found {0} at {1:F1}% and {2:F1} yds away", timer.ElapsedMilliseconds, Safe_UnitName(player), player.HealthPercent, player.Distance );

            return player != null;
#else
            var t =(from o in ObjectManager.ObjectList
                    where o is WoWUnit && healTarget.Location.Distance(o.Location) < 12
                        let p = o.ToUnit()
                    where p != null
                        && Safe_IsFriendly(p)
                        && (p.IsPlayer || (p.IsPet && hsm.HealPets))
                        && p != healTarget
                        && p.IsAlive
                        && p.HealthPercent < threshhold
                    let c =(from oo in ObjectManager.ObjectList
                            where oo is WoWUnit && p.Location.Distance(oo.Location) < 12
                                let pp = oo.ToUnit()
                            where pp != null
                                && Safe_IsFriendly(p)
                                && (pp.IsPlayer || (pp.IsPet && hsm.HealPets ))
                                && pp.CurrentHealth > 1
                                && pp.HealthPercent < threshhold
                            select pp).Count()
                    orderby c descending, p.Distance ascending 
                    select new {Unit = p, Count = c}).FirstOrDefault();

            if (t == null)
            {
                Dlog("WillChainHealHop: found no hops in range (took={0} ms)", timer.ElapsedMilliseconds);
                return false;
            }

            if (t.Count < cfg.GroupHeal.ChainHealTargets )
            {
                Dlog("WillChainHealHop:  false, only found {0} near target {1} providing {2} hop targets (took={3} ms)", Safe_UnitName(t.Unit), Safe_UnitName(healTarget), t.Count, timer.ElapsedMilliseconds);
                return false;
            }

            Dlog("WillChainHealHop:  true, found {0} near target {1} providing {2} hop targets (took={3} ms)", Safe_UnitName(t.Unit), Safe_UnitName(healTarget), t.Count, timer.ElapsedMilliseconds);
            Slog("^Chain Heal: found {0} hop targets", t.Count);
            return true;
#endif
        }


        private static bool WillHealingRainCover(WoWUnit healTarget, int minCount)
        {
            List<WoWUnit> targets = null;
            Stopwatch timer = new Stopwatch();
            double threshhold = hsm.NeedHeal;
            timer.Start();

            if (healTarget == null)
                return false;

            try
            {
                targets = (from p in HealMembersAndPets
                           where p.IsPlayer
                                && p.IsAlive
                                && p.HealthPercent < threshhold
                                && healTarget.Location.Distance(p.Location) <= 10
                           select p).ToList();
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in WillHealingRainCover()");
                Logging.WriteException(e);
            }

            Slog("^Healing Rain: found {0} targets", targets.Count );
            Dlog("WillHealingRainCover({0}): took {1} ms", minCount, timer.ElapsedMilliseconds);
            return targets != null && targets.Count >= minCount;
        }


        private static int HowManyMobsNearby(WoWUnit target, int range )
        {
            int rangeSqr = range * range;
            int cnt = 
                    (from o in ObjectManager.ObjectList
                    where o is WoWUnit && target.Location.DistanceSqr(o.Location) <= rangeSqr
                    let u = o.ToUnit()
                    where  u.Combat && u.IsAlive && u.Attackable
                        && ((u.IsPlayer && Safe_IsHostile(u)) || (!u.IsFriendly))
                    select u
                    ).Count();
            return cnt;
        }


        public static List<WoWUnit> _healTargets;
        private static readonly Countdown _refreshTimer = new Countdown();
        private static readonly Countdown _dumpTimer = new Countdown(10000);

        /// <summary>
        /// Chooses a nearby target for healing.  Selection is based upon
        /// which nearby friendly needs heals.  Includes _me in list so
        /// will handle self-healing to you low
        /// </summary>
        /// <returns>WoWUnit reference of nearby player needing heals</returns>
        public static WoWUnit ChooseHealTarget(double healLessThan, SpellRange rchk)
        {
#if COMMENT
            // use timer to ensure we aren't constantly rebuilding the list
            // .. player health and distance are very dynamic, but the number of players
            // .. in the vicinity won't change drastically in that time
            //--- NOTE:  timer is initially zero so first call builds list
            if (_healTargets == null || _refreshTimer.Done )
            {
                CreateHealTargetList();
            }

            _healTargets.Sort(CompareHealPriority);

#if LIST_HEAL_TARGETS
			for (int b = 0; b < _healTargets.Count; b++)
			{
				Slog("  {0:F0}% {1}[{2}] dist: {3:F1} in-los: {4}", _healTargets[b].HealthPercent, _healTargets[b].Name, _healTargets[b].Level, _healTargets[b].Distance, _healTargets[b].InLineOfSight );
			}
			Slog("  Total of " + _healTargets.Count);
#endif

            // restrict to 39 if movement has been disabled (so we only heal those within range of users movement)
            double searchFilter = IsMovementDisabled() ? 39 : cfg.GroupHeal.SearchRange;

            WoWUnit lowPlayer = null;

            Dlog("ChooseHealTarget: checking {0} players within {1} yards", _healTargets.Count(), searchFilter );

            // Me and Tank okay, so choose lowest health player
            for (int a = 0; a < _healTargets.Count; a++)
            {
                try
                {
                    if (!Safe_IsValid(_healTargets[a]))
                    {
                        Dlog("ChooseHealTarget: entry[{0}] failed Safe_IsValid()", a);
                        continue;
                    }
                    if (!ObjectManager.ObjectList.Contains(_healTargets[a]))
                    {
                        Dlog("ChooseHealTarget: entry[{0}] failed Is In ObjectList()", a);
                        continue;
                    }

                    // stop looking in sorted list if we reach healing threshhold
                    if (_healTargets[a].HealthPercent > healLessThan)
                    {
                        Dlog("ChooseHealTarget:  no player currently below {0}%", healLessThan);
                        break;
                    }

                    // if target is out of range, then skip this entry
                    if (_healTargets[a].Distance > searchFilter )
                        continue;

                    // since we don't rebuild the list each time, always need to retest for dead players
                    if (_healTargets[a].CurrentHealth <= 1) // _healTargets[a].Dead || _healTargets[a].IsGhost
                    {
                        Dlog("ChooseHealTarget:  entry[{0}] is dead", a);
                    }
                    else if (rchk == SpellRange.NoCheck || (_healTargets[a].Distance < 39 && _healTargets[a].InLineOfSightOCD))
                    {
                        Dlog("ChooseHealTarget: {0}[{1}] at {2:F0}% dist: {3:F1} in-los: {4}", Safe_UnitName(_healTargets[a]), _healTargets[a].Level, _healTargets[a].HealthPercent, _healTargets[a].Distance, _healTargets[a].InLineOfSightOCD);
                        lowPlayer = _healTargets[a];
                        break;
                    }
                }
                catch (ThreadAbortException) { throw; }
                catch
                {
                    // if exception dealing with this WoWUnit, then try next in array
                    Dlog("ChooseHealTarget:  exception occurred accessing entry[{0}]", a);
                }
            }

            minGroupHealth = (int) (lowPlayer == null ? 100 : lowPlayer.HealthPercent);

            // if Me or the Tank (value in unitToSaveHeal) is at risk 
            if (unitToSaveHeal != null && unitToSaveHeal.IsAlive)
            {
                if (rchk == SpellRange.NoCheck || (unitToSaveHeal.Distance < 38 && unitToSaveHeal.InLineOfSightOCD))
                {
                    Dlog("ChooseHealTarget: SAVING {0}[{1}] at {2:F0}% dist: {3:F1} in-los: {4}", Safe_UnitName(unitToSaveHeal), unitToSaveHeal.Level, unitToSaveHeal.HealthPercent, unitToSaveHeal.Distance, unitToSaveHeal.InLineOfSightOCD);
                    lowPlayer = unitToSaveHeal.ToPlayer();
                    unitToSaveHeal = null;
                }
            }

            return lowPlayer;
#else
            int searchRange = IsMovementDisabled() ? 39 : (int) cfg.GroupHeal.SearchRange;
            WoWUnit lowPlayer = null;

            // heal Me or Tank if at risk 
            try
            {
                if (unitToSaveHeal != null && unitToSaveHeal.CurrentHealth > 1)
                {
                    if (rchk == SpellRange.NoCheck || (unitToSaveHeal.Distance < 39 && unitToSaveHeal.InLineOfSightOCD))
                    {
                        Dlog("ChooseHealTarget: SAVING {0}[{1}] at {2:F0}% dist: {3:F1} in-los: {4}", Safe_UnitName(unitToSaveHeal), unitToSaveHeal.Level, unitToSaveHeal.HealthPercent, unitToSaveHeal.Distance, unitToSaveHeal.InLineOfSightOCD);
                        lowPlayer = unitToSaveHeal.ToPlayer();
                        unitToSaveHeal = null;
                    }
                }
            }
            catch
            {
                // tank or healer needs saving but reference is invalid at the moment
                unitToSaveHeal = null;
            }

            // if no low player found yet, find if there is one
            if (lowPlayer == null)
            {
                try
                {
                    lowPlayer = 
                        HealMembersAndPets
                            .Where(u => CheckValidAndNearby(u, searchRange) && u.HealthPercent <= healLessThan)
                            .OrderBy(u => u.HealthPercent)
                            .FirstOrDefault();
                }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("HB EXCEPTION in ChooseHealTarget()");
                    Logging.WriteException(e);
                    lowPlayer = null;
                }
            }

            // now account for Me not being in HealMembers
            if (lowPlayer == null)
                lowPlayer = _me.HealthPercent <= healLessThan ? _me : null;
            else if (_me.HealthPercent <= lowPlayer.HealthPercent)
                lowPlayer = _me;

            minGroupHealth = (int)(lowPlayer == null ? 100 : lowPlayer.HealthPercent);
            return lowPlayer;
#endif
        }

        public static WoWUnit ChooseNextHealTarget(WoWUnit currHealTarget, double healLessThan)
        {
#if COMMENT
            for (int a = 0; a < _healTargets.Count; a++)
            {
                try
                {
                    if (!Safe_IsValid(_healTargets[a]))
                        continue;

                    if (_healTargets[a].HealthPercent > healLessThan)
                        break;

                    if (_healTargets[a].CurrentHealth <= 1)         // skip if Dead
                        ;
                    else if (_healTargets[a] == currHealTarget)     // skip if current healtarget
                        ;
                    else if (_me.IsUnitInRange(_healTargets[a], 37))   // choose one alread in range
                    {
                        // Slog("Heal Target: {0}[{1}] at {2:F0}% dist: {3:F1} in-los: {4}", _healTargets[a].Name, _healTargets[a].Level, _healTargets[a].HealthPercent, _healTargets[a].Distance, _healTargets[a].InLineOfSight);
                        return _healTargets[a];
                    }
                }
                catch (ThreadAbortException) { throw; }
                catch
                {
                    // if exception dealing with this WoWUnit, then try next in array
                    continue;
                }
            }

            return null;
#else
            WoWUnit nextLowest = 
                HealMembersAndPets
                    .Where(u => CheckValidAndNearby(u, 36) && u.HealthPercent <= healLessThan && u != currHealTarget)
                    .OrderBy(u => u.HealthPercent)
                    .FirstOrDefault();

            // now account for Me not being in HealMembers
            if (_me != currHealTarget)
            {
                if (nextLowest == null)
                    nextLowest = _me.HealthPercent <= healLessThan ? _me : null;
                else if (_me.HealthPercent <= nextLowest.HealthPercent)
                    nextLowest = _me;
            }

            return nextLowest;
#endif
        }

        private static bool CheckValidAndNearby(WoWUnit u, int dist)
        {
            try
            {
                return Safe_IsValid( u) && u.CurrentHealth > 1 && u.IsAlive && u.Distance <= dist;
            }
            catch
            {
                return false;
            }
        }

        // sort in ascending order by Health percent
        //  ..  null pointers or dead's should be at end of list
        private static int CompareHealPriority(WoWUnit x, WoWUnit y)
        {
            try
            {
                // handle nulls/deads so that they fall to end of list
                if (x == null || !x.IsAlive)
                    return (y == null || !y.IsAlive ? 0 : 1);
                else if (y == null || !y.IsAlive)
                    return -1;

                // sort 
                double healthDiff = x.HealthPercent - y.HealthPercent;

                if (healthDiff < 0.0)
                    return -1;

                if (healthDiff > 0.0)
                    return 1;
            }
            catch (ThreadAbortException) { throw; }
            catch
            {
                Dlog("CompareHealPriority: EXCEPTION: a heal target left group or released -- ignoring");
            }

            return 0;

            /*
                * -- Eventually determine a priority based upon general health, 
                * -- targets survivability, and targets savability (my word).
                * -- this would factor in can they be saved, are they a plater 
                * -- wearer, do they have a self-heal and mana, etc.
                * 
            const double _priorityTiers = 5;

            int xHealthPriority = (int)Math.Ceiling(x.HealthPercent / _priorityTiers);
            int yHealthPriority = (int)Math.Ceiling(y.HealthPercent / _priorityTiers);

            return xHealthPriority - yHealthPriority;
            */
        }
#if COMMENT
        public static void CreateHealTargetList()
        {
            List<WoWUnit> plist = HealMembers;
            if (!plist.Contains(ObjectManager.Me))
            {
                plist.Add(ObjectManager.Me);
                Dlog("CreateHealTargetList:  added Me to list");
            }

            double searchDist = cfg.GroupHeal.SearchRange;
            _healTargets = plist.FindAll(
                unit => unit.CurrentHealth > 1
                // && unit.IsAlive 
                // && unit.Guid != ObjectManager.Me.Guid
                );

            // _refreshTimer.Remaining = IsPVP() ? 5000 : 20000;
            _refreshTimer.Remaining = 5000;
        }
#endif
        private static void DumpHealTargetList()
        {
            int a = 1;
            foreach (WoWUnit pd in HealMembersAndPets)
            {
                Dlog("     HealTarget[{0}]={1}[{2}] Health={3}% @ {4:F1} yds",
                    a++,
                    Safe_UnitName(pd),
                    pd.Level,
                    pd.HealthPercent,
                    pd.Distance
                    );
            }
        }

        class HealSpellManager
        {
            public int NeedHeal = 0;
            public bool BuffTidalWaves = false;
            public bool HealPets = false;

            WoWUnit priorHealTarget = null;
            int priorHealTick = 0;

            private List<HealSpell> healSpells;

            public HealSpellManager()
            {
                healSpells = new List<HealSpell>();

                int Heal_HW = 0;
                int Heal_GHW = 0;
                int Heal_RT = 0;
                int Heal_UE = 0;
                int Heal_CH = 0;
                int Heal_HR = 0;
                int Heal_HS = 0;
                int Heal_OS = 0;
                int Heal_GN = 0;

                if (IsRAF())
                {
                    Heal_HW = cfg.RAF_Heal.HealingWave;
                    Heal_GHW = cfg.RAF_Heal.GreaterHealingWave;
                    Heal_RT = cfg.RAF_Heal.Riptide;
                    Heal_UE = cfg.RAF_Heal.UnleashElements;
                    Heal_CH = cfg.RAF_Heal.ChainHeal;
                    Heal_HR = cfg.RAF_Heal.HealingRain;
                    Heal_HS = cfg.RAF_Heal.HealingSurge;
                    Heal_OS = cfg.RAF_Heal.OhShoot;
                    Heal_GN = cfg.RAF_Heal.GiftoftheNaaru;
                    BuffTidalWaves = cfg.RAF_Heal.TidalWaves;
                    HealPets = cfg.RAF_Heal.Pets;
                }
                else if (IsPVP())
                {
                    Heal_HW = cfg.PVP_Heal.HealingWave;
                    Heal_GHW = cfg.PVP_Heal.GreaterHealingWave;
                    Heal_RT = cfg.PVP_Heal.Riptide;
                    Heal_UE = cfg.PVP_Heal.UnleashElements;
                    Heal_CH = cfg.PVP_Heal.ChainHeal;
                    Heal_HR = cfg.PVP_Heal.HealingRain;
                    Heal_HS = cfg.PVP_Heal.HealingSurge;
                    Heal_OS = cfg.PVP_Heal.OhShoot;
                    Heal_GN = cfg.PVP_Heal.GiftoftheNaaru;
                    BuffTidalWaves = cfg.PVP_Heal.TidalWaves;
                    HealPets = cfg.PVP_Heal.Pets;
                }

                healSpells.Add(new HealSpell(Heal_HW, "Healing Wave", "Healing Wave", HealSpellManager.HealingWave));
                healSpells.Add(new HealSpell(Heal_GHW, "Greater Healing Wave", "Greater Healing Wave", HealSpellManager.GreaterHealingWave));
                healSpells.Add(new HealSpell(Heal_RT, "Riptide", "Riptide", HealSpellManager.Riptide));
                healSpells.Add(new HealSpell(Heal_UE, "Unleash Elements", "Unleash Elements", HealSpellManager.UnleashElements));
                healSpells.Add(new HealSpell(Heal_CH, "Chain Heal", "Chain Heal", HealSpellManager.ChainHeal));
                healSpells.Add(new HealSpell(Heal_HR, "Healing Rain", "Healing Rain", HealSpellManager.HealingRain));
                healSpells.Add(new HealSpell(Heal_HS, "Healing Surge", "Healing Surge", HealSpellManager.HealingSurge));
                healSpells.Add(new HealSpell(Heal_OS, "Oh Shoot Heal", "Nature's Swiftness", HealSpellManager.OhShoot));
                healSpells.Add(new HealSpell(Heal_GN, "Gift of the Naaru", "Gift of the Naaru", HealSpellManager.GiftoftheNaaru));

                healSpells.Sort(HealSpellManager.Compare);
                Dlog("### HealSpellManager Dump BEFORE PRUNE");
                Dump();

                // remove those with 0 health values
                while (healSpells.Any() && healSpells[0].Health == 0)
                    healSpells.RemoveAt(0);

                Dlog("### HealSpellManager Dump AFTER PRUNE");
                Dump();

                // find maximum to use as NeedHeal value
                HealSpell hs = healSpells.LastOrDefault();
                if (hs != null)
                {
                    NeedHeal = hs.Health;
                    Dlog("HealSpellManager:  NeedHeal set to {0}", NeedHeal);
                }
                else
                {
                    if ( !InGroup() )
                        Elog("HealSpellManager:  NOT CURRENTLY IN A GROUP, group healing will activate when you join");
                    NeedHeal = 0;
                }
            }

            public bool CastHeal(WoWUnit unit)
            {
                if (!unit.IsAlive)
                {
                    Dlog("CastHeal: Heal target is dead");
                    return false;
                }

                if (!unit.IsMe && !_me.IsUnitInRange(unit, 39))
                {
                    Dlog("CastHeal:  moving to heal target who is {0:F1} yds away", unit.Distance);
                    MoveToHealTarget(unit, 35);
                    if (!_me.IsUnitInRange(unit, 39))
                    {
                        Dlog("CastHeal:  not within healing range, Heal Target {0} is {1:F1} yds away", Safe_UnitName(unit), unit.Distance);
                        return false;
                    }

                    Dlog("CastHeal:  stopping now that Heal Target {0} is {1:F1} yds away", Safe_UnitName(unit), unit.Distance);
                }

                // note:  following seeks to make sure that Tidal Waves buff is up if option checked,
                // ... and if Tier 12 equipped will keep Riptide rolling on as many toons as possible
                if (SpellManager.HasSpell("Riptide") && ((BuffTidalWaves && !_me.ActiveAuras.ContainsKey("Tidal Waves")) || _tier12CountResto > 1))
                {
                    WoWUnit tank = GroupTank;
                    if (IsRAF() && tank != null && tank.IsAlive && tank.CurrentHealth > 1 && tank.HealthPercent < NeedHeal && !tank.Auras.ContainsKey("Riptide"))
                    {
                        Dlog("CastHeal:  attempting T12 Rolling Riptide on {0} - 1", Safe_UnitName(tank));
                        if (Riptide(tank))
                            return true;
                    }

                    if (!unit.Auras.ContainsKey("Riptide"))
                    {
                        Dlog("CastHeal:  attempting T12 Rolling Riptide on {0} - 2", Safe_UnitName(unit));
                        if (Riptide(unit))
                        {
                            return true;
                        }
                    }

                    WoWUnit rtUnit = HealMembersAndPets.FirstOrDefault(
                        u => u != null
                            && u.IsValid
                            && !u.Auras.ContainsKey("Riptide")
                            && SpellManager.CanCast(SpellManager.Spells["Riptide"], u, true, false, cfg.AccountForLag));
                    if (rtUnit != null)
                    {
                        Dlog("CastHeal:  attempting T12 Rolling Riptide on {0} - 3", Safe_UnitName(rtUnit));
                        if (Riptide(rtUnit))
                        {
                            return true;
                        }
                    }
                }

                // wait here to try and cut down on Safe_SpellCast() having to wait which provides sqewed log output
                // .. indicating we cast a higher power heal (HealingSurge) while unit had higher health %

                double currHealth = unit.HealthPercent;
                HealSpell healSpell = healSpells.FirstOrDefault(s => currHealth < s.Health);
                HealSpell baseSpell = healSpell;

                // FIND FIRST CHOICE SPELL
                // nav up: find the correct spell in the list or next higher health % spell
                while (!IsGameUnstable() && _me.IsAlive && healSpell != null)
                {
                    Dlog("CastHeal:  {0} {1}% for unit with health {2:F1}%", healSpell.DisplayName, healSpell.Health, currHealth);

                    WoWSpell wowSpell = SpellManager.Spells[healSpell.TestSpell];
                    if ( SpellHelper.OnCooldown(wowSpell)) // (wowSpell.Cooldown && wowSpell.CooldownTimeLeft.TotalMilliseconds > SpellHelper.LagAmount )
                    {
                        Dlog("CastHeal: spell '{0}' has {1} cooldown left with a lag of {2}", wowSpell.Name, wowSpell.CooldownTimeLeft.TotalMilliseconds, SpellHelper.LagAmount);
                    }
                    else
                    {
                        Safe_StopMoving( "to heal");
                        if (healSpell.Cast(unit))
                        {
                            if (!_me.Combat && !unit.Combat)
                                WaitForCurrentHeal(unit, NeedHeal);
                            return true;
                        }
                    }

                    // on CD, so move to next higher health % in list
                    healSpell = healSpells.FirstOrDefault(s => healSpell.Health < s.Health);
                }

#if ALLOW_NEXT_LOWER_HEALTH_SPELL
                // FIND SECOND CHOICE SPELL
                // nav dn: find next lower health % spell
                if ( baseSpell != null )
                    spell = spells.LastOrDefault( s => baseSpell.Health > s.Health );

                while ( !IsGameUnstable() && _me.IsAlive && spell != null )
                {
                    Dlog("CastHeal:  2nd choice {0} {1}% for unit with health {2:F1}%", spell.DisplayName, spell.Health, currHealth);
                    if (spell.Cast(unit))
                    {
                        WaitForCurrentHeal(unit, NeedHeal );
                        return true;
                    }

                    // on CD, so find next lower health % in list
                    spell = spells.LastOrDefault( s => spell.Health > s.Health );
                }
#endif
                Dlog("CastHeal:  failed to find usable healing spell for unit with health {0:F1}%", currHealth);
                return false;
            }

            public void UpdatePriorHealInfo(WoWUnit unit)
            {
                priorHealTick = System.Environment.TickCount;
                priorHealTarget = unit;
            }

            public void Dump()
            {
                Logging.WriteDebug("");
                Logging.WriteDebug("   % Spell Description");
                Logging.WriteDebug(" --- --------------------------------");
                foreach (HealSpell spell in healSpells)
                {
                    Logging.WriteDebug(" {0,3} {1}", spell.Health, spell.DisplayName);
                }
                Logging.WriteDebug("");
            }

            private static int Compare(HealSpell a, HealSpell b)
            {
                return a.Health - b.Health;
            }

            private static bool HealingWave(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Healing Wave"))
                    return false;
                return Safe_CastSpell(healTarget, "Healing Wave");
            }
            private static bool GreaterHealingWave(WoWUnit healTarget)
            {
                if (UnleashElements(healTarget))
                    WaitForCurrentCastOrGCD();

                if (!SpellManager.HasSpell("Greater Healing Wave"))
                    return false;
                return Safe_CastSpell(healTarget, "Greater Healing Wave");
            }
            private static bool ChainHeal(WoWUnit healTarget)
            {
                bool castRip = false;

                if (!SpellManager.HasSpell("Chain Heal"))
                    return false;
                if (!WillChainHealHop(healTarget))
                    return false;
                if (SpellManager.HasSpell("Riptide") && !healTarget.Auras.ContainsKey("Riptide") && SpellManager.CanCast("Riptide"))
                {
                    Dlog("ChainHeal:  prepping target with Riptide");
                    castRip = Riptide(healTarget);
                }
                return castRip || Safe_CastSpell(healTarget, "Chain Heal");
            }

            private static bool HealingRain(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Healing Rain"))
                    return false;

                if (_me.ManaPercent < cfg.EmergencyManaPercent && !_me.Auras.ContainsKey("Clearcasting"))
                    return false;

                if (!WillHealingRainCover(healTarget, cfg.GroupHeal.HealingRainTargets))
                    return false;

                if (_hasTalentFocusedInsight)
                {
                    if (_me.GotTarget && _me.CurrentTarget.IsAlive && _me.CurrentTarget.Combat && Safe_IsHostile(_me.CurrentTarget))
                    {
                        if (_me.CurrentTarget.InLineOfSightOCD && FaceToUnit(_me.CurrentTarget))
                        {
                            Slog("^Focused Insight:  buff Healing Rain");
                            Safe_CastSpell(_me.CurrentTarget, "Earth Shock");
                            WaitForCurrentCastOrGCD();
                        }
                    }
                }

                if (Safe_CastSpell( healTarget, "Healing Rain"))
                {
                    WaitForCurrentCastOrGCD();
                    if (!LegacySpellManager.ClickRemoteLocation(healTarget.Location))
                    {
                        Dlog("^Ranged AoE Click FAILED:  cancelling Healing Rain");
                        SpellManager.StopCasting();
                    }
                    else
                    {
                        Dlog("^Ranged AoE Click successful:  LET IT RAIN!!!");
                        StyxWoW.SleepForLagDuration();
                        return true;
                    }
                }

                return false;
            }

            private static bool Riptide(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Riptide"))
                    return false;
                return Safe_CastSpell(healTarget, "Riptide");
            }
            private static bool UnleashElements(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Unleash Elements"))
                    return false;
                if (!_me.HasAura("Earthliving Weapon (Passive)"))
                    return false;
                return Safe_CastSpell(healTarget, "Unleash Elements");
            }
            private static bool HealingSurge(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Healing Surge"))
                    return false;
                return Safe_CastSpell(healTarget, "Healing Surge");
            }
            private static bool OhShoot(WoWUnit healTarget)
            {
                bool WasHealCast = false;

                if (!healTarget.Combat)
                {
                    Dlog("OhShootHeal:  target {0} is not in combat, saving cd", Safe_UnitName(healTarget));
                    return false;
                }
/*
                if (SpellManager.HasSpell("Tidal Force"))
                    Safe_CastSpell("Tidal Force");
*/
                if (!SpellManager.HasSpell("Nature's Swiftness"))
                    return false;

                if (!Safe_CastSpell( _me, "Nature's Swiftness"))
                    Dlog(" Attempted Oh S@#$ heal but Nature's Swiftness not available");
                else
                {
                    if (!WasHealCast && SpellManager.HasSpell("Greater Healing Wave"))
                        WasHealCast = Safe_CastSpell(healTarget, "Greater Healing Wave");
                    if (!WasHealCast && SpellManager.HasSpell("Healing Surge"))
                        WasHealCast = Safe_CastSpell(healTarget, "Healing Surge");
                    if (!WasHealCast && SpellManager.HasSpell("Healing Wave"))
                        WasHealCast = Safe_CastSpell(healTarget, "Healing Wave");

                    if (WasHealCast)
                        Slog("Big Heals - clicked the Oh S@#$ button!");
                    else
                        Slog("Attempted Oh S@#$ heal but couldn't cast Healing Wave");
                }

                return WasHealCast;
            }

            private static bool GiftoftheNaaru(WoWUnit healTarget)
            {
                if (!SpellManager.HasSpell("Gift of the Naaru"))
                    return false;
                return Safe_CastSpell(healTarget, "Gift of the Naaru");
            }

            private void MoveToHealTarget(WoWUnit unit, double distRange)
            {
                if (!_me.IsUnitInRange(unit, distRange))
                {
                    Slog("MoveToHealTarget:  moving to Heal Target {0} who is {1:F1} yds away", Safe_UnitName(unit), unit.Distance);
                    if (IsCasting())
                        WaitForCurrentCastOrGCD();

                    MoveToObject(unit);
                    while (!IsGameUnstable() && _me.IsAlive && _me.IsMoving && unit.IsAlive && !_me.IsUnitInRange(unit, distRange) && unit.Distance < 100)
                    {
                        // while running, if someone else needs a heal throw a riptide on them
                        if (SpellManager.HasSpell("Riptide") && SpellManager.CanCast("Riptide"))
                        {
                            WoWUnit otherTarget = ChooseNextHealTarget(unit, (double)hsm.NeedHeal);
                            if (otherTarget != null)
                            {
                                Slog("MoveToHealTarget:  healing {0} while moving to heal target {1}", Safe_UnitName(otherTarget), Safe_UnitName(unit));
                                Safe_CastSpell(otherTarget, "Riptide");
                                StyxWoW.SleepForLagDuration();
                            }
                        }
                    }

                    Safe_StopMoving(String.Format("MoveToHealTarget: stopping now that Heal Target is {0:F1} yds away", unit.Distance));
                }
            }
        }

        class HealSpell
        {
            public int Health;
            public string DisplayName;
            public string TestSpell;        // for trained and on cooldown
            public HealCast Cast;

            public delegate bool HealCast(WoWUnit healTarget);

            public HealSpell(int h, string dname, string tname, HealCast fn)
            {
                if (!SpellManager.HasSpell(tname))
                {
                    h = 0;
                    Dlog("ignoring untrained healing spell '{0}' - setting health to 0%", dname);
                }

                Health = h;
                DisplayName = dname;
                TestSpell = tname;
                Cast = fn;
            }
        }
    }
}
