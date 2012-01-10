using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;

using Action = TreeSharp.Action;

namespace Durid
{
    public delegate WoWUnit TargetSelectorDelegate(object context);
    
    public partial class DuridRoutine : CombatRoutine
    {
        public override string Name { get { return "Durid"; } }

        public override WoWClass Class { get { return WoWClass.Druid; } }

        public ShapeshiftForm CurrentShapeshift { get; private set; }

        public override void Pulse()
        {
            if (Battlegrounds.IsInsideBattleground)
            {
                var target = Me.CurrentTarget;
                if (target == null || target.Dead || !Attackable(target))
                {
                    target = AlternateTarget(null);
                    if (target == null)
                        return;
                    target.Target();
                }
            }
        }

        public override void Initialize()
        {
            //foreach (var s in SpellManager.Spells)
            //{
            //    Log(s.Key + " => " + s.Value.Id);
            //}

            CurrentShapeshift = ShapeshiftForm.Cat;
            Lua.Events.AttachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLogEvent);
        }

        private void HandleCombatLogEvent(object sender, LuaEventArgs args)
        {
            // Yea, a bit of a hack, but w/e. I'm lazy. Fuck off!
            var realArgs = new CombatLogEventArgs(args.EventName, args.FireTimeStamp, args.Args);

            switch (realArgs.Event)
            {
                case "SPELL_MISSED":
                    switch (realArgs.SuffixParams[0].ToString())
                    {
                        case "IMMUNE":
                            Log("Mob is immune. We should probably add it to our immunity map. But I haven't written one!");
                            break;

                        case "EVADE":
                            WoWUnit unit = realArgs.DestUnit;
                            ulong guid;
                            if (unit == null)
                            {
                                Log("Couldn't find the evading mob to blacklist, in the object manager. Using the GUID from the event instead.");
                                guid = realArgs.DestGuid;
                            }
                            else
                            {
                                guid = unit.Guid;

                                // Make sure we clear the target if we're blacklisting it.
                                if (unit == Me.CurrentTarget)
                                {
                                    Me.ClearTarget();
                                }
                            }
                            Blacklist.Add(guid, TimeSpan.FromMinutes(5));
                            break;
                    }
                    break;
                case "SPELL_CAST_SUCCESS":

                    // We don't want to do anything, if it wasn't us that succeeded!
                    if (realArgs.SourceGuid != Me.Guid)
                    {
                        return;
                    }

                    // We need to 'sleep' for these spells. Otherwise, we'll end up double-casting them. Which will cause issues.
                    switch (realArgs.Spell.Name)
                    {
                        case "Rejuvenation":
                        case "Lifebloom":
                        case "Regrowth":
                        case "Nourish":
                        case "Healing Touch":
                            Log("Sleeping for heal success.");
                            StyxWoW.SleepForLagDuration();

                            // No, we don't need this. If there's a read that isn't volatile, we'll make sure to sleep for it specifically.
                            //ObjectManager.Update();
                            break;
                    }
                    break;
            }
        }

        #region Logging

        protected void Log(string msg)
        {
            Logging.Write(Color.Orange, msg);
        }

        protected void LogDebug(string msg)
        {
            Logging.Write(Color.Red, msg);
        }

        #endregion

        private delegate string GetSpellName(object o);
        #region Behavior Helpers

        private Composite CreateSelfCast(string spell)
        {
            return CreateCast(ret=>spell, ret => true, ret => Me);
        }

        private Composite CreateCast(GetSpellName getSpell)
        {
            return CreateCast(getSpell, ret => true, ret => StyxWoW.Me.CurrentTarget);
        }

        private Composite CreateCast(string spell)
        {
            // Default to casting on our target.
            return CreateCast(ret=>spell, ret => true, ret => StyxWoW.Me.CurrentTarget);
        }
        private Composite CreateSelfCast(string spell, CanRunDecoratorDelegate extraRun)
        {
            return CreateCast(ret=>spell, extraRun, ret => Me);
        }

        private Composite CreateCast(string spell, CanRunDecoratorDelegate extraRun)
        {
            // Default to casting on our target.
            return CreateCast(ret=>spell, extraRun, ret => StyxWoW.Me.CurrentTarget);
        }

        private Composite CreateCast(GetSpellName spell, CanRunDecoratorDelegate extraRun, TargetSelectorDelegate onTarget)
        {
            return new Decorator(extraRun,
                new PrioritySelector(
                    new Decorator(
                        // Now just check if we can cast the spell, etc.
                        ret => SpellManager.CanCast(spell(ret)),
                        new Sequence(
                            new ActionLog("Casting " + spell(null)),
                            new Action(exe => SpellManager.Cast(spell(exe)))))

                    //,new ActionLog("Can't cast " + spell + " - " + SpellManager.CanCast(spell))

                    ));
        }

        private Composite CreateSelfBuff(string spell)
        {
            return CreateBuff(spell, ret => true, ret => Me);
        }

