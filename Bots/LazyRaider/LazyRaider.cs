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
        #region Overrides of BotBase

        private readonly Version _version = new Version(2, 0, 3);

        public override string Name
        {
            get { return "LazyRaider"; }
        }

        public static LocalPlayer Me { get { return StyxWoW.Me; } }
        public static bool IsInGroup { get { return Me.IsInRaid || Me.IsInParty; } }
        public static List<WoWPlayer> GroupMembers { get { return !Me.IsInRaid ? Me.PartyMembers : Me.RaidMembers; } }
        public static List<WoWPartyMember> GroupMemberInfos { get { return !Me.IsInRaid ? Me.PartyMemberInfos : Me.RaidMemberInfos; } }


        public static WoWPoint _lastDest;
        public static bool IamTheTank = false;
        public static byte PreviousTicksPerSecond;
        public static bool UsesBT = false;

        public static bool Paused = false;

        public static PulseFlags pulseFlags;

        public SelectTankForm _frm;
        public override System.Windows.Forms.Form ConfigurationForm
        {
            get
            {
                return _frm ?? (_frm = new SelectTankForm());
            }
        }

        private static Composite _root;
        public override Composite Root
        {
            get
            {
                if (_root == null)
                {
                    _root = new PrioritySelector(
                                new Decorator(ret => Paused, new ActionAlwaysSucceed()),
                                new Decorator(ret => !(Me.Mounted && CharacterSettings.Instance.UseMount && LazyRaiderSettings.Instance.DismountOnlyWithTankOrUser),
                                    CreateRestBuffCombatBehavior()
                                    ),
                                CreateDetectTankBehavior(),
                                CreateFollowBehavior()
                                );
                }
                return _root;
            }
        }

        public override PulseFlags PulseFlags
        {
            get
            {
                return pulseFlags;
            }
        }

        public override void Initialize()
        {
            Log("Version {0} initialized", _version);
            LazyRaiderSettings.Instance.Load();
            Logic.Profiles.ProfileManager.LoadEmpty();
            RefreshSettingsCache();
            Log("Blank Profile loaded");

            base.Initialize();
        }

        public static void RefreshSettingsCache()
        {
            // reset the BT so values can be applied next tick
            _root = null;

            // set pulseFlags
            pulseFlags = PulseFlags.Objects | PulseFlags.Lua;

            if (LazyRaiderSettings.Instance.AutoTarget)
                pulseFlags |= PulseFlags.Targeting;

            if (!LazyRaiderSettings.Instance.DisablePlugins)
                pulseFlags |= PulseFlags.Plugins;

            // set the FPS to use
            TreeRoot.TicksPerSecond = (byte)LazyRaiderSettings.Instance.FPS;

            Dlog("NoTank set to {0}", LazyRaiderSettings.Instance.NoTank);
            Dlog("FollowTank set to {0}", LazyRaiderSettings.Instance.FollowTank);
            Dlog("FollowDistance set to {0}", LazyRaiderSettings.Instance.FollowDistance);
            Dlog("AutoTankSelect set to {0}", LazyRaiderSettings.Instance.AutoTankSelect);
            Dlog("AutoTarget set to {0}", LazyRaiderSettings.Instance.AutoTarget);
            Log("FPS={0}, DisablePlugins={1}, LockMemory={2}, PauseKey={3}", LazyRaiderSettings.Instance.FPS, LazyRaiderSettings.Instance.DisablePlugins, LazyRaiderSettings.Instance.LockMemory, LazyRaiderSettings.Instance.PauseKey );
        }

        public override void Start()
        {
            _root = null;
            PreviousTicksPerSecond = TreeRoot.TicksPerSecond;

            Dlog("Start: currfps={0:F0} maxfps={1} combat={2} oldtps={3}", GetFramerate(), MaxFPS(), Me.Combat, TreeRoot.TicksPerSecond);
            BotEvents.Player.OnMapChanged += Player_OnMapChanged;
            Targeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
            Targeting.Instance.RemoveTargetsFilter += RemoveTargetsFilter;
            Targeting.Instance.WeighTargetsFilter += WeighTargetsFilter;
            Lua.Events.AttachEvent("PARTY_MEMBERS_CHANGED", HandlePartyMembersChanged);
            Lua.Events.AttachEvent("MODIFIER_STATE_CHANGED", HandleModifierStateChanged);
            StyxSettings.Instance.LogoutForInactivity = false;
            Log("Version {0} Started", _version);

            if (Paused)
                Log(Color.Orange, "LazyRaider currently PAUSED - Press {0} in WOW to continue...", LazyRaiderSettings.Instance.PauseKey.ToString());
            else 
                Log(Color.Orange, "Pause LazyRaider anytime by pressing {0} key in WOW", LazyRaiderSettings.Instance.PauseKey.ToString());

            _lastDest = new WoWPoint();
        }

        public override void Stop()
        {
            TreeRoot.TicksPerSecond = PreviousTicksPerSecond;
            Lua.Events.DetachEvent("MODIFIER_STATE_CHANGED", HandleModifierStateChanged);
            Lua.Events.DetachEvent("PARTY_MEMBERS_CHANGED", HandlePartyMembersChanged);
            Targeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
            Targeting.Instance.RemoveTargetsFilter -= RemoveTargetsFilter;
            Targeting.Instance.WeighTargetsFilter -= WeighTargetsFilter;
            BotEvents.Player.OnMapChanged -= Player_OnMapChanged;
            StyxSettings.Instance.LogoutForInactivity = true;
            Log("Version {0} Stopped", _version);
        }

        #endregion

        #region MISC

        public static bool IsGameStable()
        {
            return ObjectManager.IsInGame && Me != null && Me.IsValid;
        }

        public static bool IsBtBasedCombatClass()
        {
            return !(RoutineManager.Current.CombatBehavior is TreeSharp.Action);
        }

        public static int MaxFPS()
        {
            return Convert.ToInt32(GetCVar("maxFPS"));
        }

        public static string GetCVar(string cvar)
        {
            try
            {
                List<string> ret = Lua.GetReturnValues("return GetCVar(\"" + cvar + "\")");
                if (ret != null && ret.Count > 0)
                {
                    return ret[0];
                }
            }
            catch
            {
            }
            return String.Empty;
        }

        public static double GetFramerate()
        {
            double dRate = 0;
            List<string> ret = Lua.GetReturnValues("return GetFramerate()");
            if (ret != null && ret.Count > 0)
            {
                double.TryParse(ret[0], out dRate);
            }
            return dRate;
        }

        private static uint lineCount = 0;

        public static void Status(string msg, params object[] args)
        {
            TreeRoot.StatusText = "[Lazy] " + String.Format(msg, args);
        }

        public static void Log(string msg, params object[] args)
        {
            Log(Color.Gold, msg, args);
        }

        public static void Log(Color clr, string msg, params object[] args)
        {
            try
            {
                // following linecount hack is to stop dup line suppression of Log window
                Logging.Write(clr, "[LazyRaider] " + msg + (++lineCount % 2 == 0 ? "" : " "), args);
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Logging.Write(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred logging msg: \n\t\"" + SafeLogException(msg) + "\"");
                Logging.WriteException(e);
            }
        }

        public static void Dlog(string msg, params object[] args)
        {
            try
            {
                // following linecount hack is to stop dup line suppression of Log window
                Logging.WriteDebug("+LazyRaider+ " + msg + (((++lineCount) & 1) == 0 ? "" : " "), args);
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Logging.Write(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred logging msg: \n\t\"" + SafeLogException(msg) + "\"");
                Logging.WriteException(e);
            }
        }

        public static string SafeLogException(string msg)
        {
            msg = msg.Replace("{", "(");
            msg = msg.Replace("}", ")");
            return msg;
        }

        #endregion

        #region Targeting Filter
