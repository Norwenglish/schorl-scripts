using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Styx;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Durid
{
    partial class DuridRoutine
    {
        public LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        public bool IsRooted
        {
            get
            {
                var rootedAurasCount =
                    Me.ActiveAuras.Values.Where(
                        a => a.IsHarmful && (a.Spell.Mechanic == WoWSpellMechanic.Rooted || a.Spell.Mechanic == WoWSpellMechanic.Frozen)).Count();

                return rootedAurasCount != 0 || StyxWoW.Me.GetAllAuras().Select(a => a.Name).ContainsAny(
                    new[] { "Frost Nova", "Shattered Barrier", "Improved Hamstring", "Freeze" });
            }
        }

        public void EnsureCurrentForm()
        {
            //Log("Mine: " + Me.Shapeshift + ", Wanted: " + CurrentShapeshift);
            if (StyxWoW.Me.Shapeshift != CurrentShapeshift)
            {
                Logging.Write("Shifting to " + CurrentShapeshift.FormFromShift());
                if (SpellManager.CanCast(CurrentShapeshift.FormFromShift()))
                    SpellManager.Cast(CurrentShapeshift.FormFromShift());
            }
        }
        public bool Attackable(WoWUnit u)
        {
            return !u.HasAura("Deterrence") && !u.HasAura("Divine Shield") &&
                   !u.HasAura("Hand of Protection") &&
                   !u.HasAura("Ice Block");
        }

        public WoWUnit AlternateTarget(WoWUnit currentTarget)
        {
            if (currentTarget != null)
            {
                var tars = (from u in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                            where u.IsAlliance != Me.IsAlliance
                                  && u.IsAlive
                                  && u.Guid != currentTarget.Guid
                                  && Attackable(u)
                                  && u.Distance < currentTarget.Distance
                            orderby u.Distance ascending
                            select u);
                return tars.FirstOrDefault();
            }
            else
            {
                var tars = (from u in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                            where u.IsAlliance != Me.IsAlliance
                                  && u.IsAlive
                                  && u.Distance < 40
                                  && Attackable(u)
                            orderby u.Distance ascending
                            select u);
                return tars.FirstOrDefault();
            }
        }
    }

    public static class Extensions
    {
        public static string FormFromShift(this ShapeshiftForm ss)
        {
            switch (ss)
            {
                case ShapeshiftForm.Bear:
                case ShapeshiftForm.DireBear:
                    return "Bear Form";

                case ShapeshiftForm.Cat:
                    return "Cat Form";

                case ShapeshiftForm.Travel:
                    return "Travel Form";

                case ShapeshiftForm.Aqua:
                    return "Aquatic Form";

                case ShapeshiftForm.Moonkin:
                    return "Moonkin Form";

                case ShapeshiftForm.TreeOfLife:
                    return "Tree of Life";

                default:
                    return null;
            }
        }
    }
}
