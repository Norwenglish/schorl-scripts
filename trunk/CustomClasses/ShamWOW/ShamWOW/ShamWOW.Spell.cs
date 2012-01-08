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

#define DEBUG_LAG 

#pragma warning disable 642

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bobby53
{
    public sealed class GCDHelper
    {
        private static readonly GCDHelper instance = new GCDHelper();
        public static GCDHelper Instance    { get { return instance; } }

        static uint latencyCache;
        static Countdown timerLatencyCache = new Countdown();
        static Countdown timerSpellDelay = new Countdown();

        private GCDHelper() 
        {
        }

        public static bool Active
        {
            get
            {
                if (!timerSpellDelay.Done)
                    return true;

                if (!Spell.Cooldown)
                    return false;

                uint leftGCD = 2;
                uint leftLAG = 1;
                if ( Shaman.cfg.AccountForLag )
                {
                    leftGCD = TimeLeft;
                    leftLAG = LagAmount;
#if DEBUG_LAG
                    bool canCast = SpellManager.CanCast(Spell, StyxWoW.Me, false, false, true);
                    Shaman.Dlog("GCD:  gcd={0}, gcdleft:{1}, lagamt:{2}, cancast: {3}, ongcd:{4}",
                        Spell.Cooldown,
                        leftGCD,
                        leftLAG, 
                        canCast,
                        SpellManager.GlobalCooldown);
#endif
                }

                return leftGCD > leftLAG;
            }
        }


        public static WoWSpell _spellCheckGCD = null;

        public static WoWSpell Spell
        {
            get
            {
                if (_spellCheckGCD == null)
                {
                    if (!SpellManager.HasSpell("Lightning Shield"))
                        return SpellManager.Spells["Lightning Bolt"];

                    _spellCheckGCD = SpellManager.Spells["Lightning Shield"];
                }

                return _spellCheckGCD;
            }
        }

        private static uint TimeLeft
        {
            get
            {
#if ASDF
                return (uint) SpellManager.GlobalCooldownLeft.TotalMilliseconds;
#else
                return (uint) Math.Max( 0, Spell.CooldownTimeLeft.TotalMilliseconds);
#endif
            }
        }

        public static uint LagAmount
        {
            get
            {
                if (timerLatencyCache.Done)
                {
                    latencyCache = StyxWoW.WoWClient.Latency;
                    latencyCache += latencyCache;
                    timerLatencyCache.StartTimer(1000);
                }

                return latencyCache;
            }
        }

        public static void HandleSuccessfullSpellCast()
        {
            timerSpellDelay.StartTimer((int) LagAmount * 2);
        }
    }

    partial class Shaman
    {
        static WoWUnit unitLastSpellTarget = null;  // target of last spell cast

        // replacement for SpellManager.GlobalCooldown -- bug in 1.9.2.3 and later on some systems causing it to always be true
        private static bool GCD()
        {
            if (cfg.AccountForLag)
                return GCDHelper.Active;

            return GCDHelper.Spell.Cooldown;
        }


        public static bool IsCasting()
        {
            if (!_me.IsCasting)
                return false;

            uint leftCAST = 1;
            uint leftLAG = 0;
            if (cfg.AccountForLag)
            {
                leftCAST = (uint) Math.Max( 0, _me.CurrentCastTimeLeft.TotalMilliseconds);
                leftLAG = GCDHelper.LagAmount;
#if DEBUG_LAG
                Dlog("CAST:  casting={0},  remaining: {1},  lag: {2}", 
                    _me.IsCasting, 
                    leftCAST, 
                    leftLAG
                    );
#endif
            }

            return leftCAST > leftLAG;
        }

        public static bool IsCastingOrGCD()
        {
            return GCD() || IsCasting(); //  || 0 != _me.ChanneledCasting;
        }

        /*
         * Safe_CastSpell()
         * 
         * several different overloads providing the ability to safely
         * cast a spell with or without a range check
         */

        public enum SpellRange { NoCheck, Check };
        public enum SpellWait { NoWait, Complete };

        public static bool Safe_CastSpell(string sSpellName)
        {
            return Safe_CastSpell(sSpellName, SpellRange.NoCheck, SpellWait.Complete);
        }

        public static bool Safe_CastSpellWithRangeCheck(string sSpellName)
        {
            return Safe_CastSpell(sSpellName, SpellRange.Check, SpellWait.Complete);
        }

        public static bool Safe_CastSpell(string sSpellName, SpellRange chkRng, SpellWait chkWait)
        {
            return Safe_CastSpell(null, sSpellName, chkRng, chkWait);
        }

        public static bool Safe_CastSpell(WoWUnit unit, int spellId, SpellRange chkRng, SpellWait chkWait)
        {
            try
            {
                WoWSpell spell = WoWSpell.FromId(spellId);
                if (spell != null)
                    return Safe_CastSpell(unit, spell, chkRng, chkWait);
            }
            catch
            {
                ;
            }

            Slog("Error:  attempt to cast unknown spell #{0}", spellId);
            return false;
        }

        public static bool Safe_CastSpell(WoWUnit unit, string sSpellName, SpellRange chkRng, SpellWait chkWait)
        {
            WoWSpell spell = null;

            try
            {
                // spell = SpellManager.Spells[sSpellName];
                spell = SpellManager.Spells[sSpellName];
                System.Diagnostics.Debug.Assert(spell != null);
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> HB EXCEPTION in SpellManager.Spells[" + sSpellName + "]");
                Logging.WriteDebug(">>> Spell '" + sSpellName + "' believed to be " + (SpellManager.HasSpell(sSpellName) ? "KNOWN" : "UNKNOWN") + " was used ");
                Logging.WriteException(e);
                throw;
                // return false;
            }

            return Safe_CastSpell(unit, spell, chkRng, chkWait);
        }

        public static bool Safe_CastSpellWithRangeCheck(WoWSpell spell)
        {
            return Safe_CastSpell(null, spell, SpellRange.Check, SpellWait.Complete);
        }

        public static bool Safe_CastSpell(WoWSpell spell)
        {
            return Safe_CastSpell(null, spell, SpellRange.NoCheck, SpellWait.Complete);
        }

        public static bool Safe_CastSpell(WoWUnit unit, WoWSpell spell, SpellRange chkRng, SpellWait chkWait)
        {
            bool bCastSuccessful = false;
            WoWUnit unitChkDist;

            if (unit != null)
                unitChkDist = unit;
            else if (_me.GotTarget)
                unitChkDist = _me.CurrentTarget;
            else
                unitChkDist = null;

            // enoughPower = (_me.GetCurrentPower(spell.PowerType) >= spell.PowerCost);
            if (MeSilenced())
                ;
            else if (chkRng == SpellRange.Check && spell.HasRange && unitChkDist != null && (unitChkDist.Distance - HitBoxRange(unitChkDist)) >= spell.MaxRange)
            {
                Dlog("Safe_CastSpell: Spell '{0}' not cast -- max range {1:F1}, but target {2:F1} away with {3:F1} hit box", 
                    spell.Name, 
                    spell.MaxRange, 
                    (unitChkDist == null ?  -1 : unitChkDist.Distance),
                    (unitChkDist == null ?  -1 : unitChkDist.CombatReach));
            }
            else
            {
                bCastSuccessful = Safe_CastSpellIgnoreSilence(unit, spell);
                if (chkWait == SpellWait.Complete)
                {
                    WaitForCurrentCastOrGCD();
                }
            }

            return bCastSuccessful;
        }

        public static bool Safe_CastSpellIgnoreSilence(WoWUnit unit, string spellName)
        {
            if ( !SpellManager.HasSpell( spellName))
                return false;

            WoWSpell spell = SpellManager.Spells[spellName];
            return Safe_CastSpellIgnoreSilence(unit, spell);
        }

        public static bool Safe_CastSpellIgnoreSilence(WoWUnit unit, int spellId)
        {
            return Safe_CastSpellIgnoreSilence(unit, WoWSpell.FromId(spellId));
        }

        public static bool Safe_CastSpellIgnoreSilence(WoWUnit unit, WoWSpell spell)
        {
            bool bCastSuccessful = false;
            bool isCooldownActive = false;

            if ( !_me.Combat )
                WaitForCurrentCastOrGCD();

            uint timeLeft = (uint) Math.Max( 0, spell.CooldownTimeLeft.TotalMilliseconds);
            uint timeLag = GCDHelper.LagAmount;
            uint currMana = _me.CurrentMana;

            isCooldownActive = spell.Cooldown;
            if ( isCooldownActive && cfg.AccountForLag)
                isCooldownActive = (timeLeft > timeLag);

            if (!isCooldownActive)
                isCooldownActive = !SpellManager.CanCast(spell, unit, false, false, cfg.AccountForLag);

            if (isCooldownActive)
            {
                Dlog("Safe_CastSpell: cannot cast '{0}' - cd={1}, castleft={2}, lagamt={3}, cost={4}, mana={5}",
                    spell.Name,
                    spell.Cooldown,
                    timeLeft,
                    timeLag,
                    spell.PowerCost,
                    currMana
                    );
            }
            else
            {
                double udist = -1;
                double uhealth = -1;

                try
                {
                    if (unit == null)
                        bCastSuccessful = SpellManager.Cast(spell);
                    else
                    {
                        udist = unit.Distance;
                        uhealth = unit.HealthPercent;
                        bCastSuccessful = SpellManager.Cast(spell, unit);
                    }
                }

                catch (ThreadAbortException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("HB EXCEPTION in spell.Cast([" + spell.Id + ":" + spell.Name + "])");
                    Logging.WriteException(e);
                    return false;
                }

                unitLastSpellTarget = unit;

                if (!bCastSuccessful)
                    Dlog("Safe_CastSpell: cast of {0} failed", spell.Name);
                else
                {
                    GCDHelper.HandleSuccessfullSpellCast();

                    string info = "";
                    System.Drawing.Color clr = spell.Mechanic == WoWSpellMechanic.Healing ?
                        Color.ForestGreen : Color.DodgerBlue;

                    // spell.Mechanic always equals None currently
                    if (unit != null) // && spell.Mechanic == WoWSpellMechanic.Healing)
                        info = string.Format(" on {0} at {1:F1} yds at {2:F1}%", Safe_UnitName(unit), udist, uhealth);

                    Log(clr, "*" + spell.Name + info);

                    if ( !cfg.AccountForLag || IsMeOrMyGroup(unit))
                        StyxWoW.SleepForLagDuration();
                }
            }

            return bCastSuccessful;
        }


        // if its a heal spell, will wait until completed (or cancelled)
        // .. otherwise just return if cast is in progress.  this function
        // .. should be used at CC entry points to prevent movement causing
        // .. an unintended spell cancellation/interruption
        public bool HandleCurrentSpellCast()
        {
            if (IsHealer() && IsHealSpellToWatch(_me.CastingSpellId) && hsm != null)
                WaitForCurrentHeal(unitLastSpellTarget, hsm.NeedHeal);

            return IsCasting();
        }

        // identify heal spells which could have cast time.  we'll monitor these
        // .. to see if we already reached heal threshhold and should cancel
        // .. don't worry about instants since we can't cancel anyway
        public static bool IsHealSpellToWatch(int id)
        {
            switch (id)
            {
                case 331:       // Healing Wave
                case 1064:      // Chain Heal
                case 8004:      // Healing Surge
                case 77472:     // Greater Healing Wave
                    return true;
            }
            return false;
        }

        // waits for current spell in progress and if target reaches health %
        // .. will cancel.  assumes it will only be called when heal is being cast
        // DOES NOT WAIT ON INSTANTS!!!!  THIS IS INTENTIONAL AS OTHER TASKS
        // CAN BE COMPLETED AFTER AN ENTRY POINT WHILE GCD TICKS OFF RATHER
        // THAN WAITING HERE IN A DO NOTHING LOOP
        public static bool WaitForCurrentHeal(WoWUnit unit, int healValue)
        {
            if (IsCasting()) 
                Dlog("WaitForCurrentHeal:  spell cast in progress");

            while (!IsGameUnstable() && _me.IsAlive && IsCasting())
            {
                // we hit target value, so cancel current cast
                if (unit == null)
                {
                    ;
                }
                else if (unit.HealthPercent >= healValue)
                {
                    // only cancel if we are casting
                    SpellManager.StopCasting();
                    Slog(Color.Orange, "/stopcasting - heal target reached {0:F1}%", unit.HealthPercent);

                    // return now because we know health is above threshhold, so no need to wait further
                    return false;
                }
                else if (IsHealer()) 
                {
                    unitToSaveHeal = null;      // unit we should throw a saving heal at the expense of current heal target

                    // check if we need to bail on this heal to save self or the tank
                    if (IsRAFandTANK() && GroupTank.HealthPercent < ConfigValues.EmergencySavingHealPct && HealMembers.Contains(GroupTank))
                        unitToSaveHeal = GroupTank;
                    else if (!unit.IsMe && _me.HealthPercent < ConfigValues.EmergencySavingHealPct)
                        unitToSaveHeal = _me;

                    if (unitToSaveHeal != null && unitToSaveHeal.IsAlive && unit.Guid != unitToSaveHeal.Guid && IsCasting())
                    {
                        SpellManager.StopCasting();
                        Slog(Color.Orange, "/stopcasting - switch to {0} who is dangerously low @ {1:F1}%", Safe_UnitName(unitToSaveHeal), unitToSaveHeal.HealthPercent);
                        return false;
                    }
                }

                // InterruptEnemyCast();

                // pause briefly
                Thread.Sleep(10);
            }


            if (IsGameUnstable())
                Dlog("WaitForCurrentSpell:  game appears to be Unstable");
            else if (IsCasting())
                Dlog("WaitForCurrentHeal:  done waiting but i am still casting???");

            return true;
        }


        // waits for current spell in progress and if target reaches health %
        // .. will cancel.  assumes it will only be called when heal is being cast
        // DOES NOT WAIT ON INSTANTS!!!!  THIS IS INTENTIONAL AS OTHER TASKS
        // CAN BE COMPLETED AFTER AN ENTRY POINT WHILE GCD TICKS OFF RATHER
        // THAN WAITING HERE IN A DO NOTHING LOOP
        public static bool WaitForHealerDamageSpell()
        {
            if (!IsCastingOrGCD())
            {
                Dlog("WaitForHealerDamageSpell:  no cast or gcd in progress");
            }
            else
            {
                Dlog("WaitForHealerDamageSpell:  waiting until gcd and/or cast are complete");
                while (!IsGameUnstable() && _me.IsAlive && IsCastingOrGCD())
                {
                    WoWUnit p = ChooseHealTarget(hsm.NeedHeal - 5, SpellRange.Check);
                    if (p != null)
                    {
                        Slog(Color.Orange, "/stopcasting - {0} dropped to {1:F1}%", Safe_UnitName(p), p.HealthPercent);
                        SpellManager.StopCasting();
                        return false;   // let them know didn't complete cast
                    }

                    // pause briefly
                    Thread.Sleep(10);
                }
            }

            if (IsGameUnstable())
                Dlog("WaitForTelluricCurrents:  game appears to be Unstable");
            else if (!_me.IsAlive)
                Dlog("WaitForTelluricCurrents:  I died while waiting");
            else if (IsCastingOrGCD())
                Dlog("WaitForTelluricCurrents:  done waiting but i am still casting???");

            return true;
        }

        public static void WaitForCurrentCast()
        {
            if (!IsCasting())
            {
                Dlog("WaitForCurrentCast:  no cast in progress");
            }
            else
            {
                Dlog("WaitForCurrentCast:  waiting until cast is complete");
                while (!IsGameUnstable() && _me.IsAlive && IsCasting())
                {
                    InterruptEnemyCast();

                    // give a small time slice back
                    Thread.Sleep(10);
                }
            }
        }

        public static void WaitForCurrentCastOrGCD()
        {
            if (!IsCastingOrGCD() )
            {
                Dlog("WaitForCurrentCastOrGCD:  no cast or gcd in progress");
            }
            else
            {
                Dlog("WaitForCurrentCastOrGCD:  waiting until gcd and/or cast are complete");
                while (!IsGameUnstable() && _me.IsAlive && IsCastingOrGCD())
                {
                    // give a small time slice back
                    Thread.Sleep(10);
                }
            }
        }


        private static Dictionary<int, bool> _dictIntSpell = new Dictionary<int, bool>();

        public static bool IsTargetCastInterruptible(WoWUnit target)
        {
            if (target == null || !target.IsCasting)
                return false;

            int spellId = target.CastingSpell.Id;
            string spellName = target.CastingSpell.Name;

            if (_dictIntSpell.ContainsKey(spellId))
                return _dictIntSpell[spellId];

            int idxList = 8;
            List<string> list = Lua.GetReturnValues("return UnitCastingInfo(\"target\")");
            if (list == null)
            {
                idxList = 7;
                list = Lua.GetReturnValues("return UnitChannelInfo(\"target\")");
            }

            if (list == null)
            {
                Dlog("SpellInterrupt:  null return from casting information check for {0} #{1}", spellName, spellId);
                return false;
            }
#if LIST_SPELL_INFO
            Dlog("SpellInterrupt:  list[{0}]={1}", list.Count, list);
            for (int i = 0; i < list.Count; i++)
                Dlog("  list[{0}]='{1}'", i, list[i]);
#endif
            bool canInterrupt = string.IsNullOrEmpty(list[idxList]);
            _dictIntSpell.Add(spellId, canInterrupt);
            Dlog("SpellInterrupt:  added canInterrupt:{0} for spell:{1}(#{2})", canInterrupt, spellName, spellId);
            return canInterrupt;
        }

        public static bool AddSpellToBlacklist(string spellName)
        {
            if (!SpellManager.HasSpell(spellName))
                return false;

            WoWSpell s = SpellManager.Spells[spellName];
            return AddSpellToBlacklist(s);
        }

        public static bool AddSpellToBlacklist( int id)
        {
            return AddSpellToBlacklist(WoWSpell.FromId(id));
        }

        public static bool AddSpellToBlacklist(WoWSpell s)
        {
            if (s == null)
                return false;

            uint timeToBlacklist = 1000;
            if (s.CastTime > 0)
            {
                timeToBlacklist = s.CastTime;
            }

            timeToBlacklist -= 150;
            timeToBlacklist -= GCDHelper.LagAmount;

            Blacklist.Add((ulong)s.Id, new TimeSpan(0, 0, 0, 0, (int) timeToBlacklist));
            Dlog("AddSpellToBlacklist: {0} for {1} ms", s.Name, timeToBlacklist );
            return true;
        }



        public static bool IsSpellBlacklisted(string spellName)
        {
            if (!SpellManager.HasSpell(spellName))
                return false;

            WoWSpell s = SpellManager.Spells[spellName];
            return IsSpellBlacklisted(s);
        }
        
        public static bool IsSpellBlacklisted(int id)
        {
            WoWSpell s = WoWSpell.FromId(id);
            return IsSpellBlacklisted(s);
        }
  
        public static bool IsSpellBlacklisted(WoWSpell s)
        {
            return s == null ? false : Blacklist.Contains((ulong)s.Id);
        }
    }

}