#if NOT_CURRENTLY_USED
        private static WoWUnit GetBestTarget()
        {
            if (StyxWoW.Me.GotTarget && StyxWoW.Me.CurrentTarget.Attackable)
            {
                return StyxWoW.Me.CurrentTarget;
            }

            if (RaFHelper.Leader != null && RaFHelper.Leader.GotTarget && RaFHelper.Leader.CurrentTarget.Attackable)
            {
                return RaFHelper.Leader.CurrentTarget;
            }

            if (Battlegrounds.IsInsideBattleground)
            {
                return (from o in ObjectManager.ObjectList
                        where o is WoWPlayer && o.Location.DistanceSqr(Me.Location) < Targeting.PullDistanceSqr
                        let p = o.ToUnit().ToPlayer()
                        where p.IsHorde != Me.IsHorde
                        orderby p.CurrentHealth
                        select p.ToUnit()).FirstOrDefault();
            }
            else
            {
                return (from o in ObjectManager.ObjectList
                        where o is WoWUnit && o.Location.DistanceSqr(Me.Location) < Targeting.PullDistanceSqr
                        let unit = o.ToUnit()
                        where unit.Attackable && (!unit.IsPlayer ? !unit.IsFriendly : unit.ToPlayer().IsHorde != Me.IsHorde)
                        orderby unit.Location.DistanceSqr(Me.Location)
                        select unit).FirstOrDefault();
            }
        }
