 using System;
using System.Drawing;
using System.Linq;
using System.Text;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System;
using System.Drawing;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace UltimatePaladinHealerBT
{
    public partial class UltimatePalaHealerBT
    {
        #region Helpers

        private void slog(string format, params object[] args)
        {
            Logging.Write(format, args);
        }

        private void slog(System.Drawing.Color color, string format, params object[] args)
        {
            Logging.Write(color, format, args);
        }

        private void slog(System.Drawing.Color color, string format)
        {
            Logging.Write(color, format);
        }

        private void Player_OnMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            Beahviour();
            //Why would we create same behaviors all over ?
            if (lastBehaviour == usedBehaviour)
            {
                return;
            }

            slog("Context changed. New context: " + usedBehaviour + ". Rebuilding behaviors.");
            CreateBehaviors();
        }

        private void Beahviour()
        {
            actualBehaviour = Me.PvPState.ToString();
            /*if ((actualBehaviour == "None") && lastBehaviour == null)
            {
                usedBehaviour = "Starting";
            }
            else */
            if (Me.IsGhost)
            {
                usedBehaviour = "Ghost";
            }
            else if (!Me.IsInInstance && !Me.IsInParty && !Me.IsInRaid)
            {
                usedBehaviour = "Solo";
            }
            else if ((actualBehaviour == "None") && !Me.IsInInstance)
            {
                usedBehaviour = "Party or Raid";
            }
            else if ((actualBehaviour == "FFAPVP") && Me.IsInInstance)
            {
                usedBehaviour = "Arena";
            }
            else if (!Me.IsInInstance && actualBehaviour == "PVP")
            {
                usedBehaviour = "World PVP";
            }
            else if (Me.IsInInstance && actualBehaviour == "PVP")
            {
                usedBehaviour = "Battleground";
            }
            else if ((Me.IsInInstance) && (Me.IsInParty) && (!Me.IsInRaid) && actualBehaviour != "FFAPVP" && actualBehaviour != "PVP")
            {
                usedBehaviour = "Dungeon";
            }
            else if ((Me.IsInInstance) && (Me.IsInRaid) && actualBehaviour != "FFAPVP" && actualBehaviour != "PVP")
            {
                usedBehaviour = "Raid";
            }
            else
            {
                usedBehaviour = "WTF are you doing?";
            }
        }

        private void HandleCombatLog(object sender, LuaEventArgs args)
        {
            var e = new CombatLogEventArgs(args.EventName, args.FireTimeStamp, args.Args);
            //Logger.WriteDebug("[CombatLog] " + e.Event + " - " + e.SourceName + " - " + e.SpellName);
            switch (e.Event)
            {
                case "SPELL_AURA_APPLIED":
                case "SPELL_CAST_SUCCESS":
                    if (e.SourceGuid != Me.Guid)
                    {
                        return;
                    }

                    // reset target of current spell since complete
                    CastingSpellTarget = null;

                    // Update the last spell we cast. So certain classes can 'switch' their logic around.
                    LastSpellCast = e.SpellName;
                    //Logger.WriteDebug("Successfully cast " + LastSpellCast);
                    /*
                    if (_sleepAfterSuccessSpells.Contains(e.SpellName))
                    {
                        StyxWoW.SleepForLagDuration();
                    }

                    // Force a wait for all summoned minions. This prevents double-casting it.
                    if (myClass == WoWClass.Warlock && e.SpellName.StartsWith("Summon "))
                    {
                        StyxWoW.SleepForLagDuration();
                    }
                     * */
                    break;

                case "SPELL_MISSED":
                    //Logger.Write(e.Args.ToRealString());
                    if (e.Args[11].ToString() == "EVADE")
                    {
                        slog("Mob is evading. Blacklisting it!");
                        Blacklist.Add(e.DestGuid, TimeSpan.FromMinutes(30));
                        if (StyxWoW.Me.CurrentTargetGuid == e.DestGuid)
                        {
                            StyxWoW.Me.ClearTarget();
                        }

                        BotPoi.Clear("Blacklisting evading mob");
                        StyxWoW.SleepForLagDuration();
                    }
                    break;
            }
        }


        public void AttachEventHandlers()
        {
            BotEvents.Player.OnMapChanged += Player_OnMapChanged;

            // DO NOT EDIT THIS UNLESS YOU KNOW WHAT YOU'RE DOING!
            // This ensures we only capture certain combat log events, not all of them.
            // This saves on performance, and possible memory leaks. (Leaks due to Lua table issues.)
            Lua.Events.AttachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLog);
            if (
                !Lua.Events.AddFilter(
                    "COMBAT_LOG_EVENT_UNFILTERED",
                    "return args[2] == 'SPELL_CAST_SUCCESS' or args[2] == 'SPELL_AURA_APPLIED' or args[2] == 'SPELL_MISSED'"))
            {
                slog("ERROR: Could not add combat log event filter! - Performance may be horrible, and things may not work properly!");
            }
        }

        public bool InRaid()
        {
            return Me.RaidMembers.Count > 0;
        }

        public bool InParty()
        {
            return Me.PartyMember1 != null;
        }
        public void populate_nameofRM()
        {
            if (unitcheck(Me) && Me.IsInRaid)
            {
                for (i = 1; i <= 40; i++)
                {
                    NameorRM[i] = Nameofraidmember(i);
                }
            }
        }

        public void populate_raidsubg()
        {
            if (unitcheck(Me) && Me.IsInRaid)
            {
                for (i = 1; i <= 40; i++)
                {
                    Raidsbugroup[i] = subgroupofraidmember(i);
                }
            }
        }
        public void Inizialize_raidorder()
        {
            for (i = 0; i < 41; i++)
            {
                Raidorder[i] = i;
                OrganizedNames[i] = "";
                healornot[i] = false;
                _heal_raid_member[i] = false;
            }
            SB1C = 1;
            SB2C = 1;
            SB3C = 1;
            SB4C = 1;
            SB5C = 1;

        }
        public void populate_heal_or_not()
        {
            healornot[0] = UPaHBTSetting.Instance.Heal_raid_member0;
            healornot[1] = UPaHBTSetting.Instance.Heal_raid_member1;
            healornot[2] = UPaHBTSetting.Instance.Heal_raid_member2;
            healornot[3] = UPaHBTSetting.Instance.Heal_raid_member3;
            healornot[4] = UPaHBTSetting.Instance.Heal_raid_member4;
            healornot[5] = UPaHBTSetting.Instance.Heal_raid_member5;
            healornot[6] = UPaHBTSetting.Instance.Heal_raid_member6;
            healornot[7] = UPaHBTSetting.Instance.Heal_raid_member7;
            healornot[9] = UPaHBTSetting.Instance.Heal_raid_member8;
            healornot[9] = UPaHBTSetting.Instance.Heal_raid_member9;
            healornot[10] = UPaHBTSetting.Instance.Heal_raid_member10;
            healornot[11] = UPaHBTSetting.Instance.Heal_raid_member11;
            healornot[12] = UPaHBTSetting.Instance.Heal_raid_member12;
            healornot[13] = UPaHBTSetting.Instance.Heal_raid_member13;
            healornot[14] = UPaHBTSetting.Instance.Heal_raid_member14;
            healornot[15] = UPaHBTSetting.Instance.Heal_raid_member15;
            healornot[16] = UPaHBTSetting.Instance.Heal_raid_member16;
            healornot[17] = UPaHBTSetting.Instance.Heal_raid_member17;
            healornot[18] = UPaHBTSetting.Instance.Heal_raid_member18;
            healornot[19] = UPaHBTSetting.Instance.Heal_raid_member19;
            healornot[20] = UPaHBTSetting.Instance.Heal_raid_member20;
            healornot[21] = UPaHBTSetting.Instance.Heal_raid_member21;
            healornot[22] = UPaHBTSetting.Instance.Heal_raid_member22;
            healornot[23] = UPaHBTSetting.Instance.Heal_raid_member23;
            healornot[24] = UPaHBTSetting.Instance.Heal_raid_member24;
            healornot[25] = UPaHBTSetting.Instance.Heal_raid_member25;
            healornot[26] = UPaHBTSetting.Instance.Heal_raid_member26;
            healornot[27] = UPaHBTSetting.Instance.Heal_raid_member27;
            healornot[28] = UPaHBTSetting.Instance.Heal_raid_member28;
            healornot[29] = UPaHBTSetting.Instance.Heal_raid_member29;
            healornot[30] = UPaHBTSetting.Instance.Heal_raid_member30;
            healornot[31] = UPaHBTSetting.Instance.Heal_raid_member31;
            healornot[32] = UPaHBTSetting.Instance.Heal_raid_member32;
            healornot[33] = UPaHBTSetting.Instance.Heal_raid_member33;
            healornot[34] = UPaHBTSetting.Instance.Heal_raid_member34;
            healornot[35] = UPaHBTSetting.Instance.Heal_raid_member35;
            healornot[36] = UPaHBTSetting.Instance.Heal_raid_member36;
            healornot[37] = UPaHBTSetting.Instance.Heal_raid_member37;
            healornot[38] = UPaHBTSetting.Instance.Heal_raid_member38;
            healornot[39] = UPaHBTSetting.Instance.Heal_raid_member39;

        }
        public void populate_heal_raid()
        {
            j = 0;
            for (i = 1; i < 41; i++)
            {
                if (OrganizedNames[i] != null && OrganizedNames[i].Count() > 2)
                {
                    _heal_raid_member[j++] = healornot[i];
                }
            }
            /*
            int j=0;
            for (i = 1; i < 41; i++)
            {
                if (
          //OrganizedNames[i] != null && OrganizedNames[i].Count()>2 && Raidsbugroup[i]!=10&& 
            healornot[i] == true)
                { _heal_raid_member[j++] = true; }
            }*/
        }
        public void Populate_organized_names()
        {
            for (i = 1; i <= 40; i++)
            {
                if (Raidsbugroup[i] == 1)
                {
                    OrganizedNames[SB1C] = NameorRM[i];
                    SB1C++;
                }
                else if (Raidsbugroup[i] == 2)
                {
                    OrganizedNames[SB2C + 5] = NameorRM[i];
                    SB2C++;
                }
                else if (Raidsbugroup[i] == 3)
                {
                    OrganizedNames[SB3C + 10] = NameorRM[i];
                    SB3C++;
                }
                else if (Raidsbugroup[i] == 4)
                {
                    OrganizedNames[SB4C + 15] = NameorRM[i];
                    SB4C++;
                }
                else if (Raidsbugroup[i] == 5)
                {
                    OrganizedNames[SB5C + 20] = NameorRM[i];
                    SB5C++;
                }
            }
        }
        public string Nameofraidmember(int index)
        {
            return Lua.GetReturnValues("return GetRaidRosterInfo('" + index + "')").First();
            /*
            if(unitcheck(Me) && Me.IsInRaid)
            {
                return Lua.GetReturnValues("return GetRaidRosterInfo('"+index+"')").First();
            }
            else
            {
                return "";
            }
             */
        }

        public int subgroupofraidmember(int index)
        {
            if (unitcheck(Me) && Me.IsInRaid && NameorRM[index] != null && NameorRM[index] != "")
            {
                return int.Parse(Lua.GetReturnValues("return GetRaidRosterInfo('" + index + "')")[2]);
            }
            else
            {
                return 10;
            }
        }
        
        public bool IsTank(WoWPlayer p)
        {
            if (p != null)
            {
                return Lua.GetReturnValues("return UnitGroupRolesAssigned('" + DeUnicodify(p.Name) + "')").First() == "TANK";
            }
            else return false;
        }

        private WoWPlayer tankfromfocus()
        //Code adapted from twistedintel Plugin
        {
            if (!_get_tank_from_focus) { return null; }
/*
            // Clear our focus variable (not our actual in-game focus)
            focus = null;
  
 */         // Get a list (should only return 1 value since our focus should only have 1 GUID) of our focus's GUID(s) 
            GUIDlist = Lua.GetReturnValues("return UnitGUID(\"focus\")");
            // Check to make sure we actually have a focus, then trim the 0x off of the front to prep it for conversion into a ulong
            if (GUIDlist != null && GUIDlist[0]!=null && GUIDlist[0].Count()>2)
            {
                FormattedGUID = GUIDlist[0].Substring(2);
            }
            else
            {
                return null;
            }
            // Try to convert the string into a ulong to use in the Object Manager, else catch the exceptions            
            try
            {
                FinalGuid = ulong.Parse(FormattedGUID, System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (FormatException)
            {
                Logging.Write(Color.Magenta, "Format Exception: {0}", FormattedGUID);
                return null;
            }
            catch (OverflowException)
            {
                Logging.Write(Color.Magenta, "Overflow!: {0}", FormattedGUID);
                return null;
            }
            // Search the Object Manager for a player with the formatted GUID
            return ObjectManager.GetAnyObjectByGuid<WoWPlayer>(FinalGuid);
            /*
            // Finally Check if we have a focus, make sure that they are in our party, and then check to make sure they are not already the tank, else return
            if (focus == null || !focus.IsInMyPartyOrRaid || (focus == RaFHelper.Leader && RaFHelper.Leader != null))
            {
                return null;
            }
            else if (focus != RaFHelper.Leader)
            {
                RaFHelper.SetLeader(focus.Guid);
                Logging.Write(Color.Teal, "Setting {0} as LazyRaider Tank", focus.Name);
            }
             */
        }

        private WoWPlayer focusunit()
        {
            GUIDlist = Lua.GetReturnValues("return UnitGUID(\"focus\")");
            // Check to make sure we actually have a focus, then trim the 0x off of the front to prep it for conversion into a ulong
            if (GUIDlist != null && GUIDlist[0] != null && GUIDlist[0].Count() > 2)
            {
                FormattedGUID = GUIDlist[0].Substring(2);
            }
            else
            {
                return null;
            }
            // Try to convert the string into a ulong to use in the Object Manager, else catch the exceptions            
            try
            {
                FinalGuid = ulong.Parse(FormattedGUID, System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (FormatException)
            {
                Logging.Write(Color.Magenta, "Format Exception: {0}", FormattedGUID);
                return null;
            }
            catch (OverflowException)
            {
                Logging.Write(Color.Magenta, "Overflow!: {0}", FormattedGUID);
                return null;
            }
            // Search the Object Manager for a player with the formatted GUID
            return ObjectManager.GetAnyObjectByGuid<WoWPlayer>(FinalGuid);
        }

        private WoWPlayer tankfromlua()
        {
            if (InRaid())
            {
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    if (IsTank(p))
                    {
                        return p;
                    }
                }
            }
            else
            {
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    if (IsTank(p))
                    {
                        return p;
                    }
                }
            }
            return null;
        }
        private string privacyname(WoWUnit unit)
        {
            string name;
            if (unit == Me)
            {
                return "Myself";
            }
            else if (unit == tank)
            {
                return "Tank " + tank.Name[0] + tank.Name[1] + "****";
            }
            else if (unit is WoWPlayer)
            {
                name = unit.Class.ToString() + " " + unit.Name[0] + unit.Name[1] + "****";
                return name;
            }
            else return unit.Name;
        }
        private string DeUnicodify(string s)
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            foreach (byte b in bytes)
            {
                if (b != 0)
                    sb.Append("\\" + b);
            }
            return sb.ToString();
        }
        private WoWPlayer GetTank()
        {
            luatank = tankfromlua();
            focustank = tankfromfocus();
            if (_get_tank_from_focus && unitcheck(focustank))
            {
                if (fallbacktank != focustank)
                {
                    slog(Color.DarkRed, "Selecting the tank {0} from our Focus Unit!", privacyname(focustank));
                }
                RaFHelper.SetLeader(focustank);
                fallbacktank = focustank;
                fallbacktankname = focustank.Name;
                return focustank;
            } 
            else if (unitcheck(RaFHelper.Leader))
            {
                if (fallbacktank != RaFHelper.Leader)
                {
                    slog(Color.DarkRed, "Selecting the tank {0} from LazyRaider!", privacyname(RaFHelper.Leader));
                }
                fallbacktank = RaFHelper.Leader;
                fallbacktankname = RaFHelper.Leader.Name;
                return RaFHelper.Leader;
            }
            else if (_get_tank_from_lua && unitcheck(luatank))
            {
                slog(Color.DarkRed, "You did not selected anyone as tank, also {0} as the Role of tank, selecting him as tank", privacyname(luatank));
                RaFHelper.SetLeader(luatank);
                fallbacktank = luatank;
                return luatank;
            }
            else if (unitcheck(fallbacktank))
            {
                slog(Color.DarkRed, "mm.. we are using the fallbacktank {0} not that good.. please report this", privacyname(fallbacktank));
                return fallbacktank;
            }
            else if (fallbacktankname != null && fallbackinparty() != null)
            {
                fallbacktank = fallbackinparty();
                slog(Color.DarkRed, "tank selected with his fallback name {0}", privacyname(fallbacktank));
                RaFHelper.SetLeader(fallbacktank);
                if (unitcheck(fallbacktank))
                {
                    return fallbacktank;
                }
                else
                {
                    if (unitcheck(Me))
                    {
                        if (usedBehaviour == "Raid" || usedBehaviour == "Dungeon")
                        {
                            slog(Color.DarkRed, "We are in dungeon or raid but no valid tank is found, i'm tanking and that's not good at all! Is tank dead? if not select a tank from lazyraider or perform a RoleCheck!");
                        }
                        return Me;
                    }
                    else
                    {
                        slog(Color.DarkRed, "Noone is a valid tank, even Myself, CC is in PAUSE");
                        return null;
                    }
                }
            }
            else
            {
                if (unitcheck(Me))
                {
                    if (usedBehaviour == "Raid" || usedBehaviour == "Dungeon")
                    {
                        slog(Color.DarkRed, "We are in dungeon or raid but no valid tank is found, i'm tanking and that's not good at all! Is tank dead? if not select a tank from lazyraider or perform a RoleCheck!");
                    }
                    return Me;
                }
                else
                {
                    slog(Color.DarkRed, "Noone is a valid tank, even Myself, CC is in PAUSE");
                    return null;
                }
            }
        }
        private WoWPlayer fallbackinparty()
        {
            foreach (WoWPlayer p in Me.PartyMembers)
            {
                if (unitcheck(p) && p.Name == fallbacktankname)
                {
                    return p;
                }
            }
            return null;
        }
        private WoWPlayer GetFortitudeTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where !GotBuff("Power Word: Fortitude", unit)
                    where !GotBuff("Commanding Shout",unit)
                    where !unit.HasAura("Qiraji Fortitude")
                    where !GotBuff("Qiraji Fortitude",unit)
                    where !GotBuff("Blood Pact",unit)
                    where !unit.HasAura("Blood Pact")
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetShadowProtectionTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where !GotBuff("Shadow Protection", unit)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetBlessTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where ((_should_king && !GotBuff("Blessing of Kings", unit) && !GotBuff("Mark of the Wild", unit)) || (!_should_king && !GotBuff("Blessing of Might", unit)))
                    select unit).FirstOrDefault();
        }
        //FIXME finire getblesstarget
        private WoWPlayer GetHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    //orderby (tank_healer)?(roleweighthealth(unit)):(unit.HealthPercent) ascending
                    orderby unit.HealthPercent ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where unit.HealthPercent < _do_not_heal_above
                    where !unit.Auras.ContainsKey("Bloodletting")
                    //where !unit.Auras.ContainsKey("Finkle\'s Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle\'s Mixture") && unit.CurrentHealth < 10000)
                    //FIXME the escape code is needed or not? mah!
                    //where !unit.Auras.ContainsKey("Finkle's Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle's Mixture") && unit.CurrentHealth < 10000)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetBindingHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.IsInMyPartyOrRaid
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where unit.HealthPercent < _binding_heal_min_hp
                    where !unit.Auras.ContainsKey("Bloodletting")
                    where unit!=Me
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetWhoHavePrayerOfMending()
        {
            return (from unit in NearbyFriendlyPlayers
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where (GotBuff("Prayer of Mending",unit) && unit.ActiveAuras["Prayer of Mending"].CreatorGuid==Me.Guid)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetTopOffNoRenewTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.InLineOfSight
                    where (unit.HealthPercent >_Heal_min_hp && unit.HealthPercent<_do_not_heal_above)
                    where !unit.Auras.ContainsKey("Bloodletting")
                    where (!GotBuff("Renew",unit) || unit==tank)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetWorldPVPHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where ((Me.IsAlliance && unit.IsAlliance)||(Me.IsHorde && unit.IsHorde))
                    where unit.Distance < _max_healing_distance
                    where unit.HealthPercent < _do_not_heal_above
                    where unit.InLineOfSight
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetBattlegroundHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.HealthPercent < _do_not_heal_above
                    where unit.InLineOfSight
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetSelectiveRaidHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.HealthPercent < _do_not_heal_above
                    where unit.InLineOfSight
                    where Hastobehealed(unit)
                    select unit).FirstOrDefault();
        }

        private bool Hastobehealed(WoWPlayer unit)
        {
            for (i = 0; i < Me.RaidMembers.Count; i++)
            {
                if (healornot[i] && unit.Name==WoWnames[i])
                {
                //    slog("unit has to be healer healornot {0} i {1}", healornot[i],i);
                    return true;
                }

            }
           // slog("unit {0} has NOT to be healed!",unit.Name);
            return false;
        }
        private WoWPlayer GetSpecialRaidHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    //orderby (tank_healer)?(roleweighthealth(unit)):(unit.HealthPercent) ascending
                    orderby unit.HealthPercent ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Distance < _max_healing_distance
                    where unit.HealthPercent < _do_not_heal_above
                    //where !unit.Auras.ContainsKey("Bloodletting")
                    where !unit.Auras.ContainsKey("Finkle\'s Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle\'s Mixture") && unit.CurrentHealth < 10000)
                    //FIXME the escape code is needed or not? mah!
                    where !unit.Auras.ContainsKey("Finkle's Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle's Mixture") && unit.CurrentHealth < 10000)
                    where unit.InLineOfSight
                    select unit).FirstOrDefault();

        }

        private WoWPlayer GetRaidHealTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    //orderby (tank_healer)?(roleweighthealth(unit)):(unit.HealthPercent) ascending
                    orderby unit.HealthPercent ascending
                    //where !Blacklist.Contains(unit.Guid, true)  //do not heal a blacklisted target
                    where !unit.Dead
                    where !unit.IsGhost
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.InLineOfSight
                    where unit.Distance < _max_healing_distance
                    where unit.HealthPercent < _do_not_heal_above
                    //where !unit.Auras.ContainsKey("Bloodletting")
                    //where !unit.Auras.ContainsKey("Finkle\'s Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle\'s Mixture") && unit.CurrentHealth < 10000)
                    //FIXME the escape code is needed or not? mah!
                    //where !unit.Auras.ContainsKey("Finkle's Mixture") || IsTank(unit) || (unit.Auras.ContainsKey("Finkle's Mixture") && unit.CurrentHealth < 10000)
                    select unit).FirstOrDefault();

        }
        private WoWUnit RAFGiveEnemy(int distance)
        {
            WoWUnit enemy;
            enemy = (from unit in NearbyUnFriendlyPlayers
                     where unitcheck(unit)
                             where unitcheck(tank) && (tank==Me || (unitcheck(tank.CurrentTarget) &&  tank.CurrentTarget==unit))
                             where !unit.IsPet
                             where unit.Distance < distance
                             where unit.InLineOfSight
                             select unit
                            ).FirstOrDefault();
            if (!unitcheck(enemy))
            {
                enemy = (from unit in NearbyUnfriendlyUnits
                         //where unit.IsHostile
                         //where !unit.Dead
                         where (tank != null && tank.CurrentTargetGuid == unit.Guid)
                         where !unit.IsPet
                         where unit.Distance < distance
                         where unit.InLineOfSight
                         select unit
                                ).FirstOrDefault();
            }
            return enemy;
        }
        private WoWUnit PVEGiveEnemy(int distance)
        {
            WoWUnit enemy = (from unit in NearbyUnfriendlyUnits

                             //where unit.IsHostile
                             //where !unit.Dead
                             where (tank != null && tank.CurrentTargetGuid == unit.Guid)
                             where !unit.IsPet
                             where unit.Distance < distance
                             where unit.InLineOfSight
                             select unit
                            ).FirstOrDefault();
            return enemy;
        }
        private WoWPlayer BattlegroundGiveEnemy(int distance)
        {
            WoWPlayer enemy = (from unit in NearbyUnFriendlyPlayers
                               orderby unit.HealthPercent ascending
                               where ((Me.IsAlliance && unit.IsHorde) || (Me.IsHorde && unit.IsAlliance))
                               where unit.InLineOfSight
                               select unit
                            ).FirstOrDefault();
            return enemy;
        }
        private WoWPlayer WorldPVPGiveEnemy(int distance)
        {
            WoWPlayer enemy = (from unit in NearbyUnFriendlyPlayers
                               orderby unit.HealthPercent ascending
                               where unit.InLineOfSight
                               select unit
                        ).FirstOrDefault();
            return enemy;
        
        }
        private WoWPlayer GetResTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance ascending
                    where !Blacklist.Contains(unit.Guid, true)  //do not ress a blacklisted target
                    where unit.Dead
                    where unit.IsInMyPartyOrRaid
                    where !unit.IsGhost
                    where !unit.IsMe
                    where unit.Distance < 100
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetCleanseTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 80
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.InLineOfSight
                    where NeedsCleanse(unit, _can_dispel_disease, _can_dispel_magic, _can_dispel_poison)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer PVPGetCleanseTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 80
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.InLineOfSight
                    where PVPNeedsCleanse(unit, _can_dispel_disease, _can_dispel_magic, _can_dispel_poison)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer PVPGetUrgentCleanseTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 80
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where PVPNeedsUrgentCleanse(unit, _can_dispel_disease, _can_dispel_magic, _can_dispel_poison)
                    select unit).FirstOrDefault();
        }
        private WoWPlayer GetHoFTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance descending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 30
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where NeddHoF(unit)
                    select unit).FirstOrDefault();
        }
        private bool NeddHoF(WoWPlayer p)
        {
            foreach (WoWAura a in p.Debuffs.Values)
                if (a.Name == "Entangling Roots" || a.Name == "Frost Nova" || a.Name == "Chains of Ice")
                {
                    return true;
                }
            return false;
        }
        private bool PVPNeedsUrgentCleanse(WoWPlayer p, bool can_dispel_disease, bool can_dispel_magic, bool can_dispel_poison)
        {
            /*if (p.ActiveAuras.ContainsKey("Unstable Affliction") || p.ActiveAuras.ContainsKey("Vampiric Touch") || p.ActiveAuras.ContainsKey("Flame Shock"))
            {
                return false;
            }*/
            foreach (WoWAura b in p.Debuffs.Values)
            {
                foreach (string s in _do_not_touch)
                {
                    if (b.Name == s)
                    {
                        return false;
                    }
                }
            }
            foreach (WoWAura a in p.Debuffs.Values)
            {
                foreach (string q in _dispell_ASAP)
                {
                    if (a.Name == q)
                    {
                        WoWDispelType t = a.Spell.DispelType;
                        //slog(Color.Orange, "There is a urgent buff to dispell!");
                        if ((can_dispel_disease && t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                        {
                            //                slog(Color.Orange, "And is dispellable");
                            urgentdebuff = a.Name;
                            return true;
                        }
                        //return true;
                    }
                }
            }
            /*
            foreach (WoWAura a in p.Debuffs.Values)
            {
                if (a.Name == "Fear" || a.Name == "Polymorph" || a.Name == "Freezing Trap" || a.Name == "Wyvern Sting" || a.Name == "Seduction" || a.Name == "Mind Control" || a.Name == "Repetance" || a.Name == "Psychic Scream"
                    || a.Name == "Hammer of Justice" || a.Name == "Intimidating Shout" || a.Name == "Howl of Terror" || a.Name == "Deep Freeze" || a.Name=="Ring of Frost" || a.Name =="Hungering Cold" || a.Name=="Repentance")
                {
                    WoWDispelType t = a.Spell.DispelType;
                    //slog(Color.Orange, "There is a urgent buff to dispell!");
                    if ((can_dispel_disease && t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                    {
                        //                slog(Color.Orange, "And is dispellable");
                        urgentdebuff = a.Name;
                        return true;
                    }
                    //return true;
                }
            }*/
            return false;
        }
        private WoWPlayer PVEGetUrgentCleanseTarget()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.HealthPercent ascending
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 80
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.InLineOfSight
                    where PVENeedsUrgentCleanse(unit, _can_dispel_disease,_can_dispel_magic,_can_dispel_poison)
                    select unit).FirstOrDefault();
        }


        private bool PVENeedsUrgentCleanse(WoWPlayer p, bool can_dispel_disease, bool can_dispel_magic, bool can_dispel_poison)
        {
            /*
            foreach (WoWAura a in p.Debuffs.Values)//p.ActiveAuras.Values)
            {
                if (a.Name == "Fear" || a.Name == "Static Cling" || a.Name == "Flame Shock" || a.Name == "Static Discharge" || a.Name == "Consuming Darkness" || a.Name == "Lash of Anguish" || a.Name == "Static Disruption"
                    || a.Name == "Accelerated Corruption")
                {
                    WoWDispelType t = a.Spell.DispelType;
                    if ((can_dispel_disease&& t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                    {
                        urgentdebuff = a.Name;
                        return true;
                    }
                }
            }*/
            foreach (WoWAura a in p.Debuffs.Values)
            {
                foreach (string q in _dispell_ASAP)
                {
                    if (a.Name == q)
                    {
                        WoWDispelType t = a.Spell.DispelType;
                        if ((can_dispel_disease && t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                        {
                            urgentdebuff = a.Name;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool NeedsCleanse(WoWPlayer p, bool can_dispel_disease, bool can_dispel_magic, bool can_dispel_poison)
        {
            //int dispelme;
            //dispelme = 0;
            /*if ((p.ActiveAuras.ContainsKey("Blackout")) || (p.ActiveAuras.ContainsKey("Toxic Torment")) || (p.ActiveAuras.ContainsKey("Frostburn Formula")) || (p.ActiveAuras.ContainsKey("Burning Blood")))
            {
                return false;
            }*/
            foreach (WoWAura b in p.Debuffs.Values)
            {
                foreach (string q in _do_not_touch)
                {
                    if (b.Name == q)
                    {
                        return false;
                    }
                }
            }
            foreach (WoWAura a in p.Debuffs.Values)
            {
                WoWDispelType t = a.Spell.DispelType;
                if ((can_dispel_disease && t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                {
                    //dispelme += 1;
                    return true;
                }
            }
            //if (dispelme > 0) { return true; } else { return false; }
            return false;
        }

        private bool PVPNeedsCleanse(WoWPlayer p, bool can_dispel_disease, bool can_dispel_magic, bool can_dispel_poison)
        {
            //int dispelme;
            //dispelme = 0;
            /*if (p.ActiveAuras.ContainsKey("Unstable Affliction") || p.ActiveAuras.ContainsKey("Vampiric Touch") || p.ActiveAuras.ContainsKey("Flame Shock"))
            {
                return false;
            }*/
            foreach (WoWAura b in p.Debuffs.Values)
            {
                foreach (string s in _do_not_touch)
                {
                    if (b.Name == s)
                    {
                        return false;
                    }
                }
            }
            foreach (WoWAura a in p.Debuffs.Values)
            {
                WoWDispelType t = a.Spell.DispelType;
                if ((can_dispel_disease && t == WoWDispelType.Disease) || (can_dispel_magic && t == WoWDispelType.Magic) || (can_dispel_poison && t == WoWDispelType.Poison))
                {
                    //dispelme += 1;
                    return true;
                }
            }
            //if (dispelme > 0) { return true; } else { return false; }
            return false;
        }

        private WoWUnit GiveEnemyPet(int distance)
        {
            WoWUnit enemy = (from unit in NearbyUnfriendlyUnits
                             orderby unit.Distance descending
                             where unitcheck(unit)
                             where unit.Distance < distance
                             where unit.InLineOfSight
                             where unit.IsPet
                             where unit.IsTargetingMyPartyMember
                             select unit
                            ).FirstOrDefault();
            return enemy;
        }
        /*
        private IEnumerable<WoWPlayer> GetResurrectTargets()
        {
            return (from unit in NearbyFriendlyPlayers
                    orderby unit.Distance ascending
                    where unit.Dead
                    where unit.IsInMyPartyOrRaid
                    where !unit.IsGhost
                    where !unit.IsMe
                    where unit.Distance < 100
                    select unit);
        }
         */
        private bool CanCast(string spell, WoWUnit target)
        {
            return SpellManager.HasSpell(spell) && SpellManager.CanCast(WoWSpell.FromId(SpellManager.Spells[spell].Id), target, false, false, _account_for_lag);
        }

        private bool CanCast(string spell)
        {
            return SpellManager.HasSpell(spell) && SpellManager.CanCast(WoWSpell.FromId(SpellManager.Spells[spell].Id), Me, false, false, _account_for_lag);
        }

        private bool Cast(string spell, WoWUnit target, string reason)
        {
            return internal_Cast(spell, target, 80, "Default", reason);
        }
        private bool Cast(string spell, string type, string reason)
        {
            return internal_Cast(spell, Me, 5, type, reason);
        }
        private bool Cast(string spell, WoWUnit target, string type, string reason)
        {
            return internal_Cast(spell, target, 80, type, reason);
        }
        private bool Cast(string spell, string reason)
        {
            return internal_Cast(spell, Me, 5, "Default", reason);
        }

        private bool Cast(string spell, WoWUnit target, int max_distance, string reason)
        {
            return internal_Cast(spell, target, max_distance, "Default", reason);
        }

        private bool Cast(string spell, WoWUnit target, int max_distance, string type, string reason)
        {
            return internal_Cast(spell, target, max_distance, type, reason);
        }

        private bool castaress(string spell, WoWUnit target, int max_distance, string type, string reason)
        {
            if (target != null && target.IsValid && target.Dead && target.InLineOfSight && (target.Distance < (double)max_distance || (max_distance==5 && target.IsWithinMeleeRange)) && CanCast(spell, target))
            {
                lastCast = target;
                formatslog(type, reason, spell, target);
                return SpellManager.Cast(spell, target);
            }
            else if (target != null && target.IsValid && target.Dead && target.InLineOfSight && target.Distance > (double)max_distance)
            {
                slog(Color.DarkRed, reason + ": but Target {0} at {1} max spell distance {2} Out of range! Move to him?", privacyname(target), Round(target.Distance), max_distance);
                //Blacklist.Add(target, new TimeSpan(0, 0, 5));
                return false;
            }
            else if (target != null && target.IsValid && target.Dead && !target.InLineOfSight)
            {
                slog(Color.DarkRed, reason + ": but Target {0} at {1} max spell distance {2} Out of LoS! Move to him?", privacyname(target), Round(target.Distance), max_distance);
                //Blacklist.Add(target, new TimeSpan(0, 0, 5));
                return false;

            }
            return false;
        }

        private bool internal_Cast(string spell, WoWUnit target, int max_distance, string type, string reason)
        {
            //LastSpellCast = spell;
            LastSpell = spell;
            if (target != null && target.IsValid && !target.Dead && target.InLineOfSight && (target.Distance < (double)max_distance || (max_distance == 5 && target.IsWithinMeleeRange)) && CanCast(spell, target))
            {
                if (target == Me)
                {
                    lastCast = null;
                    formatslog(type, reason, spell);
                    return SpellManager.Cast(spell, Me);
                }
                else
                {
                    lastCast = target;
                    formatslog(type, reason, spell, target);
                    return SpellManager.Cast(spell, target);
                }
            }
            else if (target != null && target.IsValid && !target.Dead && target.InLineOfSight && target.Distance > (double)max_distance)
            {
                slog(Color.DarkRed, reason + ": but Target {0} at {1} max spell distance {2} Out of range! Move to him?", privacyname(target), Round(target.Distance), max_distance);
                //Blacklist.Add(target, new TimeSpan(0, 0, 5));
                return false;
            }
            else if (target != null && target.IsValid && !target.Dead && !target.InLineOfSight)
            {
                slog(Color.DarkRed, reason + ": but Target {0} at {1} max spell distance {2} Out of LoS! Move to him?", privacyname(target), Round(target.Distance), max_distance);
                //Blacklist.Add(target, new TimeSpan(0, 0, 5));
                return false;

            }
            if(IsCrowdControlled(Me))
            {
                slog("I'm Under CrowdControll, cannot cast!");
            }
            else
            {
            slog(Color.DarkRed,"REPORTME! I was tryng to cast {0} on {1} at hp {2} BUT an ERROR occured and I do not know what error!", spell, privacyname(target), target.HealthPercent);
            }
            return false;
        }

        private void formatslog(string type, string reason, string spell, WoWUnit target)
        {
            System.Drawing.Color textcolor;
            switch (type)
            {
                case "Heal":
                    textcolor = Color.Green;
                    break;
                case "Cleanse":
                    textcolor = Color.Magenta;
                    break;
                case "Buff":
                    textcolor = Color.Brown;
                    break;
                case "OhShit":
                    textcolor = Color.Red;
                    break;
                case "Mana":
                    textcolor = Color.Blue;
                    break;
                case "DPS":
                    textcolor = Color.Violet;
                    break;
                case "Utility":
                    textcolor = Color.Orange;
                    break;
                default:
                    textcolor = Color.Black;
                    break;
            }
            slog(textcolor, reason + ": casting {0} on {1} at distance {2} with type {3} at hp {4}", spell, privacyname(target), Round(target.Distance), type, Round(target.HealthPercent));
        }

        private void formatslog(string type, string reason, string spell)
        {
            formatslog(type, reason, spell, Me);
        }
        private bool GotBuff(string name)
        {
            return GotBuff(name, Me);
        }

        private bool GotBuff(string name, WoWUnit u)
        {
            return u.ActiveAuras.ContainsKey(name);
            
        }

        private double Round(double d)
        {
            return Math.Round(d, 2);
        }

        private bool MoveTo(WoWUnit u)
        {
            if (rng == null)
            {
                rng = new Random();
            }
            if (SpellManager.HasSpell("Holy Radiance") && ! SpellManager.Spells["Holy Radiance"].Cooldown && !Me.Mounted) { Cast("Holy Radiance", "Buff", "Moving faaaaaaaaaaster"); }
            if (!Me.IsMoving && u != null)//&& u.Distance > 35)
            {
                Navigator.MoveTo(WoWMathHelper.CalculatePointAtSide(u.Location, u.Rotation, rng.Next(10), rng.Next(2) == 1));
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool BeaconNeedsRefresh(WoWUnit u)
        {
            if (GotBuff("Beacon of Light", u) && u.Distance < 40)
            {
                return u.ActiveAuras["Beacon of Light"].TimeLeft.TotalSeconds <= 5;
            }
            else
            {
                return true;
            }
        }

        private bool SealNeedRefresh()
        {
            if (SpellManager.HasSpell("Seal of Insight") && GotBuff("Seal of Insight", Me))
            {
                return Me.ActiveAuras["Seal of Insight"].TimeLeft.TotalSeconds <= 5;
            }
            else if (SpellManager.HasSpell("Seal of Insight") && !GotBuff("Seal of Insight", Me))
            {
                return true;
            }
            else if(!SpellManager.HasSpell("Seal of Insight") && SpellManager.HasSpell("Seal of Righteousness") && !GotBuff("Seal of Righteousness"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private double LoD()
        {
            double LoDcounter;
            int LoDmax, i, LoDnot, maxAOEh;
            double[] LoDeffect = new double[41];
            LoDcounter = 0;
            LoDmax = 0;
            LoDnot = 0;
            if (tar.HealthPercent >= 60)
            {
                maxAOEh = 95;
            }
            else
            {
                maxAOEh = (int)maxAOEhealth;
            }
            foreach (int cont in LoDeffect)
            {
                LoDeffect[cont] = 0;
            }
            i = 0;
            if (InRaid())
            {
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    i++;
                    if (unitcheck(p) && (
                        (p.Distance <= 5 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 150)) ||
                        (p.Distance <= 10 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 90)) ||
                        (p.Distance <= 15 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 53)) ||
                        (p.Distance <= 20 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 36)) ||
                        (p.Distance <= 25 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 28)) ||
                        (p.Distance <= 30 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 22))
                    ))
                    {
                        if (p == tank && p != Me)
                        {
                            LoDcounter++;
                            LoDeffect[i] = 1;
                        }
                        else if (p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            {
                                LoDcounter++;
                                LoDeffect[i] = 1;
                            }
                            else
                            {
                                LoDcounter += 1.5;
                                LoDeffect[i] = 1.5;
                            }
                        }
                        LoDmax++;
                        slog("{0} is in range and have {1} hp", privacyname(p), p.HealthPercent);
                        //    if (LoDmax == 5) { slog(Color.DarkRed, "Light of Dawn weight is {0} there are {1} injuried players in range", LoDcounter, LoDmax); return LoDcounter; }
                    }
                }
                if (Me.HealthPercent < dontHealAbove)
                {
                    if (tank.HealthPercent < dontHealAbove)
                    {
                        if (Me != tank)
                        {
                            LoDmax++;
                            LoDeffect[0] = 1.5;
                        }
                        else
                        {
                            LoDmax++;
                            LoDeffect[0] = 1;
                        }
                    }
                    else
                    {
                        LoDeffect[0] = 1;
                    }
                    slog("{0} is in range and have {1} hp", privacyname(Me), Me.HealthPercent);
                    LoDmax++;
                }
                else
                {
                    if (tank.HealthPercent < dontHealAbove)
                    {
                        LoDmax++;
                        LoDeffect[0] = 0.5;
                    }
                    slog("{0} is in range and have {1} hp (overhealing)", privacyname(Me), Me.HealthPercent);
                    LoDnot++;
                }
                i = 0;
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    i++;
                    if (unitcheck(p) && (
                        (p.Distance <= 5 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 150)) ||
                        (p.Distance <= 10 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 90)) ||
                        (p.Distance <= 15 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 53)) ||
                        (p.Distance <= 20 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 36)) ||
                        (p.Distance <= 25 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 28)) ||
                        (p.Distance <= 30 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 22))
                    ))
                    {
                        if (p == tank && p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            { }
                            else
                            {
                                LoDcounter++;
                                LoDeffect[i] = 1;
                            }
                        }
                        else if (p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            { }
                            else
                            {
                                LoDcounter += 0.5;
                                LoDeffect[i] = 0.5;
                            }
                        }
                        slog("{0} is in range and have {1} hp (overhealing)", privacyname(p), p.HealthPercent);
                        LoDnot++;
                        //if (LoDmax == 5) { slog(Color.DarkRed, "Light of Dawn weight is {0} there are {1} injuried players in range", LoDcounter, LoDmax); return LoDcounter; }
                    }
                }
                //if (Me.HealthPercent > maxAOEhealth && tank.HealthPercent < dontHealAbove) { LoDcounter += 0.5; LoDmax++; }
            }
            else if (InParty())
            {
                i = 0;
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    i++;
                    if (unitcheck(p) && (
                        (p.Distance <= 5 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 150)) ||
                        (p.Distance <= 10 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 90)) ||
                        (p.Distance <= 15 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 53)) ||
                        (p.Distance <= 20 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 36)) ||
                        (p.Distance <= 25 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 28)) ||
                        (p.Distance <= 30 && p.HealthPercent <= maxAOEh && Me.IsSafelyFacing(p, 22))
                    ))
                    {
                        if (p == tank && p != Me)
                        {
                            LoDcounter++;
                            LoDeffect[i] = 1;
                        }
                        else if (p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            {
                                LoDcounter += 1;
                                LoDeffect[i] = 1;
                            }
                            else
                            {
                                LoDcounter += 1.5;
                                LoDeffect[i] = 1.5;
                            }
                        }
                        slog("{0} is in range and have {1} hp", privacyname(p), p.HealthPercent);
                        LoDmax++;
                    }
                }
                //if (Me.HealthPercent <= maxAOEhealth && tank.HealthPercent < dontHealAbove) { LoDcounter += 1.5; LoDmax++; } else if (Me.HealthPercent <= maxAOEhealth) { LoDcounter++; LoDmax++; }

                if (Me.HealthPercent < dontHealAbove)
                {
                    if (tank.HealthPercent < dontHealAbove)
                    {
                        if (tank != Me)
                        {
                            LoDmax++;
                            LoDeffect[0] = 1.5;
                        }
                        else
                        {
                            LoDmax++;
                            LoDeffect[0] = 1;
                        }
                    }
                    else
                    {
                        LoDeffect[0] = 1;
                    }
                    LoDmax++;
                    slog("{0} is in range and have {1} hp", privacyname(Me), Me.HealthPercent);
                }
                else
                {
                    if (tank.HealthPercent < dontHealAbove)
                    {
                        LoDmax++;
                        LoDeffect[0] = 0.5;
                    }
                    slog("{0} is in range and have {1} hp", privacyname(Me), Me.HealthPercent);
                    LoDnot++;
                }
                i = 0;
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    i++;
                    if (unitcheck(p) && (
                        (p.Distance <= 5 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 150)) ||
                        (p.Distance <= 10 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 90)) ||
                        (p.Distance <= 15 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 53)) ||
                        (p.Distance <= 20 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 36)) ||
                        (p.Distance <= 25 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 28)) ||
                        (p.Distance <= 30 && p.HealthPercent > maxAOEh && Me.IsSafelyFacing(p, 22))
                    ))
                    {
                        if (p == tank && p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            { }
                            else
                            {
                                LoDcounter++;
                                LoDeffect[i] = 1;
                            }
                        }
                        else if (p != Me)
                        {
                            if (tank.HealthPercent > dontHealAbove)
                            { }
                            else
                            {
                                LoDcounter += 0.5;
                                LoDeffect[i] = 0.5;
                            }
                        }
                        slog("{0} is in range and have {1} hp (overhealing)", privacyname(p), p.HealthPercent);
                        LoDnot++;
                    }
                }
                //  if (Me.HealthPercent > maxAOEhealth && tank.HealthPercent > dontHealAbove) { } else { LoDcounter += 0.5; LoDmax++; }
            }
            // slog(Color.DarkRed, "Light of Dawn weight is {0} there are {1} injuried players in range", LoDcounter, LoDmax);
            //    return LoDcounter;
            i = 0;
            Array.Sort(LoDeffect);
            Array.Reverse(LoDeffect);
            double sum;
            sum = 0;
            for (int con = 0; con < 5; con++)
            {
                sum += LoDeffect[con];
            }
            if (Global_debug)
            {
                for (int cos = 0; cos < LoDeffect.Length; cos++)
                {
                    slog("{0}    {1}", cos, LoDeffect[cos]);
                }
            }
            slog(Color.DarkRed, "Light of Dawn weight is {0} there are {1} injuried players and {2} not injurie player in range", sum, LoDmax, LoDnot);
            return sum;
        }

        private double WoG()
        {
            double WoGcounter;
            WoGcounter = 0;
            if (tar.Guid == tank.Guid)
            {
                WoGcounter = 1;
            }
            else if (tar.Guid != Me.Guid)
            {
                WoGcounter = 1.5;
            }
            else
            {
                WoGcounter = 1.17;
            }
            slog(Color.DarkOliveGreen, "Wog weight is {0} modified {1}", WoGcounter, WoGcounter * 2.37 * 1.3);
            return WoGcounter;
        }

        private bool HolyPowerDump()
        {
            double last_word_modifier;
            if (Me.CurrentHolyPower < 3)
            {
                return false;
            }
            if (tar.HealthPercent <= 35)
            {
                last_word_modifier = 1 * (1 - Talent_last_word * 0.3) + 1.5 * (Talent_last_word * 0.3);
            }
            else
            {
                last_word_modifier = 1;
            }

            if (WoG() * 2.37 * 1.3 * last_word_modifier > LoD())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool ShouldHolyRadiance(int how_many, int how_far, int how_much_health)
        {
            int counter;
            counter = 0;
            if (!SpellManager.HasSpell("Holy Radiance") || SpellManager.Spells["Holy Radiance"].Cooldown)
            {
                return false;
            }
            if (InRaid())
            {
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    if (unitcheck(p) && p.Distance < how_far && p.HealthPercent < how_much_health && (!p.Auras.ContainsKey("Finkle\'s Mixture") || (p.Auras.ContainsKey("Finkle\'s Mixture") && p.CurrentHealth < 10000)))
                    {
                        counter++;
                    }
                }
            }
            else
            {
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    if (unitcheck(p) && p.Distance < how_far && p.HealthPercent < how_much_health)
                    {
                        counter++;
                    }
                }
                if (Me.HealthPercent < how_much_health) { counter++; }
            }
            //slog(Color.DarkRed,"there are {0} injuried unit in  yard", counter);
            if (counter >= how_many)
            {
                slog(Color.DarkRed, "Holy Radiacen: there are {0} injuried unit in {1} yard", counter, how_far);
                /*
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.Name, Round(Me.Distance), Round(Me.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember1.Name, Round(Me.PartyMember1.Distance), Round(Me.PartyMember1.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember2.Name, Round(Me.PartyMember2.Distance), Round(Me.PartyMember2.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember3.Name, Round(Me.PartyMember3.Distance), Round(Me.PartyMember3.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember4.Name, Round(Me.PartyMember4.Distance), Round(Me.PartyMember4.HealthPercent));
                */
                return true;
            }
            return false;
        }
        private bool IsPaladinAura(string aura)
        {
            return Me.HasAura(aura);
            //string s = Lua.GetReturnVal<string>("return UnitAura(\"player\", \"" + aura + "\")", 0);
            //return s != null;
        }

        private bool ShouldKing()
        {
            //slog("calling should king");
            if (InParty())
            {
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    if (p.Class == WoWClass.Druid) { return false; }
                    else if ((p.Class == WoWClass.Paladin) && (p.Guid != Me.Guid))
                    {
                        if (GotBuff("Blessing of Kings", p) && !GotBuff("Blessing of Might", p) && p.ActiveAuras["Blessing of Kings"].CreatorGuid != Me.Guid)
                        {
                            return false;
                        }
                    }
                }
                if (GotBuff("Blessing of Kings", Me) && !GotBuff("Blessing of Might", Me) && Me.ActiveAuras["Blessing of Kings"].CreatorGuid != Me.Guid)
                {
                    return false;
                }
                return true;
            }
            else if (InRaid())
            {
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    if (p.Class == WoWClass.Druid && usedBehaviour != "Battleground") { return false; }
                    else if ((p.Class == WoWClass.Paladin) && (p.Guid != Me.Guid) && usedBehaviour != "Battleground")
                    {
                        if (GotBuff("Blessing of Kings", p) && !GotBuff("Blessing of Might", p) && p.ActiveAuras["Blessing of Kings"].CreatorGuid != Me.Guid)
                        {
                            return false;
                        }
                    }
                }
                if (GotBuff("Blessing of Kings", Me) && !GotBuff("Blessing of Might", Me) && Me.ActiveAuras["Blessing of Kings"].CreatorGuid != Me.Guid)
                {
                    return false;
                }
                return true;
            }
            else
            {
                if (GotBuff("Blessing of Kings", Me) && !GotBuff("Blessing of Might", Me) && Me.ActiveAuras["Blessing of Kings"].CreatorGuid != Me.Guid)
                {
                    return false;
                }
                return true;
            }
        }

        private WoWUnit SoloGiveEnemy(int distance)
        {
            WoWUnit enemy=null;
            if (_answer_PVP_attacks)
            {
                enemy=(from unit in NearbyUnFriendlyPlayers
                       orderby unit.HealthPercent ascending
                       where ((Me.IsAlliance && unit.IsHorde) || (Me.IsHorde && unit.IsAlliance))
                       where unit.CurrentTarget == Me || (unit.Combat && Me.CurrentTarget == unit)
                       where unit.InLineOfSight
                       select unit
                            ).FirstOrDefault();
            }
            if (!unitcheck(enemy))
            {
                enemy = (from unit in NearbyUnfriendlyUnits
                         orderby unit.HealthPercent ascending
                         //where unit.IsHostile
                         //where unit.CurrentTarget == Me
                         //where !unit.Dead
                         where unit.Distance < distance
                         where unit.CurrentTarget == Me || (unit.Combat && Me.CurrentTarget == unit)
                         where !unit.IsPet
                         select unit
                            ).FirstOrDefault();
            }
            return enemy;
        }

        private bool Decice_if_special_or_normal_raid()
        {
            if (Me.Auras.ContainsKey("Finkle\'s Mixture") || Me.Auras.ContainsKey("Finkle's Mixture"))
            {
                Global_chimaeron = true;
                Global_chimaeron_p1 = true;
            }
            else if (!Me.Combat)
            {
                Global_chimaeron = false;
                Global_chimaeron_p1 = false;
            }
            else
            {
                Global_chimaeron_p1 = false;
            }
            if (_general_raid_healer==1)
            { 
                _raid_healer = true; 
            }
            else
            { 
                _raid_healer = false; 
            }
            return true;
        }
        private bool Check_special_healing()
        {
            if (!select_heal_watch.IsRunning)
            {
                select_heal_watch.Start();
            }
            if (select_heal_watch.Elapsed.TotalSeconds > 10)
            {
                for (int i = 0; i < Me.RaidMembers.Count; i++)
                {
                    if (Me.RaidMembers[i] != null && Me.RaidMembers[i].IsValid)
                    {
                        //slog("index {0} unit {1} {2} (organized name {3} )will be healed {4} (hp: {5})", i, Me.RaidMembers[i].Class, privacyname(Me.RaidMembers[i]), OrganizedNames[i + 1], /*_heal_raid_member[i]*/ healornot[i], Round(Me.RaidMembers[i].HealthPercent));
                        slog("index {0} unit {1} {2} (organized name {3} )will be healed {4} (hp: {5})", i, Me.RaidMembers[i].Class, privacyname(Me.RaidMembers[i]), WoWnames[i], healornot[i], Round(Me.RaidMembers[i].HealthPercent));
                    }
                }
                select_heal_watch.Reset();
                //specialhealing_warning = true;
                return true;
            }
            else { 
                return false; 
            }
        }
        public bool Inizialize_raid_names()
        {
            //RaidNames.Clear();
            for (i = 0; i < 40; i++)
            {
                if (unitcheck(Me) && Me.IsInRaid && i<Me.RaidMembers.Count && unitcheck(Me.RaidMembers[i]))
                {
                    RaidNames[i]=Me.RaidMembers[i].Name;
                }
                else
                {
                    RaidNames[i]="";

                }
            }
            /*for (i = 0; i < 40; i++)
            {
                slog("i {0} raidname {1} name {2} check {3}", i,RaidNames[i],(i<Me.RaidMembers.Count)?( Me.RaidMembers[i].Name):(""), (i<Me.RaidMembers.Count)?(unitcheck(Me.RaidMembers[i]).ToString()):("unità finite"));
            }*/
            return true;
        }
        public bool Inizialize_raid_role()
        {
            for (i = 0; i < 40; i++)
            {
                for(indext=0;indext<40;indext++)
                {
                    if (WoWnames[indext] == Me.RaidMembers[i].Name)
                    {
                        if (unitcheck(Me) && Me.IsInRaid && i < Me.RaidMembers.Count && unitcheck(Me.RaidMembers[i]))
                        {
                            Raidrole[indext] = IsTank(Me.RaidMembers[i]);
                        }
                        else
                        {
                            Raidrole[indext] = false;

                        }
                    }
                }
                
            }
            return true;
        }
        private bool Select_composite(out Composite composite)
        {
            switch (usedBehaviour)
            {
                case "Solo":
                    composite = Composite_Solo();
                    break;
                case "Party or Raid":
                    composite = Composite_Party_or_Raid();
                    break;
                case "Arena":
                    composite = Composite_Arena();
                    break;
                case "World PVP":
                    composite = Composite_WorldPVP();
                    break;
                case "Battleground":
                    composite = Composite_Battleground();
                    break;
                case "Dungeon":
                    composite = Composite_Dungeon();
                    break;
                case "Raid":
                    composite = Composite_Raid();
                    break;
                default:
                    slog("No good behaviour found, the CC will stop now!");
                    composite = null;
                    return false;
            }
            return true;
        }
        private bool Select_rest_composite(out Composite composite)
        {
            switch (usedBehaviour)
            {
                case "Solo":
                    composite = Composite_SoloRest();
                    break;
                case "Party or Raid":
                    composite = Composite_PVERest();
                    break;
                case "Arena":
                    composite = Composite_PVPRest();
                    break;
                case "World PVP":
                    composite = Composite_PVPRest();
                    break;
                case "Battleground":
                    composite = Composite_PVPRest();
                    break;
                case "Dungeon":
                    composite = Composite_PVERest();
                    break;
                case "Raid":
                    composite = Composite_PVERest();
                    break;
                default:
                    slog("No good rest behaviour found, the CC will stop now!");
                    composite = null;
                    return false;
            }
            return true;

        }
        public void Inizialize_variable_for_GUI()
        {
            Inizialize_raid_names();
            //Inizialize_raid_role();
            Inizialize_raidorder();
            populate_nameofRM();
            populate_raidsubg();
            Inizialize_raidorder();
            Populate_organized_names();
            populate_heal_or_not();
            //populate_heal_raid();
        }
        public static uint GetAuraStackCount(WoWUnit unit, string auraName)
        {
            uint stackCount = 0;
            bool isPresent = IsAuraPresent(unit, auraName, out stackCount);
            return stackCount;
        }

        public static bool IsAuraPresent(WoWUnit unit, string sAura, out uint stackCount)
        {
            stackCount = 0;
            if (unit == null) { return false; }

            WoWAura aura = GetAura(unit, sAura);
            if (aura == null) { return false; }

            stackCount = aura.StackCount;
            return true;
        }

        public static WoWAura GetAura(WoWUnit unit, string auraName)
        {
            if (unit == null) { return null; }

            WoWAura aura = (from a in unit.Auras
                            where 0 == string.Compare(a.Value.Name, auraName, true)
                            select a.Value).FirstOrDefault();
            
            return aura;
        }

        private bool Should_AOE(WoWPlayer center, string spell, int how_many, int distance, int how_much_health)
        {
            if (!SpellManager.HasSpell(spell) || SpellManager.Spells[spell].Cooldown)
            {
                return false;
            }
            if (How_Many_Inside_AOE(center, distance, how_much_health) >= how_many)
            {
                return true;
            }
            else 
            { 
                return false; 
            }
        }
        private bool Should_AOE(WoWPlayer center, int how_many, int distance, int how_much_health)
        {
            if (How_Many_Inside_AOE(center, distance, how_much_health) >= how_many)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool Should_PoH(WoWPlayer center, int how_many, int distance, int how_much_health, int subgroup)
        {
            if (How_Many_Inside_AOE(center, distance, how_much_health,subgroup) >= how_many)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool Should_AOE(string spell, int how_many, int distance, int how_much_health, out WoWPlayer target)
        {
            if (!SpellManager.HasSpell(spell) || SpellManager.Spells[spell].Cooldown)
            {
                target = null;
                return false;
            }
            i = 0;
            foreach (WoWPlayer p in NearbyFriendlyPlayers)
            {
                if (i < 41 && unitcheck(p) && p.Distance<_max_healing_distance && p.InLineOfSight)
                {
                    check_aoe[i] = How_Many_Inside_AOE(p, distance, how_much_health);
                    i++;
                }
            }
            if (check_aoe.Max() < how_many)
            {
                target = null;
                return false;
            }
            else
            {
                for (i = 0; i < check_aoe.Length; i++)
                {
                    if (check_aoe.Max() == check_aoe[i])
                    {
                        slog("Found a center for spell {0}! {1} units will be healied!", spell, check_aoe.Max());
                        target = NearbyFriendlyPlayers[i];
                        return true;
                    }
                }
                target = null;
                return false;
            }
        }

        private bool Should_AOE(int how_many, int distance, int how_much_health, out WoWPlayer target)
        {
            i = 0;
            foreach (WoWPlayer p in NearbyFriendlyPlayers)
            {
                if (i < 41 && unitcheck(p) && p.Distance < _max_healing_distance && p.InLineOfSight)
                {
                    check_aoe[i] = How_Many_Inside_AOE(p, distance, how_much_health);
                    i++;
                }
            }
            if (check_aoe.Max() < how_many)
            {
                target = null;
                return false;
            }
            else
            {
                for (i = 0; i < check_aoe.Length; i++)
                {
                    if (check_aoe.Max() == check_aoe[i])
                    {
                        slog("Found a center! {0} units will be healied!", check_aoe.Max());
                        target = NearbyFriendlyPlayers[i];
                        return true;
                    }
                }
                target = null;
                return false;
            }
        }
        private int How_Many_Inside_AOE(WoWPlayer center, int distance, int how_much_health)
        {
            return (from unit in NearbyFarFriendlyPlayers
                            where unitcheck(unit)
                            where (unit.IsInMyPartyOrRaid || unit.IsMe)
                            where unit.Location.Distance(center.Location) < distance
                            where unit.HealthPercent < how_much_health
                            select unit).Count();
        }
        private int How_Many_Inside_AOE(WoWPlayer center, int distance, int how_much_health, int subgroup)
        {
            return (from unit in NearbyFarFriendlyPlayers
                    where unitcheck(unit)
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where unit.Location.Distance(center.Location) < distance
                    where unit.HealthPercent < how_much_health
                    where SameSubgroup(unit.Name,subgroup)
                    select unit).Count();
        }
        private bool SameSubgroup(string name, int subgroup)
        {
            for (counti = 0; counti < Me.RaidMembers.Count; counti++)
            {
                if (subgroup== Subgroup[counti] && name == WoWnames[counti])
                {
                    //    slog("unit has to be healer healornot {0} i {1}", healornot[i],i);
                    return true;
                }

            }
            // slog("unit {0} has NOT to be healed!",unit.Name);
            return false;
        }
        private int SubgroupFromName(string name)
        {
            for (countj = 0; countj < Me.RaidMembers.Count; countj++)
            {
                if (WoWnames[countj] == name)
                {
                    return countj;
                }
            }
            return -1;
        }
        /*
        private bool ShouldAOE(WoWPlayer center, string spell, int how_many, int distance, int how_much_health)
        {
            int counter;
            counter = 0;
            if (!SpellManager.HasSpell(spell) || SpellManager.Spells[spell].Cooldown)
            {
                return false;
            }
            if (InRaid())
            {
                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    if (unitcheck(p) && p.Location.Distance(center.Location) < distance && p.HealthPercent < how_much_health && (!p.Auras.ContainsKey("Finkle\'s Mixture") || (p.Auras.ContainsKey("Finkle\'s Mixture") && p.CurrentHealth < 10000)))
                    {
                        counter++;
                    }
                }
            }
            else
            {
                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    if (unitcheck(p) && p.Location.Distance(center.Location) < distance && p.HealthPercent < how_much_health)
                    {
                        counter++;
                    }
                }
                if (unitcheck(Me) && Me.Location.Distance(center.Location) < distance && Me.HealthPercent < how_much_health) { counter++; }
            }
            //slog(Color.DarkRed,"there are {0} injuried unit in  yard", counter);
            if (counter >= how_many)
            {
                slog(Color.DarkRed, "{0}: there are {1} injuried unit in {2} yard from {3}", spell, counter, distance, privacyname(center));
                /*
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.Name, Round(Me.Distance), Round(Me.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember1.Name, Round(Me.PartyMember1.Distance), Round(Me.PartyMember1.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember2.Name, Round(Me.PartyMember2.Distance), Round(Me.PartyMember2.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember3.Name, Round(Me.PartyMember3.Distance), Round(Me.PartyMember3.HealthPercent));
                slog(Color.DarkRed, "Player {0} discance {1} life {2} %", Me.PartyMember4.Name, Round(Me.PartyMember4.Distance), Round(Me.PartyMember4.HealthPercent));
                */
        /*
      return true;
  }
  return false;
}
*/
        private bool TankGotTarget()
        {
            return unitcheck(tank.CurrentTarget) && !tank.CurrentTarget.IsFriendly;
        }
        private bool IsEnemyAttakingMe()
        {
            foreach (WoWUnit unit in NearbyUnfriendlyUnits)
            {
                if (unit.CurrentTarget == Me && (unit.Distance < 10 || unit.IsCasting))
                {
                    return true;
                }
            }
            return false;
            
        }

        private bool Confrontname(string firstname, string secondname)
        {
            for (indexj = 0; indexj < firstname.Length; indexj++)
            {
                if (firstname[indexj] != secondname[indexj])
                {
                    return false;
                }
            }
            return true;
        }

        private int FindWoWIndexFromName(string name)
        {
            slog("searching {0}", name);
            for (index = 0; index < Me.RaidMembers.Count /*int.Parse(Lua.GetReturnValues("return GetNumRaidMembers()").FirstOrDefault())*/; index++)
            {
                supportname = WoWnames[index];
                //supportname = Lua.GetReturnValues("return GetRaidRosterInfo('" + index + "')")[0].ToString();
                //Subgroup[i] = int.Parse(Lua.GetReturnValues("return GetRaidRosterInfo('" + i + "')")[2]);
                slog("searching {0} actual {1} i {2}", name, supportname, index);
                //if (Lua.GetReturnValues("return GetRaidRosterInfo('" + index + "')")[0].ToString() == name)
                if(Confrontname(name,supportname))
                {
                //    slog("found {0} in position {1}", name, index);
                    return index;
                }
            }
            slog("ERROR: Cannot find {0} in current raid..", name);
            return -1;
        }

        public void FillWoWNames()
        {
            int i;
            for (i = 1; i <= Me.RaidMembers.Count; i++)
            {
                WoWnames[i-1] = Lua.GetReturnValues("return GetRaidRosterInfo('" + i + "')")[0].ToString();
            }
        }

        public void BuildSubGroupArray()
        {
            int i;
            if (!subgroupSW.IsRunning) { subgroupSW.Start(); }
            FillWoWNames();
            slog("raidmember.count {0} luacount {1}", Me.RaidMembers.Count, int.Parse(Lua.GetReturnValues("return GetNumRaidMembers()").FirstOrDefault()));
            for (i = 0; i < Me.RaidMembers.Count /* int.Parse(Lua.GetReturnValues("return GetNumRaidMembers()").FirstOrDefault())*/; i++)
            {
                Subgroup[i] = int.Parse(Lua.GetReturnValues("return GetRaidRosterInfo('" + FindWoWIndexFromName(Me.RaidMembers[i].Name) + "')")[2]);
            }
            for (i = 0; i < Me.RaidMembers.Count /*int.Parse(Lua.GetReturnValues("return GetNumRaidMembers()").FirstOrDefault())*/; i++)
            {
                slog("i {0} name {1}{2}**** subgroup {3}", i, /*Lua.GetReturnValues("return GetRaidRosterInfo('" + (i+1) + "')")[0].ToString(), */WoWnames[i][0], WoWnames[i][1], Subgroup[i]);
            }
            slog("time required to build the subgrouparray {0}", subgroupSW.Elapsed.TotalSeconds);
            subgroupSW.Reset();
            subgroupSW.Stop();
        }

        private bool Dismount()
        {
            return (!Me.Mounted || (Me.Mounted && Me.Combat && !_do_not_dismount_EVER) || (Me.Mounted && !Me.Combat && !_do_not_dismount_EVER && !_do_not_dismount_ooc && unitcheck(tank) && tank.Combat));
        }

        private bool GCD()
        {
            if (_account_for_lag)
            {
                return SpellManager.GlobalCooldownLeft.TotalMilliseconds >= (2U * StyxWoW.WoWClient.Latency);
            }
            else
            {
                return SpellManager.GlobalCooldown;
            }
        }

        public bool IsCasting()
        {
            if (_account_for_lag)
            {
                return Me.CurrentCastTimeLeft.TotalMilliseconds >= (2U * StyxWoW.WoWClient.Latency);
            }
            else
            {
                return ObjectManager.Me.IsCasting;
            }
        }
        public bool IsCastingOrGCD()
        {
            return GCD() || IsCasting();
        }
        public bool IsCrowdControlled(WoWUnit unit)
        {
            return unit.GetAllAuras().Any(
                a => a.IsHarmful &&
                     (a.Spell.Mechanic == WoWSpellMechanic.Shackled ||
                      a.Spell.Mechanic == WoWSpellMechanic.Polymorphed ||
                      a.Spell.Mechanic == WoWSpellMechanic.Horrified ||
                      a.Spell.Mechanic == WoWSpellMechanic.Rooted ||
                      a.Spell.Mechanic == WoWSpellMechanic.Frozen ||
                      a.Spell.Mechanic == WoWSpellMechanic.Stunned ||
                      a.Spell.Mechanic == WoWSpellMechanic.Fleeing ||
                      a.Spell.Mechanic == WoWSpellMechanic.Banished ||
                      a.Spell.Mechanic == WoWSpellMechanic.Sapped));
        }
        public bool Load_Trinket()
        {
            //var hearthstone = ObjectManager.GetObjectsOfType(false).FirstOrDefault(i => i.Entry == 6948);
            /*var Trinket1old=ObjectManager.ObjectList.FirstOrDefault(i => i.Name == name);
            if (Trinket1old != null)
            {
                slog("modo 1 ok");
                _trinket1_ID = Trinket1old.Entry;
                _trinket1_CD = Trinket1old.ToItem().Cooldown;
                slog("ID {0} CD {0}", _trinket1_ID, _trinket1_CD);
            }
            else { slog("modo 1 null"); }*/
            //var Trinket1 = ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWItem>(false).FirstOrDefault(i => i.Name == name);
            Trinket1 = ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWItem>(false).FirstOrDefault(i => i.BagSlot<0 && i.ItemInfo.EquipSlot==Styx.InventoryType.Trinket && i.Usable);
            if (Trinket1 != null)
            {
                slog("Primo Trinket");
                UPaHBTSetting.Instance.Trinket1_name = Trinket1.Name;
                UPaHBTSetting.Instance.Trinket1_ID = Trinket1.Entry;

                if (!IsTrinketPassive2(Trinket1))
                {
                    UPaHBTSetting.Instance.Trinket1_passive = false;
                    //_trinket1_passive = false;
                    //_trinket1_CD = Trinket1.ItemInfo.SpellCooldown[0] / 1000f;
                    UPaHBTSetting.Instance.Trinket1_CD = Trinket1.ItemInfo.SpellCooldown[0] / 1000f;
                }
                else
                {
                    UPaHBTSetting.Instance.Trinket1_CD = 0;
                    UPaHBTSetting.Instance.Trinket1_passive = true;
                    //_trinket1_passive = true;
                }
                if (Trinket1.Cooldown == 0) { _trinket1_usable = true; } else { _trinket1_usable = false; }
                //slog("Name {0} EQCD {1}", Trinket1.Name, Trinket1.TriggersSpell);
                //slog("Name {0} name {1} desc {2} count {3} category {4} toolt {5} effect1 {6}", Trinket1.Name, Trinket1.ItemSpells[0].ActualSpell.Name, Trinket1.ItemSpells[0].ActualSpell.Description, Trinket1.ItemSpells.Count, Trinket1.ItemSpells[0].Category, Trinket1.ItemSpells[0].ActualSpell.Tooltip, Trinket1.ItemSpells[0].ActualSpell.SpellEffects[0].AuraType.ToString().Contains("Proc"));
                //slog("Aura type {0}", Trinket1.ItemSpells[0].ActualSpell.SpellEffect1.AuraType.ToString().Contains("Proc"));
                
                /*
                slog("ID1 {0} CD1 {1} usable {2} spellCD {3} ID {4} how many{5} bagslot {6} bag index {7}", Trinket1.Entry, Trinket1.Cooldown, _trinket1_usable, Trinket1.ItemInfo.SpellCooldown[0], Trinket1.ItemInfo.Id, Trinket1.ItemInfo.SpellCooldown.Length, Trinket1.BagSlot, Trinket1.BagIndex);
                //for (i = 0; i < Trinket1.ItemInfo.SpellCooldown.Length; i++) { slog("spellCD {0} : {1}", i, Trinket1.ItemInfo.SpellCooldown[i]); }
                slog("eq slot {0}", Trinket1.ItemInfo.EquipSlot);
                slog("flags {0}", Trinket1.Flags);
                slog("usable {0}", Trinket1.Usable);
                slog("objectflag {0}", Trinket1.ObjectFlags);*/
                //bagslot<0 iteminfo.equislot==trinket
            }
            else 
            { 
                slog("First Trinket null");
                UPaHBTSetting.Instance.Trinket1_name = "No Trinket Found";
                UPaHBTSetting.Instance.Trinket1_ID = 0;
                UPaHBTSetting.Instance.Trinket1_CD = 0;
                UPaHBTSetting.Instance.Trinket1_use_when = 0;
            }
            if (Trinket1 != null)
            {
                Trinket2 = ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWItem>(false).LastOrDefault(i => i.BagSlot < 0 && i.ItemInfo.EquipSlot == Styx.InventoryType.Trinket && i.Usable && !i.Equals(Trinket1));
            }
            else
            {
                Trinket2 = null;
            }
            if (Trinket2 != null)
            {
                slog("Secondo Trinket");
                UPaHBTSetting.Instance.Trinket2_name = Trinket2.Name;
                UPaHBTSetting.Instance.Trinket2_ID = Trinket2.Entry;
                if (!IsTrinketPassive2(Trinket2))
                {
                    UPaHBTSetting.Instance.Trinket2_passive = false;
                    UPaHBTSetting.Instance.Trinket2_CD = Trinket2.ItemInfo.SpellCooldown[0] / 1000f;
                    //_trinket2_passive = false;
                    //_trinket2_CD = Trinket2.ItemInfo.SpellCooldown[0] / 1000f;
                }
                else
                {
                    UPaHBTSetting.Instance.Trinket2_CD = 0;
                    UPaHBTSetting.Instance.Trinket2_passive = true;
                    //_trinket2_passive = true;
                }
                if (Trinket2.Cooldown == 0) { _trinket2_usable = true; } else { _trinket2_usable = false; }
                //slog("Name {0} name {1} desc {2} count {3} categ {4} tool {5} effect1 {6}", Trinket2.Name, Trinket2.ItemSpells[0].ActualSpell.Name, Trinket2.ItemSpells[0].ActualSpell.Description, Trinket2.ItemSpells.Count, Trinket2.ItemSpells[0].Category, Trinket2.ItemSpells[0].ActualSpell.Tooltip, Trinket2.ItemSpells[0].ActualSpell.SpellEffect1);
                //slog("Aura type {0}", Trinket2.ItemSpells[0].ActualSpell.SpellEffect1.AuraType.ToString().Contains("Proc"));
                /*
                slog("ID1 {0} CD1 {1} usable {2} spellCD {3} ID {4} how many{5} bagslot {6} bag index {7}", Trinket2.Entry, Trinket2.Cooldown, _trinket2_usable, Trinket2.ItemInfo.SpellCooldown[0], Trinket2.ItemInfo.Id, Trinket2.ItemInfo.SpellCooldown.Length, Trinket2.BagSlot, Trinket2.BagIndex);
                //for (i = 0; i < Trinket1.ItemInfo.SpellCooldown.Length; i++) { slog("spellCD {0} : {1}", i, Trinket1.ItemInfo.SpellCooldown[i]); }
                slog("eq slot {0}", Trinket2.ItemInfo.EquipSlot);
                slog("flags {0}", Trinket2.Flags);
                slog("usable {0}", Trinket2.Usable);
                slog("objectflag {0}", Trinket2.ObjectFlags);
                //bagslot<0 iteminfo.equislot==trinket
                                                 */
            }
            else 
            {
                slog("Second Trinket null");
                UPaHBTSetting.Instance.Trinket2_name = "No trinket Found";
                UPaHBTSetting.Instance.Trinket2_ID = 0;
                UPaHBTSetting.Instance.Trinket2_CD = 0;
                UPaHBTSetting.Instance.Trinket2_use_when = 0;
            }
            if (Trinket1 != null && Trinket2 != null)
            {
                slog("2 Differents Trinkets Found");
            }
            else if (Trinket1 == null && Trinket2 == null)
            {
                slog("No Trinkets Found At ALL");
            }
            else
            {
                slog("Just 1 Trinket Found");
            }
            //slog("trinket trovati {0}", ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWItem>(false).FindAll(i => i.BagSlot < 0 && i.ItemInfo.EquipSlot == Styx.InventoryType.Trinket && i.Usable).Count);
            return true;
        }

        public bool IsTrinketPassive2(WoWItem Trinket)
        {
            int this_passive;
            this_passive = 0;
            for (trinketcount = 0; trinketcount < Trinket.ItemSpells.Count; trinketcount++)
            {
                slog("name {0} count {1}", Trinket.Name, trinketcount);
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1.AuraType.ToString());
                }
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2.AuraType.ToString());
                }
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3.AuraType.ToString());
                }

                if ((Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1 != null && !Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1.AuraType.ToString().Contains("Proc")))
                {
                    this_passive += 1;
                }
                else
                {
                    this_passive += 0;
                }
                if ((Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2 != null && !Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2.AuraType.ToString().Contains("Proc")) )
                {
                    this_passive += 1;
                }
                else
                {
                    this_passive += 0;
                }
                if ((Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3 != null && !Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3.AuraType.ToString().Contains("Proc")) )
                {
                    this_passive += 1;
                }
                else
                {
                    this_passive += 0;
                }
            }
            if (this_passive == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsTrinketPassive(WoWItem Trinket)
        {
            for (trinketcount = 0; trinketcount < Trinket.ItemSpells.Count; trinketcount++)
            {
                slog("name {0} count {1}", Trinket.Name, trinketcount);
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1.AuraType.ToString());
                }
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2.AuraType.ToString());
                }
                if (Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3 != null)
                {
                    slog("auratype {0}", Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3.AuraType.ToString());
                }
                if ((Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1!=null && Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect1.AuraType.ToString().Contains("Proc")) || ( Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2!=null&& Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect2.AuraType.ToString().Contains("Proc")) ||( Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3!=null&& Trinket.ItemSpells[trinketcount].ActualSpell.SpellEffect3.AuraType.ToString().Contains("Proc")))
                {
                    slog("found Proc");
                    return true;
                }
            }
            slog("No proc found");
            return false;
        }

        public bool Inizialize_Trinket()
        {
            if (Trinket1 != null)
            {
                if (Trinket1.Cooldown == 0)
                {
                    _trinket1_usable = true;
                }
                else
                {
                    _trinket1_usable = false;
                    Trinket1_sw.Start();
                }
            }
            else
            {
                _trinket1_usable = false;
            }
            if (Trinket2 != null)
            {
                if (Trinket2.Cooldown == 0)
                {
                    _trinket2_usable = true;
                }
                else
                {
                    _trinket2_usable = false;
                    Trinket2_sw.Start();
                }
            }
            else
            {
                _trinket2_usable = false;
            }
            return true;
        }
        public bool Check_Trinket()
        {
            if ((!Trinket1_sw.IsRunning || Trinket1_sw.Elapsed.TotalSeconds > _trinket1_CD)&& Trinket1!=null)
            {
                _trinket1_usable = true;
                if (Trinket1_sw.IsRunning)
                {
                    Trinket1_sw.Reset();
                }
            }
            else
            {
                _trinket1_usable = false;
            }

            if ((!Trinket2_sw.IsRunning || Trinket2_sw.Elapsed.TotalSeconds > _trinket2_CD) && Trinket2!=null)
            {
                _trinket2_usable = true;
                if (Trinket2_sw.IsRunning)
                {
                    Trinket2_sw.Reset();
                }
            }
            else
            {
                _trinket2_usable = false;
            }
                return true;
        }
        #endregion
    }
}
