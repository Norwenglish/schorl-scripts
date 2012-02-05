using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.Combat.CombatRoutine;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;
using Styx.Logic.Combat;
using Styx.Helpers;
using Styx.Logic.Pathing;
using Styx;
using Styx.Logic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Drawing;

namespace HazzDruid
{
    class class2 : CombatRoutine
    {
        private WoWUnit lastCast;
        private WoWUnit tank;

        public override void Pulse()
        {
            {
                tank = GetTank();
                if (tank == null)
                {
                    tank = Me;
                }
                Combat();
            }
        }

        public override void Initialize()
        {
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
            HazzDruid.HazzDruidConfig f1 = new HazzDruid.HazzDruidConfig();
            f1.ShowDialog();
        }

        public override void Combat()
        {
            if (StyxWoW.GlobalCooldown)
                return;
            else if (Forms())
                return;
            else if (SelfBuff())
                return;
            else if (Tranquility())
                return;
            else if (TreeForm())
                return;
            else if (Innervate())
                return;
            else if (Cleansing())
                return;
            else if (Healing())
                return;
            else if (Lifebloom())
                return;
            else if (Harmony())
                return;
            else if (Defense())
                return;
            else if (MoonkinHeal())
                return;
            else if (MoonkinDoT())
                return;
            else if (Mushrooms())
                return;
            else if (Moonkin())
                return;
            else if (Buff())
                return;
            else if (Revive())
                return;
            else if (Rebirth())
                return;
        }

        private bool Mounted()
        {
            WoWUnit u = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsHostile && unit.IsAlive && unit.Distance2D < 60 && !unit.Mounted);
            WoWUnit tar = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsFriendly && unit.IsAlive && unit.HealthPercent < 99 && unit.Distance2D < 60 && !unit.Mounted);