#endif
        #endregion

        #region Behaviors

        #region Combat Behavior

#if NO_PULL_CURRENTLY
        private static bool NeedPull(object context)
        {
            var target = StyxWoW.Me.CurrentTarget;

            if (target == null)
                return false;

            if (!target.InLineOfSight)
                return false;

            if (target.Distance > Targeting.PullDistance)
                return false;

            return true;
        }
#endif
        private static Composite CreateDetectTankBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !(LazyRaiderSettings.Instance.NoTank || Tank.IsLeader()),
                    new Action(delegate
                        {
                            return SyncTankWithRaFLeader() ? RunStatus.Success : RunStatus.Failure;
                        })
                    )
                );
        }

        private static Composite CreateRestBuffCombatBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !Me.IsAlive, new ActionAlwaysSucceed()),

                new Decorator(ret => Me.Combat, CreateCombatBehavior()),

                new PrioritySelector(
                    RoutineManager.Current.RestBehavior,
                    RoutineManager.Current.PreCombatBuffBehavior

#if NO_PULL_CURRENTLY
                        ,
                    // new ActionDebugString("[Combat] Pull"),
                // Don't pull, unless we've decided to pull already.
                    new Decorator(ret => BotPoi.Current.Type == PoiType.Kill,
                        new PrioritySelector(
                // Make sure we have a valid target list.
                            new Decorator(ret => Targeting.Instance.TargetList.Count != 0,
                // Force the 'correct' POI to be our target.
                                new Decorator(ret => BotPoi.Current.AsObject != Targeting.Instance.FirstUnit &&
                                    BotPoi.Current.Type == PoiType.Kill,
                                    new Sequence(

                                        new Action(ret => BotPoi.Current = new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)),
                                        new Action(ret => BotPoi.Current.AsObject.ToUnit().Target())
                                        )
                                    )
                                ),

                            new Decorator(ctx => NeedPull(ctx),
                                new PrioritySelector(
                                    RoutineManager.Current.PullBuffBehavior,
                                    RoutineManager.Current.PullBBehavior
                                    )
                                )
                            )
                        )