        private Composite CreateBuff(string spell)
        {
            return CreateBuff(spell, ret => true, ret => Me.CurrentTarget);
        }
        private Composite CreateSelfBuff(string spell, CanRunDecoratorDelegate extraRun)
        {
            return CreateBuff(spell, extraRun, ret => Me);
        }

        private Composite CreateBuff(string spell, CanRunDecoratorDelegate extraRun)
        {
            return CreateBuff(spell, extraRun, ret => Me.CurrentTarget);
        }

        private Composite CreateBuff(string spell, CanRunDecoratorDelegate extraRun, TargetSelectorDelegate onTarget)
        {

            return new Decorator(
                extraRun,
                new PrioritySelector(
                    new Decorator(
                        // Now just check if we can cast the spell, etc.
                        ret => SpellManager.CanBuff(spell, onTarget(ret), true),
                        new Action(exe => SpellManager.Buff(spell, onTarget(exe))))));
        }

        private Composite EnsureFacingTarget()
        {
            return new Decorator(
                // Use a 90 degree cone, instead of 150. This ensures we're always *actually* facing the mob.
                ret => !Me.IsSafelyFacing(StyxWoW.Me.CurrentTarget, 90f), 
                new Action(ret => StyxWoW.Me.CurrentTarget.Face()));
        }

        #endregion

        #region Simple Wrappers

        private static List<WoWUnit> Adds
        {
            get
            {
                try
                {
                    // Get adds sorted by distance, and health percent.
                    IOrderedEnumerable<WoWUnit> ret = from o in ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                              where
                                  o.IsValid && (StyxWoW.Me.CurrentTarget == null || o != StyxWoW.Me.CurrentTarget) && o.Attackable && !o.IsFriendly &&
                                  !Blacklist.Contains(o) && !o.IsTotem &&
                                  (o.IsTargetingMeOrPet || o.IsTargetingMyPartyMember || o.IsTargetingMyRaidMember)
                              orderby o.DistanceSqr ascending
                              orderby o.HealthPercent ascending
                              select o;
                    return ret.ToList();
                }
                catch (ArgumentNullException)
                {
                    return new List<WoWUnit>();
                }
            }
        }

        #endregion

        #region Stolen from Shredder. Thanks to exemplar / regecksqt for the base code. Modified to be a bit less... silly

        public bool PvpAttackable(WoWUnit u)
        {
            IEnumerable<string> auras = u.GetAllAuras().Select(a => a.Name);

            return !auras.ContainsAny(new[] { "Deterrence", "Divine Shield", "Hand of Protection", "Ice Block" });
        }
        public bool IsBehind(WoWUnit target)
        {
            return WoWMathHelper.IsBehind(StyxWoW.Me.Location, target.Location, target.Rotation, (float)Math.PI);
        }

        public WoWPoint PointBehind(WoWUnit target)
        {
            return WoWMathHelper.CalculatePointBehind(target.Location, target.Rotation, 1);
        }

        public WoWUnit AlternatePvpTarget(WoWUnit current)
        {
            if (current != null)
            {
                var tars = (from u in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                            where
                                u.IsAlliance != Me.IsAlliance && u.IsAlive && u.Guid != current.Guid && PvpAttackable(u) &&
                                u.Distance < current.Distance
                            // DistanceSqr is slightly faster than Distance. So, since we're only using it for ordering, just use it.
                            orderby u.DistanceSqr ascending
                            select u);
                return tars.FirstOrDefault();
            }
            else
            {
                var tars = (from u in ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                            where u.IsAlliance != Me.IsAlliance && u.IsAlive && u.Distance < 40 && PvpAttackable(u)
                            orderby u.DistanceSqr ascending
                            select u);
                return tars.FirstOrDefault();
            }
        }

        #endregion

    }

    /// <summary>
    ///   Just a small action to facilitate logging in the BT branches
    /// </summary>
    /// <remarks>
    ///   Created 12/29/2010.
    /// </remarks>
    public class ActionLog : Action
    {
        public delegate string GetLogMessage(object ctx);
        private readonly bool _debug;

        private readonly string _message;

        private GetLogMessage _getMessage;
        public ActionLog(GetLogMessage getMessage)
        {
            _getMessage = getMessage;
        }

        public ActionLog(string message)
        {
            _message = message;
        }

        public ActionLog(string message, bool debug) : this(message)
        {
            _debug = debug;
        }

        protected override RunStatus Run(object context)
        {
            string msg = null;
            if (_getMessage != null)
                msg = _getMessage(context);
            else
                msg = _message;

            if (_debug)
            {
                Logging.WriteDebug(msg);
            }
            else
            {
                Logging.Write(Color.Orange, msg);
            }

            // Selectors continue when the node fails. So we want to let it continue.););
            if (Parent is Selector)
            {
                return RunStatus.Failure;
            }

            // Everything else only continues if we succeed. So do that.
            return RunStatus.Success;
        }
    }
}