            if (Me.Mounted)
            {
                if (Battlegrounds.IsInsideBattleground && u.Distance2D < 40)
                {
                    return false;
                }
                if (Battlegrounds.IsInsideBattleground && tar.Distance2D < 40)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CancelHeal()
        {
            if (Me.IsCasting && (lastCast != null && !lastCast.Dead && lastCast.HealthPercent >= 90))
            {
                lastCast = null;
                SpellManager.StopCasting();
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

        private bool CheckUnit(WoWUnit unit)
        {
            return unit != null && unit.IsValid && unit.IsAlive;
        }

        private void ReTarget(WoWUnit target)
        {
            if (CheckUnit(Me) && CheckUnit(target) && (!Me.GotTarget || Me.IsTargetingMeOrPet || Me.CurrentTarget.IsFriendly))
            {
                target.Target();
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

        private bool Forms()
        {
            if (HazzDruidSettings.Instance.UseTravel && Battlegrounds.IsInsideBattleground && Me.IsOutdoors && !Me.Mounted && !Me.Combat && !Me.Dead && !Me.HasAura("Travel Form"))
            {
                Logging.Write("Travel Form");
                C("Travel Form");
                return true;
            }
            if (HazzDruidSettings.Instance.UseTravel && Battlegrounds.IsInsideBattleground && Me.IsIndoors && !Me.Mounted && !Me.Combat && !Me.Dead && !Me.HasAura("Cat Form"))
            {
                Logging.Write("Cat Form");
                C("Cat Form");
                return true;
            }
            if (HazzDruidSettings.Instance.SpecBalance && HazzDruidSettings.Instance.UseMoonkin && !Me.Mounted && !Me.Dead && !Me.HasAura("Travel Form") && !Me.HasAura("Cat Form") && !Me.HasAura("Moonkin Form"))
            {
                Logging.Write("Moonkin Form");
                C("Moonkin Form");
                return true;
            }
            return false;
        }

        private bool Innervate()
        {
            if (Me.ManaPercent < HazzDruidSettings.Instance.InnervatePercent && CC("Innervate"))
            {
                Logging.Write("Innervate");
                C("Innervate", Me);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool Mushrooms()
        {
            WoWUnit u = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsHostile && unit.IsAlive && unit.Distance2D < 40 && !unit.Mounted && unit.InLineOfSight);

            if (u != null)
            {
                if (!HazzDruidSettings.Instance.SpecBalance)
                {
                    return false;
                }
                else if (!HazzDruidSettings.Instance.UseMushroom)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && u.Distance2D > 40 && u.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, u.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    StyxWoW.Me.CurrentTarget.Face();

                    if (Me.ActiveAuras["Wild Mushroom"].StackCount < HazzDruidSettings.Instance.MushroomPercent)
                    {
                        Logging.Write("Wild Mushroom");
                        C("Wild Mushroom");
                        LegacySpellManager.ClickRemoteLocation(u.Location);
                        return true;
                    }
                    if (Me.ActiveAuras["Wild Mushroom"].StackCount >= HazzDruidSettings.Instance.MushroomPercent)
                    {
                        Logging.Write("Detonate Mushrooms");
                        C("Wild Mushroom: Detonate", Me);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool TreeForm()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && tar.Distance2D > 40 && tar.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, tar.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    if (FriendlyCount(50) >= 3 && HazzDruidSettings.Instance.UseTree && CC("Tree of Life"))
                    {
                        Logging.Write("Tree of Life");
                        C("Tree of Life", Me);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Tranquility()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && tar.Distance2D > 40 && tar.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, tar.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    if (FriendlyCount(40) >= 3 && HazzDruidSettings.Instance.UseTranquility && CC("Tranquility"))
                    {
                        Logging.Write("Tranquility");
                        C("Tranquility", Me);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Healing()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && tar.Distance2D > 40 && tar.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, tar.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return true;
                }
                else
                {
                    if (!CheckUnit(Me))
                    {
                        return false;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.SwiftmendPercent && CC("Swiftmend", tar) && isAuraActive("Rejuvenation", tar))
                    {
                        Logging.Write("Swiftmend");
                        C("Swiftmend", tar);
                        return true;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.NaturesPercent && CC("Nature's Swiftness", tar))
                    {
                        Logging.Write("Nature's Swiftness");
                        C("Nature's Swiftness", tar);
                        return true;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.RejuvenationPercent && !isAuraActive("Rejuvenation", tar))
                    {
                        Logging.Write("Rejuvenation");
                        C("Rejuvenation", tar);
                        return true;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.WildGrowthPercent && CC("Wild Growth", tar) && !isAuraActive("Wild Growth", tar))
                    {
                        Logging.Write("Wild Growth");
                        C("Wild Growth", tar);
                        return true;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.HealingTouchPercent && Me.ActiveAuras.ContainsKey("Harmony"))
                    {
                        Logging.Write("Healing Touch");
                        PlayerMover();
                        C("Healing Touch", tar);
                        return true;
                    }
                    if (tar.HealthPercent < HazzDruidSettings.Instance.RegrowthPercent && !isAuraActive("Regrowth", tar))
                    {
                        Logging.Write("Regrowth");
                        PlayerMover();
                        C("Regrowth", tar);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool MoonkinHeal()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecBalance && !HazzDruidSettings.Instance.UseMoonkinHeal)
                {
                    return true;
                }
                else
                {
                    if (!Me.Combat && tar.HealthPercent < 90 && !isAuraActive("Rejuvenation", Me))
                    {
                        Logging.Write("Rejuvenation");
                        C("Rejuvenation", Me);
                        return true;
                    }
                    if (!Me.Combat && tar.HealthPercent < 70 && !isAuraActive("Regrowth", Me))
                    {
                        Logging.Write("Regrowth");
                        PlayerMover();
                        C("Regrowth", Me);
                        return true;
                    }
                    if (!Me.Combat && tar.HealthPercent < 50)
                    {
                        Logging.Write("Healing Touch");
                        PlayerMover();
                        C("Healing Touch", Me);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Defense()
        {
            WoWUnit u = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsHostile && unit.IsAlive && unit.Distance2D < 40 && !unit.Mounted && unit.InLineOfSight);

            if (u != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && u.Distance2D > 40 && u.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, u.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return true;
                }
                else
                {
                    StyxWoW.Me.CurrentTarget.Face();

                    if (HazzDruidSettings.Instance.UseCombat && !isAuraActive("Insect Swarm", u))
                    {
                        Logging.Write("Insect Swarm");
                        PlayerMover();
                        C("Insect Swarm", u);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseCombat && !isAuraActive("Moonfire", u))
                    {
                        Logging.Write("Moonfire");
                        C("Moonfire", u);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseCombat)
                    {
                        Logging.Write("Wrath");
                        PlayerMover();
                        C("Wrath", u);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool MoonkinDoT()
        {
            WoWUnit u = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsHostile && unit.IsAlive && unit.Distance2D < 40 && !unit.Mounted && unit.InLineOfSight);

            if (u != null)
            {
                if (!HazzDruidSettings.Instance.SpecBalance)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && u.Distance2D > 40 && u.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, u.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    StyxWoW.Me.CurrentTarget.Face();

                    if (HazzDruidSettings.Instance.UseSwarm && !isAuraActive("Insect Swarm", u))
                    {
                        Logging.Write("Insect Swarm");
                        PlayerMover();
                        C("Insect Swarm", u);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseMoonfire && !isAuraActive("Moonfire", u))
                    {
                        Logging.Write("Moonfire");
                        C("Moonfire", u);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseFoN && CC("Force of Nature", u))
                    {
                        Logging.Write("Force of Nature");
                        C("Force of Nature");
                        LegacySpellManager.ClickRemoteLocation(u.Location);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseSolarBeam && CC("Solar Beam", u))
                    {
                        Logging.Write("Solar Beam");
                        C("Solar Beam", u);
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseStarsurge && CC("Starsurge", u))
                    {
                        Logging.Write("Starsurge");
                        C("Starsurge", u);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Moonkin()
        {
            WoWUnit u = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(unit => unit.IsHostile && unit.IsAlive && unit.Distance2D < 40 && !unit.Mounted && unit.InLineOfSight);

            if (u != null)
            {
                if (!HazzDruidSettings.Instance.SpecBalance)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && u.Distance2D > 40 && u.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, u.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    StyxWoW.Me.CurrentTarget.Face();

                    if (CC("Wrath", u) && Me.HasAura("Eclipse (Solar)"))
                    {
                        Logging.Write("Wrath");
                        PlayerMover();
                        C("Wrath", u);
                        return true;
                    }
                    if (Me.HasAura("Eclipse (Lunar)"))
                    {
                        Logging.Write("Starfire");
                        PlayerMover();
                        C("Starfire", u);
                        return true;
                    }
                    if (CC("Starfire", u) && Me.CurrentEclipse >= 1 && !Me.HasAura("Eclipse (Solar)"))
                    {
                        Logging.Write("Starfire");
                        PlayerMover();
                        C("Starfire", u);
                        return true;
                    }
                    if (Me.CurrentEclipse <= 0 && !Me.HasAura("Eclipse (Lunar)"))
                    {
                        Logging.Write("Wrath");
                        PlayerMover();
                        C("Wrath", u);
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool SelfBuff()
        {
            WoWPlayer tar = Threat();

            if (tar != null)
            {
                    StyxWoW.Me.CurrentTarget.Face();

                    if (HazzDruidSettings.Instance.UseBarkskin && CC("Barkskin") && !Me.HasAura("Barkskin"))
                    {
                        Logging.Write("Barkskin");
                        C("Barkskin", Me);
                        return true;
                    }
                    if (HazzDruidSettings.Instance.UseThorns && CC("Thorns") && !Me.HasAura("Thorns"))
                    {
                        Logging.Write("Thorns");
                        C("Thorns", Me);
                        return true;
                    }
                    if (HazzDruidSettings.Instance.UseThorns && CC("Typhoon"))
                    {
                        Logging.Write("Typhoon");
                        C("Typhoon", Me);
                        return true;
                    }
                    if (HazzDruidSettings.Instance.UseGrasp && CC("Nature's Grasp") && !Me.HasAura("Nature's Grasp"))
                    {
                        Logging.Write("Nature's Grasp");
                        C("Nature's Grasp", Me);
                        return true;
                    }
                    return false;
                }
                return false;
            }

        private bool Lifebloom()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                if (!HazzDruidSettings.Instance.UseLifebloom)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && tar.Distance2D > 40 && tar.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, tar.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return false;
                }
                else
                {
                    String s = null;
                    bool needCast = false;
                    double hp = tar.HealthPercent;

                    if (!CheckUnit(Me))
                    {
                        return false;
                    }
                    if (HazzDruidSettings.Instance.UseLifebloom && tar.Guid == tank.Guid && CC("Lifebloom") && !isAuraActive("Lifebloom", tar))
                    {
                        Logging.Write("Lifebloom");
                        C("Lifebloom", tar);
                        return true;
                    }
                    if (HazzDruidSettings.Instance.UseLifebloom && tar.Guid == tank.Guid && CC("Lifebloom") && isAuraActive("Lifebloom", tar) && tar.ActiveAuras["Lifebloom"].StackCount < 3)
                    {
                        Logging.Write("Lifebloom");
                        C("Lifebloom", tar);
                        return true;
                    }
                    if (HazzDruidSettings.Instance.UseLifebloom && tar.Guid == tank.Guid && CC("Lifebloom") && isAuraActive("Lifebloom", tar) && tar.Auras["Lifebloom"].TimeLeft.TotalSeconds < 3)
                    {
                        Logging.Write("Lifebloom");
                        C("Lifebloom", tar);
                        return true;
                    }
                    if (s != null && CC(s, tar))
                    {
                        if (!C(s, tar))
                        {
                        }
                        if (!needCast)
                        {
                        }
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

        private bool Harmony()
        {
            WoWPlayer tar = GetHealTarget();

            if (tar != null)
            {
                if (!HazzDruidSettings.Instance.SpecRestoration)
                {
                    return false;
                }
                else if (Battlegrounds.IsInsideBattleground && tar.Distance2D > 40 && tar.Distance2D < 60)
                {
                    WoWPoint moveTo = WoWMathHelper.CalculatePointFrom(Me.Location, tar.Location, 15f);

                    if (Navigator.CanNavigateFully(Me.Location, moveTo))
                    {
                        Navigator.MoveTo(moveTo);
                    }
                    return true;
                }
                else
                {
                    if (!CheckUnit(Me))
                    {
                        return false;
                    }
                    if (Me.ActiveAuras.ContainsKey("Harmony") && Me.ActiveAuras["Harmony"].TimeLeft.TotalSeconds < 3)
                    {
                        Logging.Write("Harmony Nourish");
                        PlayerMover();
                        C("Nourish", tar);
                        return true;
                    }
                }
                return false;
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

        private bool C(string spell, WoWUnit target)
        {
            if (SpellManager.Cast(spell, target))
            {
                lastCast = target;
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
            return SpellManager.Cast(spell);
        }

        private bool PlayerMover()
        {
            if (!HazzDruidSettings.Instance.StillCasting)
            {
                Navigator.PlayerMover.MoveStop();
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool Cleansing()
        {
            if (HazzDruidSettings.Instance.UseRemoveCurse)
            {
                WoWPlayer p = GetCleanseTarget();
                if (p != null)
                {
                    if (p.Distance2D > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.SpecRestoration && CC("Remove Corruption", p))
                    {
                        Logging.Write("Remove Corruption");
                        C("Remove Corruption", p);
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

        private bool NeedsCleanse(WoWPlayer p)
        {
            foreach (WoWAura a in p.ActiveAuras.Values)
            {
                if (a.IsHarmful && Me.ManaPercent > 50)
                {
                    WoWDispelType t = a.Spell.DispelType;
                    if (t == WoWDispelType.Curse || t == WoWDispelType.Magic || t == WoWDispelType.Poison)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private WoWPlayer GetHealTarget()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true)
                    orderby unit.HealthPercent ascending
                    where unit.IsInMyPartyOrRaid || unit.IsMe
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance < 40
                    where unit.HealthPercent < 99
                    select unit).FirstOrDefault();

        }
        
        private WoWPlayer GetCleanseTarget()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(false, true)
                    orderby unit.HealthPercent ascending
                    where unit.IsInMyPartyOrRaid || unit.IsMe
                    where !unit.Dead
                    where !unit.IsGhost
                    where unit.Distance2D < 40
                    where NeedsCleanse(unit)
                    select unit).FirstOrDefault();
        }

        private WoWPlayer Threat()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(false, true)
                    orderby unit.Distance2D ascending
                    where unit.IsAutoAttacking
                    where unit.Distance2D < 5
                    where unit.IsHostile
                    select unit).FirstOrDefault();
        }

        private IEnumerable<WoWPlayer> GetResurrectTargets()
        {
            return (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                    orderby unit.Distance2D ascending
                    where unit.Dead
                    where unit.IsInMyPartyOrRaid
                    where !unit.IsGhost
                    where unit.Distance2D < 40
                    select unit);
        }

        private bool Rebirth()
        {
            foreach (WoWPlayer p in GetResurrectTargets())
            {
                if (Blacklist.Contains(p.Guid, true))
                {
                    continue;
                }
                else
                {
                    if (p.Distance2D > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseRebirth && CC("Rebirth", p))
                    {
                        Logging.Write("Rebirth" + p);
                        C("Rebirth", p);
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

        private int FriendlyCount(int hp)
        {
            int count = 0;
            foreach (WoWPlayer p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true))
            {
                if (p.IsFriendly
                    && p.IsAlive
                    && p.Distance2D < 40
                    && p.HealthPercent <= hp)
                {
                    count++;
                }
            }
            return count;
        } 

        private int HostileCount(int hp)
        {
            int count = 0;
            foreach (WoWPlayer p in ObjectManager.GetObjectsOfType<WoWPlayer>(true, true))
            {
                if (p.IsHostile
                    && p.IsAlive
                    && p.Distance2D < 40
                    && p.HealthPercent <= hp)
                {
                    count++;
                }
            }
            return count;
        } 

        private bool Revive()
        {
            foreach (WoWPlayer p in GetResurrectTargets())
            {
                if (Blacklist.Contains(p.Guid, true))
                {
                    continue;
                }
                else
                {
                    if (p.Distance2D > 40 || !p.InLineOfSight)
                    {
                        return true;
                    }
                    else if (HazzDruidSettings.Instance.UseRevive && CC("Revive", p))
                    {
                        Logging.Write("Revive " + p);
                        C("Revive", p);
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

        private bool Buff()
        {
            if (HazzDruidSettings.Instance.UseMoTW && !Me.IsGhost && !Me.Mounted && !isAuraActive("Mark of the Wild"))
            {
                Logging.Write("Mark of the Wild");
                C("Mark of the Wild", Me);
                return false;
            }
            foreach (WoWPlayer p in Me.PartyMembers)
            {
                if (p.Distance2D > 40 || p.Dead || p.IsGhost)
                    continue;
                else if (HazzDruidSettings.Instance.UseMoTW && !isAuraActive("Blessing of Kings", p) && !isAuraActive("Mark of the Wild", p))
                {
                    Logging.Write("Mark of the Wild");
                    C("Mark of the Wild", p);
                    return false;
                }
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

        public override sealed string Name { get { return "HazzDruid EliT3.4"; } }

        public override WoWClass Class { get { return WoWClass.Druid; } }

        private static LocalPlayer Me { get { return ObjectManager.Me; } }

        public override bool NeedRest
        {
            get
            {
                if (Me.ManaPercent < HazzDruidSettings.Instance.ManaPercent && Me.ActiveAuras.ContainsKey("Drink"))
                {
                    Logging.Write("Drinking");
                    if (Me.Mounted) { return false; }
                    if (Me.IsMoving) { return false; }
                    if (Me.Dead || Me.IsGhost || Me.Combat) { return false; }
                    if (Me.Auras.ContainsKey("Resurrection Sickness")) { return true; }
                    if (Me.IsSwimming) { return false; }
                    if (Me.ManaPercent <= 99) { return false; }
                }
                if (Me.ManaPercent < HazzDruidSettings.Instance.ManaPercent && Me.ActiveAuras.ContainsKey("Food"))
                {
                    Logging.Write("Eating");
                    if (Me.Mounted) { return false; }
                    if (Me.IsMoving) { return false; }
                    if (Me.Dead || Me.IsGhost || Me.Combat) { return false; }
                    if (Me.Auras.ContainsKey("Resurrection Sickness")) { return true; }
                    if (Me.IsSwimming) { return false; }
                    if (Me.HealthPercent <= 99) { return false; }
                }
                return false;
            }
        }
        public override void Rest()
        {
            Styx.Logic.Common.Rest.Feed();
        }

        public override bool NeedPullBuffs { get { Pulse(); return false; } }

        public override bool NeedCombatBuffs { get { Pulse(); return false; } }

        public override bool NeedPreCombatBuffs { get { Pulse(); return false; } }

    }
}