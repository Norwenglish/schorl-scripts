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
        public class SpellPriorityList : List<SpellAdapter>
        {
            public SpellPriorityList()
            {
            }

            public SpellPriorityList(List<SpellAdapter> init)
            {
                InsertRange(0, init);
                RemoveUnknownSpells();
                SortSpellsByPriority();
            }

            public SpellAdapter ChooseBestSpell()
            {
                return this.FirstOrDefault(s => s.CanCast);
            }

            private void RemoveUnknownSpells()
            {
                while (this.Any() && !this[0].IsKnown)
                    this.RemoveAt(0);
            }

            private void SortSpellsByPriority()
            {
                this.Sort(SpellAdapter.CompareByPriority);
            }
        }

        public class SpellAdapter
        {
            public SpellAdapter()
            {
            }

            public SpellAdapter(int prio, string spellName)
            {
                Priority = prio;
                SpellName = spellName;
            }

            protected WoWSpell Spell { get; set; }

            public virtual int Priority { get; set; }
            public virtual string SpellName { get; set; }

            public virtual bool IsKnown
            {
                get
                {
                    return SpellManager.HasSpell(SpellName);
                }
            }

            public virtual bool CanCast
            {
                get
                {
                    if (Safe_IsMoving())
                    {
                        if (Spell.CastTime != 0)
                        {
                            const int SPIRITWALKERS_GRACE = 79206;
                            if (!StyxWoW.Me.Buffs.Any(b => b.Value.SpellId == SPIRITWALKERS_GRACE))
                            {
                                return false;
                            }
                        }
                    }

                    return Spell.CanCast;
                }
            }

            public virtual bool Cast(WoWUnit unit)
            {
                if (IsImmune(_me.CurrentTarget, Spell.School))
                {
                    Dlog("skipping Lightning Bolt since {0}[{1}] is immune to {2} damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry, Spell.School);
                    return false;
                }

                return Safe_CastSpell(unit, Spell);
            }

            public static int CompareByPriority(SpellAdapter x, SpellAdapter y)
            {
                try
                {
                    if (x == null)
                        return (y == null ? 0 : 1);

                    if (y == null)
                        return -1;

                    return x.Priority - y.Priority;
                }
                catch (ThreadAbortException) { throw; }
                catch
                {
                    Dlog("SpellAdapter.Compare: EXCEPTION: a heal target left group or released -- ignoring");
                }

                return 0;
            }
        }

        public class InstantSpell : SpellAdapter
        {
            public InstantSpell(int prio, string spellName)
            {
                Priority = prio;
                SpellName = spellName;
            }

            public override bool CanCast
            {
                get
                {
                    return Spell.CanCast;
                }
            }
        }

        public class SpellLightningBolt : SpellAdapter
        {
            public override string SpellName { get { return "Lightning Bolt"; } }
            public override bool CanCast
            {
                get
                {
                    if (Safe_IsMoving() && !_hasGlyphOfUnleashedLightning)
                        return false;
                    return base.CanCast;
                }
            }
        }

        public class PriorityAdapter : SpellAdapter
        {
            PriorityAdapter(int prio, string spellName, ConfigValues.SpellPriority userPrio)
            {
                Priority = prio;
                SpellName = spellName;
                UserPriority = userPrio;
            }

            public ConfigValues.SpellPriority UserPriority;

            public override bool CanCast
            {
                get
                {
                    if (priorityPurge == ConfigValues.SpellPriority.None)
                        return false;

                    if (priorityPurge != UserPriority)
                        return false;

                    return base.CanCast;
                }
            }
        }

        public class SearingTotemAdapter : SpellAdapter
        {
            public override string SpellName { get { return "Searing Totem"; } }
            public override bool CanCast
            {
                get
                {
                    return base.CanCast;
                }
            }
        }
    }
}
