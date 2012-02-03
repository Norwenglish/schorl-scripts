//CustomClass Template - Created by CodenameGamma
//Replace Layout with the CC name, 
//and WoWClass.Mage with the Class your Designing for.
//Created July, 3rd 2010
//For use with Honorbuddy
//CC written with methods from HazzDruid CC
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System.Text;
using System.IO;
using System.Drawing;
using TreeSharp;

namespace Disc
{
    partial class DiscPriest : CombatRoutine
    {
        public override sealed string Name { get { return "Disc CC v3.2"; } }
        public override WoWClass Class { get { return WoWClass.Priest; } }
        public static LocalPlayer Me { get { return ObjectManager.Me; } }
        private WoWUnit lastCast;
        private WoWUnit tank;        

        private void slog(string format, params object[] args) //use for slogging
        {
            Logging.Write(format, args);
        }

        public override void Pulse()
        {

            InitializeLists();

            if (Me != null && Me.IsValid && Me.IsAlive)
            {
                if (Me.FocusedUnit != null)
                {
                    tank = Me.FocusedUnit;
                }
                else
                {
                    tank = GetTank();
                }
                if (tank == null)
                {
                    tank = Me;
                }
                Combat();
            }
        }

        public override bool WantButton
        {
            get
            {
                return true;
            }
        }    

        public override void OnButtonPress()
        {
            settingsForm Set_Form = new settingsForm();                  
            Set_Form.Show();                
        }

        public void InitializeLists()
        {
            if (DiscSettings.Instance.HealBlackList == null)
            {
                DiscSettings.Instance.HealBlackList = new List<string>();
            }
            if (DiscSettings.Instance.UrgentDispelList == null)
            {
                DiscSettings.Instance.UrgentDispelList = new System.ComponentModel.BindingList<Dispels>();
            }
        }