#endif
                    // , new ActionAlwaysSucceed()
                    )
                );
        }

        private static Composite CreateCombatBehavior()
        {
            return CreateCombatBehaviorContent();
        }
        private static Composite CreateCombatBehaviorFL()
        {
            return new FrameLockSelector(
                CreateCombatBehaviorContent()
                );
        }
        private static Composite CreateCombatBehaviorContent()
        {
            return new PrioritySelector(
                new Decorator(ret => LazyRaiderSettings.Instance.AutoTarget && Targeting.Instance.FirstUnit != null,
                    new PrioritySelector(
                        new Decorator(
                            ret => BotPoi.Current == null
                                || BotPoi.Current.AsObject == null
                                || BotPoi.Current.AsObject.ToUnit() == null
                                || BotPoi.Current.Type == PoiType.None,
                            new Sequence(
                                new Action(ret => Dlog("Setting POI to best target.")),
                                new ActionSetPoi(true, ret => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)))),

                        new Decorator(
                            ret => BotPoi.Current.AsObject.ToUnit() != Targeting.Instance.FirstUnit,
                            new Sequence(
                                new Action(ret => Dlog("Current POI is not the best target. Changing.")),
                                new ActionSetPoi(true, ret => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)))),

                        // Make sure we have the proper unit as target
                        new Decorator(
                            ret => Me.CurrentTarget == null || Me.CurrentTarget != BotPoi.Current.AsObject.ToUnit(),
                            new Sequence(
                                new Action(ret => BotPoi.Current.AsObject.ToUnit().Target()),
                // new Action(ret => TargetTimer.Reset()),
                                new WaitContinue(
                                    2,
                                    ret => Me.CurrentTarget != null && Me.CurrentTarget == BotPoi.Current.AsObject.ToUnit(),
                                    new ActionAlwaysSucceed())
                                ))
                        )
                    ),

                RoutineManager.Current.HealBehavior,
                RoutineManager.Current.CombatBuffBehavior,
                RoutineManager.Current.CombatBehavior,
                new ActionAlwaysSucceed()
                );
        }

        private static Composite CreateCombatBehaviorNonBT()
        {
            return new PrioritySelector(
                RoutineManager.Current.HealBehavior,
                RoutineManager.Current.CombatBuffBehavior,
                RoutineManager.Current.CombatBehavior,
                new ActionAlwaysSucceed()
                );
        }

        #endregion

        #region Find Leader Behavior

        private static bool SyncTankWithRaFLeader()
        {
            try
            {
                // check if Tank we saved is now in range 
                if (Tank.Current != null && Tank.Current.ToPlayer() != null)
                {
                    Dlog("SyncTankWithRaFLeader: Tank now in range, so setting RaFHelper");
                    Tank.SetAsLeader();
                    return false;
                }

                // otherwise, tank out of range pointer for WoWPlayer so clear
                if (RaFHelper.Leader != null)
                {
                    Dlog("SyncTankWithRaFLeader: Tank doesn't match RaFHelper so clearing");
                    RaFHelper.ClearLeader();
                }

                // user always wants to control choosing so bail whether tank or not
                if (!LazyRaiderSettings.Instance.AutoTankSelect)
                    return false;

                // have Tank, so keep it until user changes
                if (Tank.Current != null && Tank.Current.IsOnline)
                    return false;

                WoWPartyMember tank = (from pm in GroupMemberInfos
                                       where GetGroupRoleAssigned(pm) == WoWPartyMember.GroupRole.Tank
                                           && pm.Guid != Me.Guid
                                           && pm.IsOnline 
                                           && pm.ToPlayer() != null
                                       orderby pm.Location3D.Distance(Me.Location) ascending
                                       select pm).FirstOrDefault();
                if (tank != null)
                {
                    Tank.Current = tank;
                    Log("Tank set to {0} based upon role", LazyRaider.Safe_UnitName(Tank.Current));
                    TreeRoot.StatusText = String.Format("[lr] tank is {0}", Safe_UnitName(tank));
                    return true;
                }

#if DONT_SET_BY_MAX_HEALTH_FOR_NOW
                tank = (from pm in GroupMemberInfos
                        where pm.Guid != Me.Guid
                            && pm.IsOnline
                            && pm.ToPlayer() != null
                        orderby pm.HealthMax descending
                        select pm).FirstOrDefault();
                if (tank != null)
                {
                    Log("Tank set to {0} based upon Max Health", tank.ToPlayer().Class);
                    Tank.Current = tank;
                    TreeRoot.StatusText = String.Format("[lr] tank is {0}", Safe_UnitName(tank));
                    return true;
                }
#endif
                return false;
            }
            catch
            {
                return true;
            }
        }

        static bool haveWeChecked;

        private static bool DoWeNeedToFindLeader()
        {
            // check flag that avoids spamming check log message
            if (haveWeChecked)
                return false;

            if (Tank.Current == null && LazyRaiderSettings.Instance.NoTank)
                return false;

            return Tank.Current == null;
        }

        private static RunStatus DetectTheTank()
        {
            haveWeChecked = true;
            IamTheTank = (GetGroupRoleAssigned(Me) == WoWPartyMember.GroupRole.Tank);

            if (IamTheTank)
            {
                Log("Tank set to -ME-, max health {0}", Me.MaxHealth);
                Tank.Guid = Me.Guid;
                Tank.SetAsLeader();
                return RunStatus.Failure;
            }

            if (LazyRaiderSettings.Instance.NoTank)
            {
                Log("No Tank option enabled");
                Tank.Clear();
                return RunStatus.Failure;
            }

            if (!IsInGroup)
            {
                Log("Not in group, no tank needed");
                Tank.Clear();
                return RunStatus.Failure;
            }

            if (RaFHelper.Leader != null && !RaFHelper.Leader.IsValid)
            {
                Dlog("DetectTheTank: RaFHelper.Leader invalid, resetting");
                Tank.Clear();
            }

            // find one if no tank or autochoose and current not tank role
            if (RaFHelper.Leader == null)
            {
                Dlog("DetectTheTank: RaFHelper.Leader null, so searching");
            }
            else if (LazyRaiderSettings.Instance.AutoTankSelect && WoWPartyMember.GroupRole.Tank != GetGroupRoleAssigned(RaFHelper.Leader))
            {
                Dlog("DetectTheTank: RaFHelper.Leader not a tank role and AutoTankSelect enabled, so searching");
            }
            else
            {
                Dlog("DetectTheTank: current RaFHelper.Leader is best option");
                return RunStatus.Failure;
            }

            WoWPartyMember tank = (from pm in GroupMemberInfos
                                   where GetGroupRoleAssigned(pm) == WoWPartyMember.GroupRole.Tank && pm.Guid != Me.Guid
                                   orderby pm.Location3D.Distance(Me.Location) ascending
                                   select pm).FirstOrDefault();

            if (tank != null)
            {
                WoWPlayer p = tank.ToPlayer();
                if (p != null)
                    Log("Tank set to {0}, max health {1} based upon Group Role/Proximity", p.Class, p.MaxHealth);
                else
                    Log("Tank set to max health {0} {1:F1} yds away based upon Group Role/Proximity", tank.HealthMax, tank.Location3D.Distance(StyxWoW.Me.Location));
            }
            else
            {
                tank = (from pm in GroupMemberInfos
                        where pm.Guid != Me.Guid
                        orderby pm.HealthMax descending
                        select pm).FirstOrDefault();
                if (tank != null)
                {
                    Log("Tank set to {0}, max health {1} based upon Max Health", tank.ToPlayer().Class, tank.HealthMax);
                }
            }

            if (tank != null)
            {
                Tank.Current = tank;
                if (tank.ToPlayer() != null)
                {
                    Dlog("DetectTheTank:  setting selected tank {0} as leader", Safe_UnitName(tank.ToPlayer()));
                    Tank.SetAsLeader();
                }
                else
                {
                    Dlog("DetectTheTank:  no WoWPlayer object exists for Tank, so can't share with CC yet");
                }

                return RunStatus.Failure;
            }

            if (IsInGroup)
            {
                Log(Color.Red, "Could not find suitable unit to Tank!");
            }

            return RunStatus.Failure;
        }

        private void Player_OnMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            _root = null;
        }

        private void HandlePartyMembersChanged(object sender, LuaEventArgs args)
        {
            // not really used at the moment
        }

        private void HandleModifierStateChanged(object sender, LuaEventArgs args)
        {
            if ( LazyRaiderSettings.Instance.PauseKey == LazyRaiderSettings.Keypress.None )
                return;

            if ( Convert.ToInt32(args.Args[1]) == 1 )
            {
                if ( args.Args[0].ToString() == LazyRaiderSettings.Instance.PauseKey.ToString())
                {
                    Paused = !Paused;
                    if ( Paused )
                    {
                        Log(Color.Orange, "LazyRaider PAUSED, press {0} in WOW to continue", LazyRaiderSettings.Instance.PauseKey.ToString());
                    }
                    else 
                    {
                        Log(Color.Orange, "LazyRaider Running....");
                    }
                }   
            }
        }

        public static string Safe_UnitName(WoWUnit unit)
        {
            if (unit == null)
                return "(null)";

            return unit.Class.ToString() + " (max health:" + unit.MaxHealth + ")";
        }

        public static string Safe_UnitName(WoWPartyMember pm)
        {
            if (pm == null)
                return "(null)";

            WoWPlayer p = pm.ToPlayer();
            if (p != null)
                return Safe_UnitName(p);

            return GetGroupRoleAssigned(pm).ToString() + " (max health:" + pm.HealthMax + ")";
        }

