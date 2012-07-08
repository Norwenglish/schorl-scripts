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

//#define DEBUG_LAG 

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
    public sealed class SpellHelper
    {
        private static readonly SpellHelper instance = new SpellHelper();
        public static SpellHelper Instance    { get { return instance; } }

        static uint latencyCache;
        static Countdown timerLatencyCache = new Countdown();
        static Countdown timerSpellDelay = new Countdown();

        private SpellHelper() 
        {
        }

        public static bool IsGCD
        {
            get
            {
                if (!timerSpellDelay.Done)
                {
#if DEBUG_LAG
                    Shaman.Dlog("IsGCD:  timerSpellDelay is not Done");
#endif
                    return true;
                }

                if (!GCDSpell.Cooldown)
                {
                    return false;
                }

                uint leftGCD = 2;
                uint leftLAG = 1;
                if ( Shaman.cfg.AccountForLag )
                {
                    leftGCD = GCDTimeLeft;
                    leftLAG = LagAmount;
#if DEBUG_LAG
                    bool canCast = SpellManager.CanCast(GCDSpell, StyxWoW.Me, false, false, true);
                    Shaman.Dlog("GCD:  gcd={0}, gcdleft:{1}, lagamt:{2}, cancast: {3}, ongcd:{4}",
                        GCDSpell.Cooldown,
                        leftGCD,
                        leftLAG, 
                        canCast,
                        SpellManager.GlobalCooldown);
#endif
                }

#if DEBUG_LAG
                Shaman.Dlog("IsGCD:  GCDleft {0} > LAGleft {1} = {2}", leftGCD, leftLAG, leftGCD > leftLAG );
#endif
                return leftGCD > leftLAG;
            }
        }


        public static WoWSpell _spellCheckGCD = null;

        public static WoWSpell GCDSpell
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

        private static uint GCDTimeLeft
        {
            get
            {
#if ASDF
                return (uint) SpellManager.GlobalCooldownLeft.TotalMilliseconds;
#else
                int timeLeft = (int) GCDSpell.CooldownTimeLeft.TotalMilliseconds;
                if (timeLeft < 0)
                    timeLeft = 0;
                else if (timeLeft > 1500)
                    timeLeft = 1500;
                return (uint) timeLeft;
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
                    // latencyCache = ((latencyCache * 2) * 90) / 100;
                    latencyCache += latencyCache;
                    timerLatencyCache.StartTimer(1000);
                }

                return latencyCache;
            }
        }

        public static void HandleSuccessfullSpellCast()
        {
            HandleSuccessfullSpellCast((int)LagAmount * 3);
        }

        public static void HandleSuccessfullSpellCast(int delay)
        {
            timerSpellDelay.StartTimer(delay);
        }

        public static bool OnCooldown(int id)
        {
            WoWSpell spell = SpellManager.Spells.First(s => s.Value.Id == id).Value;
            return OnCooldown(spell);
        }

        public static bool OnCooldown(WoWSpell spell)
        {
            if (!Shaman.cfg.AccountForLag)
                return spell.Cooldown || Shaman.IsSpellBlacklisted(spell);

            if (spell.Cooldown)
            {
                return LagAmount < (uint) Math.Max( 0, spell.CooldownTimeLeft.TotalMilliseconds);
            }

            return Shaman.IsSpellBlacklisted(spell);
        }
    }

    partial class Shaman
    {
        public static WoWUnit unitLastSpellTarget = null;  // target of last spell cast

        // replacement for SpellManager.GlobalCooldown -- bug in 1.9.2.3 and later on some systems causing it to always be true
        private static bool GCD()
        {
#if OLD_VERSION
            if (cfg.AccountForLag)
                return SpellHelper.IsGCD;

            return SpellHelper.GCDSpell.Cooldown;
#else
            return SpellHelper.IsGCD;
#endif
        }


        public static bool IsCasting()
        {
            if (!_me.IsCasting)
                return false;

            uint leftCAST = 1;
            uint leftLAG = 0;
            if (cfg.AccountForLag)
            {
                leftCAST = (uint) Math.Max( 0, (int)_me.CurrentCastTimeLeft.TotalMilliseconds);
                leftLAG = SpellHelper.LagAmount;
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


        public static bool Safe_CanCastSpell(WoWUnit unit, string sSpellName)
        {
            WoWSpell spell = null;

            try
            {
                spell = SpellManager.Spells[sSpellName];
                System.Diagnostics.Debug.Assert(spell != null);
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> HB EXCEPTION in SpellManager.Spells[" + sSpellName + "]");
                Logging.WriteDebug(">>> Spell '" + sSpellName + "' believed to be " + (SpellManager.HasSpell(sSpellName) ? "KNOWN" : "UNKNOWN") + " was used ");
                Logging.WriteException(e);
                throw;
                // return false;
            }

            return Safe_CanCastSpell(unit, spell);
        }

        public static bool Safe_CanCastSpell(WoWUnit unit, int spellId)
        {
            try
            {
                WoWSpell spell = WoWSpell.FromId(spellId);
                if (spell != null)
                    return Safe_CanCastSpell(unit, spell);
            }
            catch
            {
                ;
            }

            Slog("Error:  attempt to cast unknown spell #{0}", spellId);
            return false;
        }

        public static bool Safe_CanCastSpell(WoWUnit unit, WoWSpell spell)
        {
            if (IsSpellBlacklisted(spell))
            {
                Dlog("CanCastSpell: spell {0} #{1} temporarily blacklisted", spell.Name, spell.Id);
                return false;
            }

            if (spell.IsMeleeSpell)
            {
                double distCheck = _me.MeleeRange(unit);
                if (unit.Distance > distCheck)
                {
                    Dlog("CanCastSpell: cannot cast spell {0} @ {1:F1} yds (melee range is {2:F1} yds)", spell.Name, unit.Distance, distCheck);
                    return false;
                }
            }
            else if (spell.IsSelfOnlySpell)
            {
                ;
            }
            else if (spell.HasRange && unit != null)
            {
                double distCheck = unit.IsMe ? 0 : _me.CombatDistance(unit);
                if (0 < spell.MinRange && distCheck < spell.MinRange)
                {
                    Dlog("SpellCast: cannot cast spell {0} @ {1:F1} yds - minimum range is {2:F1}", spell.Name, unit.Distance, spell.MinRange);
                    return false;
                }

                if (distCheck >= spell.MaxRange)
                {
                    Dlog("SpellCast: cannot cast spell {0} @ {1:F1} yds - maximum range is {2:F1}", spell.Name, distCheck, spell.MaxRange);
                    return false;
                }
            }

            if (spell.PowerType == WoWPowerType.Mana && _me.CurrentMana < spell.PowerCost)
            {
                Dlog("CanCastSpell: spell {0} requires {1} mana but only {2} available", spell.Name, spell.PowerCost, _me.CurrentMana);
                return false;
            }

            if (_me.IsMoving && spell.CastTime > 0)
            {
                Dlog("CanCastSpell: spell {0} is not instant ({1} ms cast time) and we are moving", spell.Name, spell.CastTime);
                return false;
            }

            uint timeLeft = (uint)Math.Max(0, spell.CooldownTimeLeft.TotalMilliseconds);
            uint timeLag = SpellHelper.LagAmount;
            uint currMana = _me.CurrentMana;

            if (SpellHelper.OnCooldown(spell))
            {
                Dlog("CanCastSpell: cannot cast '{0}' spell on cooldown - cd={1}, cdleft={2}, lagamt={3}, cost={4}, mana={5}, blacklist={6}",
                    spell.Name,
                    spell.Cooldown,
                    timeLeft,
                    timeLag,
                    spell.PowerCost,
                    currMana,
                    IsSpellBlacklisted(spell)
                    );
                return false;
            }

            return true;
        }

        public static bool Safe_CastSpell(WoWUnit unit, string sSpellName)
        {
            WoWSpell spell = null;

            try
            {
                spell = SpellManager.Spells[sSpellName];
                System.Diagnostics.Debug.Assert(spell != null);
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> HB EXCEPTION in SpellManager.Spells[" + sSpellName + "]");
                Logging.WriteDebug(">>> Spell '" + sSpellName + "' believed to be " + (SpellManager.HasSpell(sSpellName) ? "KNOWN" : "UNKNOWN") + " was used ");
                Logging.WriteException(e);
                throw;
                // return false;
            }

            return Safe_CastSpell(unit, spell);
        }

        public static bool Safe_CastSpell(WoWUnit unit, int spellId)
        {
            try
            {
                WoWSpell spell = WoWSpell.FromId(spellId);
                if (spell != null)
                    return Safe_CastSpell(unit, spell );
            }
            catch
            {
                ;
            }

            Slog("Error:  attempt to cast unknown spell #{0}", spellId);
            return false;
        }

        public static bool Safe_CastSpell(WoWUnit unit, WoWSpell spell)
        {
            bool bCastSuccessful = false;

            if ( !_me.Combat )
                WaitForCurrentCastOrGCD();

            if (IsSpellBlacklisted(spell))
            {
                Dlog("Safe_CastSpell: spell {0} #{1} temporarily blacklisted", spell.Name, spell.Id);
                return false;
            }

            uint timeLeft = (uint)spell.CooldownTimeLeft.TotalMilliseconds; //  (uint)Math.Max(0, spell.CooldownTimeLeft.TotalMilliseconds);
            uint timeLag = SpellHelper.LagAmount;
            uint currMana = _me.CurrentMana;

            if (SpellHelper.OnCooldown(spell))
            {
                Dlog("Safe_CastSpell: cannot cast '{0}' spell on cooldown - cd={1}, castleft={2}, lagamt={3}, cost={4}, mana={5}, blacklist={6}",
                    spell.Name,
                    spell.Cooldown,
                    timeLeft,
                    timeLag,
                    spell.PowerCost,
                    currMana,
                    IsSpellBlacklisted(spell)
                    );
                return false;
            }

            if (spell.IsMeleeSpell)
            {
                double distCheck = _me.MeleeRange(unit);
                if (unit.Distance > distCheck )
                {
                    Dlog("SpellCast: cannot cast spell {0} @ {1:F1} yds (melee range is {2:F1} yds)", spell.Name, unit.Distance, distCheck );
                    return false;
                }
            }
            else if (spell.IsSelfOnlySpell || !_me.GotTarget)
            {
                ;
            }
            else if (spell.HasRange)
            {
                double distCheck = unit.IsMe ? 0 : _me.CombatDistance(unit);
                if (0 < spell.MinRange && distCheck < spell.MinRange)
                {
                    Dlog("SpellCast: cannot cast spell {0} @ {1:F1} yds - minimum range is {2:F1}", spell.Name, unit.Distance, spell.MinRange);
                    return false;
                }

                if (distCheck >= spell.MaxRange)
                {
                    Dlog("SpellCast: cannot cast spell {0} @ {1:F1} yds - maximum range is {2:F1}", spell.Name, distCheck, spell.MaxRange);
                    return false;
                }
            }

            if (spell.PowerType == WoWPowerType.Mana && _me.CurrentMana < spell.PowerCost)
            {
                Dlog("Safe_CastSpell: spell {0} requires {1} mana but only {2} available", spell.Name, spell.PowerCost, _me.CurrentMana );
                return false;
            }

            if (_me.IsMoving && spell.CastTime > 0)
            {
                Dlog("Safe_CastSpell: spell {0} is not instant ({1} ms cast time) and we are moving", spell.Name, spell.CastTime);
                return false;
            }

#if NOPE
            else if (!SpellManager.CanCast(spell, unit, false, false, cfg.AccountForLag))
            {
                Dlog("Safe_CastSpell: cannot cast '{0}' cancast says no - cd={1}, castleft={2}, lagamt={3}, cost={4}, mana={5}, blacklist={6}",
                    spell.Name,
                    spell.Cooldown,
                    timeLeft,
                    timeLag,
                    spell.PowerCost,
                    currMana,
                    IsSpellBlacklisted(spell)
                    );
            }
#endif
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
                catch (GameUnstableException) { throw; }
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
                    SpellHelper.HandleSuccessfullSpellCast();
                    AddSpellToBlacklist(spell);

                    string info = "";
                    System.Drawing.Color clr = spell.Mechanic == WoWSpellMechanic.Healing ?
                        Color.ForestGreen : Color.DodgerBlue;

                    // spell.Mechanic always equals None currently
                    if (unit != null) // && spell.Mechanic == WoWSpellMechanic.Healing)
                        info = string.Format(" on {0} at {1:F1} yds at {2:F1}%", Safe_UnitName(unit), udist, uhealth);

                    Log(clr, "*" + spell.Name + info);

                    if ( !cfg.AccountForLag || IsMeOrMyGroup(unit))
                        SleepForLagDuration();
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
            if (IsHealer() && IsHealSpellWeMonitorForCancel(_me.CastingSpellId) && hsm != null)
                WaitForCurrentHeal(unitLastSpellTarget, hsm.NeedHeal);

            return IsCasting();
        }

        // identify heal spells which could have cast time
        public static bool IsHealSpell(int id)
        {
            switch (id)
            {
                case 331:       // Healing Wave
                case 8004:      // Healing Surge
                case 77472:     // Greater Healing Wave
                case 1064:      // Chain Heal
                case 73920:     // Healing Rain
                    return true;
            }

            return false;
        }

        // identify heal spells we allow to be interrupted
        public static bool IsHealSpellWeMonitorForCancel(int id)
        {
            switch (id)
            {
                case 331:       // Healing Wave
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
            const int HEALING_RAIN = 73920;
            const int CHAIN_HEAL = 1064;

            if (IsCasting())
                Dlog("WaitForCurrentHeal:  spell cast in progress");

            while (!IsGameUnstable() && _me.IsAlive && IsCasting())
            {
                // we hit target value, so cancel current cast
                if (unit == null)
                {
                    ;
                }
                else if (unit.HealthPercent >= healValue
                    && !((_me.CastingSpellId == CHAIN_HEAL || _me.CastingSpellId == HEALING_RAIN)))
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
                // Sleep(10);
            }


            if (IsGameUnstable())
                Dlog("WaitForCurrentSpell:  game appears to be Unstable");
            else if (IsCasting())
                Dlog("WaitForCurrentHeal:  done waiting but i am still casting???");

            return true;
        }


        public static bool AllowCastToFinish()
        {
            const int LIGHTNING_BOLT = 403;

            if (IsHealer())
            {
                WoWUnit p = ChooseHealTarget(hsm.NeedHeal, SpellRange.Check);
                if (p == null)
                    ;
                else if (_me.CastingSpellId == LIGHTNING_BOLT && p.HealthPercent >= cfg.TC_StopCastAtHealth)
                    ;
                else if (_me.CastingSpellId == LIGHTNING_BOLT && _me.ManaPercent < cfg.TC_CastIfManaBelow && p.HealthPercent >= cfg.TC_CastUnlessHealthBelow)
                    ;
                else if (IsHealSpell(_me.CastingSpellId))
                {
                    if (IsHealSpellWeMonitorForCancel(_me.CastingSpellId))
                    {
                        return AllowHealToFinish();
                    }
                }
                else
                {
                    Slog(Color.Orange, "/stopcasting - {0} dropped to {1:F1}%", Safe_UnitName(p), p.HealthPercent);
                    SpellManager.StopCasting();
                    return false;   // let them know didn't complete cast
                }
            }

            return true;
        }

        public static bool AllowHealToFinish()
        {
            Dlog("AllowHealToFinish:  spell cast in progress");
            if (unitLastSpellTarget != null && hsm != null )
            {
                if (!unitLastSpellTarget.IsValid)
                {
                    // last spell target left group / released / etc
                    return false;
                }

                if (unitLastSpellTarget.HealthPercent >= hsm.NeedHeal)
                {
                    SpellManager.StopCasting();
                    Slog(Color.Orange, "/stopcasting - heal target reached {0:F1}%", unitLastSpellTarget.HealthPercent);
                    return false;
                }

                if (IsHealer())
                {
                    unitToSaveHeal = null;      // unit we should throw a saving heal at the expense of current heal target

                    // check if we need to bail on this heal to save self or the tank
                    if (IsRAFandTANK() && GroupTank.HealthPercent < ConfigValues.EmergencySavingHealPct && HealMembers.Contains(GroupTank))
                        unitToSaveHeal = GroupTank;
                    else if (!unitLastSpellTarget.IsMe && _me.HealthPercent < ConfigValues.EmergencySavingHealPct)
                        unitToSaveHeal = _me;

                    if (unitToSaveHeal != null && unitToSaveHeal.IsAlive && unitLastSpellTarget.Guid != unitToSaveHeal.Guid && IsCasting())
                    {
                        SpellManager.StopCasting();
                        Slog(Color.Orange, "/stopcasting - switch to {0} who is dangerously low @ {1:F1}%", Safe_UnitName(unitToSaveHeal), unitToSaveHeal.HealthPercent);
                        return false;
                    }
                }
            }

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
                    if (p == null)
                        ;
                    else if (p.HealthPercent > cfg.TC_StopCastAtHealth)
                        ;
                    else if ( _me.ManaPercent < cfg.TC_CastIfManaBelow && p.HealthPercent >= cfg.TC_CastUnlessHealthBelow)
                        ;
                    else
                    {
                        Slog(Color.Orange, "/stopcasting - {0} dropped to {1:F1}%", Safe_UnitName(p), p.HealthPercent);
                        SpellManager.StopCasting();
                        return false;   // let them know didn't complete cast
                    }

                    // pause briefly
                    Sleep(10);
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
                    Sleep(10);
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
                    Sleep(10);
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
            bool canInterrupt = false;

            if (_dictIntSpell.ContainsKey(spellId))
                return _dictIntSpell[spellId];

#if LUA_SPELL_INTERRUPT_CHECK
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
            canInterrupt = string.IsNullOrEmpty(list[idxList]);
#else
            canInterrupt = target.CanInterruptCurrentSpellCast;
#endif
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

            uint timeToBlacklist = 900;
#if ADJUST_WAIT_TO_CAST_TIME
            if (s.CastTime > 0)
            {
                timeToBlacklist = s.CastTime;
            }
            timeToBlacklist -= (SpellHelper.LagAmount + 150);
#endif
            if (timeToBlacklist < SpellHelper.LagAmount)
                timeToBlacklist = SpellHelper.LagAmount;

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