        public bool PlayerIsBlacklisted(WoWPlayer p)
        {
            if(DiscSettings.Instance.HealBlackList.Contains(p.Name))
            {
                Logging.Write(p.Name + " Blacklisted");
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool PlayerIsBlacklisted(WoWUnit p)
        {
            if (DiscSettings.Instance.HealBlackList.Contains(p.Name))
            {
                Logging.Write(p.Name + " Blacklisted");
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ImEatingOrDrinking()
        {
            if (StyxWoW.Me.Auras.ContainsKey("Drink") || StyxWoW.Me.Auras.ContainsKey("Food"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HaltingAuras()
        {
            if (Me.HasAura("Power Word:Barrier")
                || Me.HasAura("Mass Dispel")
                || Me.HasAura("Mind Control"))
            {
                return true;
            }
            return false;
        }

        public override void Combat()
        {
            if (StyxWoW.GlobalCooldown)
                return;
            else if (Disc.DiscSettings.Instance.Stop_SET)
                return;
            else if (ImEatingOrDrinking())
                return;
            else if (Mounted())
                return;
            else if (CancelHeal())
                return;
            else if (Self())
                return;
            else if (CleansingUrgent())
                return;
            else if (ShieldTank())
                return;
            else if (DPS())
                return;
            else if (Healing())
                return;
            else if (Cleansing())
                return;
            else if (StackEvangelism())
                return;
            else if (ClearWeakenedSoulTank())
                return;
            else if (Buff())
                return;
            else if (Resurrect())
                return;
        }

        private bool Mounted()
        {
            if (Me.Mounted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CancelHeal()
        {
            if (Me.IsCasting && (lastCast != null && !lastCast.Dead && lastCast.HealthPercent >= 96) && !lastCast.HasAura("Weakened Soul"))
            {
                lastCast = null;
                //SpellManager.StopCasting();
                return true;
            }
            else if (Me.IsCasting)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private WoWPlayer GetTank()
        {
            foreach (WoWPlayer p in Me.PartyMembers)
            {
                if (IsTank(p))
                {
                    return p;
                }
            }
            return null;
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

        private bool IsTank(WoWPlayer p)
        {
            return Lua.GetReturnValues("return UnitGroupRolesAssigned('" + DeUnicodify(p.Name) + "')").First() == "TANK";
        }

        private bool HaveAggro()
        {
            foreach (WoWUnit u in ObjectManager.GetObjectsOfType<WoWUnit>(true, true))
            {
                if (u.Aggro)
                {
                    return true;
                }
            }
            return false;
        }

        private bool PlayerHasAggro(WoWPlayer p)
        {
            foreach (WoWUnit u in ObjectManager.GetObjectsOfType<WoWUnit>(true, true))
            {
                if (u.Attackable && u.IsTargetingMyPartyMember && u.IsAlive && !u.IsFriendly && u.Combat && (u.CurrentTarget.Name.Equals(p.Name)))
                {   
                    return true;
                }
            }
            return false;
        }

        private bool ShieldTank()
        {
            if (tank!=null && Me.IsInInstance && tank.IsAlive)
            {
                if (tank.Distance > 40 || !tank.InLineOfSight || PlayerIsBlacklisted(tank))
                {
                    return false;
                }                

                else if (!tank.HasAura("Power Word: Shield") && !tank.HasAura("Weakened Soul") && tank.Combat && DiscSettings.Instance.TankHealing_SET)
                {
                    if (CC("Power Word: Shield", tank))
                    {
                        //Casting Shield on tank
                        C("Power Word: Shield", tank);
                        Logging.Write("Shielding Tank");

                        //Cast Pain Suppression if ready
                        if (CC("Pain Suppression", tank) && tank.HealthPercent < DiscSettings.Instance.PainSuppression_SET)
                        {
                            C("Pain Suppression", tank);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (tank.Combat
                    && tank.CurrentTarget != null
                    && tank.CurrentTarget.HealthPercent > 99
                    && CC("Penance", tank))
                {
                    C("Penance", tank);
                    return true;
                }

                else
                {
                    return false;
                }
            }
            return false;
        }

        private bool ClearWeakenedSoulTank()
        {
            if (DiscSettings.Instance.DPS_SET.Equals("Heal First"))
            {
                if (Me.Combat
                    && Me.HealthPercent > DiscSettings.Instance.DPShealth_SET
                    && Me.ManaPercent > DiscSettings.Instance.DPSmana_SET
                    && okToDpsPlayerCheck(DiscSettings.Instance.DPShealth_SET)
                    && Me.CurrentTarget != null
                    && !Me.CurrentTarget.IsFriendly
                    && Me.CurrentTarget.Distance < 30)
                {
                    return DPSRotation(Me.CurrentTarget);
                }
            }
            if (Me.IsInInstance && tank.IsAlive && tank.Combat && DiscSettings.Instance.WeakenedSoul_SET)
            {
                if (tank.Distance > 40 || !tank.InLineOfSight || PlayerIsBlacklisted(tank))
                {
                    return true;
                }
                else if (tank.HasAura("Weakened Soul") && CC("Heal", tank))
                {
                    C("Heal", tank);
                    Logging.Write("Clearing Weakened Soul");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
               return false;
            }
        }

        private bool Self()
        {
            if (tank.Combat && tank.CurrentTarget !=null 
                && tank.CurrentTarget.IsAlive 
                && !tank.CurrentTarget.IsFriendly 
                && DiscSettings.Instance.UseTankTarget_SET 
                && Me.CurrentTarget == null)
            {
                tank.CurrentTarget.Target();
            }
            if (Me.ManaPercent <= DiscSettings.Instance.ShadowFiend_SET && CC("Shadowfiend") && tank.Combat)
            {
                Logging.Write("Shadowfiend");
                tank.CurrentTarget.Target();
                C("Shadowfiend");
                return true;
            }
            if (Me.ManaPercent <= DiscSettings.Instance.HymnHope_SET && CC("Hymn of Hope"))
            {
                C("Hymn of Hope");
                return true;
            }
            if (DiscSettings.Instance.Fade_SET && HaveAggro() && CC("Fade"))
            {
                C("Fade");
            }
            if (DiscSettings.Instance.FearWard_SET && Me.Combat 
                && !StyxWoW.Me.Auras.ContainsKey("Fear Ward") 
                && CC("Fear Ward"))
            {
                C("Fear Ward", Me);
                return true;
            }
            if (MassDispelCount() >= 3
                && CC("Mass Dispel"))
            {
                C("Mass Dispel");                
                StyxWoW.SleepForLagDuration();
                LegacySpellManager.ClickRemoteLocation(StyxWoW.Me.Location);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CloseDistance(WoWUnit u, int distance)
        {
            Navigator.PlayerMover.MoveTowards(u.Location);               
            while (u.Distance > distance)
            {
                if (!Me.IsMoving)
                {
                    Navigator.PlayerMover.MoveTowards(u.Location);
                }
                Thread.Sleep(100);                
            }
            Navigator.PlayerMover.MoveStop();
        }

        private void SmartCloseDistance(WoWUnit u, int distance)
        {
            WoWPoint[] PathToTarget = Navigator.GeneratePath(Me.Location, u.Location);
            int CurrentPoint = 0;
            Logging.Write("Moving this many points: " + PathToTarget.Length);
            while (u.Distance > distance && CurrentPoint<=PathToTarget.Length)
            {                
                Logging.Write("Moving to point " + PathToTarget[CurrentPoint]); 
                Navigator.MoveTo(PathToTarget[CurrentPoint]);
                CurrentPoint++;
            }
            Navigator.PlayerMover.MoveStop();
        }

        private void CloseDistance(WoWPlayer p, int distance)
        {
            Navigator.PlayerMover.MoveTowards(p.Location);
            while (p.Distance > distance)
            {
                if (!Me.IsMoving)
                {
                    Navigator.PlayerMover.MoveTowards(p.Location);
                }
                Thread.Sleep(100);
            }
            Navigator.PlayerMover.MoveStop();
        }

        private int GetDistance(WoWPoint p1, WoWPoint p2)
        {
            double XDif = Math.Pow((p2.X - p1.X), 2);
            double YDif = Math.Pow((p2.Y - p1.Y), 2);
            //double ZDif = Math.Pow((p2.Z - p1.Z), 2);
            int distance = (int)Math.Sqrt(XDif + YDif);
            
            return distance;
        }

        private bool DPSRotation(WoWUnit tar)
        {
            if (tar == null || !tar.IsAlive || tar.Distance > 30 || !tar.InLineOfSight || tar.IsFriendly)
                {
                    return true;
                }                
                else{
                    //Logging.Write("Distance to target: " + GetDistance(Me.Location, tar.Location) + " " + tar.Distance);
                    if (DiscSettings.Instance.FaceTarget_SET)
                    {
                        FaceTarget();
                    }
                    /*if (Me.Auras["Evangelism"].StackCount >= 5 && CC("Archangel"))
                    {
                        Logging.Write("Archangel");
                        C("Archangel");
                        return true;
                    }*/
                    
                    if (!tar.HasAura("Devouring Plague") && DiscSettings.Instance.DevPlague_SET && CC("Devouring Plague", tar))
                    {
                        Logging.Write("Devouring Plague");
                        C("Devouring Plague", tar);
                        return true;
                    }
                    if (!tar.HasAura("Shadow Word: Pain") && DiscSettings.Instance.SWPain_SET && CC("Shadow Word: Pain", tar))
                    {
                        Logging.Write("Shadow Word: Pain");
                        C("Shadow Word: Pain", tar);
                        return true;
                    }                    
                    if (!tar.HasAura("Holy Fire") && DiscSettings.Instance.HolyFire_SET && CC("Holy Fire", tar))
                    {
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        Logging.Write("No Archangel so Smite!");
                        C("Holy Fire", tar);
                        return true;
                    }
                    if (tar.HasAura("Holy Fire") && DiscSettings.Instance.Smite_SET && CC("Smite", tar))
                    {
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        Logging.Write("Has Holy Fire so Smite!");
                        C("Smite", tar);
                        return true;
                    }
                    else if (DiscSettings.Instance.PenanceDPS_SET && CC("Penance", tar))
                    {
                        C("Penance", tar);
                        return true;
                    }
                    if (DiscSettings.Instance.Smite_SET && CC("Smite", tar))
                    {
                        C("Smite", tar);
                        return true;
                    } 
                    else
                    {
                        return false;
                    }
                }
        }

        private bool DPS()
        {
            if (okToDps())
            {
                WoWUnit tar = Me.CurrentTarget;

                return DPSRotation(tar);
            }
            else
            {
                return false;
            }
        }

        private WoWPlayer CheckPrayerOfHealing()
        {
            if (Me.IsInParty || Me.IsInRaid)
            {
                WoWPlayer[] targets = new WoWPlayer[(Me.RaidMembers.Count / 5)];
                int[] count = new int[(Me.RaidMembers.Count / 5)];
                int CurrentPlayerInParty = 0;
                int CurrentParty = 0;
                int MeCount = 0;
                int CompareCounts = 0;

                foreach (WoWPlayer p in Me.PartyMembers)
                {
                    if (p.Distance < 30
                        && p.HealthPercent < DiscSettings.Instance.PrayerHealingMax_SET
                        && p.HealthPercent > DiscSettings.Instance.PrayerHealingMin_SET
                        && !p.HasAura("Divine Aegis"))
                    {
                        MeCount++;
                    }                    
                }
                
                if (Me.Distance < 30
                        && Me.HealthPercent < DiscSettings.Instance.PrayerHealingMax_SET
                        && Me.HealthPercent > DiscSettings.Instance.PrayerHealingMin_SET
                        && !Me.HasAura("Divine Aegis"))
                {
                    MeCount++;
                }


                if (!Me.IsInRaid)
                {
                    if (MeCount >= DiscSettings.Instance.PrayerHealingNum_SET)
                    {
                        Logging.Write("PoH Party Count: " + MeCount);
                        return Me;
                    }
                    else
                    {
                        return null;
                    }
                }

                foreach (int c in count)
                {
                    count[c] = 0;
                }

                foreach (WoWPlayer p in Me.RaidMembers)
                {
                    if (!p.IsMe && !p.IsInMyParty)
                    {
                        if (CurrentPlayerInParty > 4)
                        {
                            CurrentPlayerInParty = 0;
                            CurrentParty++;
                        }
                        if (CurrentPlayerInParty == 0)
                        {
                            targets[CurrentParty] = p;
                        }
                        if (p.Distance < 30
                            && p.HealthPercent < DiscSettings.Instance.PrayerHealingMax_SET
                            && p.HealthPercent > DiscSettings.Instance.PrayerHealingMin_SET
                            && !p.HasAura("Divine Aegis"))
                        {
                            count[CurrentParty]++;
                        }
                        CurrentPlayerInParty++;
                    }                    
                }

                for (int i = 0; i < targets.Length; i++)
                {
                    if (count[i] > count[CompareCounts])
                    {
                        CompareCounts = i;
                    }
                }

                if (MeCount >= DiscSettings.Instance.PrayerHealingNum_SET
                    || count[CompareCounts] >= DiscSettings.Instance.PrayerHealingNum_SET)
                {
                    if (count[CompareCounts] == null)
                    {
                        return Me;
                    }
                    if (MeCount >= count[CompareCounts])
                    {
                        return Me;
                    }
                    else
                    {
                        return targets[CompareCounts];
                    }
                }
            }
            return null;
        }

        private bool CheckDivineHymn()
        {
            int count = 0;
            foreach (WoWPlayer p in Me.RaidMembers)
            {
                if (p.Distance < 40 && p.HealthPercent <= DiscSettings.Instance.DivineHymnHealth_SET)
                {
                    count++;
                }
            }
            if (count >= DiscSettings.Instance.DivHymnNum_SET)
            {
                return true;
            }
            return false;
        }

        private void FaceTarget()
        {
            WoWUnit u = Me.CurrentTarget;
            if (Me.Combat && Me.IsAlive && u!=null && u.IsAlive && u.Distance<30 && !u.IsFriendly && u.InLineOfSight)
            {
                u.Face();
            }
        }

        private bool Healing()
        {
            if (SpellManager.Spells.Keys.Contains("Archangel") && (Me.Combat || tank.Combat))
            {
                //Logging.Write("Player has Attonement, so using Attonement healing rotation");
                return AtonmentHealing();
            }
            WoWPlayer tar = GetHealTarget();
            WoWPlayer PrayerTar = CheckPrayerOfHealing();
            if (tar != null)
            {
                Logging.Write(tar.Name + "---" + Convert.ToInt16(tar.HealthPercent));
                if (tar.Distance > 40 || !tar.InLineOfSight)
                {                    
                    return true;
                }
                else
                {
                    double hp = tar.HealthPercent;
                    if(CheckDivineHymn() && CC("Divine Hymn"))
                    {
                        C("Divine Hymn");
                    }
                    if (hp>DiscSettings.Instance.PrayerHealingMin_SET
                        && PrayerTar != null
                        && CC("Prayer of Healing", PrayerTar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        C("Prayer of Healing", PrayerTar);
                        return true;
                    }
                    if (NeedPrayerOfMending()
                        && CC("Prayer of Mending", tar)
                        && DiscSettings.Instance.PrayerMending_SET)
                    {
                        C("Prayer of Mending", tar);
                    }
                    if ((hp < DiscSettings.Instance.PWShield_SET
                        || (DiscSettings.Instance.ShieldAggro_Heal_SET && PlayerHasAggro(tar))) 
                        && CC("Power Word: Shield", tar)
                        && !tar.HasAura("Power Word: Shield")
                        && !tar.HasAura("Weakened Soul"))
                    {
                        C("Power Word: Shield", tar);

                        if (CC("Pain Suppression", tar) && hp<DiscSettings.Instance.PainSuppression_SET 
                            && !DiscSettings.Instance.TankHealing_SET)
                        {
                            C("Pain Suppression", tar);
                        }
                        return true;
                    }
                    if (hp < DiscSettings.Instance.Penance_SET && CC("Penance", tar))
                    {
                        C("Penance", tar);
                        return true;
                    }
                    if (hp < DiscSettings.Instance.FlashHeal_SET && CC("Flash Heal", tar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        C("Flash Heal", tar);
                        return true;
                    }                                        
                    if (hp < DiscSettings.Instance.Renew_SET && CC("Renew", tar) && !tar.HasAura("Renew"))
                    {
                        C("Renew", tar);
                        return true;
                    }
                    if (tar != Me && Me.HealthPercent < DiscSettings.Instance.BindHeal_SET 
                        && hp < DiscSettings.Instance.GHeal_SET && CC("Binding Heal", tar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        C("Binding Heal", tar);
                        return true;
                    }
                    if (hp < DiscSettings.Instance.GHeal_SET && CC("Greater Heal", tar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        C("Greater Heal", tar);
                        return true;
                    }
                    if (hp < DiscSettings.Instance.Heal_SET && CC("Heal", tar))
                    {                     
                            C("Heal", tar);
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private bool NeedStackEvangelism()
        {
            TimeSpan ts = new TimeSpan(0, 0, 0, 4, 0);
            if (!Me.HasAura("Evangelism")
                || (Me.HasAura("Evangelism")
                && Me.Auras["Evangelism"].StackCount < 5
                || Me.Auras["Evangelism"].TimeLeft < ts))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ArchangelCheck()
        {
            if (Me.HasAura("Evangelism")
                && Me.Auras["Evangelism"].StackCount >= 5
                && CC("Archangel"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool AtonmentHealing()
        {
            WoWPlayer tar = GetHealTarget();
            WoWPlayer PrayerTar = CheckPrayerOfHealing();

            //int AtonementDistance = 0;
            if (tar != null && (Me.Combat || tank.Combat))
            {                
                if (tar.Distance > 40 || !tar.InLineOfSight)
                {
                    return true;
                }
                else
                {
                    Logging.Write(tar.Name + "---" + Convert.ToInt16(tar.HealthPercent));
                    double hp = tar.HealthPercent;
                    
                    //tank.CurrentTarget.Target();
                    //AtonementDistance += GetDistance(tar.Location, Me.CurrentTarget.Location);

                    //Divine Hymn
                    if (CheckDivineHymn() && CC("Divine Hymn"))
                    {
                        if (ArchangelCheck())
                        {
                            Logging.Write("Archangel");
                            C("Archangel");
                        }
                        C("Divine Hymn");
                        return true;
                    }

                    //Prayer of Healing
                    if (hp > DiscSettings.Instance.PrayerHealingMin_SET
                        && PrayerTar != null
                        && CC("Prayer of Healing", PrayerTar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        if (ArchangelCheck())
                        {
                            Logging.Write("Archangel");
                            C("Archangel");
                        }
                        C("Prayer of Healing", PrayerTar);
                        return true;
                    }

                    //Power Word: Shield
                    if ((hp < DiscSettings.Instance.PWShield_SET
                        || (DiscSettings.Instance.ShieldAggro_Heal_SET && PlayerHasAggro(tar)))
                        && CC("Power Word: Shield", tar)
                        && !tar.HasAura("Power Word: Shield")
                        && !tar.HasAura("Weakened Soul"))
                    {
                        C("Power Word: Shield", tar);

                        if (CC("Pain Suppression", tar) && hp < DiscSettings.Instance.PainSuppression_SET
                            && !DiscSettings.Instance.TankHealing_SET)
                        {
                            C("Pain Suppression", tar);
                        }
                        return true;
                    }

                    //Prayer of Mending
                    if (hp <= 75 
                        && NeedPrayerOfMending()
                        && CC("Prayer of Mending", tar)
                        && DiscSettings.Instance.PrayerMending_SET)
                    {
                        if (ArchangelCheck())
                        {
                            Logging.Write("Archangel");
                            C("Archangel");
                        }
                        C("Prayer of Mending", tar);
                        return true;
                    }

                    //Penance
                    if (hp < DiscSettings.Instance.Penance_SET && CC("Penance", tar))
                    {
                        C("Penance", tar);
                        return true;
                    }

                    //Flash Heal
                    if (hp < DiscSettings.Instance.FlashHeal_SET && CC("Flash Heal", tar))
                    {
                        if (CC("Inner Focus") && DiscSettings.Instance.InnerFocus_SET)
                        {
                            C("Inner Focus");
                        }
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        if (ArchangelCheck())
                        {
                            Logging.Write("Archangel");
                            C("Archangel");
                        }
                        C("Flash Heal", tar);
                        return true;
                    }
                    if (!Me.IsMoving)
                    {
                        Me.CurrentTarget.Face();
                    }
                    //Atonement Healing
                    //Holy Fire                    
                    if (hp < DiscSettings.Instance.Heal_SET 
                        && !Me.CurrentTarget.HasAura("Holy Fire") 
                        //&& AtonementDistance <= 15 
                        && CC("Holy Fire", Me.CurrentTarget))
                    {
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        Logging.Write("Healing with Atonement");
                        C("Holy Fire", Me.CurrentTarget);
                        return true;
                    }
                    //Evangelism stack building
                    if (NeedStackEvangelism()
                        && !Me.CurrentTarget.HasAura("Holy Fire")
                        && CC("Holy Fire", Me.CurrentTarget))
                    {
                        Logging.Write("Stacking Evangelism");
                        C("Holy Fire", Me.CurrentTarget);
                        return true;
                    }
                    if (NeedStackEvangelism()
                        && Me.CurrentTarget.HasAura("Holy Fire")
                        && CC("Smite", Me.CurrentTarget))
                    {
                        Logging.Write("Stacking Evangelism");
                        C("Smite", Me.CurrentTarget);
                        return true;
                    }

                    //Smite
                    if (hp < DiscSettings.Instance.Heal_SET
                        && CC("Smite", Me.CurrentTarget))
                    {
                        if (CC("Power Infusion") && DiscSettings.Instance.PowerInfusion_SET)
                        {
                            C("Power Infusion");
                        }
                        Logging.Write("Healing with Atonement");
                        C("Smite", Me.CurrentTarget);
                        return true;
                    }
                    else
                    {
                        //Blacklist.Add(tar, new TimeSpan(0, 0, 2));
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private bool StackEvangelism()
        {
            if (Me.Combat
                && Me.CurrentTarget != null
                && SpellManager.Spells.Keys.Contains("Archangel")
                && NeedStackEvangelism())
            {
                if (!Me.IsMoving)
                {
                    Me.CurrentTarget.Face();
                }
                //Evangelism stack building
                if (!Me.CurrentTarget.HasAura("Holy Fire")
                    && CC("Holy Fire", Me.CurrentTarget))
                {
                    Logging.Write("Stacking Evangelism");
                    C("Holy Fire", Me.CurrentTarget);
                    return true;
                }
                if (Me.CurrentTarget.HasAura("Holy Fire")
                    && CC("Smite", Me.CurrentTarget))
                {
                    Logging.Write("Stacking Evangelism");
                    C("Smite", Me.CurrentTarget);
                    return true;
                }
            }
            return false;
        }

        private bool CC(string spell, WoWUnit target)
        {
            return SpellManager.CanCast(spell, target);
        }

        private bool CC(string spell)
        {
            return SpellManager.CanCast(spell);
        }

        private void ChainSpells(params string[] spells)
        {
            string macro = "";
            foreach (string s in spells)
            {
                macro += "CastSpellByName(\"" + s + "\", true);";
            }
            Lua.DoString(macro);
        }

        private bool C(string spell, WoWUnit target)
        {
            if (SpellManager.Cast(spell, target))
            {
                lastCast = target;
                Logging.Write("Casting " + spell + " on " + target.Name);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool C(string spell)
        {
            lastCast = null;
            Logging.Write("Casting " + spell);
            return SpellManager.Cast(spell);
        }

        private bool Cleansing()
        {
            if (DiscSettings.Instance.Dispel_SET)
            {
                WoWPlayer p = GetCleanseTarget();
                if (p != null)
                {
                    if (p.Distance > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if ((NeedsCleanse(p)==1) && CC("Cure Disease", p))
                    {
                        C("Cure Disease", p);
                        return true;
                    }
                    else if ((NeedsCleanse(p) == 2) && CC("Dispel Magic", p))
                    {
                        C("Dispel Magic", p);
                        return true;
                    }
                    else
                    {
                        Logging.Write("Cure Disease");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            } return false;
        }

        private bool CleansingUrgent()
        {
            if (DiscSettings.Instance.DispelUrgent_SET)
            {
                WoWPlayer p = GetCleanseTarget();
                if (p != null)
                {
                    if (p.Distance > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if ((NeedsCleanseUrgent(p) == 1) && CC("Cure Disease", p))
                    {
                        C("Cure Disease", p);
                        Logging.Write("Curing Urgent Disease");
                        return true;
                    }
                    else if ((NeedsCleanseUrgent(p) == 2) && CC("Dispel Magic", p))
                    {
                        C("Dispel Magic", p);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            } return false;
        }

        private WoWPlayer GetCleanseTarget()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true)
                    orderby unit.HealthPercent ascending
                    where (unit.IsInMyPartyOrRaid || unit.IsMe)
                    where !PlayerIsBlacklisted(unit)
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 80
                    where NeedsCleanse(unit)!=0
                    select unit).FirstOrDefault();
        }

        private int NeedsCleanse(WoWPlayer p)
        {
            foreach (WoWAura a in p.ActiveAuras.Values)
            {
                if (a.IsHarmful && Me.ManaPercent > 50)
                {
                    WoWDispelType t = a.Spell.DispelType;
                    if (t == WoWDispelType.Disease)
                    {
                        return 1;
                    }
                    else if (t == WoWDispelType.Magic)
                    {
                        return 2;
                    }
                }
            }
            return 0;
        }

        private int MassDispelCount()
        {
            int count = 0;
            foreach (WoWPlayer p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true))
            {
                if (p.IsInMyPartyOrRaid 
                    && p.Distance <=15
                    && NeedsCleanse(p)>0)
                {
                    count++;
                }
            }
            return count;
        }

        private int NeedsCleanseUrgent(WoWPlayer p)
        {
            foreach (WoWAura a in p.ActiveAuras.Values)
            {
                if (a.IsHarmful && Me.ManaPercent > 50 && UrgentDebuff(a))
                {
                    WoWDispelType t = a.Spell.DispelType;
                    if (t == WoWDispelType.Disease)
                    {
                        return 1;
                    }
                    else if (t == WoWDispelType.Magic)
                    {
                        return 2;
                    }
                }
            }
            return 0;
        }

        private bool UrgentDebuff(WoWAura a)
        {
            foreach (Dispels d in DiscSettings.Instance.UrgentDispelList)
            {
                if (d.ListItem.ToString().Equals(a.Name))
                {
                    return true;
                }
            }
            return false;
        }

        private bool okToDpsPlayerCheck(int percent)
        {
            if (Me.IsInMyPartyOrRaid)
            {
                foreach (WoWPlayer p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true))
                {
                    if (p.IsFriendly
                            && p.Distance < 40
                            && p.IsAlive
                            && PlayerHasAggro(p)
                            && p.InLineOfSight
                            && DiscSettings.Instance.ShieldAggroed_SET
                            && !p.HasAura("Weakened Soul")
                            && !p.HasAura("Power Word: Shield")
                            && CC("Power Word: Shield", p))
                    {
                        C("Power Word: Shield", p);
                    }
                    if (p.Distance < 40 && p.HealthPercent <= percent && p.IsAlive && p.InLineOfSight)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool okToDps()
        {
            if (Me.Combat
                && DiscSettings.Instance.DPS_SET.Equals("DPS First")
                && Me.HealthPercent > DiscSettings.Instance.DPShealth_SET
                && Me.ManaPercent > DiscSettings.Instance.DPSmana_SET
                && okToDpsPlayerCheck(DiscSettings.Instance.DPShealth_SET)
                && Me.CurrentTarget != null 
                && !Me.CurrentTarget.IsFriendly 
                && Me.CurrentTarget.Distance < 30                  
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private WoWPlayer GetHealTarget()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true)
                        orderby unit.HealthPercent ascending
                        where (unit.IsInMyPartyOrRaid || unit.IsMe)
                        where !PlayerIsBlacklisted(unit)
                        where !Blacklist.Contains(unit)
                        where !unit.Dead
                        where !unit.IsGhost
                        where unit.IsFriendly
                        where unit.Distance < 80
                        where unit.HealthPercent < 99
                        select unit).FirstOrDefault();            
        }

        private bool NeedPrayerOfMending()
        {
            if (Me.HasAura("Prayer of Mending"))
            {
                    return false;
            }
            foreach (WoWPlayer p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true))
            {
                if (p.IsInMyPartyOrRaid && p.HasAura("Prayer of Mending"))
                {
                    return false;
                }                
            }
            return true;
                
        }

        private IEnumerable<WoWPlayer> GetResurrectTargets()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                    orderby unit.Distance ascending
                    where !PlayerIsBlacklisted(unit)
                    where unit.Dead
                    where unit.IsInMyPartyOrRaid
                    where !unit.IsGhost
                    where unit.Distance < 100
                    select unit);
        }

        private bool Resurrect()
        {
            foreach (WoWPlayer p in GetResurrectTargets())
            {
                if (Blacklist.Contains(p.Guid, true))
                {
                    continue;
                }
                else
                {
                    if (p.Distance > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if (CC("Resurrection", p))
                    {
                        C("Resurrection", p);
                        Logging.Write("Resurrection " + p);
                        Blacklist.Add(p, new TimeSpan(0, 0, 30));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        private bool NeedRaidBuff(String str)
        {
            foreach (WoWPlayer p in Me.RaidMembers)
            {
                if (p.Distance < 40 && p.IsAlive &&  !p.HasAura(str))
                {
                    return true;
                }
            }
            return false;
        }

        private bool NeedPartyBuff(String str)
        {
            foreach (WoWPlayer p in Me.PartyMembers)
            {
                if (p.Distance < 40 && p.IsAlive && !p.HasAura(str))
                {
                    return true;
                }
            }
            return false;
        }

        private bool NeedABuff(String str)
        {
            if (NeedPartyBuff(str) || NeedRaidBuff(str) || !StyxWoW.Me.Auras.ContainsKey(str))
            {
                return true;
            }
            return false;
        }
                
        private bool Buff()
        {
            if (DiscSettings.Instance.PWFort_SET && NeedABuff("Power Word: Fortitude") && NeedABuff("Commanding Shout"))
            {
                Logging.Write("Power Word: Fortitude");
                C("Power Word: Fortitude");
            }
            if (DiscSettings.Instance.ShadProt_SET && NeedABuff("Shadow Protection"))
            {
                Logging.Write("Shadow Protection");
                C("Shadow Protection");                
            }
            if (!StyxWoW.Me.Auras.ContainsKey("Inner Will") && DiscSettings.Instance.FireOrWill_SET.Equals("Inner Will"))
            {
                Logging.Write("Inner Will");
                C("Inner Will");
                return true;
            }
            else if (!StyxWoW.Me.Auras.ContainsKey("Inner Fire") && DiscSettings.Instance.FireOrWill_SET.Equals("Inner Fire"))
            {
                Logging.Write("Inner Fire");
                C("Inner Fire");
                return true;
            } 
            if (Me.Combat && !StyxWoW.Me.Auras.ContainsKey("Fear Ward") && DiscSettings.Instance.FearWard_SET && CC("Fear Ward", Me))
            {
                Logging.Write("Fear Ward");
                C("Fear Ward");
                return true;
            }
            return false;
        }

        private bool isAuraActive(string name)
        {
            return isAuraActive(name, Me);
        }

        private bool isAuraActive(string name, WoWUnit u)
        {
            return u.ActiveAuras.ContainsKey(name);
        }     

        public override bool NeedRest
        {            
            get
            {
                /*
                if (Me.ManaPercent < DiscSettings.Instance.Mana_Percent &&
                    !Me.Auras.ContainsKey("Drink"))
                {
                    Logging.Write("Drinking");
                    return true;
                }
                if (Me.HealthPercent < DiscSettings.Instance.Health_Percent)
                {
                    Logging.Write("Eating");
                    return true;
                }*/

                return false;
            }
        }

        public override void Rest()
        {
            if (Me.ManaPercent < DiscSettings.Instance.Mana_Percent)
            {
                Styx.Logic.Common.Rest.Feed();
            }
            if (Me.HealthPercent < DiscSettings.Instance.Health_Percent)
            {
                Styx.Logic.Common.Rest.Feed();
            }

        }

        public override bool NeedPullBuffs { get { Pulse(); return false; } }

        public override bool NeedCombatBuffs { get { Pulse(); return false; } }

        public override bool NeedPreCombatBuffs { get { Pulse(); return false; } }



    }   
}