#if COMMENT
                        // Force the 'correct' POI to be our target.
            new Decorator(
                ret => BotPoi.Current.AsObject.ToUnit() != Targeting.Instance.FirstUnit,
                new Sequence(
                    new Action(ret => Logger.WriteDebug("Current POI is not the best target. Changing.")),
                    new ActionSetPoi(true, ret => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)))),

            // Make sure we have the proper unit as target
            new Decorator(
                ret => (TargetTimer.IsFinished || Me.CurrentTarget == null) &&
                        (Me.CurrentTarget == null || Me.CurrentTarget != BotPoi.Current.AsObject.ToUnit()),
                new Sequence(
                    new Action(ret => BotPoi.Current.AsObject.ToUnit().Target()),
                    new Action(ret => TargetTimer.Reset()),
                    new WaitContinue(
                        2,
                        ret => Me.CurrentTarget != null && Me.CurrentTarget == BotPoi.Current.AsObject.ToUnit(),
                        new ActionAlwaysSucceed())
                    )),

            Routine.HealBehavior,
            Routine.CombatBuffBehavior,
            Routine.CombatBehavior

        public static string GetGroupRoleAssigned(WoWPlayer p)
        {
            string sRole = "NONE";
            if (ObjectManager.Me.IsInParty || ObjectManager.Me.IsInRaid)
            {
                try
                {                   
                    string luaCmd = "return UnitGroupRolesAssigned(\"" + p.Name + "\")";
                    sRole = Lua.GetReturnVal<string>(luaCmd, 0);
                }
                catch
                {
                    sRole = "NONE";
                }
            }

            return sRole;
        }
#else
        public static WoWPartyMember.GroupRole GetGroupRoleAssigned(WoWPartyMember pm)
        {
            WoWPartyMember.GroupRole role = WoWPartyMember.GroupRole.None;
            if (pm != null && IsInGroup)
            {
                const int ROLEMASK = (int)WoWPartyMember.GroupRole.None | (int)WoWPartyMember.GroupRole.Tank | (int)WoWPartyMember.GroupRole.Healer | (int)WoWPartyMember.GroupRole.Damage;
                role = (WoWPartyMember.GroupRole)((int)pm.Role & ROLEMASK);
            }

            return role;
        }

        public static WoWPartyMember.GroupRole GetGroupRoleAssigned(WoWPlayer p)
        {
            WoWPartyMember.GroupRole role = WoWPartyMember.GroupRole.None;
            if (p != null && IsInGroup)
            {
                // GroupMemberInfos.FirstOrDefault(t => t.Guid == p.Guid);
                WoWPartyMember pm = new WoWPartyMember(p.Guid, true);
                if (pm != null)
                    role = GetGroupRoleAssigned(pm);
            }

            return role;
        }
#endif

        #endregion

        #endregion

        #region Follow Behavior

        private static bool botMovement = false;

        private static Composite CreateFollowBehavior()
        {
            return new PrioritySelector(

                new Decorator(ret => !LazyRaiderSettings.Instance.FollowTank,
                    new ActionAlwaysSucceed()),

                new Decorator(ret => !IsInGroup,
                    new ActionAlwaysSucceed()),

                new Decorator(ret => Tank.Current == null,
                    new ActionAlwaysSucceed()),

                new Decorator(ret => Me.CurrentHealth <= 1,     // if dead or ghost
                    new ActionAlwaysSucceed()),

                new Decorator(ret => Tank.Health <= 1,     // if dead or ghost
                    new ActionAlwaysSucceed()),

                new Decorator(ret => NeedToMount(),
                    new Action(delegate
                    {
                        WaitForMount();
                    })),

                new Decorator(ret => NeedToDismount(),
                    new Action(delegate
                    {
                        WaitForDismount();
                    })),

                new Decorator(ret => Tank.Distance > LazyRaiderSettings.Instance.FollowDistance
                                    || (RaFHelper.Leader != null && !RaFHelper.Leader.InLineOfSpellSight),
                    new Action(delegate
                    {
                        botMovement = true;

                        WoWPoint pt = Tank.Location;
                        if (pt != _lastDest || !Me.IsMoving)
                        {
                            _lastDest = pt;
                            Log("move to tank @ {0:F1} yds", pt.Distance(Me.Location));
                            // Navigator.MoveTo(pt);
                            Flightor.MoveTo(pt);
                        }

                        return RunStatus.Success;
                    })),

                new Decorator(ret => Me.IsMoving && botMovement,
                    new Action(delegate
                        {
                            botMovement = false;
                            while (IsGameStable() && Me.IsMoving)
                            {
                                WoWMovement.MoveStop();
                                if (Me.IsMoving)
                                {
                                    System.Threading.Thread.Sleep(25);
                                }
                            }

                            return RunStatus.Success;
                        }))

                );
        }

        #endregion

        public static bool NeedToDismount()
        {
            return Me.Mounted
                && CharacterSettings.Instance.UseMount
                && RaFHelper.Leader != null
                && Tank.Distance <= LazyRaiderSettings.Instance.FollowDistance
                && !RaFHelper.Leader.Mounted;
        }

        public static bool NeedToMount()
        {
            return !Me.Mounted
                && CharacterSettings.Instance.UseMount
                && Tank.Current != null
                && (Tank.Distance > NeedToMountDistance || RaFHelper.Leader.Mounted)
                && Me.IsOutdoors
                && Mount.CanMount();
        }

        public static int NeedToMountDistance
        {
            get
            {
                return Math.Max(CharacterSettings.Instance.MountDistance, LazyRaiderSettings.Instance.FollowDistance + 20);
            }
        }

        public static void WaitForDismount()
        {
            while (IsGameStable() && Me.CurrentHealth > 1 && Me.Mounted)
            {
                Lua.DoString("Dismount()");
                // Mount.Dismount();  // HB API forces Stop also, so use LUA to keep running and let Squire or CC stop if needed
                StyxWoW.SleepForLagDuration();
            }
        }

        public static void WaitForMount()
        {
            if (Me.Combat || Me.IsIndoors || !CharacterSettings.Instance.UseMount)
                return;

            WaitForStop();
            WoWPoint ptStop = Me.Location;

            var timeOut = new Stopwatch();
            timeOut.Start();

            if (MountHelper.Mounts.Count() == 0 || !Mount.CanMount())
                return;

            Log("Attempting to mount via HB...");
            Mount.MountUp();
            StyxWoW.SleepForLagDuration();

            while (IsGameStable() && Me.CurrentHealth > 1 && Me.IsCasting)
            {
                Thread.Sleep(75);
            }

            if (!Me.Mounted)
            {
                Log("unable to mount after {0} ms", timeOut.ElapsedMilliseconds);
                if (ptStop.Distance(Me.Location) != 0)
                    Log("character was stopped but somehow moved {0:F3} yds while trying to mount", ptStop.Distance(Me.Location));
            }
            else
            {
                Log("Mounted");
            }
        }

        public static void WaitForStop()
        {
            // excessive attempt to make sure HB doesn't have any cached movement
#if DO_CRAZY_EXCESSIVE_STOP_EVERY_MOVEMENT
            WoWMovement.MoveStop(WoWMovement.MovementDirection.AutoRun);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.ClickToMove);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.Forward);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.ForwardBackMovement);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.PitchDown);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.PitchUp);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.StrafeLeft);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.StrafeRight);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.TurnLeft);
            WoWMovement.MoveStop(WoWMovement.MovementDirection.TurnRight);
#endif
            WoWMovement.MoveStop(WoWMovement.MovementDirection.All);
            WoWMovement.MoveStop();
            Navigator.PlayerMover.MoveStop();

            do
            {
                StyxWoW.SleepForLagDuration();
            } while (IsGameStable() && Me.CurrentHealth > 1 && Me.IsMoving);
        }


        public class FrameLockSelector : PrioritySelector
        {
            public FrameLockSelector(params Composite[] children)
                : base(children)
            {
            }

            public override RunStatus Tick(object context)
            {
                using (new FrameLock())
                {
                    return base.Tick(context);
                }
            }
        }

    }
}

