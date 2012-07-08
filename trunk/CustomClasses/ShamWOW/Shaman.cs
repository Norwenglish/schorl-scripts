/*
 * NOTE:    DO NOT POST ANY MODIFIED VERSIONS OF THIS TO THE FORUMS.
 * 
 *          DO NOT UTILIZE ANY PORTION OF THIS COMBAT CLASS WITHOUT
 *          THE PRIOR PERMISSION OF AUTHOR.  PERMITTED USE MUST BE
 *          ACCOMPANIED BY CREDIT/ACKNOWLEDGEMENT TO ORIGINAL AUTHOR.
 * 
 * ShamWOW Shaman CC - Version: 4.5.21
 * 
 * Author:  Bobby53
 * 
 */

/*************************************************************************
 *   !!!!! DO NOT CHANGE ANYTHING IN THIS FILE !!!!!
 *   
 *   User customization is only supported through changing the values
 *   in the SHAMWOW-REALM-CHARNAME.CONFIG file in your Custom Classes folder tree
*************************************************************************/

#define DUMP_PLAYER_INFO

//#define BUNDLED_WITH_HONORBUDDY
#define HIDE_PLAYER_NAMES
// #define DEBUG
// #define LIST_HEAL_TARGETS
// #define DISABLE_TARGETING_FOR_INSTANCEBUDDY
// #define HONORBUDDY_SEQUENCE_MANAGER_FIXED
// #define HEALER_DONT_WINDSHEAR
// #define HEALER_IGNORE_TELLURIC_CURRENTS
// #define HEALER_IGNORE_FOCUSED_INSIGHT
#define FAST_PVP_TARGETING
// #define COLLECT_NEW_PURGEABLES
// #define AURA_CHECK_VIA_LUA

//#define DEBUG_GRIND
//#define DEBUG_PVP
//#define DEBUG_RAF

#if DEBUG_GRIND && (DEBUG_PVP || DEBUG_RAF)
#error Only define one of DEBUG_GRIND, DEBUG_PVP, or DEBUG_RAF
#elif DEBUG_PVP && (DEBUG_GRIND || DEBUG_RAF)
#error Only define one of DEBUG_GRIND, DEBUG_PVP, or DEBUG_RAF
#elif DEBUG_RAF && (DEBUG_GRIND || DEBUG_PVP)
#error Only define one of DEBUG_GRIND, DEBUG_PVP, or DEBUG_RAF
#endif

#pragma warning disable 642


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;

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
using System.Runtime.Serialization;

using ObjectManager = Styx.WoWInternals.ObjectManager;

namespace Bobby53
{
    public static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }

    partial class Shaman : CombatRoutine
    {
        public static string Version { get { return "4.5.21"; } }
        public override WoWClass Class { get { return WoWClass.Shaman; } }
#if    BUNDLED_WITH_HONORBUDDY
		public override string Name { get { return "Default Shaman v" + Version + " by Bobby53"; } }
#else
        public override string Name { get { return "ShamWOW v" + Version + " by Bobby53"; } }
#endif

        public static ConfigValues cfg;
        private static HealSpellManager hsm;
        public static Shaman _local;
       
        private readonly TalentManager talents = new TalentManager();

        public static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), Path.Combine("CustomClasses", "Config"));
        public static readonly string CCPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), Path.Combine("CustomClasses", "ShamWOW"));

        public static string ConfigFilename;

        private uint _countDrinks;
        private uint _countFood;
        private readonly Countdown timeStart = new Countdown();

#if SUPPORT_IMMUNITY_DETECTION
        private static readonly Dictionary<WoWSpellSchool, HashSet<uint>> ImmunityMap = new Dictionary<WoWSpellSchool, HashSet<uint>>();
#endif

        private static BotEvents.OnBotStartDelegate botStart;
        private static BotEvents.OnBotStopDelegate botStop;
        private static BotEvents.Player.MapChangedDelegate mapChanged;
       
        // private static BotEvents.Player.PlayerDiedDelegate playerDied;

        private static List<WoWItem> trinkMana;      // regens mana
        private static List<WoWItem> trinkHealth;    // regens health
        private static List<WoWItem> trinkPVP;       // restores player control
        private static List<WoWItem> trinkCombat;    // use off cd when in combat

        private static ConfigValues.SpellPriority priorityCleanse;
        private static ConfigValues.SpellPriority priorityPurge;

        public delegate void CombatDelegate( );
        public CombatDelegate CombatLogicDelegate;

        enum ShieldType
        {
            None = 0,
            Lightning = 324,
            Water = 52127,
            Earth = 974
        }

        public const float SAFE_CONE_SIZE = 90f;

        private const int HEALING_WAVE = 331;
        private const int ITEM_HEALTHSTONE = 5512;
        const int SHINY_FISH_SCALES = 17057;

        private static WoWPartyMember.GroupRole _myGroupRole;

        private static WoWPlayer ___GroupHealer = null;
        private static WoWPlayer GroupHealer
        {
            get
            {
                try
                {
                    // we don't care about .IsAlive other than it will throw an exception if reference invalid
                    if (___GroupHealer != null && ___GroupHealer.CurrentHealth > 1)
                        return ___GroupHealer;
                }
                catch (GameUnstableException) { throw; }
                catch
                {
                    Dlog("GroupHealer:  healer reference is now invalid, so resetting ShamWOW's GroupHealer reference");
                    ___GroupHealer = null;
                }
                return ___GroupHealer;
            }

            set
            {
                ___GroupHealer = value;
            }
        }

        private static WoWPlayer GroupTank
        {
            get
            {
                try
                {
#if SIMPLE_TANK_VALIDATION
                    // simple test to protect from bad address exceptions and someone declaring me as tank
                    if (RaFHelper.Leader != null && !RaFHelper.Leader.IsMe)
                        return RaFHelper.Leader;
#else
                    if (RaFHelper.Leader != null)
                    {
                        return ObjectManager.GetObjectByGuid<WoWPlayer>(RaFHelper.Leader.Guid);
                    }
#endif
                }
                catch (GameUnstableException)
                {
                    throw;
                }
                catch
                {
                    Slog("GroupTank:  tank object invalid (user dc or leave?), treating as [null]x value");
                    RaFHelper.ClearLeader();
                }

                return null;
            }
        }

        private static int minGroupHealth = 100;

        private static double _maxSpellRange
        {
            get
            {
                // default to std range for shocks
                double dist = 25.0;

                // then try to grab Lightning Bolt range 
                try
                {
                    dist = SpellManager.Spells["Lightning Bolt"].MaxRange;
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch
                { ;/* do nothing, default initialized above */ }

                return dist;
            }
        }

        private static double _maxDistForRangeAttack
        {
            get
            {
                // default to our maximum ranged attack distance
                double dist = _maxSpellRange;

                try
                {
                    // shorten for PVP, RAF, or Fast Pull type
                    if (IsPVP() || IsRAF() || cfg.PVE_PullType == ConfigValues.TypeOfPull.Fast)
                    {
                        dist = Math.Min(dist, SpellManager.Spells["Earth Shock"].MaxRange);
                    }
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch
                { ;/* do nothing, default initialized above */ }

                return dist;
            }
        }

        private double _offsetForRangedPull
        {
            get
            {
                double dist = _maxDistForRangeAttack;
                if (_me.GotTarget && IsImmunneToNature(_me.CurrentTarget))
                {
                    if (SpellManager.HasSpell("Flame Shock"))
                        dist = SpellManager.Spells["Flame Shock"].MaxRange;
                    else
                        dist = _offsetForMeleePull + 4;

                    Slog("Nature immune:  making Ranged pull distance for target {0:F1} yds", dist - 4);
                }

                return dist - 4.0;
            }
        }

        public const int STD_MELEE_RANGE = 5;
        private const double _maxDistForMeleeAttack = STD_MELEE_RANGE;

        public static double _offsetForMeleePull
        {
            get
            {
                if (IsPVP())
                {
                    if (_me.GotTarget && _me.CurrentTarget.IsMoving)
                    {
                        Dlog("offsetForMeleePull:  using moving target range of 0.0 for {0}", Safe_UnitName(_me.CurrentTarget));
                        return 0.0;
                    }

                    return 2.0;
                }

                return 3.0;
            }
        }

        public static List<WoWUnit> HealMembers
        {
            get
            {
                if (!_me.IsInRaid)  // meaning if we are either solo or in a party
                    return GroupMembers.Select( p => p.ToUnit()).ToList();

                if (!_me.Combat || typeShaman != ShamanType.Resto || cfg.Raid_HealStyle == ConfigValues.RaidHealStyle.Auto)
                    return _me.RaidMembers.Select( p => p.ToUnit()).ToList();

                if (cfg.Raid_HealStyle == ConfigValues.RaidHealStyle.PartyOnly && _me.IsInRaid )
                {
                    WoWPartyMember pmMe = new WoWPartyMember(_me.Guid, true);
                    return (from pm in _me.PartyMemberInfos
                            where pm.GroupNumber == pmMe.GroupNumber
                            select pm.ToPlayer().ToUnit()).ToList();
                }

                if (cfg.Raid_HealStyle == ConfigValues.RaidHealStyle.TanksOnly)
                {
                    return (from p in _me.RaidMembers
                            where p == RaFHelper.Leader || GetGroupRoleAssigned(p) == WoWPartyMember.GroupRole.Tank
                            select p.ToUnit()).ToList();
                }

                if (cfg.Raid_HealStyle == ConfigValues.RaidHealStyle.RaidOnly)
                {
                    return (from p in _me.RaidMembers
                            where p != RaFHelper.Leader && GetGroupRoleAssigned(p) != WoWPartyMember.GroupRole.Tank
                            select p.ToUnit()).ToList();
                }

                if (cfg.Raid_HealStyle == ConfigValues.RaidHealStyle.FocusOnly)
                {
                    List<WoWUnit> listPlayers = new List<WoWUnit>();
#if CURRENTFOCUS_FIXED_IN_HONORBUDDY
                    WoWPlayer p = ObjectManager.GetAnyObjectByGuid<WoWPlayer>((ulong)_me.CurrentFocus);
#else
                    WoWUnit u = GetFocusedFriendlyUnit();
#endif
                    if (Safe_IsFriendly(u))
                        listPlayers.Add(u);

                    return listPlayers;
                }

                throw new Exception("HealMembers: BAD HEAL STYLE");
            }
        }

        public static List<WoWUnit> HealMembersAndPets
        {
            get 
            {
                if (!cfg.GroupHeal.Pets)
                    return HealMembers;

                return HealMembers.Union(
                    from p in HealMembers
                    where p.GotAlivePet && p.Pet.Distance < cfg.GroupHeal.SearchRange 
                    select p.Pet).ToList();
            }
        }

        public static List<WoWPlayer> GroupMembers
        {
            get
            {
                return _me.IsInRaid ? _me.RaidMembers : _me.PartyMembers;
            }
        }

        public static List<WoWPartyMember> GroupMemberInfos 
        { 
            get 
            { 
                return !_me.IsInRaid ? _me.PartyMemberInfos : _me.RaidMemberInfos; 
            } 
        }

        public static LocalPlayer _me { get { return ObjectManager.Me; } }

        private Countdown _pullTimer = new Countdown();             // kill timer from when we target for pull
        private ulong _pullTargGuid;
        // private bool _pullTargHasBeenInMelee;       // flag indicating that we got in melee range of mob
        private int _pullAttackCount;
        // private WoWPoint _pullStart;
        private bool _pullIsRaidBehavior;

        private uint _killCount;
        private uint _killCountBase;
        private uint _deathCount;
        private uint _deathCountBase;

        private ShieldType _lastShieldUsed;       // toggle remember which shield used last shield twist

        private bool _RecallTotems;

        // private bool _lastCheckWasInCombat = true;  
        private bool _castCleanse;
        private bool _castWaterBreathing;
        private WoWPlayer _rezTarget;

        private static bool _BigScaryGuyHittingMe;      // mob sufficiently higher than us (3+ lvls)
        private static bool _OpposingPlayerGanking;    // player from other faction attacking me
        private static int _countMeleeEnemy;               // # of melee mobs in combat with me
        private static int _count8YardEnemy;               // # of mobs in combat with me within  8 yards
        private static int _count10YardEnemy;               // # of mobs in combat with me within 10 yards
        private static int _countRangedEnemy;              // # of ranged mobs in combat with me
        private static int _countAoe8Enemy;          // # of mobs within 8 yards of current target
        private static int _countAoe12Enemy;          // # of mobs within 12 yards of current target
        private static int _countFireNovaEnemy;    // # of mobs within Fire Nova range of fire totem
        private static int _radiusFireNova;
        private static int _rangeSearingTotem;
        private int _countMobs;                     // # of faction mobs as spec'd in current profile
        //  private double _distClosestEnemy ;           // distance to closest hostile or faction mob
        //  private WoWPoint _ptClosestEnemy;

        // private bool _needCheckWeaponImbues = true;
        private bool _needTotemBarSetup = true;
#if HB_DIDNT_BREAK_LOGIC_THAT_HAS_BEEN_IN_PLACE_FOR_YEARS
        private bool _needTravelForm = false;       // true if NeedRest says we need to mount or do ghost wolf 
#endif
        private bool _lastCheckInBattleground;    // 
        private bool _lastCheckInGroup;
        private int lastCheckTalentGroup;
        private int[] lastCheckTabPoints = new int[4];
        private bool lastCheckConfig;
        private int lastCheckSpellCount = 0;

        private static string localName_MaelstromWeapon;
        private static string localName_LightningShield;
        private static string localName_TidalWaves;

#if EQUIP_SUPPORTED
		private string EquipDefault;
		private string EquipPVP;
		private string EquipRAF;
#endif
        // public static WoWPlayer _followTarget;

        private static bool _isPluginMrAutoFight;
        private static bool _isPluginAntiDrown;
        private static bool _isBotLazyRaider;
        private static bool _isBotInstanceBuddy;
        private static bool _isBotGatherBuddy;
        private static bool _isBotArchaeologyBuddy;
        private static bool _isBotQuesting;
        private static bool _isBotBGBuddy;
        private static bool _isBotProfessionBuddy;

        // public static bool foundMobsThatFear;
        // public static ulong foundMobToInterrupt; 
        private readonly Stopwatch _potionTimer = new Stopwatch();
        private static Countdown _potionCountdown = new Countdown();


        // private int _pointsImprovedStormstrike;
        public static bool _hasTalentFulmination;
        public static int _ptsTalentReverberation;
        public static bool _hasTalentImprovedCleanseSpirit;
        public static bool _hasTalentAncestralSwiftness;
        // public static bool _hasTalentImprovedLavaLash;
        public static bool _hasTalentEarthenPower;
        public static bool _hasTalentMaelstromWeapon;
        public static bool _hasTalentFocusedInsight;
        public static bool _hasTalentTelluricCurrents;
        public static int _cntTalentElementalReach;
        public static int _cntTalentTotemicReach;
        public static bool _hasGlyphOfChainLightning;
        public static bool _hasGlyphOfHealingStreamTotem;
        public static bool _hasGlyphOfStoneClaw;
        public static bool _hasGlyphOfShamanisticRage;
        public static bool _hasGlyphOfFireNova;
        public static bool _hasGlyphOfThunderstorm;
        public static bool _hasGlyphOfWaterWalking;
        public static bool _hasGlyphOfWaterBreathing;
        private static bool _hasGlyphOfUnleashedLightning;

#if MAKELOVE
        private static bool _hasAchieveMakeLoveNotWarcraft;
#endif
        private static int _tier12CountResto;
        private static int _tier13CountResto;
        private static bool _checkIfTagged;

        public enum ShamanType
        {
            Unknown,
            Elemental,
            Enhance,
            Resto
        };

        public static ShamanType typeShaman = ShamanType.Unknown;

        private static int countEnemy
        {
            get
            {
                return _countMeleeEnemy + _countRangedEnemy;
            }
        }

        public static bool IsFightStressful()
        {
            return countEnemy >= cfg.PVE_StressfulMobCount || _BigScaryGuyHittingMe || _OpposingPlayerGanking;
        }

        private bool DidWeSwitchModes()
        {
            return DidWeSwitchModes(true);
        }

        private bool DidWeSwitchModes(bool verboseCheck)
        {
            bool dirtyFlag = false;

            if (lastCheckConfig)
            {
                if (verboseCheck)
                    Slog(Color.DarkGreen, ">>> Configuration updated, Initializing...");
                dirtyFlag = true;
                lastCheckConfig = false;
            }

            if (_lastCheckInGroup != InGroup())
            {
                if (verboseCheck)
                    Slog(Color.DarkGreen, ">>> Left/joined a group, Initializing...");
                dirtyFlag = true;
                _lastCheckInGroup = InGroup();
            }

            if (_lastCheckInBattleground != IsPVP())
            {
                if (verboseCheck)
                    Slog(Color.DarkGreen, ">>> Left/joined a battleground, Initializing...");
                dirtyFlag = true;
                _lastCheckInBattleground = IsPVP();

#if COLLECT_NEW_PURGEABLES
                if (!IsPVP())
                {
                    ListSpecialInfoCollected();
                }
#endif
            }

            if (lastCheckTalentGroup != talents.GroupIndex)
            {
                if (verboseCheck)
                    Slog(Color.DarkGreen, ">>> New talent group active, Initializing...");
                dirtyFlag = true;
                lastCheckTalentGroup = talents.GroupIndex;
            }

            for (int tab = 1; tab <= 3; tab++)
            {
                if (lastCheckTabPoints[tab] != talents.TabPoints[tab])
                {
                    if (!dirtyFlag)
                        Slog(Color.DarkGreen, ">>> Talent spec changed, Initializing...");

                    dirtyFlag = true;
                    lastCheckTabPoints[tab] = talents.TabPoints[tab];
                }
            }

            return dirtyFlag;
        }


#if CTOR_NO_LONGER_NEEDED      

		/*
		 * Ctor
		 * 
		 * initialize and post load messages/checks for user
		 */
		public ShamWOW()
		{
			if (_me.Class != WoWClass.Shaman)
			{
				return;
			}
		}

		/*
		 * Dtor
		 */
		~ShamWOW()
		{
			if (_me.Class != WoWClass.Shaman)
			{
				return;
			}

			Dlog("UNLOAD:  " + Name);
		}

#endif

        private static bool firstInitialize = true;
        private bool watchingCombatLog = false;

        public override void Initialize()
        {
            if (firstInitialize)
            {
                firstInitialize = false;
                InitializeOnce();
            }

            Dlog("Initialize:  -- beginning contextual initialization");

            if (watchingCombatLog && (IsRaidBehavior() || _me.CurrentMap.IsRaid ))
            {
                watchingCombatLog = false;
                Dlog("Initialize: ending combat log monitor");
                Lua.Events.RemoveFilter("COMBAT_LOG_EVENT_UNFILTERED");
                Lua.Events.DetachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLogEvent);
            }
            else if (!watchingCombatLog && !(IsRaidBehavior() || _me.CurrentMap.IsRaid))
            {
                watchingCombatLog = true;
                Dlog("Initialize: starting log monitor");
                Lua.Events.AttachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLogEvent);
                if (!Lua.Events.AddFilter("COMBAT_LOG_EVENT_UNFILTERED", "return args[2] == 'SPELL_MISSED' or args[2] == 'SPELL_CAST_FAILED'"))
                {
                    Elog("ERROR: unable to filter combat log, thing may not work!");
                }
            }
            else
            {
                Dlog("Initialize: log monitor is currently {0}", watchingCombatLog ? "enabled" : "disabled");
            }

            talents.Load();

            _hashCleanseBlacklist = new HashSet<int>();
            _hashPurgeWhitelist = new HashSet<int>();
            _hashPvpGroundingTotemWhitelist = new HashSet<int>();

            _dictMob = new Dictionary<int, Mob>();

            string MainConfig = Path.Combine(ConfigPath, "ShamWOW.config");
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(MainConfig);
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (System.IO.FileNotFoundException fnfe)
            {
                Elog("Error: global config file not found: '{0}'", fnfe.FileName);
                Elog("... see ShamWOW FAQ for installation steps");
                TreeRoot.Stop();
            }
            catch (XmlException xe)
            {
                Log("");
                Elog("ERROR: Stopping Bot!!!!  Unable to load global ShamWOW Config", MainConfig);
                Log("");
                Elog("File: {0}", MainConfig);
                Elog("Line: {0}", xe.LineNumber);
                Elog("Reason: {0}", xe.Message);
                Log("");
                Logging.WriteDebug(">>> EXCEPTION: Global config file ShamWOW.config contains bad XML:");
                Logging.WriteException(xe);
                TreeRoot.Stop();
            }
            catch (Exception e)
            {
                Elog("Error processing Global ShamWOW Config info from '{0}'", MainConfig);
                Logging.WriteDebug(">>> EXCEPTION: Unable to load Global config file ShamWOW.config:");
                Logging.WriteException(e);
                TreeRoot.Stop();
            }

            Dlog("");
            Dlog("CLEANSE BLACKLIST");
            XmlNodeList nodeList = doc.SelectNodes("//ShamWOW/CleanseBlacklist/Spell");
            foreach (XmlElement spell in nodeList)
            {
                int id = int.Parse(spell.GetAttribute("Id"));
                Dlog("spellId:{0}  spellName:{1}", id, WoWSpell.FromId(id));
                _hashCleanseBlacklist.Add(id);
            }

            Dlog("");
            Dlog("PURGE WHITELIST");
            nodeList = doc.SelectNodes("//ShamWOW/PurgeWhitelist/Spell");
            foreach (XmlElement spell in nodeList)
            {
                int id = int.Parse(spell.GetAttribute("Id"));
                Dlog("spellId:{0}  spellName:{1}", id, WoWSpell.FromId(id));
                _hashPurgeWhitelist.Add(int.Parse(spell.GetAttribute("Id")));
            }

            Dlog("");
            Dlog("PVP GROUNDING TOTEM WHITELIST");
            nodeList = doc.SelectNodes("//ShamWOW/PvpGroundingTotemWhitelist/Spell");
            foreach (XmlElement spell in nodeList)
            {
                int id = int.Parse(spell.GetAttribute("Id"));
                Dlog("spellId:{0}  spellName:{1}", id, WoWSpell.FromId(id));
                _hashPurgeWhitelist.Add(int.Parse(spell.GetAttribute("Id")));
            }

            Dlog("");
            Dlog("MOB CUSTOMIZATION LIST");
            nodeList = doc.SelectNodes("//ShamWOW/MobList/Mob");
            foreach (XmlElement node in nodeList)
            {   // broke these up to get line numbers in stack trace 
                string sId = node.GetAttribute("Id");
                string sName = node.GetAttribute("Name");
                string sHitBox = node.GetAttribute("HitBox");
                int iId = int.Parse(sId);
                int iHitBox = int.Parse(sHitBox);
                Mob mob = new Mob(iId, sName, iHitBox);
                Dlog("Mob Id:{0} Name:{1} HitBox:{2}", mob.Id, mob.Name, mob.HitBox);
                _dictMob.Add(int.Parse(node.GetAttribute("Id")), mob);
            }

            Dlog("");
            Dlog("BOSS LIST - contains {0} pre-initialized values", _hashBossList.Count() );
            nodeList = doc.SelectNodes("//ShamWOW/BossList/Boss");
            foreach (XmlElement node in nodeList)
            {   // broke these up to get line numbers in stack trace 
                string sId = node.GetAttribute("Id");
                uint iId = uint.Parse(sId);
                _hashBossList.Add(iId);
            }
            Dlog("            final boss list has {0} value after config file load", _hashBossList.Count());

            Dlog("");
            if (talents.Spec == 1)
                typeShaman = ShamanType.Elemental;
            else if (talents.Spec == 2)
                typeShaman = ShamanType.Enhance;
            else if (talents.Spec == 3)
                typeShaman = ShamanType.Resto;
            else if (cfg.MeleeCombatBeforeLevel10)
            {
                typeShaman = ShamanType.Enhance;
                Slog("Low-level Shaman played as Enhancement due to config setting.");
            }
            else
            {
                typeShaman = ShamanType.Unknown;
                Log("Low-level Shaman being played as Elemental.  See the [Melee Combat Before Level 10] configuration setting to force melee combat.");
            }

            InitializeDelegates();

            localName_MaelstromWeapon = GetLocalSpellName(51530);
            localName_LightningShield = GetLocalSpellName(324);
            localName_TidalWaves = GetLocalSpellName(51564);

            // reset totem position
            _ptTotems = new WoWPoint();
            TotemsWereSet = false;

            // reset the Heal Targets list and force a refresh
            _healTargets = null;
            minGroupHealth = 100;

            // get important group assignments.  we want our role and to identify the healer (in case its not us)
            CheckGroupRoleAssignments();

            Log("");
            string sSpecType = talents.TabNames[talents.Spec];
            Log(Color.White, "Your Level " + _me.Level + " " + _me.Race + " " + sSpecType + " Shaman Build is:  ");
            Log(Color.White, talents.TabNames[1].Substring(0, 5) + "/" + talents.TabNames[2].Substring(0, 5) + "/" + talents.TabNames[3].Substring(0, 5)
            + "   " + talents.TabPoints[1] + "/" + talents.TabPoints[2] + "/" + talents.TabPoints[3]);

            string sRunningAs = "Solo";
            string sRunningWith;

            if ( !InGroup() )
                sRunningWith = "Solo";
            else
            {
                if (_me.IsInRaid)
                    sRunningWith = "{0}m Raid";
                else if ( _me.IsInParty )
                    sRunningWith = "{0}m Party";
                else
                    sRunningWith = "{0}m Group";

                sRunningWith = String.Format(sRunningWith, GroupMembers.Count());
            }

            if (IsPVP())
                sRunningAs = "in PVP " + sRunningWith;
            else if (IsRAF())
                sRunningAs = "in RAF " + sRunningWith;
            else
                sRunningAs = "Solo";

            Log(Color.White, "... running the {0} bot {1} as {2} in {3}",
                 GetBotName(),
                 sRunningAs,
                 IsHealerOnly() ? "Healer Only" : (IsHealer() ? "Healer over Combat" : "Combat Only"),
                 _me.RealZoneText
                );

            
            Logging.WriteDebug(" ");
            Logging.WriteDebug("Initialize:  Battleground: {0}", IsPVP());
            Logging.WriteDebug("Initialize:  RAF.........: {0}", IsRAF());
            Logging.WriteDebug("Initialize:  IsInInstance: {0}", _me.IsInInstance);
            Logging.WriteDebug("Initialize:  IsCombatOnly: {0}", IsCombatOnly());
            Logging.WriteDebug("Initialize:  IsHealer....: {0}", IsHealer());
            Logging.WriteDebug("Initialize:  IsHealerOnly: {0}", IsHealer());
            Logging.WriteDebug("Initialize:  MyHitBoxSize: {0:F1}", _me.CombatReach);

            Slog("");

            if (talents.UnspentPoints > 0)
                Slog("WARNING: {0} unspent Talent Points. Use a talent plug-in or spec manually", talents.UnspentPoints);

            _isBotInstanceBuddy = IsBotInUse("INSTANCEBUDDY");
            if (_isBotInstanceBuddy)
                Slog("InstanceBuddy Bot detected... ShamWOW fast attack targeting disabled");

            _isBotInstanceBuddy = IsBotInUse("QUESTING");
            if (_isBotInstanceBuddy)
                Slog("Questing Bot detected...");

            _isBotGatherBuddy = IsBotInUse("GATHERBUDDY");
            if (_isBotGatherBuddy)
            {
                if (cfg.DisableMovement == ConfigValues.When.Always)
                    Slog("GatherBuddy Bot detected... ignoring movement disabled setting");
                Slog("GatherBuddy Bot detected... Ghost Wolf disabled");
            }

            _isBotQuesting  = IsBotInUse("QUEST");
            if (_isBotQuesting)
            {
                if (cfg.DisableMovement == ConfigValues.When.Always)
                    Slog("Questing Bot detected... ignoring movement disabled setting");
            }

            _isBotBGBuddy = IsBotInUse("BGBUDDY");
            if (_isBotBGBuddy && cfg.DisableMovement == ConfigValues.When.Always)
                Slog("BGBuddy Bot detected... ignoring movement disabled setting");

            _isBotProfessionBuddy = IsBotInUse("PROFESSION");
            if (_isBotProfessionBuddy)
            {
                if (cfg.DisableMovement == ConfigValues.When.Always)
                    Slog("ProfessionBuddy Bot detected... ignoring movement disabled setting");
            }

            _isBotArchaeologyBuddy = IsBotInUse("ARCH");
            if (_isBotArchaeologyBuddy)
            {
                if (cfg.DisableMovement == ConfigValues.When.Always)
                    Slog("ArchaeologyBuddy Bot detected... ignoring movement disabled setting");
                Slog("ArchaeologyBuddy Bot detected... Ghost Wolf disabled");
            }

            _isBotLazyRaider = IsBotInUse("LAZYRAIDER");
            if (_isBotLazyRaider)
                Slog("LazyRaider Bot detected...");

            _isPluginMrAutoFight = CharacterSettings.Instance.EnabledPlugins != null && CharacterSettings.Instance.EnabledPlugins.Contains("Mr.AutoFight");
            _isPluginAntiDrown = CharacterSettings.Instance.EnabledPlugins != null && CharacterSettings.Instance.EnabledPlugins.Contains( "Anti Drown");

            if (cfg.WaterBreathing && SpellManager.HasSpell("Water Breathing"))
            {
                if (!_hasGlyphOfWaterBreathing && null == CheckForItem(SHINY_FISH_SCALES))
                    Wlog("Warning: cannot cast Water Breathing without glyph or Shiny Fish Scales");

                if (_isPluginAntiDrown)
                    Slog("Anti Drown Plugin detected - Water Breathing cast with less than 65 secs of breath remaining");
                else
                    Slog("Water Breathing cast when less than 5 seconds of breath remaining");
            }

            if (cfg.DisableMovement == ConfigValues.When.Always)
            {
                Slog("CC movevement disabled due to Config Setting");
            }
            else if (cfg.DisableMovement == ConfigValues.When.Auto)
            {
                if (_isBotLazyRaider)
                    Wlog("CC movevement auto-disabled to work with LazyRaider Bot");
                else if (_isPluginMrAutoFight)
                    Wlog("CC movevement disabled to work with Mr.AutoFight");
            }

            if (IsTargetingDisabled())
                Slog("CC targeting disabled due to Config Setting");

            _checkIfTagged = false;
            if (IsTargetingDisabled() || IsMovementDisabled())
                Dlog("Initialize: suppressed tagged mob check because targeting or movement is disabled");
            else if (IsPVP() || IsRAF())
                Dlog("Initialize: suppressed tagged mob check because PVP or RAF");
            else
                _checkIfTagged = true;

            _deathCountBase = InfoPanel.Deaths;

            Slog("Max Pull Ranged:   {0}", _maxDistForRangeAttack);
            Slog("HB Pull Distance:  {0}", Targeting.PullDistance);
            
            GetGhostWolfDistance();

            Slog("");

            _hasTalentFulmination = 0 < talents.GetTalentInfo(1, 13);
            _ptsTalentReverberation = talents.GetTalentInfo(1, 6);
            _hasTalentImprovedCleanseSpirit = SpellManager.HasSpell("Cleanse Spirit") && 0 < talents.GetTalentInfo(3, 12);
            _hasTalentAncestralSwiftness = SpellManager.HasSpell("Ghost wolf") && (2 == talents.GetTalentInfo(2, 6));
            _hasTalentMaelstromWeapon = (1 <= talents.GetTalentInfo(2, 17));
            _hasTalentEarthenPower = (1 <= talents.GetTalentInfo(2, 14));
            // _hasTalentImprovedLavaLash = SpellManager.HasSpell("Lava Lash") && (1 <= talents.GetTalentInfo(2, 18));
            _hasTalentFocusedInsight = (1 <= talents.GetTalentInfo(3, 6));
            _hasTalentTelluricCurrents = (1 <= talents.GetTalentInfo(3, 16));
            _cntTalentElementalReach = talents.GetTalentInfo(1, 10);
            _cntTalentTotemicReach = talents.GetTalentInfo(2, 7);
            _hasGlyphOfChainLightning = SpellManager.HasSpell("Chain Lightning") && talents._glyphs.ContainsKey(55449);
            _hasGlyphOfHealingStreamTotem = HasTotemSpell(TotemId.HEALING_STREAM_TOTEM) && talents._glyphs.ContainsKey(55456);
            _hasGlyphOfStoneClaw = SpellManager.HasSpell((int)TotemId.STONECLAW_TOTEM) && talents._glyphs.ContainsKey(63298);
            _hasGlyphOfShamanisticRage = SpellManager.HasSpell("Shamanistic Rage") && talents._glyphs.ContainsKey(63280);
            _hasGlyphOfFireNova = SpellManager.HasSpell("Fire Nova") && talents._glyphs.ContainsKey(55450);
            _hasGlyphOfThunderstorm = SpellManager.HasSpell("Thunderstorm") && talents._glyphs.ContainsKey(62132);
            _hasGlyphOfWaterWalking = SpellManager.HasSpell("Water Walking") && talents._glyphs.ContainsKey(58057);
            _hasGlyphOfWaterBreathing = SpellManager.HasSpell("Water Breathing") && talents._glyphs.ContainsKey(89646);
            _hasGlyphOfUnleashedLightning = SpellManager.HasSpell("Lightning Bolt") && talents._glyphs.ContainsKey(101052);

            if (_hasTalentFulmination)
                Slog("[talent] Fulmination: will wait for 7+ stacks of Lightning Shield before using Earth Shock");
            if (_ptsTalentReverberation > 0)
                Slog("[talent] Reverberation:  has {0} points", _ptsTalentReverberation);
            if (_hasTalentImprovedCleanseSpirit)
                Slog("[talent] Cleanse Spirit: can remove Curses and Magic");
            if (SpellManager.HasSpell("Ghost wolf"))
                Slog("[talent] Ancestral Swiftness: {0}", _hasTalentAncestralSwiftness ? "can cast Ghost Wolf on the run" : "must stop to cast Ghost Wolf");
            if (_hasTalentMaelstromWeapon)
            {
                if (IsRAF())
                    Slog("[talent] Maelstrom Weapon: will cast Lightning Bolt or Chain Lightning at 5 stacks");
                else if (IsPVP())
                    Slog("[talent] Maelstrom Weapon: will cast Greater Healing Wave or Healing Rain at 5 stacks");
                else if ( cfg.PVE_HealOnMaelstrom )
                    Slog("[talent] Maelstrom Weapon: will cast Greater Healing Wave or Healing Rain at 5 stacks");
                else
                    Slog("[talent] Maelstrom Weapon: will cast Lightning Bolt or Chain Lightning at 5 stacks unless in stressful fight");
            }
            if (_hasTalentEarthenPower)
                Slog("[talent] Earthen Power: will use Earthbind Totem to break snares");
            // if (SpellManager.HasSpell("Lava Lash"))
            //      Slog("Lava Lash: cast {0}", _hasTalentImprovedLavaLash ? "will wait for 5 stacks of Searing Flames" : "when off cooldown");
            if (_hasTalentFocusedInsight)
                Slog("[talent] Focused Insight: will cast Earth Shock to boost Healing Rain");
            if (_hasTalentTelluricCurrents)
                Slog("[talent] Telluric Currents: will cast Lightning Bolt to regen mana");
            if (SpellManager.HasSpell("Earthquake"))
                Slog("[glyph] Chain Lightning: {0}, will use for AoE instead of Earthquake unless {1}+ mobs in range", _hasGlyphOfChainLightning ? "found" : "not found", _hasGlyphOfChainLightning ? 7 : 5);
            if (_hasGlyphOfStoneClaw)
                Slog("[glyph] Stoneclaw Totem: will use as Shaman Bubble");
            if (_hasGlyphOfShamanisticRage)
                Slog("[glyph] Shamanistic Rage: will use as Magic Cleanse");

            if (_hasGlyphOfThunderstorm)
                Slog("[glyph] Thunderstorm: will not use Thunderstorm for knockback or interrupt");


            _radiusFireNova = 0;
            if (SpellManager.HasSpell("Fire Nova"))
            {
                _radiusFireNova = 10;
                if (_hasGlyphOfFireNova)
                {
                    _radiusFireNova += 5;
                    Slog("[glyph] Fire Nova: found, range extended by 5 yards");
                }
            }

            _rangeSearingTotem = 25;
            if (1 == _cntTalentElementalReach)
            {
                _rangeSearingTotem += 7;
            }
            else if (2 == _cntTalentElementalReach)
            {
                _rangeSearingTotem += 15;
            }


            if (_hasGlyphOfHealingStreamTotem)
                Slog("[glyph] Healing Stream Totem: found, will use instead of Elemental Resistance Totem");
            if (_hasGlyphOfUnleashedLightning)
                Slog("[glyph] Unleashed Lightning: found, will allow use Lightning Bolt while moving");

            if (IsPVP() && cfg.PVP_PrepWaterWalking && SpellManager.HasSpell("Water Walking") && _hasGlyphOfWaterWalking)
                Slog("[glyph] Water Walking: found, will buff battleground and arena members in range");
            if (IsPVP() && cfg.PVP_PrepWaterBreathing && SpellManager.HasSpell("Water Breathing") && _hasGlyphOfWaterBreathing)
                Slog("[glyph] Water Breathing: found, will buff battleground and arena members in range");

            Slog("");

#if MAKELOVE
            _hasAchieveMakeLoveNotWarcraft = AchieveCompleted(247);
            if (!_hasAchieveMakeLoveNotWarcraft)
            {
                Slog("[achieve] Make Love Not Warcraft:  will /hug players killed");
            }
#endif
            hsm = new HealSpellManager();
            if (InGroup())
            {
                Logging.WriteDebug("-- Current {0} Heal Settings --", _me.IsInRaid ? "Raid" : "Party");
                hsm.Dump();
            }

            DidWeSwitchModes(false);                 // set the mode change tracking variables

            _needTotemBarSetup = false;
            TotemSetupBar();

            // InfoPanel.Reset();
            _killCountBase = InfoPanel.MobsKilled;
            _deathCountBase = InfoPanel.Deaths;

            HandlePlayerEquipmentChanged(null, null);

            if (IsPVP())
            {
                priorityCleanse = cfg.PVP_CleansePriority;
                priorityPurge = cfg.PVP_PurgePriority;
            }
            else if (IsRAF())
            {
                priorityCleanse = cfg.RAF_CleansePriority;
                priorityPurge = cfg.RAF_PurgePriority;
            }
            else
            {
                priorityCleanse = ConfigValues.SpellPriority.Low;
                priorityPurge = ConfigValues.SpellPriority.Low;
            }

            Slog("");

            Dlog("Effective Cleanse Priority: {0}", priorityCleanse);
            Dlog("Effective Purge Priority: {0}", priorityPurge);

            Slog("");

            TotemManagerUpdate();

            Dlog("Initialize:  -- ending contextual initialization");
        }

        private void InitializeDelegates()
        {
            CombatLogicDelegate = null;
            if (IsPVP())
            {
                if (ShamanType.Elemental == typeShaman)
                    ;
                else if (ShamanType.Enhance == typeShaman)
                {
                    Dlog("Initialize:  setting CombatLogicEnhancePVP as combat delegate");
                    CombatLogicDelegate = new CombatDelegate(this.CombatLogicEnhancePVP);
                }
                else if (ShamanType.Resto == typeShaman)
                    ;
            }
            else if (IsRaidBehavior() && _me.Level >= 85)
            {
                if (ShamanType.Elemental == typeShaman)
                {
                    // Dlog("Initialize:  setting CombatElementalRaid as combat delegate");
                    // CombatLogicDelegate = new CombatDelegate(this.CombatElementalRaid);
                }
                else if (ShamanType.Enhance == typeShaman)
                {
                    Dlog("Initialize:  setting CombatMeleeRaid as combat delegate");
                    CombatLogicDelegate = new CombatDelegate(this.CombatMeleeRaid);
                }
                else if (ShamanType.Resto == typeShaman)
                    ;
            }
            else if (IsRAF() )
            {
                if (ShamanType.Elemental == typeShaman)
                    ;
                else if (ShamanType.Enhance == typeShaman)
                    ;
                else if (ShamanType.Resto == typeShaman)
                    ;
            }
            else // Solo
            {
                if (ShamanType.Elemental == typeShaman)
                    ;
                else if (ShamanType.Enhance == typeShaman)
                    ;
                else if (ShamanType.Resto == typeShaman)
                    ;
            }

            if (CombatLogicDelegate == null)
            {
                if (ShamanType.Unknown != typeShaman)
                {
                    Dlog("Initialize:  setting default CombatLogic as combat delegate");
                    CombatLogicDelegate = new CombatDelegate(this.CombatLogic);
                }
                else if (cfg.MeleeCombatBeforeLevel10)
                {
                    Dlog("Initialize: setting CombatUndefined as combat delegate");
                    typeShaman = ShamanType.Enhance;
                    CombatLogicDelegate = new CombatDelegate(this.CombatUndefined);
                }
                else // unknown and play as caster
                {
                    Dlog("Initialize: setting CombatUndefined as combat delegate");
                    typeShaman = ShamanType.Elemental;
                    CombatLogicDelegate = new CombatDelegate(this.CombatUndefined);
                }
            }
        }

        private uint GearScore()
        {
            uint sumLvl = 0;
            uint resil = 0;

            for (uint slot = 0; slot < _me.Inventory.Equipped.Slots; slot++)
            {
                WoWItem item = _me.Inventory.Equipped.GetItemBySlot(slot);
                uint itemLvl = GearScore(item);
                if (null == item)
                {
                    // Dlog("  none:  item[{0}]: not equipped", slot);
                }
                else
                {
                    if (!IsItemImportantToGearScore(item))
                        ; //  Dlog("  fail:  item[{0}]: {1}  [{2}] (ignored)", slot, itemLvl, item.Name);
                    else
                    {
                        sumLvl += itemLvl;
                        // Dlog("  good:  item[{0}]: {1}  [{2}]", slot, itemLvl, item.Name);
                    }

                    foreach ( KeyValuePair<StatTypes,int> kvp in item.ItemStats.Stats )
                    {
                        if ( kvp.Key == StatTypes.ResilienceRating )
                        {
                            resil += (uint) kvp.Value;
                        }
                    }
                }
            }

            // double main hand score if have a 2H equipped
            if ( _me.Inventory.Equipped.MainHand != null && _me.Inventory.Equipped.MainHand.ItemInfo.InventoryType == InventoryType.TwoHandWeapon)
                sumLvl += GearScore(_me.Inventory.Equipped.MainHand);

            Dlog("");
            Dlog( "Equipped Average Item Level:  {0:F0}", ((double)sumLvl) / 17.0);
            Dlog( "Health:     {0}", _me.MaxHealth);
            Dlog( "Agility:    {0}", _me.Agility);
            Dlog("Intellect:   {0}", _me.Intellect);
            Dlog("Spirit:      {0}", _me.Spirit);
            Dlog("Resilience:  {0} (approx)", resil);

            Dlog("");
            foreach (KeyValuePair<uint, string> glyph in talents._glyphs.OrderBy(g => g.Value).Select(g => g).ToList())
            {
                Dlog("--- {0:d4} #{1}", glyph.Value, glyph.Key );
            }

            if (!talents._glyphs.Any())
                Dlog("--- no glyphs equipped");

            Dlog("");
            return sumLvl;
        }

        private uint GearScore(WoWItem item)
        {
            uint iLvl = 0;
            try
            {
                if (item != null)
                    iLvl = (uint) item.ItemInfo.Level;
            }
            catch
            {
                ;
            }

            return iLvl;
        }

        private bool IsItemImportantToGearScore(WoWItem item)
        {
            if (item != null && item.ItemInfo != null)
            {
                switch (item.ItemInfo.InventoryType)
                {
                    case InventoryType.Head:
                    case InventoryType.Neck:
                    case InventoryType.Shoulder:
                    case InventoryType.Cloak:
                    case InventoryType.Body:
                    case InventoryType.Chest:
                    case InventoryType.Robe:
                    case InventoryType.Wrist:
                    case InventoryType.Hand:
                    case InventoryType.Waist:
                    case InventoryType.Legs:
                    case InventoryType.Feet:
                    case InventoryType.Finger:
                    case InventoryType.Trinket:
                    case InventoryType.Relic:
                    case InventoryType.Ranged:
                    case InventoryType.Thrown:

                    case InventoryType.Holdable:
                    case InventoryType.Shield:
                    case InventoryType.TwoHandWeapon:
                    case InventoryType.Weapon:
                    case InventoryType.WeaponMainHand:
                    case InventoryType.WeaponOffHand:
                        return true;
                }
            }

            return false;
        }

        private string GetBotName()
        {
            string sFoundName = "[null]";

            if (TreeRoot.Current != null)
            {
                if (!(TreeRoot.Current is NewMixedMode.MixedModeEx))
                    sFoundName = TreeRoot.Current.Name;
                else
                {
                    NewMixedMode.MixedModeEx mmb = (NewMixedMode.MixedModeEx)TreeRoot.Current;
                    if (mmb == null)
                    {
                        Dlog("Mixed bot selected but not initialized");
                    }
                    else
                    {
                        string sPrimary = mmb.PrimaryBot != null ? mmb.PrimaryBot.Name : "[primary null]";
                        string sSecondary = mmb.SecondaryBot != null ? mmb.SecondaryBot.Name : "[secondary null]" ;
                        Dlog("Mixed Bot setup is Primary='{0}', Secondary='{1}'", sPrimary, sSecondary);
                        if (_me.IsInInstance || IsPVP())
                            sFoundName = sSecondary ;
                        else
                            sFoundName = sPrimary ;
                    }
                }
            }

            return sFoundName;
        }

        private bool IsBotInUse(string botNameContains)
        {
            return GetBotName().ToUpper().Contains(botNameContains.ToUpper());
        }

        public static bool IsMovementDisabled()
        {
            if (cfg.DisableMovement == ConfigValues.When.Always)
            {
                return !(_isBotArchaeologyBuddy || _isBotGatherBuddy || _isBotInstanceBuddy || _isBotQuesting);
            }

            return cfg.DisableMovement == ConfigValues.When.Auto && (_isPluginMrAutoFight || _isBotLazyRaider);
        }

        public static bool IsTargetingDisabled()
        {
            return cfg.DisableTargeting; //&& IsMovementDisabled();
        }

        public static bool IsImmunityCheckDisabled()
        {
#if SUPPORT_IMMUNITY_DETECTION
            return !cfg.DetectImmunities || IsTargetingDisabled();
#else
            return true;
#endif
        }

        public static bool IsAutoShieldApplyDisabled()
        {
            return cfg.FarmingLowLevel || cfg.ShieldsDisabled;
        }

        private void InitializeOnce()
        {
            Logging.WriteDebug( "InitializeOnce:  one time initialization running");
            cfg = new ConfigValues();   // needs to be the first thing done

            double tickCount = (double)((uint)System.Environment.TickCount);
            Log("");
            Log("{0:F1} days since Windows was restarted", TimeSpan.FromMilliseconds(tickCount).TotalHours / 24.0);
            Dlog("{0} FPS currently in WOW", GetFPS());
            Dlog("{0} ms of Latency in WOW", StyxWoW.WoWClient.Latency);
            Log("");

            //==============================================================================================
            //  Now do ONE TIME Initialization (needs to occur after we know what spec we are)
            //==============================================================================================
            Slog("LOADED:  " + Name);
            _local = this;

            List<string> option = CallLUA( "return GetCVar(\"autoSelfCast\")");
            if ( option == null || string.IsNullOrEmpty(option[0]) || option[0] != "1")
            {
                Wlog("Enabling 'Auto Self Cast' -- WOW Interface option must be checked");
                RunLUA("SetCVar(\"autoSelfCast\", 1)");
            }

            ResetTotemData();

            // load config file (create if doesn't exist)
            Logging.WriteDebug("% InitializeOnce:  getting realm name");
            string realmName = Lua.GetReturnVal<string>("return GetRealmName()", 0);
            ConfigFilename = Path.Combine(ConfigPath, "ShamWOW-" + realmName + "-" + _me.Name + ".config");

            if (!Directory.Exists(ConfigPath))
            {
                try
                {
                    Directory.CreateDirectory(ConfigPath);
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch
                {
                    Wlog("Folder could not be created: '{0}'", ConfigPath);
                    Wlog("Create the folder manually if needed and restart HB");
                    return;
                }
            }

            bool didUpgrade;
            if (File.Exists(ConfigFilename))
            {
                cfg.FileLoad(ConfigFilename, out didUpgrade);
                Slog("Character specific config file loaded");
            }
            else
            {
                didUpgrade = true;
                cfg.Save(ConfigFilename);
                Slog("Creating a character specific config file ");
            }



            if (didUpgrade)
            {
                MessageBox.Show(
#if SAVE_THIS_MESSAGE
                    "Click the CC Configuration button found"     +Environment.NewLine +
                    "on the General tab of HB for options."       +Environment.NewLine +
                    ""                                          +Environment.NewLine +
                    "All issues/problems posted on the forum"   +Environment.NewLine +
                    "must include a complete debug LOG FILE."   +Environment.NewLine +
                    "Posts missing this will be ignored or"     +Environment.NewLine +
                    "receive a response requesting the file."   + Environment.NewLine +
                    "" + Environment.NewLine +
                    "If there is anything you would like this"   + Environment.NewLine +
                    "CC to do, ask in the forum for direction"   + Environment.NewLine +
                    "because it probably already does."          + Environment.NewLine +
#else
"ShamWOW BETA User Acknowledgement" + Environment.NewLine +
                    "" + Environment.NewLine +
                    "By using this BETA software, you agree" + Environment.NewLine +
                    "to ATTACH A COMPLETE DEBUG LOG FILE to" + Environment.NewLine +
                    "any forum post you make containing a" + Environment.NewLine +
                    "question, criticism, or bug report." + Environment.NewLine +
#endif
 "" + Environment.NewLine +
                    "Thanks, Bobby53" + Environment.NewLine,
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                    );
            }

            cfg.DebugDump();

            lastCheckSpellCount = LegacySpellManager.KnownSpells.Count();

#if  USE_CUSTOM_PATH_PRECISION
            const float minPathPrecision = 2.5f;
			float prevPrec = Navigator.PathPrecision;
			if (prevPrec != minPathPrecision)
			{
				Navigator.PathPrecision = minPathPrecision;
				Slog("Changed Navigator precision from {0} to {1}", prevPrec, minPathPrecision);
			}
#else
            Dlog("Navigator.PathPrecision is: {0}", Navigator.PathPrecision);
#endif

            botStart = new BotEvents.OnBotStartDelegate(OnBotStart);
            BotEvents.OnBotStart += botStart;
            botStop = new BotEvents.OnBotStopDelegate(OnBotStop);
            BotEvents.OnBotStop += botStop;
            mapChanged = new BotEvents.Player.MapChangedDelegate(OnMapChanged);
            BotEvents.Player.OnMapChanged += mapChanged;
            // BotEvents.Player.OnPlayerDied += playerDied;


            // Targeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;

            Mount.OnMountUp += new EventHandler<MountUpEventArgs>(Mount_OnMountUp);
            
            //////////////////////////////////// Following would go in dtor ///////////////////
            // BotEvents.OnBotStart -= btStrt;
            // BotEvents.OnBotStopped -= btStp;
#if HONORBUDDY_SEQUENCE_MANAGER_FIXED
			SequenceManager.AddSequenceExecutorOverride(Sequence.ReleaseSpirit, SequenceOverride_ReleaseSpirit);
#endif
            // Lua.Events.AttachEvent("UNIT_SPELLCAST_INTERRUPTIBLE", HandleCastInterruptible);
            // Lua.Events.AttachEvent("UNIT_SPELLCAST_NOT_INTERRUPTIBLE", HandleCastNotInterruptible);
            Lua.Events.AttachEvent("ACTIVE_TALENT_GROUP_CHANGED", HandleTalentGroupChange); //goes to init()
            Lua.Events.AttachEvent("PLAYER_TALENT_UPDATE", HandlePlayerTalentUpdate); //goes to init()
            Lua.Events.AttachEvent("PARTY_MEMBERS_CHANGED", HandlePartyMembersChanged);
            Lua.Events.AttachEvent("TRAINER_CLOSED", HandleTrainerClosed);

            Lua.Events.AttachEvent("PLAYER_TARGET_CHANGED", HandlePlayerTargetChanged);
            Lua.Events.AttachEvent("UNIT_TARGET", HandlePlayerTargetChanged);
            Lua.Events.AttachEvent("PLAYER_DEAD", HandlePlayerDead);
            Lua.Events.AttachEvent("PLAYER_EQUIPMENT_CHANGED", HandlePlayerEquipmentChanged);
            Lua.Events.AttachEvent("END_BOUND_TRADEABLE", HandleEndBoundTradeable);

#if COMMENT
			if (SpellManager.HasSpell(331))
				DumpSpellEffectInformation(WoWSpell.FromId(331));
			if (SpellManager.HasSpell(77472))
				DumpSpellEffectInformation(WoWSpell.FromId(77472));
			if (SpellManager.HasSpell(8004))
				DumpSpellEffectInformation(WoWSpell.FromId(8004));
#endif
            //==============================================================================================
            //  end of first time initialize
            //==============================================================================================            
        }

        private static uint GetFPS()
        {
            List<string> result = CallLUA("return GetFramerate()");
            if (result == null || String.IsNullOrEmpty(result[0]))
                return 0;

            return (uint)double.Parse(result[0]);
        }

        void Mount_OnMountUp(object sender, MountUpEventArgs e)
        {
            Dlog("MountUp:  detected HonorBuddy trying to mount");

            if (_me.Mounted)
            {
                Dlog("MountUp:  already mounted, aborting mount request");
                e.Cancel = true;
                return;
            }

            if (_isBotQuesting && _me.IsOnTransport)
            {
                Dlog("MountUp:  am on a quest vehicle, aborting mount request");
                e.Cancel = true;
                return;
            }

            // wait here until any current cast has completed
            // .. but don't wait here on a GCD
            WaitForCurrentCast();

            // add a check here for ....
            //  ... if we are healing and heal targets nearby, suppress
            //  ... if we have attack targets nearby, suppress
            if (IsRAF() && GroupTank != null && !GroupTank.Mounted && !GroupTank.IsFlying && GroupTank.Distance < 30)
            {
                Dlog("MountUp:  suppressed for HB RaF Combat Assist /follow bug - leader not mounted and only {0:F1} yds away", GroupTank.Distance );
                e.Cancel = true;
                return;
            }

            if (typeShaman == ShamanType.Resto
                || (IsPVP() && cfg.PVP_CombatStyle != ConfigValues.PvpCombatStyle.CombatOnly)
                || (IsRAF() && cfg.RAF_CombatStyle != ConfigValues.RafCombatStyle.CombatOnly))
            {
                WoWUnit p = ChooseHealTarget(hsm.NeedHeal, SpellRange.Check);
                if (p != null && !p.IsMe )
                {
                    if ( !( _isBotLazyRaider && IsPVP()))
                    {
                        Dlog("MountUp:  cancelling mount request, player needs heal {0} @ {1:F1}%", Safe_UnitName(p), p.HealthPercent);
                        e.Cancel = true;
                        return;
                    }

                    Dlog("MountUp:  LazyRaider/PVP so mount instead of healing {0} @ {1:F1}%", Safe_UnitName(p), p.HealthPercent);
                }
            }

            if (TotemsWereSet)
            {
                Dlog("MountUp:  HB wants to mount and totems exist... recalling totems");
                _local.RecallTotemsForMana();
            }

            if (!InGhostwolfForm() && cfg.WaterWalking && WaterWalking())
            {
                WaitForCurrentCastOrGCD();
            }


            if (!IsMovementDisabled())
            {
                bool castGhostWolf = false;
                if (_isBotGatherBuddy || _isBotArchaeologyBuddy)
                {
                    Dlog("OnMountUp: Ghost Wolf suppressed because of Bot in use");
                    castGhostWolf = false;
                }
                else if (cfg.UseGhostWolfForm && (!CharacterSettings.Instance.UseMount || MountHelper.NumMounts == 0 || !Mount.CanMount()))
                {
                    Dlog("OnMountUp: unable to mount so using Ghost Wolf");
                    castGhostWolf = true;
                }
                else if (e.Destination != null && e.Destination.Distance(_me.Location) < Mount.MountDistance )
                {
                    Dlog("OnMountUp:  Destination provided is {0:F1} yds away, casting ghost wolf", e.Destination.Distance(_me.Location));
                    castGhostWolf = true;
                }
                else if (Styx.Logic.POI.BotPoi.Current == null || Styx.Logic.POI.BotPoi.Current.Type == Styx.Logic.POI.PoiType.None)
                {
                    if (CheckForSafeDistance("Mount and no PoI", _me.Location, Mount.MountDistance))
                        Dlog("OnMountUp:  No POI, using Mount since no enemy within {0:F1} yds", Mount.MountDistance);
                    else if (CheckForSafeDistance("Ghost Wolf and no PoI", _me.Location, GetGhostWolfDistance()))
                    {
                        castGhostWolf = true;
                        Dlog("OnMountUp:  No POI, using Ghost Wolf since no enemy within {0:F1} yds", GetGhostWolfDistance());
                    }
                }
                else if (Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location) > Mount.MountDistance)
                    Dlog("OnMountUp:  POI={0} @ {1:F1} yds, using Mount", Styx.Logic.POI.BotPoi.Current.Type.ToString(), Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location));
                else if (Styx.Logic.POI.BotPoi.Current.Type == Styx.Logic.POI.PoiType.Hotspot)
                    Dlog("OnMountUp:  POI={0} @ {1:F1} yds, always use Mount for this type", Styx.Logic.POI.BotPoi.Current.Type.ToString(), Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location));
                else if (Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location) > GetGhostWolfDistance())
                {
                    castGhostWolf = true;
                    Dlog("OnMountUp:  POI={0} @ {1:F1} yds, using Ghost Wolf", Styx.Logic.POI.BotPoi.Current.Type.ToString(), Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location));
                }

                if (cfg.UseGhostWolfForm && castGhostWolf)
                {
                    if (InGhostwolfForm())
                    {
                        Dlog("OnMountUp: already in Ghost Wolf form");
                        e.Cancel = true;
                    }
                    else
                    {
                        Dlog("OnMountUp:  attempting to cast Ghost Wolf");
                        if (GhostWolf())
                        {
                            e.Cancel = true;
                            if (_isBotBGBuddy)
                            {
                                // this is a hack to fix problem where BGBuddy moves to next objective
                                //  ..  before UI reflects that a cast is underway
                                while (!IsGameUnstable() && _me.IsAlive && !_me.Combat && (GCD() || HandleCurrentSpellCast()))
                                {
                                    Dlog("Mount_OnMountUp: waiting for GCD/Cast to complete");
                                }
                            }
                        }
                        else
                        {
                            Dlog("OnMountUp:  Ghost Wolf failed...");
                        }
                    }
                }
            }
        }

        private static WoWPoint TravellingToPOI()
        {
            WoWPoint pt = WoWPoint.Empty;
            if (Styx.Logic.POI.BotPoi.Current != null)
                pt = Styx.Logic.POI.BotPoi.Current.Location;
            return pt;
        }

        private static bool GotAliveMinion()
        {
            return _me.GotAlivePet || TotemExist(TotemId.EARTH_ELEMENTAL_TOTEM) || TotemExist(TotemId.FIRE_ELEMENTAL_TOTEM);
        }

        private static void IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            int cntOut = 0;
            if (!GotAliveMinion())
                return;

            for (int i = 0; i < incomingUnits.Count; i++)
            {
                if (incomingUnits[i] is WoWUnit)
                {
                    WoWUnit u = incomingUnits[i].ToUnit();
                    if ((IsPVP() && u.Distance < 80) || (u.Combat && IsTargetingMeOrMyStuff(u)))
                    {
                        cntOut++;
                        outgoingUnits.Add(u);
                        Dlog("IncludeTargetsFilter: added {0} targeting {1}", Safe_UnitName(u), Safe_UnitName(u.CurrentTarget));
                    }
                }
            }

            Dlog("IncludeTargetsFilter:  got alive pet(s) and added {0} mobs targeting them", cntOut);
        }

        private static void DumpSpellEffectInformation(WoWSpell spell)
        {
            Dlog("----------- {0} -----------", spell.Name.ToUpper());
            Dlog("Tooltip >>> {0}", spell.Tooltip);
            for (int i = 0; i <= 4; i++)
            {
                SpellEffect se = spell.GetSpellEffect(i);
                if (se == null)
                    Dlog("SpellEffect({0}):  null", i);
                else
                {
                    Dlog("SpellEffect({0}): Amplitude          {1}", i, se.Amplitude);
                    Dlog("SpellEffect({0}): AuraType           {1}", i, se.AuraType);
                    Dlog("SpellEffect({0}): BasePoints         {1}", i, se.BasePoints);
                    Dlog("SpellEffect({0}): EffectType         {1}", i, se.EffectType);
                    Dlog("SpellEffect({0}): Mechanic           {1}", i, se.Mechanic);
                    Dlog("SpellEffect({0}): MiscValueA         {1}", i, se.MiscValueA);
                    Dlog("SpellEffect({0}): MiscValueB         {1}", i, se.MiscValueB);
                    Dlog("SpellEffect({0}): MultipleValue      {1}", i, se.MultipleValue);
                    Dlog("SpellEffect({0}): RadiusIndex        {1}", i, se.RadiusIndex);
                    Dlog("SpellEffect({0}): RealPointsPerLevel {1}", i, se.RealPointsPerLevel);
                    Dlog("SpellEffect({0}): TriggerSpell       {1}", i, se.TriggerSpell);
                    Dlog("SpellEffect({0}):  {1}", i, se.ToString());
                }
            }
        }


        public static string SafeLogException(string msg)
        {
            msg = msg.Replace("{", "(");
            msg = msg.Replace("}", ")");
            return msg;
        }

        /* Log()
         * 
         * write 'msg' to log window.  message is suppressed if it is identical
         * to prior message.  Intent is to prevent log window spam
         */
        public static void Log(string msg, params object[] args)
        {
            Log(Color.DarkSlateGray, msg, args);
        }


        private static uint lineCount = 0;

        public static void Log(Color clr, string msg, params object[] args)
        {
            try
            {
                // following linecount hack is to stop dup line suppression of Log window
                Logging.Write(clr, msg + (++lineCount % 2 == 0 ? "" : " "), args);
                _Slogspam = msg;
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred logging msg: \n\t\"" + SafeLogException(msg) + "\"");
                Logging.WriteException(e);
            }
        }


        /* Slog()
         * 
         * write 'msg' to log window.  message is suppressed if it is identical
         * to prior message.  Intent is to prevent log window spam
         */
        private static string _Slogspam;

        public static void Slog(Color clr, string msg, params object[] args)
        {
            try
            {
                msg = String.Format(msg, args);
                if (msg == _Slogspam)
                    return;

                Log(clr, msg);
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred logging msg: \n\t\"" + SafeLogException(msg) + "\"");
                Logging.WriteException(e);
            }
        }

        public static void Slog(string msg, params object[] args)
        {
            Slog(Color.Black, msg, args);
        }


        public static void Elog(string msg, params object[] args)
        {
            Slog(Color.Red, msg, args);
        }

        /* Wlog()
         * 
         * write 'msg' to log window, but only if it hasn't been written already.
         */
        private static readonly List<string> _warnList = new List<string>();    // tracks warning messages issued by Wlog()

        public static void Wlog(string msg, params object[] args)
        {
            msg = String.Format(msg, args);
            String found = _warnList.Find(s => 0 == s.CompareTo(msg));
            if (found == null)
            {
                _warnList.Add(msg);
                Log(Color.Red, msg);
            }
        }

        /* Dlog()
         * 
         * Write Debug log message to log window.  message is suppressed if it
         * is identical to prior log message or verbose mode is turned off.  These
         * messages are trace type in nature to follow in more detail what has occurred
         * in the code.
         * 
         * NOTE:  I am intentionally putting debug message in the Log().  At this point,
         * it helps more having all data be time sequenced in the same window.  This will
         * near the close of development move to the Debug window instead
         */
        static string _Dlogspam;

        public static void Dlog(string msg, params object[] args)
        {
            try
            {
                msg = String.Format(msg, args);
/*
                if (msg == _Dlogspam) // || cfg.Debug == false)
                    return;
*/
                if (cfg.Debug)
                    Logging.WriteDebug("%   " + msg);
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred logging msg: \n\t\"" + SafeLogException(msg) + "\"");
                Logging.WriteException(e);
            }

            _Dlogspam = msg;
        }

        private void ReportBodyCount()
        {
            bool rptKill = (_killCountBase + _killCount) < InfoPanel.MobsKilled;
            bool rptDeath = (_deathCountBase + _deathCount) < InfoPanel.Deaths;

            if (rptDeath)
            {
                _deathCount = InfoPanel.Deaths - _deathCountBase;
                Slog("! Death #{0} at {1:F1} per hour fighting at x={2},y={3},z={4}",
                InfoPanel.Deaths,
                InfoPanel.DeathsPerHour,
                _me.Location.X,
                _me.Location.Y,
                _me.Location.Z
                );
            }

            if (rptKill)
            {
                _killCount = InfoPanel.MobsKilled - _killCountBase;
                Slog("! Kill #{0} at {1:F0} xp per hour fighting at x={2},y={3},z={4}",
                InfoPanel.MobsKilled,
                InfoPanel.XPPerHour,
                _me.Location.X,
                _me.Location.Y,
                _me.Location.Z
                );
            }
        }


        private bool CheckForAuraError(string sAuraName)
        {
            uint stackHB = 0, stackLUA = 0;
            _me.IsAuraPresent( sAuraName, out stackHB);
            IsAuraPresentOnMeLUA(sAuraName, out stackLUA);

            if (stackHB != stackLUA)
            {
                Dlog("AURA ERROR!!!!!  '{0}' has {1} stacks but HonorBuddy says it has {2} -- cancelling aura", sAuraName, stackLUA, stackHB);
                CancelAura(sAuraName);
            }

            return stackHB != stackLUA;
        }

        //private delegate void startBot();
        private void OnBotStart(EventArgs args)
        {
            Slog("");
            Slog(Color.DarkGreen, ">>> STARTING {0}", Name);

            Initialize();
        }

        private void OnBotStop(EventArgs args)
        {
            Slog("");
            Slog(Color.DarkGreen, ">>> STOPPING {0}", Name);

#if COLLECT_NEW_PURGEABLES
            ListSpecialInfoCollected();
#endif
        }

        private void OnMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            Dlog( "OnMapChanged:  handling change to different map id");
            Initialize();
        }


#if HONORBUDDY_SEQUENCE_MANAGER_FIXED
		private static void SequenceOverride_ReleaseSpirit()
		{
			_countMeleeEnemy = 0;               // # of melee mobs in combat with me
			_count10YardEnemy = 0;             // # of mobs withing 10 yards in combat with me
			_countRangedEnemy = 0;              // # of ranged mobs in combat with me
			TotemsWereSet = false;

			// next phase
			// ... if selfrez available, inspect surrounding area if clear within
			// ... a configurable time delay, then selfrez, otherwise repop
			List<string> hasSoulstone = Lua.GetReturnValues("return HasSoulstone()", "hawker.lua");
			if (hasSoulstone != null && hasSoulstone.Count > 0 && hasSoulstone[0] != "" && hasSoulstone[0].ToLower() != "nil")
			{
				/*
				Lua.DoString("UseSoulstone()");
                Countdown tickCount = new Countdown( 7500);
				while (!ObjectManager.Me.IsAlive && !tickCount.Done )
					Sleep(100);
				if (ObjectManager.Me.IsAlive)
					return;
				 */

				Slog("Skipping use of '{0}'", hasSoulstone[0]);
			}

			// SequenceManager.CallDefaultSequenceExecutor(Sequence.ReleaseSpirit);
			Lua.DoString("RepopMe()", "hawker.lua");

            Countdown tickCount = new Countdown( 10000);
			while (!ObjectManager.Me.IsAlive && !tickCount.Done)
				Sleep(100);

			if (!ObjectManager.Me.IsAlive)
			{
				SequenceManager.CallDefaultSequenceExecutor(Sequence.ReleaseSpirit);
			}
		}

#endif

        private void HandleTalentGroupChange(object sender, LuaEventArgs args) // to anywhere
        {
            Dlog("HandleTalentGroupChange:  event received");
            talents.Load();

            if (DidWeSwitchModes())
            {
                Slog("^EVENT:  Active Talent Group Changed : initializing...");
                Initialize();
            }
        }

        private void HandlePlayerTalentUpdate(object sender, LuaEventArgs args) // to anywhere
        {
            Dlog("HandlePlayerTalentUpdate:  event received");
            talents.Load();

            if (DidWeSwitchModes())
            {
                Slog("^EVENT:  Player Level/Talent Update : initializing...");
                Initialize();
            }
        }

        private void HandleTrainerClosed(object sender, LuaEventArgs args)
        {
            Dlog("HandleTrainerClosed:  event received");
            LegacySpellManager.Refresh();
            int chkCount = LegacySpellManager.KnownSpells.Count();
            if (chkCount != lastCheckSpellCount)
            {
                Slog("^EVENT:  Trainer Window closed : initializing...");
                Initialize();
                lastCheckSpellCount = chkCount;
            }
        }


        private void HandleCombatLogEvent(object sender, LuaEventArgs args)
        {
            const int ARG_EVENT = 1;
            const int ARG_SOURCEGUID = 3;
            const int ARG_DESTGUID = 7;
            const int ARG_SPELLID = 11;
            const int ARG_SPELLNAME = 12;
#if SUPPORT_IMMUNITY_DETECTION
            const int ARG_SPELLSCHOOL = 13;
#endif
            const int ARG_MISSTYPE = 14;
            const int ARG_CASTFAILTYPE = 14;

#if SUPPORT_IMMUNITY_DETECTION
            const int SPELLID_THUNDERSTORM = 51490;
#endif
            if (args.Args[ARG_EVENT].ToString() != "SPELL_MISSED")
            {
                if (cfg.Debug)
                {
                    if (args.Args[ARG_EVENT].ToString() == "SPELL_CAST_FAILED")
                    {
                        ulong guidSource = ulong.Parse(args.Args[ARG_SOURCEGUID].ToString().Substring(2), NumberStyles.HexNumber);
                        if (guidSource == _me.Guid)
                        {
                            ulong guidDest = ulong.Parse(args.Args[ARG_DESTGUID].ToString().Replace("0x", ""), NumberStyles.HexNumber);
                            WoWUnit unitDest = ObjectManager.GetObjectsOfType<WoWUnit>(true, false).FirstOrDefault(o => o.DescriptorGuid == guidDest);
                            if (guidDest == 0)
                            {
                                Dlog("SPELL_CAST_FAILED:  {0} on guid:0 due to '{2}'",
                                    args.Args[ARG_SPELLNAME].ToString(),
                                    args.Args[ARG_DESTGUID].ToString(),
                                    args.Args[ARG_CASTFAILTYPE].ToString());
                            }
                            else if (unitDest == null)
                            {
                                Dlog("SPELL_CAST_FAILED:  {0} on guid:{1}(cant find obj) due to '{2}'",
                                    args.Args[ARG_SPELLNAME].ToString(),
                                    args.Args[ARG_DESTGUID].ToString(),
                                    args.Args[ARG_CASTFAILTYPE].ToString());
                            }
                            else
                            {
                                Dlog("SPELL_CAST_FAILED:  {0} on {1} due to '{2}'", 
                                    args.Args[ARG_SPELLNAME].ToString(),
                                    Safe_UnitName(unitDest),
                                    args.Args[ARG_CASTFAILTYPE].ToString());
                            }
                        }
                    }
                }
                return;
            }

            try
            {
                ulong guidSource = ulong.Parse(args.Args[ARG_SOURCEGUID].ToString().Substring(2), NumberStyles.HexNumber);
                WoWUnit unitSource = ObjectManager.GetObjectsOfType<WoWUnit>(true, false).FirstOrDefault(o => o.DescriptorGuid == guidSource);

                if (unitSource == null)
                {
                    Dlog("@@@ Spell Missed for unknown source 0x{0}", guidSource);
                    return;
                }

                if (!unitSource.IsMe)
                {
                    return;
                }

                ulong guidTarget = ulong.Parse(args.Args[ARG_DESTGUID].ToString().Replace("0x", ""), NumberStyles.HexNumber);
                WoWUnit unitTarget = ObjectManager.GetObjectsOfType<WoWUnit>(true, false).FirstOrDefault(o => o.DescriptorGuid == guidTarget);
                if (unitTarget == null || !unitTarget.IsValid)
                {
                    Dlog("@@@ Spell Missed for unknown target 0x{0}", guidTarget);
                    return;
                }

                int spellId = (int)(double)args.Args[ARG_SPELLID];
                string spellName;
                try 
                {
                    WoWSpell spell = WoWSpell.FromId(spellId);
                    if (spell == null)
                        spellName = "(null)";
                    else
                        spellName = spell.Name;
                }
                catch 
                {
                    spellName = "(error)";
                }

                Dlog("@@@  SPELL_MISSED {0} cast {1} on {2} - misstype='{3}'", 
                    Safe_UnitName( unitSource), Safe_UnitName( unitTarget), spellName, args.Args[ARG_MISSTYPE].ToString());

                switch (args.Args[ARG_MISSTYPE].ToString())
                {
                    case "EVADE":
                        if ( _me.IsInInstance || IsPVP() )
                        {
                            if (guidSource == _me.Guid)
                            {
                                Dlog("### Ignoring EVADE message for {0}", Safe_UnitName(unitTarget));
                            }
                        }
                        else
                        {
                            Slog("### EVADE mob, blacklisting: {0} for 1 hour", Safe_UnitName(unitTarget));
                            Blacklist.Add(unitTarget.Guid, System.TimeSpan.FromHours(1));
                            if (_me.CurrentTarget == unitTarget)
                            {
                                StopAutoAttack();
                                Safe_SetCurrentTarget(null);
                            }
                        }
                        break;

#if SUPPORT_IMMUNITY_DETECTION
                    case "IMMUNE":
                        WoWSpellSchool spellSchool = (WoWSpellSchool)(int)(double)args.Args[ARG_SPELLSCHOOL];
                        if (unitTarget == null || unitTarget.IsPlayer || IsImmunityCheckDisabled())
                            break;

                        uint entry = unitTarget.Entry;
                        if (SPELLID_THUNDERSTORM == (int)(double)args.Args[ARG_SPELLID])
                        {
                            Dlog("### Ignoring {0} IMMUNE message for {1} on {2} [#{3}]", spellSchool, args.Args[ARG_SPELLNAME], unitTarget.Name, entry);
                            return;
                        }

                        KeyValuePair<string,WoWAura> aImmune = unitTarget.Buffs.FirstOrDefault(a => a.Value.Spell.GetSpellEffect(0).AuraType == WoWApplyAuraType.SchoolImmunity);
                        if (aImmune.Value!= null)
                        {
                            Slog("### Ignoring TEMPORARILY IMMUNITY to {0}, should not cast {1} on {2} [#{3}] until {4} buff expires", spellSchool, args.Args[ARG_SPELLNAME], unitTarget.Name, entry, aImmune.Value.Name);
                            return;
                        }

                        if (!ImmunityMap.ContainsKey(spellSchool))
                            ImmunityMap[spellSchool] = new HashSet<uint>();
                        if (ImmunityMap[spellSchool].Contains(entry))
                        {
                            // Slog("### IMMUNE to {0}, should not cast {1} on {2} [#{3}]", spellSchool, args.Args[ARG_SPELLNAME], unit.Name, entry);
                        }
                        // Divine Shield cast by some mobs providing temporary immunity... 
                        // .. ignore or blacklist for a few seconds
                        else if (!unitTarget.HasAura("Divine Shield"))
                        {
                            Slog("### IMMUNE mob, adding {0} [#{1}] to {2} list due to immune result to {3}", unitTarget.Name, entry, spellSchool, args.Args[ARG_SPELLNAME]);
                            ImmunityMap[spellSchool].Add(entry);
                        }
                        break;
#endif
                }
            }
            catch (Exception )
            {
                // Logging.Write(e.ToString());
            }
        }

        private void HandlePlayerDead(object sender, LuaEventArgs args)
        {
            if (IsPVP())
            {
                Slog("! Died in PVP fighting at x={0},y={1},z={2}",
                    _me.Location.X,
                    _me.Location.Y,
                    _me.Location.Z
                    );
            }

            _countMeleeEnemy = 0;               // # of melee mobs in combat with me
            _count10YardEnemy = 0;             // # of mobs withing 10 yards in combat with me
            _countRangedEnemy = 0;              // # of ranged mobs in combat with me
            TotemsWereSet = false;

            List<string> hasSoulstone = Lua.GetReturnValues("return HasSoulstone()", "hawker.lua");
            if (hasSoulstone != null && hasSoulstone.Count > 0 && !String.IsNullOrEmpty(hasSoulstone[0]) && hasSoulstone[0].ToLower() != "nil")
            {
                if (IsMovementDisabled())
                {
                    Log(Color.Aquamarine, "Suppressing {0} behavior since movement disabled...", hasSoulstone[0]);
                    return;
                }

                Countdown waitClearArea = new Countdown(cfg.ReincarnateMaxWait * 1000);
                Log( Color.Aquamarine, "Waiting up to {0} seconds for clear area to use {1}...", cfg.ReincarnateMaxWait, hasSoulstone[0]);
                int countMobs;
                do 
                {
                    SleepForLagDuration();
                    countMobs = (from u in AllEnemyMobs where u.Distance < cfg.ReincarnateMinClearDistance  select u).Count();
                    Dlog("HandlePlayerDead:  {0} enemies within {1} yds and {2} seconds remaining", countMobs, cfg.ReincarnateMinClearDistance, waitClearArea.ElapsedMilliseconds / 1000 );
                } while ( countMobs > cfg.ReincarnateMaxEnemiesNear && !waitClearArea.Done && !_me.IsAlive && !_me.IsGhost );

                if (_me.IsGhost)
                {
                    Log(Color.Aquamarine, "Insignia taken or something else released the corpse");
                    return;
                }

                if (_me.IsAlive)
                {
                    Log(Color.Aquamarine, "Ressurected by something other than ShamWOW...");
                    return;
                }

                if (countMobs > cfg.ReincarnateMaxEnemiesNear)
                {
                    Log(Color.Aquamarine, "Still {0} enemies within {1} yds, skipping {2}", countMobs, cfg.ReincarnateMinClearDistance, hasSoulstone[0]);
                    return;
                }

                if (!IsGameUnstable())
                {
                    RunLUA("UseSoulstone()");
                    WaitForCurrentCastOrGCD();
                }

                Countdown tickCount = new Countdown(1000);
                while (!IsGameUnstable() && !_me.IsAlive && !tickCount.Done)
                {
                    Sleep(50);
                }

                if (_me.IsAlive)
                {
                    Log(Color.Aquamarine, "^{0} successful!'", hasSoulstone[0]);
                }
            }
            else
            {

            }
        }

        private void HandlePartyMembersChanged(object sender, LuaEventArgs args)
        {
            Dlog("HandlePartyMembersChanged:  event received - found {0} in group", GroupMembers.Count());
            CheckGroupRoleAssignments();

            if (NeedToBuffRaid())
            {
                Slog("HandlePartyMembersChanged: buffing raid");
                BuffRaid();
            }
        }

        private void HandlePlayerTargetChanged(object sender, LuaEventArgs args)
        {
            if (!_me.GotTarget && GotAliveMinion() && !IsPVP())
            {
                Dlog("HandlePlayerTargetChanged:  no current target, so finding one");
                FindAggroTarget();
            }
        }

        private void HandlePlayerInventoryChanged(object sender, LuaEventArgs args)
        {
        }

        private void HandlePlayerEquipmentChanged(object sender, LuaEventArgs args)
        {
            Log("");
            if (sender == null)
                Dlog("HandlePlayerEquipmentChanged:  Initialization Call");
            else
                Slog("^EVENT HandlePlayerEquipmentChanged");
            GearScore();
            Log("");

            // reset trinket pointers
            trinkMana = new List<WoWItem>();
            trinkHealth = new List<WoWItem>();
            trinkPVP = new List<WoWItem>();
            trinkCombat = new List<WoWItem>();

            // now assign item points based upon use spell
            CheckGearForUsables(_me.Inventory.Equipped.Trinket1);
            CheckGearForUsables(_me.Inventory.Equipped.Trinket2);

            foreach (WoWItem item in trinkPVP)
                Slog("Detected PVP Trinket:  {0}", item.Name);
            foreach (WoWItem item in trinkHealth)
                Slog("Detected Health Trinket:  {0}", item.Name);
            foreach (WoWItem item in trinkMana)
                Slog("Detected Mana Trinket:  {0}", item.Name);
            foreach (WoWItem item in trinkCombat)
                Slog("Detected Combat Trinket:  {0}", item.Name);

            if (_me.Inventory.Equipped.Hands != null)
            {
                foreach (string tink in _hashTinkerCombat)
                {
                    if (null != _me.Inventory.Equipped.Hands.GetEnchantment(tink))
                    {
                        trinkCombat.Add(_me.Inventory.Equipped.Hands);
                        Slog("Combat Enchant:  '{0}' on {1}", tink, _me.Inventory.Equipped.Hands.Name);
                    }
                }

                if (null != _me.Inventory.Equipped.Hands.GetEnchantment("Z50 Mana Gulper"))
                {
                    trinkMana.Add(_me.Inventory.Equipped.Hands);
                    Slog("Mana Enchant:  'Z50 Mana Gulper' on {0}", _me.Inventory.Equipped.Hands.Name);
                }
                else if (null != _me.Inventory.Equipped.Hands.GetEnchantment("Spinal Healing Injector"))
                {
                    trinkHealth.Add(_me.Inventory.Equipped.Hands);
                    Slog("Combat Enchant:  'Spinal Healing Injector' on {0}", _me.Inventory.Equipped.Hands.Name);
                }
            }

            if (_me.Inventory.Equipped.Waist != null)
            {
                if (null != _me.Inventory.Equipped.Waist.GetEnchantment("Grounded Plasma Shield"))
                {
                    trinkCombat.Add(_me.Inventory.Equipped.Waist);
                    Slog("Combat Enchant:  'Grounded Plasma Shield' on {0}", _me.Inventory.Equipped.Waist.Name);
                }
            }

            _tier12CountResto = _me.Inventory.Equipped.Items.Count(o => o != null && (o.Entry == 71542 || o.Entry == 71543 || o.Entry == 71544 || o.Entry == 71545 || o.Entry == 71546 || o.Entry == 71300 || o.Entry == 71299 || o.Entry == 71298 || o.Entry == 71297 || o.Entry == 71296));
            Dlog("HandlePlayerEquipmentChanged:  detected {0} pieces of Resto T12", _tier12CountResto );
            if (_tier12CountResto >= 2)
                Slog("T12 Resto 2P Bonus equipped: Riptide cast for mana regen");

            _tier13CountResto = _me.Inventory.Equipped.Items.Count(o => o != null && _hashTier13Resto.Contains( o.Entry ));
            Dlog("HandlePlayerEquipmentChanged:  detected {0} pieces of Resto T13", _tier13CountResto);
            if (_tier13CountResto >= 4)
            {
                if (cfg.RAF_UseCooldowns)
                    Slog("T13 Resto 4P Bonus equipped: Spiritwalker's Grace used for haste buff");
                else
                {
                    Slog("T13 Resto 4P Bonus equipped: but cooldowns disabled");
                    _tier13CountResto = 0;
                }
            }
        }

        private static void HandleEndBoundTradeable(object sender, LuaEventArgs args)
        {
            Lua.DoString("EndBoundTradeable(" + args.Args[0] + ")");
        }

        HashSet<uint> _hashTier13Resto = new HashSet<uint>()
        {
            78786, 78767, 78813, 78834, 78820,
            76756, 76757, 76758, 76759, 76760,
            78691, 78672, 78718, 78739, 78725
        };

        private static string Right(string s, int c)
        {
            return s.Substring(c > s.Length ? 0 : s.Length - c);
        }

        /*
         * Safe_ Functions.  These were created to handle unexpected errors and
         * situations occurring in HonorBuddy.  try/catch handling is provided
         * where an exception is thrown by HB that shouldn't be. multiple
         * attempts at something (like dismounting) are done until the desired
         * state (!_me.Mounted) is achieved
         */
        private static string Safe_UnitID(WoWUnit unit)
        {
            if (unit == null)
                return "(null)";

            if (unit.IsMe)
                return "-ME-";

            if (unit.IsPlayer)
                return unit.Class.ToString() + "." + Right(String.Format("{0:X3}", unit.Guid), 4);

            return unit.Name + "." + Right(String.Format("{0:X3}", unit.Guid), 4);
        }

        private static string Safe_UnitName(WoWObject obj)
        {
            if (obj == null)
                return "(null)";

#if HIDE_PLAYER_NAMES
            if (obj is WoWUnit)
            {
                WoWUnit unit = obj.ToUnit();
                if (unit.IsPet)
                {
                    string sBase = "";
                    if (unit.OwnedByUnit.IsMe)
                        sBase = "-ME-";
                    else if (GroupHealer == unit.OwnedByUnit)
                        sBase = "-HEALER-";
                    else if (GroupTank == unit.OwnedByUnit)
                        sBase = "-TANK-";
                    else
                        sBase = unit.OwnedByUnit.Class.ToString() + "." + Right(String.Format("{0:X3}", unit.OwnedByUnit.Guid), 4);

                    return sBase + ":PET";
                }

                if (unit.IsMe)
                    return "-ME-";

                if (unit.IsPlayer) // && Safe_IsFriendly(unit)) // !unit.IsHostile ) // unit.IsFriendly)
                {
                    if (GroupHealer == unit)
                        return "-HEALER-";
                    if (GroupTank == unit)
                        return "-TANK-";
                    return unit.Class.ToString() + "." + Right(String.Format("{0:X3}", unit.Guid), 4);
                }
            }
#endif

            return obj.Name + "." + Right(String.Format("{0:X3}", obj.Guid), 4);
        }

        private static bool Safe_IsEnemyPlayer(WoWUnit unit)
        {
            return unit.IsPlayer && (Safe_IsHostile(unit) || (IsPVP() && unit.ToPlayer().BattlefieldArenaFaction != _me.BattlefieldArenaFaction));
        }

        // replacement for WoWUnit.IsFriendly
        // to handle bug in HB 1.9.2.5 where .IsFriendly throws exception casting WoWUnit -> WoWPlayer
        private static bool Safe_IsFriendly(WoWUnit unit)
        {
            if (unit == null)
                return false;

#if STILL_HAS_FRIENDLY_BUG
            if (!unit.IsPlayer)
                return unit.IsFriendly;

            WoWPlayer p = unit.ToPlayer();
            return p.IsHorde == ObjectManager.Me.IsHorde;
#else
            if (unit == null)
                return false;

            if (!unit.IsPlayer)
                return unit.IsFriendly;

            WoWPlayer player = unit.ToPlayer();
            if (player.IsHorde != _me.IsHorde)
                return false;

            return !IsPVP() || player.BattlefieldArenaFaction == _me.BattlefieldArenaFaction;
#endif      
        }

        // replacement for WoWUnit.IsNeutral
        // to handle bug in HB 1.9.2.5 where .IsHostile throws exception casting WoWUnit -> WoWPlayer
        private static bool Safe_IsNeutral(WoWUnit unit)
        {
            if (unit == null)
                return false;
#if STILL_HAS_FRIENDLY_BUG
            if (!unit.IsPlayer)
                return unit.IsNeutral;

            return false;
#else
            return unit.IsNeutral;
#endif
        }

        // replacement for WoWUnit.IsHostile
        // to handle bug in HB 1.9.2.5 where .IsHostile throws exception casting WoWUnit -> WoWPlayer
        private static bool Safe_IsHostile(WoWUnit unit)
        {
            if (unit == null)
                return false;

            if (!unit.IsPlayer)
                return unit.Attackable && (unit.IsHostile || HasAggro(unit) || Safe_IsProfileMob(unit));
            
            WoWPlayer player = unit.ToPlayer();
            if ( player.IsHorde != _me.IsHorde)
                return true;

            return IsPVP() && player.BattlefieldArenaFaction != _me.BattlefieldArenaFaction;
        }

        private static bool Safe_IsElite(WoWUnit unit)
        {
            if (unit != null)
            {
                if (unit.Elite && unit.MaxHealth > _me.MaxHealth && (unit.Level + 10) > _me.Level )
                    return true;

                if (unit.Level >= (_me.Level + cfg.PVE_LevelsAboveAsElite))
                    return true;
            }

            return false;
        }


        static uint idBossPrev;
        static bool wasBossPrev = false;

        private static bool Safe_IsBoss(WoWUnit unit)
        {
            if (unit == null)
                return false;

            if (unit.Entry == idBossPrev)
                return wasBossPrev;

            idBossPrev = unit.Entry;
            wasBossPrev = _hashBossList.Contains(unit.Entry);
            return wasBossPrev;
        }

        static uint idProfFactionPrev;
        static bool wasProfFactionPrev = false;

        private static bool Safe_IsProfileMob(WoWUnit unit)
        {
            if (unit == null || unit.Faction == null)
                return false;

            if (unit.Faction.Id == idProfFactionPrev)
                return wasProfFactionPrev;

            if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.Factions == null)
                return false;

            idProfFactionPrev = unit.Faction.Id;
            wasProfFactionPrev = ProfileManager.CurrentProfile.Factions.Contains(unit.Faction.Id);
            return wasProfFactionPrev;
        }

        // replacement for WoWObject.IsValid
        private static bool Safe_IsValid(WoWUnit u)
        {
            try
            {
                // simply test access to a property to confirm valid or force exception
                return u != null && u.HealthPercent > -1;
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch
            {
                return false;
            }
        }

        // replacement for WoWObject.IsValid
        private static bool Safe_IsValid(WoWObject o)
        {
#if DEBUG
			return o != null && ObjectManager.ObjectList.Contains(o);
#else
            return o != null;
#endif
        }

        private static bool MeImmobilized()
        {
            WoWAura aura = GetCrowdControlledAura(_me);
            bool isImmobile = IsImmobilizedAura(aura);
            
            if ( isImmobile )
                Slog(Color.Orange, "Your are {0} due to {1} and unable to cast", aura.Spell.Mechanic.ToString(), aura.Name );

            return isImmobile;
        }

        private static bool MeSilenced()
        {
            if (_me.IsImmobilized())
                ;
            else if (ObjectManager.Me.Silenced)
                Slog(Color.Orange, "You are silenced and unable to cast");
            else
                return false;

            return true;
        }

        private bool UseItem(WoWItem item)
        {
            if (item == null || !item.Usable)
                return false;

            if (item.Cooldown > 0)
                return false;

            const bool forceUse = true;
            item.Use(forceUse);
            Slog(Color.DodgerBlue, "/Use: {0}", item.Name);
            return true;
        }

        private static bool UseItem(List<WoWItem> list)
        {
            if (list == null || !list.Any())
                return false;

            foreach (WoWItem item in list)
            {
                if (item.Cooldown == 0)
                {
                    const bool forceUse = true;
                    if (item == null)
                        ;
                    else if (item.BaseAddress == 0)
                        Dlog("UseItem:  item='{0}' has BaseAddress=0; skipping use", item.Name);    // avoids exception
                    else if (item.Use(forceUse))
                    {
                        Slog(Color.DodgerBlue, "/Use: {0}", item.Name);
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckGearForUsables(WoWItem item)
        {
            if (item == null || !item.Usable)
                return;

            // sometimes spellId isn't in first array element, so find first non-zero
            // uint spellId = (uint)item.ItemInfo.SpellId.FirstOrDefault(si => si != 0);

            foreach (int spellId in item.ItemInfo.SpellId)
            {
                if (_hashTrinkCombat.Contains(spellId))
                    trinkCombat.Add(item);

                if (_hashTrinkHealth.Contains(spellId))
                    trinkHealth.Add(item);

                if (_hashTrinkMana.Contains(spellId))
                    trinkMana.Add(item);

                if (_hashTrinkPVP.Contains(spellId))
                    trinkPVP.Add(item);
            }
        }

        public static bool UseTrinkets()
        {
            if (typeShaman == ShamanType.Enhance && _me.Rooted && cfg.PVP_UsePVPTrinket && UseItem(trinkPVP))
            {
                // SleepForLagDuration();
                return true;
            }

            if (_me.IsImmobilized() && cfg.PVP_UsePVPTrinket && UseItem(trinkPVP))
            {
                // SleepForLagDuration();
                return true;
            }

            if (_me.HealthPercent < cfg.TrinkAtHealth && UseItem(trinkHealth))
                return true;
            
            if (_me.ManaPercent < cfg.TrinkAtMana && UseItem(trinkMana))
                return true;

            if ( _me.Combat )
            {
                if (typeShaman == ShamanType.Enhance && !_me.IsMoving && CurrentTargetInRangedDistance())
                    return UseItem(trinkCombat);
                else if (typeShaman == ShamanType.Enhance && CurrentTargetInMeleeDistance())
                    return UseItem(trinkCombat);
                else if (typeShaman == ShamanType.Resto && _me.Combat )
                    return UseItem(trinkCombat);
            }

            return false;
        }

        public static void RunLUA(string sCmd)
        {
            WaitForCurrentCastOrGCD();
            //Dlog("RunLUA: {0}", sCmd);
            Lua.DoString(sCmd, "shaman.lua");
        }

        public static List<string> CallLUA(string sCmd)
        {
            WaitForCurrentCastOrGCD();
            //Dlog("CallLUA: {0}", sCmd);
            List<string> retList = Lua.GetReturnValues(sCmd, "shaman.lua");
            return retList;
        }


        private static WoWPoint lastIsMovingLocation;
        public static bool Safe_IsMoving()
        {
#if SLOW
            bool isToonMoving = true;
            WoWPoint loc = _me.RelativeLocation;

            if (!_me.IsMoving)
            {
                float dist2D = loc.Distance2DSqr(lastIsMovingLocation);
                isToonMoving = (dist2D != 0);
            }

            lastIsMovingLocation = loc;
            return isToonMoving;
#else
            return _me.IsMoving;
#endif
        }

        /*
         * Only issues the Stop moving command if currently moving.  also
         * accounts for any lag which prevents immediate stop
         */
        public static void Safe_StopMoving( string sReason)
        {
            if (IsMovementDisabled())
                return;
            /*
            WoWMovement.MoveStop();
            Navigator.PlayerMover.MoveStop();
            */
            int countTries = 0;
            Countdown stopTimer = new Countdown(1000);

            while (!IsGameUnstable() && _me.IsAlive && ObjectManager.Me.IsMoving && !stopTimer.Done )
            {
                WoWMovement.MoveStop();
                Navigator.PlayerMover.MoveStop();
                countTries++;

                if (countTries > 1 )
                {
                    Sleep(25);
                }
                else if ( sReason != null)
                {
                    Log("Stopping: " + sReason);
                }
            }

            if (countTries > 1)
            {
                Dlog("Safe_StopMoving: {0} attempts to stop moving", countTries);
            }

            if (ObjectManager.Me.IsMoving)
            {
                if ( IsFleeing(_me))
                    Slog("Feared --- uggghhh");
                else if (_me.IsImmobilized())
                    Slog("Immobilized is true but still moving and can't stop; am I Feared?");
                else
                    Slog("ERROR: " + countTries + " attempts to stop moving and failed; character Feared?");
            }
        }

        public static void Safe_FaceTarget()
        {
            Safe_FaceUnit(_me.CurrentTarget);
        }

        public static void Safe_FaceUnit( WoWUnit unit)
        {
            if (unit == null || IsMovementDisabled())
                return;

            if (!unit.InLineOfSpellSight)
                return;

            if (!Safe_IsFriendly(_me.CurrentTarget) && !IsFacing( unit))
            {
                Dlog("FaceUnit:  turning to face {0}", Safe_UnitName(unit));
                unit.Face();
            }
        }

        public static void Safe_StopFace()
        {
            if (IsMovementDisabled())
                return;
            Dlog("StopFace:  cancelling facing");
            WoWMovement.StopFace();
        }


        /*
         * Only issues the Stop moving command if currently moving.  also
         * accounts for any lag which prevents immediate stop
         */
        private void Safe_Dismount()
        {
            if (!_me.Mounted)
                return;

            int countTries = 0;
            Stopwatch stopTimer = new Stopwatch();

            stopTimer.Start();
            while (!IsGameUnstable() && _me.IsAlive && _me.Mounted && stopTimer.ElapsedMilliseconds < 1500)
            {
                countTries++;
                Mount.Dismount();
                Sleep(25);
            }


            if (_me.Mounted)
            {
                Slog("LAG!! still mounted after {0} dismount attempts - timed out after {1} ms", countTries, stopTimer.ElapsedMilliseconds);
            }
            else if (countTries > 1)
            {
                Dlog("Dismount needed {0} attempts - took {1} ms", countTries, stopTimer.ElapsedMilliseconds);
            }
        }

        /*
         * Only issues the Stop moving command if currently moving.  also
         * accounts for any lag which prevents immediate stop
         */
        private static bool Safe_SetCurrentTarget(WoWUnit target)
        {
            if (IsTargetingDisabled())
                return true;

            Stopwatch stopTimer = new Stopwatch();

            stopTimer.Start();
            if (target == null)
                _me.ClearTarget();
            else if (!_me.GotTarget || _me.CurrentTarget.Guid != target.Guid)
                target.Target();

            while (!IsGameUnstable() && _me.IsAlive && _me.CurrentTarget != target && stopTimer.ElapsedMilliseconds < 12000)
            {
                Sleep(10);
            }

            if (_me.CurrentTarget != target)
                Dlog("Timeout:  must have died, game state change, or serious lag - .CurrentTarget not updated after {0} ms", stopTimer.ElapsedMilliseconds);
            else if (target == null)
            {
                Slog("Cleared current target");
                Dlog("Safe_SetCurrentTarget() took {0} ms to .ClearTarget", stopTimer.ElapsedMilliseconds);
                return true;
            }
            else
            {
                Slog("Setting current target to: {0}[{1}]", Safe_UnitName(target), target == null ? 0 : target.Level);
                Dlog("Safe_SetCurrentTarget() took {0} ms to set .CurrentTarget to {1}[{2}]", stopTimer.ElapsedMilliseconds, Safe_UnitName(target), target == null ? 0 : target.Level);
                return true;
            }

            return false;
        }

        private static bool IsFacing(WoWUnit unit)
        {
            return _me.IsSafelyFacing( unit, SAFE_CONE_SIZE);
        }

        private static bool FaceToUnit( WoWUnit unit)
        {
            if (unit == null )
                return false;

            if ( !IsFacing( unit ))
            {
                if (IsMovementDisabled())
                    return false;

                if (!unit.InLineOfSpellSight)
                    return false;

                if (IsPVP() || IsRAF())
                    Slog("FaceToUnit: facing to {0}:{1} thats {2:F1} yds away", unit.IsPlayer ? "player" : "npc", Safe_UnitName(unit), unit.Distance);

                Safe_FaceUnit(unit);
            }

            return true;
        }


        public static WoWPoint lastLoc;

        public static void ResetLastLocation()
        {
            lastLoc = new WoWPoint();
        }

        public static bool IsNearLastLocation( WoWPoint loc)
        {
            return loc.DistanceSqr(lastLoc) < 4;
        }

        /*
         * MoveTo()
         * 
         * if the point to move to is less than PathPrecision, then the toon
         * will not move.  This function checks if we are moving a very small
         * distance and forces movement by changing the precision if needed
         */
        public static bool MoveTo(WoWPoint newPoint)
        {
            MoveResult moveRes = MoveResult.PathGenerated;

            if (!IsMovementDisabled())
            {
                float distToMove = _me.Location.Distance(newPoint);
                float prevPrec = Navigator.PathPrecision;

                if (distToMove <= prevPrec)
                    Navigator.PathPrecision = distToMove - (float)0.1;

                Countdown stopCount = new Countdown(10000);
                while (!IsGameUnstable() && _me.IsAlive && IsCasting())
                {
                    if (stopCount.Done)
                    {
                        Slog(Color.Red, "ERROR:  Waited 10+ secs for cast to finish-- moving anyway");
                        break;
                    }
                }

                // if (Navigator.GeneratePath(_me.Location, newPoint).Length <= 0)
                if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, newPoint))
                {
                    moveRes = MoveResult.Failed;
                    Slog(Color.Red, "Mesh error - Cannot generate navigation path to new position");
                }
                else
                {
                    lastLoc = new WoWPoint(newPoint.X, newPoint.Y, newPoint.Z);

                    Stopwatch howLong = new Stopwatch();
                    howLong.Start();
                    moveRes = Navigator.MoveTo(newPoint);
                    howLong.Stop();
                    if ( moveRes == MoveResult.Failed || moveRes == MoveResult.PathGenerationFailed )
                        Slog(Color.Red, "Mesh error={0} - Cannot navigate to new position", moveRes.ToString());
                    else 
                        Log("MoveTo: point {0:F1} yds away took {1} ms", _me.Location.Distance(newPoint), howLong.ElapsedMilliseconds);

                    // if ( IsRAF())
                    //     Slog(Color.LightGray, "shamwow-move to point {0:F1} yds away from tank", newPoint.Distance(GroupTank.Location));
                }

                Navigator.PathPrecision = prevPrec;
            }

            return moveRes != MoveResult.Failed;
        }

        private bool MoveToCurrentTarget()
        {
            return MoveToUnit(_me.CurrentTarget);
        }

        public static bool MoveToUnit(WoWUnit obj)
        {
            return MoveToUnit(obj, _offsetForMeleePull);
        }

        public static bool MoveToUnit(WoWUnit unit, double dist)
        {
            if (unit == null)
                return false;

            if (IsMovementDisabled())
                return false;

            bool haveLOS = unit.InLineOfSpellSight;
            if (!haveLOS || (dist * dist) < unit.DistanceSqr)
            {
                if (IsPVP() || IsRAF())
                {
                    string typeObj = (unit is WoWPlayer ? "player" : (unit is WoWUnit ? "unit" : "Unit"));
                    Slog("MoveToUnit: moving to {0}:{1} thats {2:F1} yds away and {3}in line of sight", typeObj, Safe_UnitName(unit), unit.Distance, haveLOS ? "" : "NOT ");
                }

                WoWPoint newPoint = (dist < 0.01) ? unit.Location : WoWMovement.CalculatePointFrom(unit.Location, (float)dist);
                return MoveTo(newPoint);      // WoWMovement.ClickToMove(newPoint);
            }

            return false;
        }

        private bool MoveToHealTarget(WoWUnit unit, double distRange)
        {
            if (IsMovementDisabled())
                return false;

            if (!_me.IsUnitInRange(unit, distRange))
            {
                Slog("MoveToHealTarget:  moving within {0:F1} yds of Heal Target {1} that is {2:F1} yds away", distRange, Safe_UnitName(unit), unit.Distance);
                if (IsCasting())
                    WaitForCurrentCastOrGCD();

                Stopwatch timerLastMove = new Stopwatch();
                Stopwatch timerStuckCheck = new Stopwatch();

                while (!IsGameUnstable() && _me.IsAlive && Safe_IsValid(unit) && unit.IsAlive && !_me.IsUnitInRange(unit, distRange) && unit.Distance < 100)
                {
                    if (!Safe_IsMoving() || !timerLastMove.IsRunning || timerLastMove.ElapsedMilliseconds > 333)
                    {
                        MoveToUnit(unit);
                        timerLastMove.Reset();
                        timerLastMove.Start();
                    }

                    if (Safe_IsMoving())
                    {
                        timerStuckCheck.Reset();
                        timerStuckCheck.Start();
                    }
                    else if (timerStuckCheck.ElapsedMilliseconds > 2000)
                    {
                        Dlog("MoveToHealTarget:  stuck? for {0} ms", timerStuckCheck.ElapsedMilliseconds);
                        break;
                    }

                    if (!IsCastingOrGCD())
                    {
                        // while running, if someone else needs a heal throw a unleash elements on them
                        if (SpellManager.HasSpell("Unleash Elements") && SpellManager.CanCast("Unleash Elements"))
                        {
                            if (IsWeaponImbuedWithEarthLiving())
                            {
                                WoWUnit otherTarget = ChooseNextHealTarget(unit, (double)hsm.NeedHeal);
                                if (otherTarget != null)
                                {
                                    Slog("MoveToHealTarget:  healing {0} while moving to heal target {1}", Safe_UnitName(otherTarget), Safe_UnitName(unit));
                                    Safe_CastSpell(otherTarget, "Unleash Elements");
                                    // SleepForLagDuration();
                                    continue;
                                }
                            }
                        }

                        // while running, if someone else needs a heal throw a riptide on them
                        if (SpellManager.HasSpell("Riptide") && SpellManager.CanCast("Riptide") && !IsCastingOrGCD())
                        {
                            WoWUnit otherTarget = ChooseNextHealTarget(unit, (double)hsm.NeedHeal);
                            if (otherTarget != null)
                            {
                                Slog("MoveToHealTarget:  healing {0} while moving to heal target {1}", Safe_UnitName(otherTarget), Safe_UnitName(unit));
                                Safe_CastSpell(otherTarget, "Riptide");
                                // SleepForLagDuration();
                                continue;
                            }
                        }
                    }
                }

                if (Safe_IsMoving())
                {
                    Safe_StopMoving(String.Format("Heal Target is {0:F1} yds away", unit.Distance));
                }
            }

            return _me.IsUnitInRange(unit, distRange);
        }

        public static bool MoveToObject(WoWObject obj, double dist)
        {
            if (obj == null)
                return false;

            if (IsMovementDisabled())
                return false;

            bool haveLOS = obj.InLineOfSight;
            if (!haveLOS || (dist * dist) < obj.DistanceSqr)
            {
                if (IsPVP() || IsRAF())
                {
                    string typeObj = (obj is WoWPlayer ? "player" : (obj is WoWObject ? "Object" : "object"));
                    Slog("MoveToObject: moving to {0}:{1} thats {2:F1} yds away and {3}in line of sight", typeObj, Safe_UnitName(obj), obj.Distance, haveLOS ? "" : "NOT ");
                }

                WoWPoint newPoint = (dist < 0.01) ? obj.Location : WoWMovement.CalculatePointFrom(obj.Location, (float)dist);
                return MoveTo(newPoint);      // WoWMovement.ClickToMove(newPoint);
            }

            return false;
        }

        private bool FindBestTarget() { return FindBestTarget(_maxDistForRangeAttack); }
        private bool FindBestMeleeTarget() { return FindBestTarget(_maxDistForMeleeAttack); }

        private bool FindBestTarget(double withinDist)
        {
            // find mobs in melee distance
            WoWUnit newTarget = null;

            if (IsTargetingDisabled())
                return false;

#if DISABLE_TARGETING_FOR_INSTANCEBUDDY
			if (_me.IsInInstance && _isBotInstanceBuddy)
			{
				Dlog("InstanceBuddy: targeting disabled");
				return false;
			}
#endif
            if (IsRAF())
            {
                if (GroupTank == null)
                    return false;

                WoWUnit leaderTarget = GroupTank.CurrentTarget;

#if DISABLE_TARGETING_FOR_INSTANCEBUDDY
                if (_isBotInstanceBuddy)
                    Dlog( "Target search suppressed for InstanceBuddy");
                else 
#endif
                if (!GroupTank.IsAlive)
                    Dlog("FindBestTarget-RAF:  RaF Leader is Dead!");
                else if (!GroupTank.GotTarget)
                    Dlog("FindBestTarget-RAF:  RaF Leader does not have a current target!");
                else if (leaderTarget.CurrentHealth <= 1)
                    Dlog("Ignore RaF Leader Target -- unit {0} is dead", Safe_UnitName(leaderTarget));
                else if (!leaderTarget.Attackable)
                    Dlog("Ignore RaF Leader Target -- unit {0} is not attackable", Safe_UnitName( leaderTarget));
                else if (!Safe_IsHostile(leaderTarget))
                    Dlog("Ignore RaF Leader Target -- unit {0} is not hostile", Safe_UnitName(leaderTarget));
                else if (!leaderTarget.Combat)
                    Dlog("Ignore RaF Leader Target -- unit {0} is not in combat right now", Safe_UnitName( leaderTarget));
                else if (!_me.GotTarget || leaderTarget.Guid != _me.CurrentTarget.Guid)
                {
                    Slog(">>> SET LEADERS TARGET:  {0}[{1}] at {2:F1} yds",
                            Safe_UnitName(leaderTarget),
                            leaderTarget.Level,
                            leaderTarget.Distance
                            );
                    Safe_SetCurrentTarget(leaderTarget);
                    return true;
                }

                return false;
            }

            // otherwise, build a list of mobs based upon whether in Battlegrounds or not
            string typeList = "";

            if (!IsPVP())
            {
                typeList = "PVE";
                newTarget = (from o in ObjectManager.ObjectList
                        where o is WoWUnit
                        let unit = o.ToUnit()
                        where
                            unit.Distance <= withinDist 
                            && unit.Attackable
                            && unit.IsAlive
                            && unit.Combat
                            && !IsMeOrMyStuff(unit)
                            && (IsTargetingMeOrMyStuff(unit) || unit.CreatureType == WoWCreatureType.Totem)
                            && !Blacklist.Contains(unit.Guid)
                        orderby unit.CurrentHealth ascending
                        select unit
                            ).FirstOrDefault();
            }
            else
            {
                typeList = "PVP";
                newTarget = (from o in ObjectManager.ObjectList
                        where o is WoWUnit
                        let unit = o.ToUnit()
                        where
                            unit.Distance <= withinDist
                            && unit.Attackable
                            && unit.IsAlive 
                            && Safe_IsEnemyPlayer(unit)
                            && unit.InLineOfSpellSight 
                            && !unit.IsPet
                            && !Blacklist.Contains(unit.Guid)
                        orderby unit.CurrentHealth ascending
                        select unit
                            ).FirstOrDefault();
            }

            // now make best selection from list
            if (newTarget == null)
            {
                Dlog("FindBestTarget-{0}:  found 0 mobs within {1:F1} yds", typeList, withinDist);
            }
            else if (_me.GotTarget && newTarget.Guid == _me.CurrentTarget.Guid)
            {
                Dlog("FindBestTarget-{0}:  already targeting best mob {1}", typeList, Safe_UnitName(newTarget));
            }
            else
            {
                Slog(">>> BEST TARGET:  {0}-{1}[{2}] at {3:F1} yds",
                        newTarget.Class,
                        Safe_UnitName(newTarget),
                        newTarget.Level,
                        newTarget.Distance
                        );
                Safe_SetCurrentTarget(newTarget);
                return true;
            }

            return false;
        }

        public static WoWUnit FindRaidIconTarget(ConfigValues.RaidTarget rt)
        {
            WoWUnit raidTarget = null;
            if (rt == ConfigValues.RaidTarget.None)
                ;
            else if ( rt == ConfigValues.RaidTarget.Focus )
                raidTarget = ObjectManager.GetAnyObjectByGuid<WoWUnit>((ulong)_me.CurrentFocus);
            
            if (raidTarget != null && raidTarget.Attackable && !Safe_IsFriendly(raidTarget))
                return raidTarget;

            return null;
        }


        // compares targets to find one with the lowest health (not lowest % of health)
        // .. to hopefully score a quick kill
        private class HealthSorter : IComparer<WoWUnit>
        {
            public int Compare(WoWUnit obj1, WoWUnit obj2)
            {
                return obj1.CurrentHealth.CompareTo(obj2.CurrentHealth);
            }
        }

        public static bool HasAggro(WoWUnit unit)
        {
            return
                unit.Combat
                && unit.Attackable
                && unit.CurrentHealth > 1
                && !Safe_IsFriendly(unit)
                && (unit.Aggro || unit.PetAggro || IsTargetingMeOrMyGroup(unit));
        }

        // find any units with aggro to us (keeping us in combat)
        // ... should work for totems also
        public static bool FindAggroTarget()
        {
            if (IsTargetingDisabled())
                return false;

            if (IsPVP())
                return false;

            List<WoWUnit> mobs = (from o in ObjectManager.ObjectList
                                  where o is WoWUnit
                                  let unit = o.ToUnit()
                                  where HasAggro(unit) && !Blacklist.Contains(unit.Guid) && unit.Distance < 40
                                  orderby unit.CurrentHealth ascending
                                  select !IsMeOrMyStuff(unit) ? unit : unit.CurrentTarget
                                ).ToList();

#if COMMMENT
                        unit.GotTarget && unit.Combat &&
                        (
                            (Safe_IsHostile(unit) && IsMeOrMyStuff( unit.CurrentTarget) && unit.IsAlive )
                         || (IsMeOrMyStuff(unit) && Safe_IsHostile( unit.CurrentTarget) && unit.CurrentTarget.IsAlive)
                        )
#endif

            if (mobs != null && mobs.Any())
            {
                WoWUnit newTarget = mobs.First();
                if (newTarget.IsPet )
                    newTarget = newTarget.CreatedByUnit ?? ( newTarget.OwnedByUnit ?? newTarget );

                Slog(">>> AGGRO TARGET:  {0}-{1}[{2}] at {3:F1} yds",
                        newTarget.Class,
                        Safe_UnitName(newTarget),
                        newTarget.Level,
                        newTarget.Distance
                        );
                Safe_SetCurrentTarget(newTarget);
                if (newTarget.Aggro)
                    Dlog("FindAggroTarget: could also find using .Aggro");
                else if (newTarget.PetAggro)
                    Dlog("FindAggroTarget: could also find using .PetAggro");
                else
                    Dlog("FindAggroTarget:  could only find with IsMyStuff");

                return true;
            }

            Dlog("FindAggroTarget: no aggro mobs found");
            return false;
        }

        public static bool InGroup()
        {
#if DEBUG_GRIND
            return false;
#else
            return ObjectManager.Me.IsInParty || ObjectManager.Me.IsInRaid;
#endif
        }

        public static bool IsRAF()
        {
#if DEBUG_RAF
            return true;
#else
            return InGroup() && !IsPVP(); // from Nesox
#endif
            // old test - return !IsPVP() && ObjectManager.Me.PartyMember1 != null;
        }

        public static bool IsRAFandTANK()
        {
            return IsRAF() && GroupTank != null && GroupTank.IsValid;
        }

        public static bool IsPVP()
        {
#if DEBUG_PVP
            return true;
#else
            return Battlegrounds.IsInsideBattleground;
#endif
        }

        public static bool IsHealer()
        {
            if (IsPVP())
                return cfg.PVP_CombatStyle != ConfigValues.PvpCombatStyle.CombatOnly || typeShaman == ShamanType.Resto;

            if (!IsRAF())
                return false;

            if (cfg.RAF_CombatStyle == ConfigValues.RafCombatStyle.Auto)
            {
                // based upon spec/role
                if (typeShaman == ShamanType.Resto || _myGroupRole == WoWPartyMember.GroupRole.Healer)
                    return true;

                // based upon group health (offhealing)
                if (cfg.RAF_GroupOffHeal > 0 && null != ChooseHealTarget(cfg.RAF_GroupOffHeal, SpellRange.Check))
                    return true;

                // in an instance and healer dead/offline
                if ( _me.IsInInstance && (GroupHealer == null || !GroupHealer.IsAlive))
                    return true;

                // must be dps only
                return false;
            }

            return cfg.RAF_CombatStyle != ConfigValues.RafCombatStyle.CombatOnly || typeShaman == ShamanType.Resto;
        }

        public static bool IsHealerOnly()
        {
            if (IsPVP())
                return cfg.PVP_CombatStyle == ConfigValues.PvpCombatStyle.HealingOnly;

            if (!IsRAF())
                return false;

            if (cfg.RAF_CombatStyle == ConfigValues.RafCombatStyle.Auto)
                return _myGroupRole == WoWPartyMember.GroupRole.Healer || typeShaman == ShamanType.Resto;

            return cfg.RAF_CombatStyle == ConfigValues.RafCombatStyle.HealingOnly;
        }

        public static bool IsCombatOnly()
        {
            return !IsHealer();
        }

        public static bool IsMeOrMyStuff(WoWUnit unit)
        {
            if (unit == null)
                return false;

            // find topmost unit in CreatedByUnit chain
            while (unit.CreatedByUnit != null)
                unit = unit.CreatedByUnit;

            // check if this unit was created by me
            return unit.IsMe;
        }

        public static bool IsTargetingMeOrMyStuff(WoWUnit unit)
        {
            return unit != null && IsMeOrMyStuff(unit.CurrentTarget);
        }

        public static bool IsMeOrMyGroup(WoWUnit unit)
        {
            if (unit != null)
            {
                // find topmost unit in CreatedByUnit chain
                while (unit.CreatedByUnit != null)
                    unit = unit.CreatedByUnit;

                if (unit.IsPlayer)
                {
                    WoWPlayer p = unit.ToPlayer();
                    if (GroupMembers.Contains(p))
                        return true;
                }
            }

            return false;
        }

        public static bool IsTargetingMeOrMyGroup(WoWUnit unit)
        {
            return unit != null && IsMeOrMyGroup(unit.CurrentTarget);
        }

        public static bool IsImmune(WoWUnit unit, WoWSpellSchool spellSchool)
        {
#if SUPPORT_IMMUNITY_DETECTION
            return !IsImmunityCheckDisabled()
                && Safe_IsValid(unit)
                && ImmunityMap.ContainsKey(spellSchool)
                && ImmunityMap[spellSchool].Contains(unit.Entry);
#else
            return false;
#endif
        }

        public static bool IsImmunneToNature(WoWUnit unit)
        {
#if SUPPORT_IMMUNITY_DETECTION
            return IsImmune(unit, WoWSpellSchool.Nature);
#else
            return false;
#endif
        }

        public static bool IsImmunneToFire(WoWUnit unit)
        {
#if SUPPORT_IMMUNITY_DETECTION
            return IsImmune(unit, WoWSpellSchool.Fire);
#else
            return false;
#endif
        }

        public static bool IsImmunneToFrost(WoWUnit unit)
        {
#if SUPPORT_IMMUNITY_DETECTION
            return IsImmune(unit, WoWSpellSchool.Frost);
#else
            return false;
#endif
        }

#if NOT_DEALT_WITH_BY_CC
        public static bool IsImmunneToArcane(WoWUnit unit)
        {
            return IsImmune(unit, WoWSpellSchool.Arcane);
        }

        public static bool IsImmunneToHoly(WoWUnit unit)
        {
            return IsImmune(unit, WoWSpellSchool.Holy);
        }

        public static bool IsImmunneToShadow(WoWUnit unit)
        {
            return IsImmune(unit, WoWSpellSchool.Shadow );
        }

        public static bool IsImmunneToPhysical(WoWUnit unit)
        {
            return IsImmune(unit, WoWSpellSchool.Physical);
        }
#endif

#if OLD_STYLE_FEAR_CHECK
        public static bool IsFearMob(WoWUnit unit)
        {
            if (!Safe_IsValid(unit))
                return false;

            bool found = _hashTremorTotemMobs.Contains(unit.Entry);
            return found;
        }
#endif

        // used to cache the results of the last LUA call to check Weapon Imbues
        private bool _needMainhandImbue;
        private bool _needOffhandImbue;

        // temporary enchant numbers
        const uint IMBUE_WINDFURY_WEAPON = 283;
        const uint IMBUE_FLAMETONGE_WEAPON = 5;
        const uint IMBUE_ROCKBITER_WEAPON = 3021;
        const uint IMBUE_EARTHLIVING_WEAPON = 3345;
        const uint IMBUE_FROSTBRAND_WEAPON = 2;

        private uint ImbueId(int spellid)
        {
            const uint SPELL_WINDFURY_WEAPON = 8232;
            const uint SPELL_FLAMETONGE_WEAPON = 8024;
            const uint SPELL_ROCKBITER_WEAPON = 8017;
            const uint SPELL_EARTHLIVING_WEAPON = 51730;
            const uint SPELL_FROSTBRAND_WEAPON = 8033;

            if (spellid == SPELL_WINDFURY_WEAPON)
                return IMBUE_WINDFURY_WEAPON;

            if ( spellid == SPELL_FLAMETONGE_WEAPON)
                return IMBUE_FLAMETONGE_WEAPON;

            if ( spellid == SPELL_ROCKBITER_WEAPON)
                return IMBUE_ROCKBITER_WEAPON;

            if (spellid == SPELL_EARTHLIVING_WEAPON)
                return IMBUE_EARTHLIVING_WEAPON;

            if (spellid == SPELL_FROSTBRAND_WEAPON)
                return IMBUE_FROSTBRAND_WEAPON;

            return 0;
        }

        /*
         * Reports whether we to stop for any Weapon Buffs
         */
        private bool DoWeaponsHaveImbue()
        {
            bool doesIt = false;

            if (CanImbue(_me.Inventory.Equipped.MainHand))
                doesIt = doesIt || _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id != 0;
            if (CanImbue(_me.Inventory.Equipped.OffHand))
                doesIt = doesIt || _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id != 0;

            return doesIt;
        }

        private bool IsWeaponImbuedWithDPS()
        {
            bool doesIt = false;

            if (CanImbue(_me.Inventory.Equipped.MainHand))
            {
                doesIt = _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id != 0
                      && _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id != IMBUE_EARTHLIVING_WEAPON;
            }

            if (!doesIt && CanImbue(_me.Inventory.Equipped.OffHand))
            {
                doesIt = _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id != 0
                      && _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id != IMBUE_EARTHLIVING_WEAPON;
            }

            return doesIt;
        }

        private bool IsWeaponImbuedWithEarthLiving()
        {
            bool doesIt = false;

            if (CanImbue(_me.Inventory.Equipped.MainHand))
                doesIt = _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id == IMBUE_EARTHLIVING_WEAPON;

            if (!doesIt && CanImbue(_me.Inventory.Equipped.OffHand))
                doesIt = _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id == IMBUE_EARTHLIVING_WEAPON;

            return doesIt;
        }

        private bool IsWeaponImbuedWithFrostBrand()
        {
            bool doesIt = false;

            if (CanImbue(_me.Inventory.Equipped.MainHand))
                doesIt = _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id == IMBUE_FROSTBRAND_WEAPON;

            if (!doesIt && CanImbue(_me.Inventory.Equipped.OffHand))
                doesIt = _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id == IMBUE_FROSTBRAND_WEAPON;

            return doesIt;
        }

        private bool IsWeaponImbueNeeded()
        {
            // due to lag between imbue spellcast and wow client updating buff aura, add delay before allowing subsequent imbues check
            if (!waitImbueCast.Done)
            {
                Dlog("IsWeaponImbueNeeded():  waiting {0} ms until next imbue check", waitImbueCast.Remaining);
                return false;
            }

            _needMainhandImbue = false;
            _needOffhandImbue = false;

            if (_me.Inventory.Equipped == null)
                return false;

            // now make sure we have a mainhand weapon we can imbue
            if (!CanImbue(_me.Inventory.Equipped.MainHand))
                return false;

            if (typeShaman == ShamanType.Unknown)
                return false;

            // see if we trained any weapon enchants yet... if not then don't need to imbue weapon
            string imbueSpellMainhand;
            string imbueSpellOffhand;
            GetBestWeaponImbues(out imbueSpellMainhand, out imbueSpellOffhand);

            if (string.IsNullOrEmpty(imbueSpellMainhand))
                return false;

            uint imbueIdMainhand = 0;
            if (SpellManager.HasSpell(imbueSpellMainhand))
                imbueIdMainhand = ImbueId( SpellManager.Spells[imbueSpellMainhand].Id);

            uint imbueIdOffhand = 0;
            if (SpellManager.HasSpell(imbueSpellOffhand))
                imbueIdOffhand = ImbueId(SpellManager.Spells[imbueSpellOffhand].Id);



            // get the enchant info from LUA
#if USE_LUA_FOR_IMBUES
			List<string> weaponEnchants = CallLUA("return GetWeaponEnchantInfo()");
			if (Equals(null, weaponEnchants))
				return false;
			_needMainhandImbue = weaponEnchants[0] == "" || weaponEnchants[0] == "nil";
			if (IsOffhandWeaponEquipped())
				_needOffhandImbue = weaponEnchants[3] == "" || weaponEnchants[3] == "nil";

            if (_needMainhandImbue)
                Dlog("Mainhand weapon {0} needs imbue", _me.Inventory.Equipped.MainHand == null ? "(none)" : _me.Inventory.Equipped.MainHand.Name);
            else
            {
                //  Dlog("Mainhand weapon {0} imbued with {1} / {2}", _me.Inventory.Equipped.MainHand.Name, _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Name, weaponEnchants[0]);
            }

            if (_needOffhandImbue)
                Dlog("Offhand  weapon {0} needs imbue", _me.Inventory.Equipped.OffHand == null ? "(none)" : _me.Inventory.Equipped.OffHand.Name);
            else
            {
                // Dlog("Offhand weapon {0} imbued with {1} / {2}", _me.Inventory.Equipped.OffHand.Name, _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Name, weaponEnchants[3]);
            }
#else

            _needMainhandImbue = _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id != imbueIdMainhand ;
            if (_needMainhandImbue)
            {
                Dlog("IsWeaponImbueNeeded:  mainhand has imbue '{0}' but needs '{1}'", _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Name, imbueSpellMainhand);
            }

            if (CanImbue(_me.Inventory.Equipped.OffHand))
            {
                _needOffhandImbue = _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id != imbueIdOffhand;
                if (_needOffhandImbue)
                {
                    Dlog("IsWeaponImbueNeeded:   offhand has imbue '{0}' but needs '{1}'", _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Name, imbueSpellOffhand);
                }
            }
#endif

            return _needMainhandImbue || _needOffhandImbue;
        }

        private void GetBestWeaponImbues(out string enchantMainhand, out string enchantOffhand)
        {
            List<string> listMainhand = null;
            List<string> listOffhand = null;

            enchantMainhand = "";
            enchantOffhand = "";

            if (IsPVP())
            {
                enchantMainhand = cfg.PVP_MainhandImbue;
                enchantOffhand = cfg.PVP_OffhandImbue;
            }
            else
            {
                enchantMainhand = cfg.PVE_MainhandImbue;
                enchantOffhand = cfg.PVE_OffhandImbue;
            }

            // Dlog("gbwe1 --  mh:{0},  oh:{1}", enchantMainhand, enchantOffhand);
            switch (typeShaman)
            {
                case ShamanType.Unknown:
                    return;
                case ShamanType.Elemental:
                    // Dlog("Enchant - choosing Elemental Defaults for Auto");
                    listMainhand = _enchantElemental;
                    listOffhand = listMainhand;
                    break;
                case ShamanType.Resto:
                    // Dlog("Enchant - choosing Restoration Defaults for Auto");
                    listMainhand = _enchantResto;
                    listOffhand = _enchantResto;
                    break;
                case ShamanType.Enhance:
                    if (IsPVP())
                    {
                        // Dlog("Enchant - choosing PVP Enhancement Defaults for Auto");
                        listMainhand = _enchantEnhancementPVP_Mainhand;
                        listOffhand = _enchantEnhancementPVP_Offhand;
                    }
                    else
                    {
                        // Dlog("Enchant - choosing PVE Enhancement Defaults for Auto");
                        listMainhand = _enchantEnhancementPVE_Mainhand;
                        listOffhand = _enchantEnhancementPVE_Offhand;
                    }
                    break;
            }

            // Dlog("gbwe2 --  mh:{0},  oh:{1}", enchantMainhand, enchantOffhand);

            if ('A' == enchantMainhand.ToUpper()[0] && listMainhand != null)
            {
                enchantMainhand = listMainhand.Find(spellname => SpellManager.HasSpell(spellname));
                //Dlog("Enchant - Mainhand:  configured for AUTO so choosing '{0}'", enchantMainhand);
            }
            else
            {
                //Dlog("Enchant - Mainhand:  configured for '{0}'", enchantMainhand);
            }

            if ('A' == enchantOffhand.ToUpper()[0] && listOffhand != null)
            {
                enchantOffhand = listOffhand.Find(spellname => SpellManager.HasSpell(spellname));
                //Dlog("Enchant - Offhand:   configured for AUTO so choosing '{0}'", enchantOffhand);
            }
            else
            {
                //Dlog("Enchant - Offhand:   configured for '{0}'", enchantOffhand);
            }

            return;
        }

        private Countdown waitImbueCast = new Countdown();
        private const int IMBUE_CHECK_DELAY = 750;

        // ImbueWeapons imbues the mainhand and offhand weapons with the best determined
        // or user selected weapon imbues.  It will imbue a maximum of 1 weapon per call,
        // and will require at least a minimum time to have passed since the last weapon 
        // imbue call (to ensure 
        private bool ImbueWeapons()
        {
            bool castSpell = false;

            try
            {
                if (!_needMainhandImbue && !_needOffhandImbue)
                    return false;

                if (!waitImbueCast.Done)
                {
                    Dlog("ImbueWeapons:  waiting {0} ms until next weapon imbue spell cast", waitImbueCast.Remaining);
                    return false;
                }

                string enchantMainhand;
                string enchantOffhand;

                GetBestWeaponImbues(out enchantMainhand, out enchantOffhand);

                // weapon has an imbue but need imbue flag set, then its wrong one so clear 
                if (_needMainhandImbue && _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Id != 0)
                {
                    Dlog("ImbueWeapons:  cancelling current MAINHAND imbue '{0}'", _me.Inventory.Equipped.MainHand.TemporaryEnchantment.Name);
                    RunLUA("CancelItemTempEnchantment(1)");
                    castSpell = true;
                }

                // weapon has an imbue but need imbue flag set, then its wrong one so clear 
                if (_needOffhandImbue && _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id != 0)
                {
                    Dlog("ImbueWeapons:  cancelling current OFFHAND imbue '{0}'", _me.Inventory.Equipped.OffHand.TemporaryEnchantment.Name);
                    RunLUA("CancelItemTempEnchantment(2)");
                    castSpell = true;
                }

                if (!castSpell && _needMainhandImbue)
                {
                    if (!waitImbueCast.Done)
                    {
                        Dlog("ImbueWeapons(Mainhand):  need to wait {0} ms until next weapon imbue", waitImbueCast.Remaining);
                    }
                    else
                    {
                        Dlog("ImbueWeapons:  NeedOnMainhand={0}{1}", _needMainhandImbue, !_needMainhandImbue ? "" : "-" + enchantMainhand);
                        castSpell = Safe_CastSpell( _me, enchantMainhand);
                        WaitForCurrentCastOrGCD();
                        _needMainhandImbue = !castSpell;
                    }
                }

                if (!castSpell && _needOffhandImbue)
                {
                    if (!waitImbueCast.Done)
                    {
                        Dlog("ImbueWeapons(Offhand):  need to wait {0} ms until next weapon imbue", waitImbueCast.Remaining);
                    }
                    else
                    {
                        Dlog("ImbueWeapons:  NeedOnOffhand={0}{1}", _needOffhandImbue, !_needOffhandImbue ? "" : "-" + enchantOffhand);
                        castSpell = Safe_CastSpell(_me, enchantOffhand);
                        WaitForCurrentCastOrGCD();
                        _needOffhandImbue = !castSpell;
                    }
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in ImbueWeapons()");
                Logging.WriteException(e);
            }

            if (castSpell)
            {
                waitImbueCast.Remaining = IMBUE_CHECK_DELAY;
            }

            return castSpell;
        }

        /*
         * Checks to see if Off Hand slot currently has a weapon in it.
         * Uses a timer so that LUA call is not made more than once a minute
         */
        private bool CanImbue(WoWItem item)
        {
            if (item != null && item.ItemInfo.IsWeapon)
            {
                switch (item.ItemInfo.WeaponClass)
                {
                    case WoWItemWeaponClass.Axe:
                        return true;
                    case WoWItemWeaponClass.AxeTwoHand:
                        return true;
                    case WoWItemWeaponClass.Dagger:
                        return true;
                    case WoWItemWeaponClass.Fist:
                        return true;
                    case WoWItemWeaponClass.Mace:
                        return true;
                    case WoWItemWeaponClass.MaceTwoHand:
                        return true;
                    case WoWItemWeaponClass.Polearm:
                        return true;
                    case WoWItemWeaponClass.Staff:
                        return true;
                    case WoWItemWeaponClass.Sword:
                        return true;
                    case WoWItemWeaponClass.SwordTwoHand:
                        return true;
                }
            }

            return false;
        }

        public static uint GetMaelstromCount()
        {
            uint timeLeft;
            return GetMaelstromCount( out timeLeft);
        }

        public static uint GetMaelstromCount(out uint timeLeft)
        {
            uint stackCount;
            IsAuraPresentOnMeLUA(localName_MaelstromWeapon, out stackCount, out timeLeft);
            return stackCount;
        }

        public static uint GetLightningShieldCount()
        {
            uint stackCount;
            IsAuraPresentOnMeLUA(localName_LightningShield, out stackCount);
            return stackCount;
        }

        public static bool IsAuraPresentOnMeLUA(string sAura )
        {
            uint stackCount;
            return IsAuraPresentOnMeLUA(sAura, out stackCount);
        }

        public static uint GetTidalWavesCount()
        {
            uint timeLeft;
            return GetTidalWavesCount(out timeLeft);
        }

        public static uint GetTidalWavesCount(out uint timeLeft)
        {
            uint stackCount;
            IsAuraPresentOnMeLUA(localName_TidalWaves, out stackCount, out timeLeft);
            return stackCount;
        }

        public static bool IsAuraPresentOnMeLUA(string sAura, out uint stackCount)
        {
            List<string> myAuras = Lua.GetReturnValues("return UnitAura(\"player\",\"" + sAura + "\")");
            if (Equals(null, myAuras))
            {
                stackCount = 0;
                return false;
            }

            stackCount = (uint)Convert.ToInt32(myAuras[3]);
            return true;
        }

        public static bool IsAuraPresentOnMeLUA(string sAura, out uint stackCount, out uint timeLeft)
        {
            stackCount = 0;
            timeLeft = 0;

            // using (new FrameLock())
            {
                List<string> myAura = Lua.GetReturnValues("return UnitAura(\"player\",\"" + sAura + "\")");
                if (!Equals(null, myAura))
                {
                    stackCount = (uint)Convert.ToInt32(myAura[3]);
                    List<string> getTime = Lua.GetReturnValues("return GetTime()");
                    if (!Equals(null, getTime))
                    {
                        double endTime = Convert.ToDouble(myAura[6]);
                        double upTime = Convert.ToDouble(getTime[0]);
                        double secsRemaining = endTime - upTime;
                        timeLeft = (uint)(secsRemaining * 1000);
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetLocalSpellName( int spellId )
        {
            List<string> spellInfo = Lua.GetReturnValues("return GetSpellInfo(" + spellId.ToString() + ")");
            if (!Equals(null, spellInfo) && !String.IsNullOrEmpty(spellInfo[0]))
                return spellInfo[0];

            return String.Empty;
        }

        public static bool InVehicle()
        {
            int inVehicle = Lua.GetReturnVal<int>("return UnitInVehicle(\"player\")", 0);
            return inVehicle == 1;
        }

        private void CancelAura(string auraName)
        {
            RunLUA("/cancelaura " + auraName);
        }

        public static Dictionary<int, Mob> _dictMob = new Dictionary<int, Mob>();


        private static WoWPartyMember.GroupRole GetGroupRoleAssigned(WoWPartyMember pm)
        {
            const int ROLEMASK = 0x0FE;
 
            if (pm != null)
            {
                // .Role is returning 1's bit set but no enum established... ?
                return (WoWPartyMember.GroupRole)((int)pm.Role & ROLEMASK);
            }

            return WoWPartyMember.GroupRole.None;
        }


        private static WoWPartyMember.GroupRole GetGroupRoleAssigned(WoWPlayer p)
        {
            if (p != null)
            {
                if (p.IsMe)
                {
                    return GetMyGroupRole();
                }
                else if (InGroup())
                {
                    WoWPartyMember pm = GroupMemberInfos.FirstOrDefault(gm => gm.Guid == p.Guid);
                    return GetGroupRoleAssigned(pm);
                }
            }
            return WoWPartyMember.GroupRole.None;
        }

        private static WoWPartyMember.GroupRole GetMyGroupRole()
        {
            if (InGroup())
            {
                try
                {
                    WoWPartyMember pme = new WoWPartyMember(_me.Guid, true);
                    if (pme != null)
                    {
                        Dlog("Retrieving my group role via HB API");
                        return GetGroupRoleAssigned(pme);
                    }

                    Dlog("Retrieving my group role via WOW LUA");
                    string luaCmd = string.Format("return UnitGroupRolesAssigned(\"player\")");
                    string sRole = Lua.GetReturnVal<string>(luaCmd, 0);
                    if ( !string.IsNullOrEmpty(sRole) )
                    {
                        switch ( sRole.ToUpper()[0] )
                        {
                            case 'D':
                                return WoWPartyMember.GroupRole.Damage;
                            case 'T':
                                return WoWPartyMember.GroupRole.Tank;
                            case 'H':
                                return WoWPartyMember.GroupRole.Healer;
                        }
                    }
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("HB EXCEPTION in GroupRole");
                    Logging.WriteException(e);
                }
            }

            return WoWPartyMember.GroupRole.None;
        }

        private static WoWPlayer _prevLeader = null;

        private void CheckGroupRoleAssignments()
        {
            // if not in an RAF Group then reset settings
            if (IsPVP() || !InGroup())
            {
                if (GroupTank != null || GroupHealer != null || _myGroupRole != WoWPartyMember.GroupRole.None)
                {
                    _prevLeader = null;
                    Slog("^EVENT:  Left Party/Raid ...");
                    GroupHealer = null;
                    _myGroupRole = WoWPartyMember.GroupRole.None;
                }

                return;
            }

            // initialize my role
            WoWPartyMember.GroupRole prevMyRole = _myGroupRole;
            _myGroupRole = GetMyGroupRole();

            // initialize healer
            WoWPlayer prevGroupHealer = GroupHealer;
            GroupHealer = (_myGroupRole == WoWPartyMember.GroupRole.Healer) ? _me : null;

            // now process rest of group
//            Dlog("");
//            Dlog("GROUP COMPOSITION");
//            Dlog("-----------------");
//            Dlog("{0} : {1}", _myGroupRole.ToString(), Safe_UnitName(_me));
                
            foreach (WoWPartyMember pm in GroupMemberInfos)
            {
                WoWPartyMember.GroupRole role = GetGroupRoleAssigned(pm);
//                Dlog( "{0} : {1}", role.ToString(), Safe_UnitName(pm.ToPlayer().ToUnit() ));

                if (role == WoWPartyMember.GroupRole.Healer && pm.Guid != _me.Guid)
                    GroupHealer = pm.ToPlayer();
#if COMMENT
                else if (role == GroupRole.Tank && (GroupTank == null || GroupTank.IsMe))
                {
                    Log("ShamWOW:  No tank set yet - setting to {0}", Safe_UnitID(p));
                    RaFHelper.SetLeader(p);
                }
#endif
            }

            if (prevMyRole != _myGroupRole || _prevLeader != GroupTank || prevGroupHealer != GroupHealer)
            {
                Slog("^EVENT:  Party/Raid Members Changed ...");
                Dlog("CheckGroupRoleAssignments:  my role changed={0}, tank changed={1}, healer changed={2}",
                    BoolToYN(prevMyRole != _myGroupRole),
                    BoolToYN(_prevLeader != GroupTank),
                    BoolToYN(prevGroupHealer != GroupHealer)
                    );

                if (_myGroupRole != WoWPartyMember.GroupRole.Tank)
                    Slog("RAF:  TANK = {0}", GroupTank == null ? "none currently" : Safe_UnitID(GroupTank));
                if (_myGroupRole != WoWPartyMember.GroupRole.Healer)
                    Slog("RAF:  HEALER = {0}", GroupHealer == null ? "none currently" : Safe_UnitID(GroupHealer));
                Slog("RAF:  {0} = {1}", _myGroupRole.ToString().ToUpper(), Safe_UnitName(_me));

                _prevLeader = GroupTank;
            }
        }


        private bool HaveValidTarget()
        {
            return _me.GotTarget && _me.CurrentTarget.IsAlive;
            //                && !Blacklist.Contains( t.Guid );
        }

        /*
         * CurrentTargetInMeleeDistance()
         * 
         * Check to see if CurrentTarget is within melee range.  This allows
         * recognizing when a pulled mob is close enough to melee as well as 
         * as when a pulled mob moves out of melee
         */
        static ulong guidLastMob;
        static double meleeRangeCheck;

        private static bool CurrentTargetInMeleeDistance()
        {
            if (!_me.GotTarget)
                return false;

            if (guidLastMob != _me.CurrentTargetGuid)
            {
                // set good defaults in case we hit an exception in mob check after
                guidLastMob = _me.CurrentTargetGuid;
                meleeRangeCheck = (int) _me.MeleeRange(_me.CurrentTarget);
                Dlog("CurrentTargetInMeleeDistance:  {0} @ {1:F3} has melee range of {2:F2} yds", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Distance, meleeRangeCheck);

                // check if npc is setup with special handling / behavior
                if (!_me.CurrentTarget.IsPlayer)
                {
                    Mob mob = (from m in _dictMob where m.Key == _me.CurrentTarget.Entry select m.Value).FirstOrDefault();
                    if (mob != null)
                    {
                        meleeRangeCheck = _me.CombatReach + mob.HitBox;
                        Dlog("CurrentTargetInMeleeDistance:  {0} has id=#{1} with HitBox override of {2:F1} yds", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry, meleeRangeCheck);
                    }
                }

            }

            return _me.CurrentTarget.Distance < meleeRangeCheck && _me.CurrentTarget.InLineOfSpellSight;
        }

        /*
         * CurrentTargetInRangedDistance()
         * 
         * Check to see if CurrentTarget is within ranged attack distance and line of sight.  This allows
         * recognizing when a pulled mob is close enough to melee as well as 
         * as when a pulled mob moves out of melee
         */
        private static bool CurrentTargetInRangedDistance()
        {
            return _me.IsUnitInRange(_me.CurrentTarget, _maxDistForRangeAttack);
        }

        private bool CurrentTargetInRangedPullDistance()
        {
            return _me.IsUnitInRange(_me.CurrentTarget, _offsetForRangedPull);
        }

/*
        private static bool _me.IsUnitInRange(WoWUnit unit, double range)
        {
            double combatDistance = _me.CombatDistance(_me);
            return (unit != null && combatDistance < range && unit.InLineOfSpellSight);
        }

        private static double _me.CombatDistance(WoWUnit unit)
        {
            return _me.CombatDistance(_me, unit);
        }

        private static double _me.CombatDistance(WoWUnit x, WoWUnit y)
        {
            double hitboxDistance = x.HitBoxRange(y);
            double combatDistance = x.Location.Distance(y.Location);
            combatDistance -= hitboxDistance;
            return combatDistance;
        }

        private static double CombatDistanceSqr(WoWUnit unit)
        {
            return CombatDistanceSqr(_me, unit);
        }

        private static double CombatDistanceSqr(WoWUnit x, WoWUnit y)
        {
            double hitboxDistance = x.HitBoxRange(y);
            double combatDistance = x.Location.DistanceSqr(y.Location);
            hitboxDistance *= hitboxDistance;
            combatDistance -= hitboxDistance;
            return combatDistance;
        }
*/

        /*
         * trys to determine if 'unit' points to a mob that is a Caster.  Currently
         * only see .Class as being able to help determine.  the big question marks
         * are Druids and Shamans for grinding purposes, so even though we try
         * to guess we still make routines able to adapt on pulls, etc. to fact
         * mob may not behave like we guessed
         */
        private static bool IsCaster(WoWUnit unit)
        {
            bool isCaster = false;

            switch (unit.Class)
            {
                case WoWClass.Mage :
                case WoWClass.Warlock :
                case WoWClass.Shaman :
                case WoWClass.Priest :
                    isCaster = true;
                    break;
            }

            // following test added because of "Unyielding Sorcerer" in Hellfire
            // .. having a class of Paladin, yet they fight as ranged casters

            if (!isCaster)
            {
                // LOCALIZATION ISSUES
                if (unit.Name.ToLower().Contains("sorcerer"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("shaman"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("mage"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("warlock"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("priest"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("wizard"))
                    isCaster = true;
                else if (unit.Name.ToLower().Contains("adept"))
                    isCaster = true;
            }

            return isCaster;
        }

        private static void AddToBlacklist(ulong guidMob)
        {
            AddToBlacklist(guidMob, System.TimeSpan.FromMinutes(5));
        }

        private static void AddToBlacklist(ulong guidMob, System.TimeSpan ts)
        {
            if (Blacklist.Contains(guidMob))
                Dlog("already blacklisted mob: " + guidMob);
            else
            {
                Blacklist.Add(guidMob, ts);
                Dlog("blacklisted mob: " + guidMob);
            }
        }

        /*
         * CheckForItem()
         * 
         * Lookup an item by its item # 
         * return null if not found
         */
        private static WoWItem CheckForItem(List<uint> listId)
        {
            WoWItem item = ObjectManager.GetObjectsOfType<WoWItem>(false).Find(unit => listId.Contains(unit.Entry));
            return item;
        }

        private static WoWItem CheckForItem(uint itemId)
        {
            WoWItem item = ObjectManager.GetObjectsOfType<WoWItem>(false).Find(unit => unit.Entry == itemId);
            return item;
        }

        private static WoWItem CheckForItem(string itemName)
        {
            WoWItem item;
            uint id;

            if (uint.TryParse(itemName, out id) && id > 0)
                item = ObjectManager.GetObjectsOfType<WoWItem>(false).Find(unit => unit.Entry == id);
            else
                item = ObjectManager.GetObjectsOfType<WoWItem>(false).Find(unit => 0 == string.Compare(unit.Name, itemName, true));

            return item;
        }



        private static bool _loadingScreen = false;

        public static bool IsGameUnstable()
        {
#if LOOP_WHILE_UNSTABLE
            for (; ; )
            {
                if (!Safe_IsValid(_me) )
                {
                    _loadingScreen = true;
                    Dlog("GameUnstable: HB or WOW initializing... not ready yet (Me == null)");
                    Sleep(1000);
                    continue;
                }

                if (!ObjectManager.IsInGame)
                {
                    _loadingScreen = true;
                    Dlog("GameUnstable: Detected Loading Screen... sleeping for 1 sec");
                    Sleep(1000);
                    continue;
                }

                if (_loadingScreen)
                {
                    _loadingScreen = false;
                    Dlog("GameUnstable: wait another 2 secs after Loading Screen goes away");
                    Sleep(2000);
                    continue;
                }

                if (IsPVP() && Battlegrounds.Finished)
                {
                    Dlog("GameUnstable: detected battlefield complete / scoreboard");
                    Sleep(2000);
                    continue;
                }

                // if we make it here, we are completed so can exit
                break;
            }
#else
            if (!ObjectManager.IsInGame)
            {
                throw new GameUnstableException("Not in game.");
            }

            if (!Safe_IsValid(_me))
            {
                throw new GameUnstableException("HB or WOW initializing... not ready yet (_me == null)");
            }

            if (IsPVP() && Battlegrounds.Finished)
            {
                throw new GameUnstableException("GameUnstable: detected battlefield complete/scoreboard");
            }
#endif

            return false;
        }

        public override void Pulse()
        {
            try
            {
                if (IsGameUnstable())
                    return;

                if (_me.OnTaxi)
                    return;

                // base.Pulse();    // does nothing
                TotemManagerUpdate();

                if (!_me.IsAlive)
                    return;

                if (!_me.Combat)
                    return;

                if (!_me.GotTarget && GotAliveMinion()) // && !IsPVP())
                {
                    Dlog("Pulse:  no current target, but live pet with target=", GotAliveMinion());
                    bool foundAggro = FindAggroTarget();
                    if (foundAggro)
                    {
                        Dlog("Pulse:  found aggro");
                        return;
                    }
                }

#if MOVE_IN_COMBAT_HANDLER
#else
                // HandleMovement();
#endif
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException)
            {
                Logging.WriteDebug("Pulse: game unstable, so passing control to HonorBuddy");
            }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in NeedRest");
                Logging.WriteException(e);
            }
        }

        private void HandleMovement()
        {
            if (IsMovementDisabled())
                return;

            if (_me.GotTarget && !_me.Rooted && !_me.Fleeing && !_me.Stunned ) // && Targeting.PullDistance >= _me.CurrentTarget.Distance )
            {
                if (!Safe_IsMoving() && IsCasting())
                {
                    if (!CanMoveWhileCasting())
                    {
                        Dlog("Pulse: casting, so avoiding movement");
                        return;
                    }

                    Dlog("Pulse: casting but have SpiritWalkers Grace or Unleashed Lightning");
                }

                if (typeShaman == ShamanType.Enhance)
                {
                    if (IsPVP())
                    {
                        if (_me.CurrentTarget.DistanceSqr > 4)
                            MoveToUnit(_me.CurrentTarget, 1);
                        // else if ( !_me.IsSafelyBehind(_me.CurrentTarget))
                        //     Navigator.MoveTo( WoWMathHelper.CalculatePointBehind(_me.CurrentTarget.Location, _me.CurrentTarget.Rotation, 1));
                    }
                    else if (IsRAF() && (!CurrentTargetInMeleeDistance() || _me.CurrentTarget.IsMoving))
                        MoveToUnit(_me.CurrentTarget, 1.5);
                    else if ((!CurrentTargetInMeleeDistance() || _me.CurrentTarget.IsMoving))
                        MoveToUnit(_me.CurrentTarget, 1.5);
                    else if (_me.Combat)
                        Safe_StopMoving(String.Format("Target {0:F1} yds away", _me.CurrentTarget.Distance));
                }
                else if (typeShaman == ShamanType.Elemental)
                {
                    if (!CurrentTargetInRangedDistance())
                        MoveToUnit(_me.CurrentTarget, 3.5);
                    else
                        Safe_StopMoving(String.Format("Target {0:F1} yds away", _me.CurrentTarget.Distance));
                }
                // no Pulse movement for Resto, let its handlers do movement

            }
        }

        public override bool NeedRest
        {
            get
            {
                bool doWeNeedRest = false;
                try
                {
                    // Dlog("NEEDREST? START: " + _me.HealthPercent + "% health,  " + _me.ManaPercent + "% mana");
                    doWeNeedRest = NeedRestLogic();
                    // Dlog("NEEDREST? RETURN STATUS= {0}", doWeNeedRest  );
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException)
                {
                    Logging.WriteDebug("NeedRest: game unstable, so passing control to HonorBuddy");
                    return false;
                }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("HB EXCEPTION in NeedRest");
                    Logging.WriteException(e);
                }

                if (_isBotBGBuddy)
                {
                    // this is a hack to fix problem where BGBuddy moves to next objective
                    //  ..  before UI reflects that a cast is underway
                    while (!IsGameUnstable() && _me.IsAlive && !_me.Combat && (GCD() || HandleCurrentSpellCast()))
                    {
                        Dlog("NeedRest: waiting for GCD/Cast to complete");
                    }
                }

                return doWeNeedRest;
            }
        }

        private bool NeedRestLogic()
        {
            if (IsGameUnstable())
                return false;

            if (!_me.IsInInstance)
            {
                if (_me.IsFlying || _me.OnTaxi)
                    return false;

                if (_me.IsOnTransport && _isBotQuesting)
                    return false;
#if DISABLE_ON_TRANSPORT
                if (_me.IsOnTransport)
                    return false;
#endif
            }

            if (_isBotLazyRaider && IsPVP() && _me.Mounted)
                return false;

            // following allows returning immediately when a plug-in or other
            // .. has casted a spell and returned to HB.  this improves the
            // .. bobber recognition of AutoAngler specifically
            if (HandleCurrentSpellCast())
            {
                Dlog("NeedRest:  aborted since casting");
                return false;
            }

            if (InterruptEnemyCast())
                return false;

            if (IsHealer())
            {
                if (NeedRestLogicResto())   // don't do anything else if we have to heal
                    return false;
            }                               // .. otherwise fall through to NeedRest tests

            ReportBodyCount();

            // don't allow NeedRest=true if we just started a pull
            if (!_pullTimer.Done)
                return false;

            if (Battlegrounds.Finished)
                return false;

            // if we switched modes ( grouped, battleground, or spec chg)
            if (DidWeSwitchModes())
            {
                Slog("^OPERATIONAL MODE CHANGED:  initializing...");
                Initialize();
                return true;
            }

            if (_me.Combat)
            {
                Dlog("NeedRest:  i am in Combat.... calling Combat() from NeedRest");
                Combat();
                return false;
            }

            if (IsRAFandTANK() && GroupTank.Combat)
            {
                Dlog("NeedRest:  RAF Leader in Combat... calling Combat from here");
                Combat();
                return false;
            }

#if FAST_PVP_TARGETING
            if (!IsHealerOnly())
            {
                if (IsPVP() && !_me.Mounted)
                {
                    if (_me.GotTarget && _me.CurrentTarget.IsPlayer && Safe_IsHostile(_me.CurrentTarget) && _me.CurrentTarget.Distance < _maxDistForRangeAttack && !_me.CurrentTarget.Mounted)
                    {
                        Slog("BGCHK:  calling Combat() myself from NeedRest for CurrentTarget");
                        Combat();
                        return false;
                    }

                    if (FindBestTarget())
                    {
                        Dlog("BGCHK: calling Combat() myself from NeedRest for FindBestTarget()");
                        PullInitialize();
                        Combat();
                        return false;
                    }
                }
            }
#endif

            // check to be sure not in a travelling state before
            //.. setting switches that will cause a dismount or form change
            if (_me.Mounted || InGhostwolfForm())
            {
                // Dlog("Mounted or Ghostwolf - will wait to buff/enchant out of form");
            }
            else
            {
                if (IsRAF() && (_me.Combat || (GroupTank != null && GroupTank.Combat)))
                    ;   // suppress recall totems
                else if (IsPVP() && (TotemExist(TotemId.EARTH_ELEMENTAL_TOTEM) || TotemExist(TotemId.FIRE_ELEMENTAL_TOTEM)))
                    ;   // suppress recall totems
                else if (TotemsWereSet && cfg.RemoveOutOfRangeTotems && CheckForSafeDistance("Totem Recall", _ptTotems, CheckDistanceForRecall()))
                {
                    Dlog("Need rest: TotemsWereSet and recall CheckForSafeDistance({0:F1})= true", CheckDistanceForRecall());
                    if (!_me.GotTarget)
                        Dlog("Need rest: true, recall totems: no current target so looks good to recall");
                    else
                    {
                        WoWUnit unit = _me.CurrentTarget;
                        Dlog("Need rest: true, recall totems: target:{0} atkable:{1} hostile:{2}, profile:{3} alive:{4}", unit.Distance,
                             unit.Attackable,
                             Safe_IsHostile(unit),
                             Safe_IsProfileMob(unit),
                             unit.IsAlive);
                    }
                    _RecallTotems = true;
                    return true;
                }

                if (IsWeaponImbueNeeded())
                {
                    Dlog("Need rest: true, IsWeaponImbueNeeded mh:{0} oh:{1}", _needMainhandImbue, _needOffhandImbue);
                    return true;
                }

                if (IsShieldBuffNeeded(true))
                {
                    Dlog("Need rest: true, ShieldBuffNeeded -- Mounted={0}, Flying={1}", _me.Mounted, _me.IsFlying);
                    return true;
                }

                if (IsCleanseNeeded(_me) != null)
                {
                    Dlog("Need rest: true, IsCleanseNeeded");
                    _castCleanse = true;
                    return true;
                }

                if (IsWaterBreathingNeeded())
                {
                    Dlog("Need rest: true, IsWaterBreathingNeeded");
                    _castWaterBreathing = true;
                    return true;
                }

                if ((_me.HealthPercent <= cfg.RestHealthPercent) || (IsPVP() && IsSelfHealNeeded()))
                {
                    Dlog("Need rest: true, CurrentHealth {0:F1}% less than RestHealthPercent {1:F1}%", _me.HealthPercent, cfg.RestHealthPercent);
                    return true;
                }

                if (_me.ManaPercent <= cfg.RestManaPercent && !_me.IsSwimming)
                {
                    Dlog("Need rest: true, CurrentMana {0:F1}% less than RestManaPercent {1:F1}%", _me.ManaPercent, cfg.RestManaPercent);
                    return true;
                }

                if (IsWeaponImbueNeeded())
                {
                    Dlog("Need rest: true, Need to Imbued Weapons");
                    return true;
                }

                if (_needTotemBarSetup)
                {
                    Dlog("Need rest: true, Need to Setup Totem Bar flag is set");
                    return true;
                }

                UseFlaskIfAvailable();
            }

            // ONLY set the _rezTarget after heals and mana taken care 
            //  ..  so ready in case attacked while rezzing
            _rezTarget = null;
            if (IsRAF() && SpellManager.HasSpell("Ancestral Spirit"))
            {
                // !p.IsAFKFlagged 
                _rezTarget = (from p in GroupMembers where Safe_IsValid(p) && p.Dead && !Blacklist.Contains(p) select p).FirstOrDefault();
                if (_rezTarget != null)
                {
                    Dlog("NeedRestLogic:  dead party member {0} @ {1:F1} yds", Safe_UnitName(_rezTarget), _me.CombatDistance(_rezTarget));
                    return true;
                }
            }

#if SUPPRESS_GHOST_WOLF
            if (CharacterSettings.Instance.UseMount && cfg.UseGhostWolfForm)
            {
                Wlog("warning:  UseMount takes precedence over UseGhostWolf:  the Ghost Wolf setting ignored this session");
            }
            else 
#endif
            if (NeedToBuffRaid())
            {
                Dlog("NeedRestLogic: raid members need buff");
                return true;
            }

#if HB_DIDNT_BREAK_LOGIC_THAT_HAS_BEEN_IN_PLACE_FOR_YEARS
            if (!_me.Mounted && !_me.IsFlying && !_me.OnTaxi && !IsRAF() && cfg.UseGhostWolfForm && !IsSpellBlacklisted("Ghost Wolf"))
            {
                if (!_isBotArchaeologyBuddy && !_isBotGatherBuddy)
                {
                    if (cfg.UseGhostWolfForm && SpellManager.HasSpell("Ghost Wolf") && !InGhostwolfForm())
                    {
                        if (!_needTravelForm && CheckForSafeDistance("Ghost Wolf", _me.Location, GetGhostWolfDistance()))
                        {
                            if (Styx.Logic.POI.BotPoi.Current == null
                                || Styx.Logic.POI.BotPoi.Current.Type == Styx.Logic.POI.PoiType.None
                                || Styx.Logic.POI.BotPoi.Current.Location.Distance(_me.Location) >= GetGhostWolfDistance())
                            {
                                _needTravelForm = SpellManager.CanCast("Ghost Wolf");  // make sure we can so not stuck in loop
                                Dlog("Need rest: {0}, Ghost Wolf: {1} cast Ghost Wolf now and enemies atleast {2} yds away", _needTravelForm, _needTravelForm ? "able to" : "cannot", GetGhostWolfDistance());
                                if (_needTravelForm)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
#endif

            return false;
        }

        // bool inRest = false;
        public override void Rest()
        {
            try
            {
                ShowStatus("REST");
                // ShowStatus("Enter REST");
                RestLogic();
                // ShowStatus("Exit REST");
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException)
            {
                Logging.WriteDebug("Rest: game unstable, so passing control to HonorBuddy");
                return;
            }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug(">>> EXCEPTION: occurred in Rest()");
                Logging.WriteException(e);
            }

            if (_isBotBGBuddy)
            {
                // this is a hack to fix problem where BGBuddy moves to next objective
                //  ..  before UI reflects that a cast is underway
                while (!IsGameUnstable() && _me.IsAlive && !_me.Combat && (GCD() || HandleCurrentSpellCast()))
                {
                    Dlog("Rest: waiting for GCD to pass");
                }
            }
        }

        public void RestLogic()
        {
            bool haveRestAura = _me.IsAuraPresent( "Food");
            haveRestAura = haveRestAura || _me.IsAuraPresent( "Drink");
            haveRestAura = haveRestAura || _me.IsAuraPresent( "Nourishment");

            if (haveRestAura)
            {
                Dlog("RestLogic:  eating/drinking initiated outside of CC");
            }
            else
            {
                if (_castCleanse && CleanseIfNeeded(_me))
                {
                    _castCleanse = false;
                    return;
                }

                if (_RecallTotems)
                {
                    _RecallTotems = false;
                    RecallTotemsForMana();
                    return;
                }

                if (_castWaterBreathing )
                {
                    if (HandleWaterBreathing())
                    {
                        _castWaterBreathing = false;
                        return;
                    }
                }

                // try to use bandages, but only if we don't need Mana (if we need to drink, might as well eat too)
                if (_me.HealthPercent < cfg.RestHealthPercent && _me.ManaPercent > cfg.RestManaPercent)
                {
                    if (cfg.UseBandages)
                    {
                        if (UseBandageIfAvailable())
                            return;
                    }
                }

                // try to heal several times (quicker than eating).  also heal to higher OOC level in PVP
                if (_me.HealthPercent < GetSelfHealThreshhold() && (_me.ManaPercent >= cfg.RestManaPercent || IsPVP()))
                {
                    while (!IsGameUnstable() && _me.IsAlive && _me.HealthPercent < GetSelfHealThreshhold())
                    {
                        if (_me.Combat || _me.IsImmobilized())
                            return;

                        WaitForCurrentCastOrGCD();
                        if (!HealMyself(GetSelfHealThreshhold()))
                        {
                            Dlog("RestLogic: could not cast a heal on myself in first loop");
                            break;  // exit loop if we can't cast a heal for some reason
                        }
                    }

                    // already need to drink now, so may as well top-off health first
                    while (!IsGameUnstable() && _me.IsAlive && _me.ManaPercent < cfg.RestManaPercent && _me.HealthPercent < 85)
                    {
                        if (_me.Combat || _me.IsImmobilized())
                            return;

                        WaitForCurrentCastOrGCD();
                        if (!HealMyself(GetSelfHealThreshhold()))
                        {
                            Dlog("RestLogic: could not cast a heal on myself in second loop");
                            break;  // exit loop if we can't cast a heal for some reason
                        }
                    }
                }

                if (_needTotemBarSetup)
                {
                    _needTotemBarSetup = false;
                    TotemSetupBar();
                    Slog("");
                }

                // ressurrect target set in NeedRest
                //------------------------------------
                if (_rezTarget == null)
                    ;
                else if (!Safe_IsValid(_rezTarget))
                {
                    Dlog("Ressurection:  dead ressurection target is invalid, resetting");
                    _rezTarget = null;
                }
                else if (!_rezTarget.Dead)
                {
                    Dlog("Ressurection:  dead ressurection target {0} is no longer dead", Safe_UnitName(_rezTarget));
                    _rezTarget = null;
                }
                else if (!_me.IsUnitInRange(_rezTarget, 30))
                {
                    if (IsMovementDisabled())
                        Slog("Attention!  Move closer to {0} who is more than 30 yds away or not in line of sight", Safe_UnitName(_rezTarget));
                    else
                        MoveToUnit(_rezTarget);
                }
                else
                {
                    Safe_StopMoving(String.Format("Ressurection Target is {0:F1} yds away", _rezTarget.Distance));
                    Log("^Ressurection:  dead target {0} is {1:F1} yds away", Safe_UnitName(_rezTarget), _me.CombatDistance(_rezTarget));

#if NOTRIGHTNOW
				if ( !SpellManager.Cast("Ancestral Spirit", _rezTarget ))
				{
					Dlog("Rez:  spell cast failed?  abort");
					return;
				}
#else
                    if (!Safe_CastSpell(_rezTarget, "Ancestral Spirit"))
                        return;
#endif
                    SleepForLagDuration();
                    while (!IsGameUnstable() && _me.IsAlive && IsCastingOrGCD())
                    {
                        if (!Safe_IsValid(_rezTarget))
                        {
                            Dlog("Ressurection:  dead ressurrection target is invalid... Stop Casting...!!!");
                            SpellManager.StopCasting();
                        }
                        else if (_rezTarget.IsAlive)
                        {
                            Dlog("Ressurection:  {0} is alive... Stop Casting...!!!", Safe_UnitName(_rezTarget));
                            SpellManager.StopCasting();
                        }
                        SleepForLagDuration();
                    }

                    Log("^Ressurection:  attempt completed, blacklisting {0} for 30 seconds", Safe_UnitName(_rezTarget));
                    // blacklist so if they have a rez pending but haven't clicked yes,
                    //  ..  we move onto rezzing someone else
                    Blacklist.Add(_rezTarget, TimeSpan.FromSeconds(30));
                    _rezTarget = null;
                    return;
                }

                // Dlog("RestLogic:  before ShamanBuffs");
                //-- critical that this func leads to call resetting _needMainhandImbue AND _needOffhandImbue 
                ShamanBuffs(true);

#if HB_DIDNT_BREAK_LOGIC_THAT_HAS_BEEN_IN_PLACE_FOR_YEARS
                if (_needTravelForm)
                {
                    bool shouldWeMount = false;
                    if (Styx.Logic.POI.BotPoi.Current == null)
                    {
                        shouldWeMount = CheckForSafeDistance("Mount", _me.Location, Mount.MountDistance);
                        Dlog("RestLogic:  POI=null, mounting as {0} {1:F1} yds",
                            shouldWeMount ? "Mount because no enemies" : "Ghost Wolf because enemies within",
                            Mount.MountDistance);
                    }
                    else if (Styx.Logic.POI.BotPoi.Current.Type == Styx.Logic.POI.PoiType.None)
                    {
                        shouldWeMount = CheckForSafeDistance("Mount", _me.Location, Mount.MountDistance);
                        Dlog("RestLogic:  POI=none {0:F1}, mounting as {1} {2:F1} yds",
                            _me.Location.Distance(Styx.Logic.POI.BotPoi.Current.Location),
                            shouldWeMount ? "Mount because no enemies" : "Ghost Wolf because enemies within",
                            Mount.MountDistance);
                    }
                    else
                    {
                        shouldWeMount = _me.Location.Distance(Styx.Logic.POI.BotPoi.Current.Location) >= Mount.MountDistance;
                        Dlog("RestLogic:  mounting as {0} since POI {1:F1} is {2:F1} yds away",
                            shouldWeMount ? "Mount" : "Ghost Wolf",
                            Styx.Logic.POI.BotPoi.Current.Type.ToString(),
                            _me.Location.Distance(Styx.Logic.POI.BotPoi.Current.Location));
                    }

                    if (Safe_IsMoving() && (shouldWeMount || !_hasTalentAncestralSwiftness))
                    {
                        Safe_StopMoving(String.Format("because {0}", shouldWeMount ? "casting Mount" : "Ghost Wolf is not instant"));
                    }

                    if (shouldWeMount && !Mount.CanMount())
                    {
                        Dlog("RestLogic:  check if we can cast mount failed, trying Ghost Wolf");
                        shouldWeMount = false;
                    }

                    if (shouldWeMount && CharacterSettings.Instance.UseMount && MountHelper.NumMounts > 0 && Mount.CanMount())
                    {
                        if (Styx.Logic.POI.BotPoi.Current == null)
                        {
                            Dlog("RestLogic:  attempting to cast mount with vague location now");
                            Mount.MountUp();
                        }
                        else
                        {
                            Dlog("RestLogic:  attempting to cast mount with POI location now");
                            Mount.MountUp(); 
                        }

                        SpellHelper.HandleSuccessfullSpellCast(500);
                    }
                    else
                    {
                        Dlog("RestLogic:  attempting to casting ghost wolf now");
                        GhostWolf();
                    }

                    _needTravelForm = false;
                }
#endif

                bool noFood = false;
                bool noDrink = false;
                bool stoppedToEat = false;
                bool stoppedToDrink = false;
                bool castManaSpring = false;

                // now eat/drink if needed
                if ((_me.HealthPercent < cfg.RestHealthPercent || _me.ManaPercent < cfg.RestManaPercent) && !_me.IsSwimming)
                {
                    stoppedToEat = _me.HealthPercent < cfg.RestHealthPercent;
                    stoppedToDrink = _me.ManaPercent < cfg.RestManaPercent;

                    Safe_StopMoving("to eat or drink");

                    WaitForCurrentCastOrGCD();
                    if (stoppedToEat)
                    {
                        noFood = !UseConsumeable(CharacterSettings.Instance.FoodName);
                        if (noFood)
                            Log(Color.Red, "No [{0}] food left, staying here waiting for health to regen to {1}%", CharacterSettings.Instance.FoodName, cfg.RestHealthPercent);
                        else
                        {
                            _countFood++;
                            Dlog("Eating:  {0} total used, average {1:F0} per hour",
                                _countFood,
                                (60.0 * 60.0 * 1000.0 * _countFood) / timeStart.ElapsedMilliseconds
                                );
                        }
                    }

                    if (stoppedToDrink)
                    {
                        // confirm the drink isn't the same as the food
                        if (stoppedToEat && 0 == String.Compare(CharacterSettings.Instance.FoodName, CharacterSettings.Instance.DrinkName, true))
                        {
                            Dlog("RestLogic:  already stopped to eat and food({0}) and drink({1}) are the same", CharacterSettings.Instance.FoodName, CharacterSettings.Instance.DrinkName);
                        }
                        else
                        {
                            noDrink = !UseConsumeable(CharacterSettings.Instance.DrinkName);
                            if (noDrink)
                            {
                                Log(Color.Red, "No [{0}] drinks left, waiting for mana to regen to {1}%", CharacterSettings.Instance.DrinkName, cfg.RestManaPercent);
                                if (HasTotemSpell(TotemId.MANA_SPRING_TOTEM))
                                {
                                    castManaSpring = TotemCast(TotemId.MANA_SPRING_TOTEM);
                                }
                            }
                            else
                            {
                                _countDrinks++;
                                Dlog("Drinking:  {0} total used, average {1:F0} per hour",
                                    _countDrinks,
                                    (60.0 * 60.0 * 1000.0 * _countDrinks) / timeStart.ElapsedMilliseconds
                                    );
                            }
                        }
                    }

                    if (noFood == false && noDrink == false)
                    {
                        Slog("Stopped to {0}{1}{2}",
                             stoppedToEat ? "Eat" : "",
                             stoppedToEat && stoppedToDrink ? " and " : "",
                             stoppedToDrink ? "Drink" : ""
                            );
                    }
                }

                // wait until food/drink buffs to display

                Countdown waitForAuras = new Countdown(750);
                Dlog("RestLogic:  waiting {0} ms for buffs to appear", waitForAuras.Remaining);
                while (!IsGameUnstable())
                {
                    bool foodAura = _me.IsAuraPresent( "Food");
                    bool drinkAura = _me.IsAuraPresent( "Drink");
                    bool nourAura = _me.IsAuraPresent( "Nourishment");
                    bool manaAura = ( castManaSpring && TotemExist(TotemId.MANA_SPRING_TOTEM));

                    haveRestAura = foodAura || drinkAura || nourAura || manaAura;
                    if (haveRestAura)
                    {
                        Dlog("RestLogic:  found a rest buffs -- food({0}) drink({1}) nourishment({2}) manaspring({3})", foodAura, drinkAura, nourAura, manaAura);
                        break;
                    }

                    if (!_me.IsAlive)
                        break;

                    if (waitForAuras.Done)
                        break;

                    if (stoppedToEat && !noFood)
                        continue;

                    if (stoppedToDrink && !noDrink)
                        continue;

                    break;
                }
            }


            // wait until topped off OR Food/Drink/Nourishment buffs have expired
            while (haveRestAura && !IsGameUnstable())
            {
                bool foodWait = _me.IsAuraPresent( "Food") && _me.HealthPercent < 99;
                bool drinkWait = _me.IsAuraPresent( "Drink") && _me.ManaPercent < 99;
                bool nourWait = _me.IsAuraPresent( "Nourishment") && ( _me.HealthPercent < 99 || _me.ManaPercent < 99);
                bool manaWait = TotemExist(TotemId.MANA_SPRING_TOTEM) && _me.ManaPercent < cfg.RestManaPercent;
                bool keepWaiting = foodWait || drinkWait || nourWait || manaWait;

                if (!keepWaiting)
                {
                    Dlog("RestLogic:  eat/drink aura expired or we are topped off at H:{0:F1}% M:{1:F1}%", _me.HealthPercent, _me.ManaPercent );
                    break;
                }

                Dlog("dbg waiting:  Eating:{0} Health:{1:F0}%  /  Drinking:{2} Mana:{3:F0}%  /  Nourishment:{4}  /  Mana Spring:{5}", 
                    _me.IsAuraPresent( "Food"), _me.HealthPercent, 
                    _me.IsAuraPresent( "Drink"), _me.ManaPercent, 
                    _me.IsAuraPresent( "Nourishment"), 
                    TotemExist(TotemId.MANA_SPRING_TOTEM ));

                if ( ShouldWeStopDrinkingToFight())
                {
                    Dlog("RestLogic:  cancelling eat/drink keepwaiting={0}", keepWaiting);
                    break;
                }

                Sleep(100);
            }

            if (!IsMovementDisabled() )
            {
                if (IsAuraPresentOnMeLUA("Herbouflage"))
                {
                    Slog("Cancelling Herbouflage...");
                    RunLUA("CancelUnitBuff(\"player\", \"Herbouflage\")");
                }

                if (_me.IsAuraPresent( "Food"))
                {
                    Slog("Cancelling Food...");
                    RunLUA("CancelUnitBuff(\"player\", \"Food\")");
                }

                if (_me.IsAuraPresent( "Drink"))
                {
                    Slog("Cancelling Drink...");
                    RunLUA("CancelUnitBuff(\"player\", \"Drink\")");
                }
            }

/*
            if ( NeedToBuffRaid() )
            {
                BuffRaid();
            }
 */
        }

        private static bool ShouldWeStopDrinkingToFight()
        {
            if (_me.Combat && !InGroup())
            {
                Log("Cancelling eat/drink because solo and in Combat");
                return true;
            }

            if (!_me.IsAlive)
            {
                Log("Cancelling eat/drink because I died");
                return true;
            }

            if (IsHealer() && InGroup())
            {
                int cnt = GroupMembers.Count();
                int threshhold = 70;
                if (cnt > 5)
                    threshhold = 60;
                if (cnt > 10)
                    threshhold = 50;

                WoWUnit lowPlayer = lowPlayer =
                            HealMembersAndPets
                                .Where(u => u.Combat && CheckValidAndNearby(u, 80) && u.HealthPercent <= threshhold && !IsHealUnitToIgnore(u))
                                .OrderBy(u => u.HealthPercent)
                                .FirstOrDefault(u => !IsMovementDisabled() || u.InLineOfSpellSight);

                if (lowPlayer != null)
                {
                    Dlog("ShouldWeStopDrinkingToFight: group size is {0}, health % threshhold {1}", cnt, threshhold);
                    Log("Cancelling eat/drink because {0} at {0:F1}% and in combat", Safe_UnitName(lowPlayer), lowPlayer.HealthPercent );
                    return true;
                }
            }

            return false;
        }

        public static int GetGhostWolfDistance()
        {
            int dist = cfg.DistanceForGhostWolf;
            if (typeShaman != ShamanType.Enhance && dist < (Targeting.PullDistance + 5))
            {
                Wlog("Ranged Shaman:  Increasing Ghost Wolf Distance from {0} to {1} (Pull Distance + 5)", dist, (int)Targeting.PullDistance + 5);
                dist = (int)Targeting.PullDistance + 5;
            }

            return dist;
        }

        public static bool InGhostwolfForm()
        {
            return _me.Auras.ContainsKey("Ghost Wolf");
        }

        private static bool GhostWolf()
        {
            bool b = false;

            if (!cfg.UseGhostWolfForm)
                ;
            else if (!SpellManager.HasSpell("Ghost Wolf"))
                ;
            else if (IsSpellBlacklisted("Ghost Wolf"))
                ;
            else
            {
                if (!_hasTalentAncestralSwiftness)
                {
                    Safe_StopMoving("since ghost wolf not instant");
                }

                b = Safe_CastSpell(_me, "Ghost Wolf");
                if (!_hasTalentAncestralSwiftness)
                {
                    WaitForCurrentCastOrGCD();
                }
            }

            return b;
        }

        public static bool WaterWalking()
        {
            const int WATER_WALKING = 546;
            if ( SpellManager.HasSpell( WATER_WALKING ))
            {
                if ( !_me.IsAuraPresent(  WATER_WALKING))
                {
                    WoWSpell spell = WoWSpell.FromId( WATER_WALKING);
                    if ( spell.CanCast )
                    {
                        return Safe_CastSpell( _me, spell );
                    }
                }
            }

            return false;
        }


        public void HandleFalling() { }



        /*
		 * Note:  the following are interface functions that need to be implemented by the class.  
		 * They are not used presently in the ShamWOW implementation.  Buffs are handled within the 
		 * flow of the current Pull() and Combat() event handlers
		 */
        public override bool NeedPreCombatBuffs 
        { 
            get 
            { 
                bool need = NeedToBuffRaid();
                if (need)
                    Slog("NeedPreCombatBuffs:  need to buff raid");
                return need;
            } 
        }

        public override void PreCombatBuff() 
        {
            try
            {
                BuffRaid();
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteException(e);
            }
        }

        public override bool NeedPullBuffs { get { return false; } }
        public override void PullBuff() { }

        public override bool NeedCombatBuffs { get { return false; } }
        public override void CombatBuff() { }


        public bool ShamanBuffs(bool atRest)
        {
            bool castSpell = false; 

            if (!_me.IsAlive)
                return castSpell;
            
            // Shield Twisting:  Cast based upon amount of Mana available
            castSpell = ShieldTwisting(atRest);

            Dlog("ShamanBuffs:  AllowNonHealSpells:{0}, atrest:{1}", AllowNonHealSpells(), atRest);
            //            if (AllowNonHealSpells() && atRest != false)
            if (!castSpell && (_needMainhandImbue || _needOffhandImbue))
            {
                castSpell = ImbueWeapons();
            }

            return castSpell;
        }


        private List<ulong> RaidBuffTargets
        {
            get
            {
                if (!GroupMembers.Contains(_me))
                    GroupMembers.Add(_me);

                return (from p in GroupMembers
                        where _me.CombatDistance(p) < 27
                 where !Blacklist.Contains(p.Guid)
                    && ((_hasGlyphOfWaterWalking && !p.IsAuraPresent("Water Walking") && cfg.PVP_PrepWaterWalking )
                        || (_hasGlyphOfWaterBreathing && !p.IsAuraPresent("Water Breathing") && cfg.PVP_PrepWaterBreathing ))
                 orderby p.Distance ascending
                 select p.Guid 
                 ).ToList();
            }
        }

        private bool NeedToBuffRaid()
        {
            if (IsGameUnstable())
                return false; 

            if (!IsPVP() || _me.CurrentHealth <= 1 || GroupMemberInfos.Count() > 15)
                return false;

            if (!cfg.PVP_PrepWaterBreathing && !cfg.PVP_PrepWaterWalking)
                return false;

            if (!IsAuraPresentOnMeLUA("Preparation") && !IsAuraPresentOnMeLUA("Arena Preparation"))
                return false;

            // only include these AFTER the check for Preparation buffs
            //  ..  these here only due to not getting NeedRest calls from
            //  ..  BG Bot in start area

            if (IsShieldBuffNeeded(true))
                return true;

            if (IsWeaponImbueNeeded())
                return true;

            if (FindSoulwellNearby() != null && CheckForItem(ITEM_HEALTHSTONE) == null)
                return true;

            return RaidBuffTargets.Any();
        }

        private void BuffRaid()
        {
            if (_me.CurrentHealth <= 1)
                return;

            ShamanBuffs(true);

            HandleSoulwell();

            foreach (ulong guid in RaidBuffTargets)
            {
                if (IsGameUnstable())
                    return;

                if (!IsAuraPresentOnMeLUA("Preparation") && !IsAuraPresentOnMeLUA("Arena Preparation"))
                {
                    Slog("Stopping Buffs... start area Preparation buff faded");
                    break;
                }

                WoWPlayer p = ObjectManager.GetObjectByGuid<WoWPlayer>(guid);
                if (p != null)
                {
                    if (cfg.PVP_PrepWaterWalking && _hasGlyphOfWaterWalking && !p.IsAuraPresent( "Water Walking"))
                    {
                        Safe_CastSpell(p, "Water Walking");
                        WaitForCurrentCastOrGCD();
                    }

                    if (cfg.PVP_PrepWaterBreathing && _hasGlyphOfWaterBreathing && !p.IsAuraPresent( "Water Breathing"))
                    {
                        Safe_CastSpell(p, "Water Breathing");
                        WaitForCurrentCastOrGCD();
                    }

                    int duration = (Environment.TickCount & 1) == 0 ? 55 : 75;
                    AddToBlacklist( guid, TimeSpan.FromSeconds( duration ));
                }
            }
        }

        const int SOULWELL = 181621;
        public WoWGameObject FindSoulwellNearby()
        {
            WoWGameObject soulwell =
                (from o in ObjectManager.ObjectList
                 where o is WoWGameObject && o.Distance <= 30 && o.Entry == SOULWELL
                 select o.ToGameObject()).FirstOrDefault();
            return soulwell;
        }

        public bool HandleSoulwell( )
        {
            if (CheckForItem(ITEM_HEALTHSTONE) != null)
            {
                return false;
            }

            if (Blacklist.Contains(SOULWELL))
            {
                return false;
            }

            WoWGameObject soulwell = FindSoulwellNearby();
            if (soulwell == null)
            {
                return false;
            }

            if (!soulwell.InLineOfSight || soulwell.Distance > soulwell.InteractRange)
            {
                MoveToObject(soulwell, soulwell.InteractRange - 1);
                return true;
            }

            Slog("Picking up Healthstone");
            soulwell.Interact();
            SleepForLagDuration();
            AddToBlacklist(SOULWELL, TimeSpan.FromMilliseconds(5000));
            return true;
        }

        private static bool AchieveCompleted(int id)
        {
            // id, name, points, completed, month, day, year, description, flags, icon, rewardText, isGuildAch = GetAchievementInfo(category, index) or GetAchievementInfo(id)
            List<string> result = CallLUA( String.Format( "return GetAchievementInfo({0})", id));
            if ( result != null && result.Count() >= 4)
            {
                bool completed = false; 
                if ( Boolean.TryParse( result[3], out completed ))
                {
                    return completed;
                }
            }

            return false;
        }

        /*
		 * NeedHeal
		 * 
		 * return a true/false indicating whether the Heal() event handler should be called by the
		 * HonorBuddy engine.
		 */
        public override bool NeedHeal
        {
            get
            {
                bool isHealNeeded = false;
                try
                {
                    if (!_me.IsInInstance)
                    {
                        if (_me.IsFlying || _me.OnTaxi)
                            return false;
#if DISABLE_ON_TRANSPORT
                        if (_me.IsOnTransport)
                            return false;
#endif
                    }

                    if (IsPVP() && typeShaman == ShamanType.Enhance && _me.Combat)
                    {
                        if (!MaelstromCheckHealPriority())
                            HealMySelfInstantOnly();

                        return false;
                    }

                    isHealNeeded = !_me.Combat && IsSelfHealNeeded();
                    if (isHealNeeded)
                        ShowStatus("NeedHeal=YES!!!");
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteException(e);
                }

                return isHealNeeded;
            }
        }

        private bool IsSelfHealNeeded()
        {
            int threshhold = GetSelfHealThreshhold();

            if (_me.HealthPercent <= threshhold
                && countEnemy == 1
                && !IsFightStressful()
                && AllowNonHealSpells()
                && _me.GotTarget
                && _me.CurrentTarget.HealthPercent < 10.0
                && _me.CurrentTarget.IsAlive
                )
            {
                Log("^Enemy weak at {0:F0}%, skipping heal", _me.CurrentTarget.HealthPercent);
                return false;
            }

            return !MeSilenced() && _me.HealthPercent <= threshhold && SpellManager.HasSpell(HEALING_WAVE);
        }

        /*
         * Heal()
         * 
         * Called if a heal is needed.
         */
        public override void Heal()
        {
            ShowStatus("HEAL");
            // ShowStatus("Enter HEAL");
            HealMyself();
            // ShowStatus("Exit HEAL");
        }

        private int GetSelfHealThreshhold()
        {
            int threshhold;
            // for RAF, count on healer to heal and only self-heal in emergency
            if (IsRAF())
                threshhold = GroupHealer != null && GroupHealer.CurrentHealth > 1 ? cfg.EmergencyHealthPercent : cfg.NeedHealHealthPercent;
            // for Battlegounds, use NeedHeal in Combat, but top-off when Resting
            else if (IsPVP())
                threshhold = _me.Combat ? cfg.NeedHealHealthPercent : 85;
            // for Grinding/Questing  
            else if (!_me.Combat)
                threshhold = cfg.RestHealthPercent;
            else
                threshhold = cfg.NeedHealHealthPercent + (IsFightStressful() ? 10 : 0);

            return threshhold;
        }

        private bool AllowNonHealSpells()
        {
            return _me.ManaPercent > cfg.EmergencyManaPercent && _me.HealthPercent > cfg.EmergencyHealthPercent;
        }


        /*
         * IsCleanseNeeded()
         * 
         * Called cleanse if needed.
         */
        public WoWAura IsCleanseNeeded(WoWUnit unit)
        {
            const int SHAMANISTIC_RAGE = 30823;
            const int CLEANSE_SPIRIT = 51886;

            // if we don't have any or cleansing disabled, exit quickly
            if (MeSilenced() || !unit.Debuffs.Any() || (IsHealer() && !cfg.GroupHeal.Cleanse))
                return null;
            
            bool knowCleanseSpirit = SpellManager.CanCast(CLEANSE_SPIRIT);
            bool canCleanMagic = (knowCleanseSpirit && _hasTalentImprovedCleanseSpirit)
                                || (_hasGlyphOfShamanisticRage && SpellManager.CanCast(SHAMANISTIC_RAGE) && SpellHelper.OnCooldown(SHAMANISTIC_RAGE));
            bool canCleanCurse = knowCleanseSpirit;
            bool canStoneform = SpellManager.CanCast("Stoneform") && unit.IsMe;

            bool isBlacklisted = (from dbf in unit.Debuffs
                                  where _hashCleanseBlacklist.Contains(dbf.Value.SpellId)
                                  select dbf.Value
                                 ).Any();

            if (isBlacklisted)
                return null;

            WoWAura dispelDebuff = (
                                       from dbf in unit.Debuffs
                                       where
                                           ((dbf.Value.Spell.DispelType == WoWDispelType.Curse && canCleanCurse)
                                               || (dbf.Value.Spell.DispelType == WoWDispelType.Magic && canCleanMagic)
                                               || (dbf.Value.Spell.DispelType == WoWDispelType.Magic && unit.IsMe && _hasGlyphOfShamanisticRage)
                                               || (dbf.Value.Spell.DispelType == WoWDispelType.Poison && unit.IsMe && canStoneform)
                                               || (dbf.Value.Spell.DispelType == WoWDispelType.Disease && unit.IsMe && canStoneform))


                                       select dbf.Value
                                   ).FirstOrDefault();

            return dispelDebuff;
        }

        public bool CleanseIfNeeded(WoWUnit unit)
        {
            return CleanseIfNeeded(unit, null);
        }

        public bool CleanseIfNeeded(WoWUnit unit, WoWAura dispelDebuff)
        {
            // if we don't have any, exit quickly
            if (unit == null || !unit.Debuffs.Any())
                return false;

            bool castSpell = false;

            if (dispelDebuff == null)
                dispelDebuff = IsCleanseNeeded(unit);

            if (dispelDebuff != null)
            {
                Log("^Dispel target {0}[{1}] at {2:F1} yds has debuf '{3}' with {4} secs remaining", Safe_UnitName(unit), unit.Level, unit.Distance, dispelDebuff.Name, dispelDebuff.TimeLeft.Seconds);
                if (dispelDebuff.Spell.DispelType == WoWDispelType.Poison || dispelDebuff.Spell.DispelType == WoWDispelType.Disease)
                    castSpell = Safe_CastSpell(unit, "Stoneform");
                else if (unit.IsMe && _hasGlyphOfShamanisticRage && dispelDebuff.Spell.DispelType == WoWDispelType.Magic)
                    castSpell = Safe_CastSpell(unit, "Shamanistic Rage");
                else if (_hasTalentImprovedCleanseSpirit || dispelDebuff.Spell.DispelType == WoWDispelType.Curse)
                    castSpell = Safe_CastSpell(unit, "Cleanse Spirit");
            }

            return castSpell;
        }


        /*
		 * Pull()
		 * 
		 * Currently always do a ranged pull from '_offsetForRangedPull' way
		 * If HB has given us a mob to pull that is further away, we will
		 * run towards him up to within '_offsetForRangedPull' 
		 * 
		 */

        public override void Pull()
        {
            if (IsGameUnstable())
                return;

            if (HandleCurrentSpellCast())
            {
                Dlog("Pull:  aborted since casting");
                return;
            }

            if (_me.GotTarget && _me.CurrentTarget.Aggro)
            {
                Dlog("Pull:  have target with aggro, so calling Combat");
                Combat();
                return;
            }

            ShowStatus("PULL");
            // ShowStatus("Enter PULL");
            PullLogic();
            // ShowStatus("Exit PULL");
        }

        public void PullInitialize()
        {
            _pullTargGuid = _me.CurrentTarget.Guid;
            // _pullTargHasBeenInMelee = false;
            _pullAttackCount = 0;
            // _pullStart = _me.Location;
            _pullTimer.Remaining = ConfigValues.PullTimeout;

            bool isRaidBehavior = IsRaidBehavior();
            if (!isRaidBehavior && IsTrainingDummy(_me.CurrentTarget))
            {
                // show warning
                Dlog("PullInitialize:  User fighint Training Dummy but using Solo / Grinding behaviors");
            }

            if ( isRaidBehavior != _pullIsRaidBehavior )
            {
                _pullIsRaidBehavior = isRaidBehavior;
                Dlog("");
                Log("Switching to Raid Behaviors");
                Dlog("");
                Initialize();
            }
        }

        public static bool IsTrainingDummy(WoWUnit unit)
        {
            bool isDummy = (unit != null) && unit.Attackable && unit.CanSelect && unit.IsNeutral && unit.IsMechanical && _me.IsResting;
            if (isDummy)
                Dlog("IsTrainingDummy:  true for {0} #{1}", Safe_UnitName(unit), unit.Entry);

            return isDummy;
        }

        private bool IsRaidBehavior()
        {
            if (cfg.UseRaidBehavior == ConfigValues.When.Auto)
            {
                if ( _me.GotTarget && IsTrainingDummy( _me.CurrentTarget ))
                    return true;
                return IsRAF() && (_me.IsInRaid || (_isBotLazyRaider && _me.IsInInstance ));
            }

            return cfg.UseRaidBehavior == ConfigValues.When.Always;
        }

        public void PullLogic()
        {
            if (!_me.IsInInstance)
            {
                if (_me.IsFlying || _me.OnTaxi)
                    return;
#if DISABLE_ON_TRANSPORT
                if (_me.IsOnTransport)
                    return;
#endif
            }

            if (_isBotLazyRaider && IsPVP() && _me.Mounted)
                return ;

            if (CombatResto())
                return;

            // don't pull in these... just jump into Combat behavior
            if (IsPVP())
            {
                PullFast();
                return;
            }

            if (IsRAF())
            {
                Combat();
                return;
            }

            if (!_me.GotTarget)
            {
                Dlog("HB gave (null) pull target");
                return;
            }

            if (_me.CurrentTarget.IsPet)
            {
                WoWUnit petOwner = _me.CurrentTarget.CreatedByUnit;
                if (petOwner != null)
                {
                    Dlog("Changing target from pet {0} to owner {1}", Safe_UnitName(_me.CurrentTarget), Safe_UnitName(petOwner));
                    Safe_SetCurrentTarget(petOwner);
                }
                else
                {
                    Dlog("Appears that pet {0} does not have an owner?  guess we'll fight a pet", Safe_UnitName(_me.CurrentTarget));
                }
            }

            if (!_me.CurrentTarget.IsAlive)
            {
                Dlog("HB gave a Dead pull target: " + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "]");
                Safe_SetCurrentTarget(null);
                return;
            }

            if (_checkIfTagged && _me.CurrentTarget.TaggedByOther && !IsTargetingMeOrMyGroup(_me.CurrentTarget))
            {
                if (!_me.CurrentTarget.Aggro && !_me.CurrentTarget.PetAggro )
                {
                    WoWUnit targetTarget = _me.CurrentTarget.CurrentTarget;
                    if (targetTarget != null && targetTarget.IsPlayer)
                    {
                        Slog("Pull Target is tagged by another player -- let them have it");
                        Safe_SetCurrentTarget(null);
                        return;
                    }
                }
            }

            if (Blacklist.Contains(_me.CurrentTargetGuid))
            {
                Slog("Skipping pull of blacklisted mob: " + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "]");
                Safe_SetCurrentTarget(null);
                return;
            }

            if (IsPVP())
            {
                if (_me.GotTarget && Safe_IsEnemyPlayer(_me.CurrentTarget) && _me.CurrentTarget.Mounted)
                {
                    Slog("Skipping mounted player: " + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "]");
                    Blacklist.Add(_me.CurrentTarget.Guid, System.TimeSpan.FromSeconds(2));
                    Safe_SetCurrentTarget(null);
                    return;
                }
            }

            CheckForAdds();

            // reset state values we use to determine what point we are at in 
            //  .. in transition from Pull() to Combat()
            //---------------------------------------------------------------------------
            if (_pullTargGuid != _me.CurrentTarget.Guid)
            {
                PullInitialize();
                Slog(">>> PULL: " + (_me.CurrentTarget.Elite ? "[ELITE] " : "") + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "] at " + _me.CurrentTarget.Distance.ToString("F1") + " yds");
                Dlog("pull started at {0:F1}% health, {1:F1}% mana", _me.HealthPercent, _me.ManaPercent);
            }

            if (IsPVP() || IsMovementDisabled())  // never timeout in PVP or when disable movement set
                ;
            else if (_pullTimer.Done && !(_me.CurrentTarget.TaggedByMe || IsTargetingMeOrMyGroup(_me.CurrentTarget)))
            {
                Blacklist.Add(_me.CurrentTarget.Guid, System.TimeSpan.FromSeconds(30));
                Slog("Pull TIMED OUT for: " + _me.CurrentTarget.Class + "-" + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "] after " + _pullTimer.ElapsedMilliseconds + " ms -- blacklisted for 30 secs");
                Safe_SetCurrentTarget(null);
                return;
            }

            if (!_me.CurrentTarget.IsPlayer)
            {
                double distCheck = _me.CurrentTarget.Distance;
                WoWUnit closerAggroTarget = (from unit in AllEnemyMobs
                                             where unit.Aggro
                                                 && unit.Guid != _me.CurrentTargetGuid
                                                 && !unit.IsPet
                                                 && unit.Distance < distCheck 
                                                 && !Blacklist.Contains(unit.Guid)
                                             orderby unit.CurrentHealth ascending
                                             select unit).FirstOrDefault();
                if (closerAggroTarget != null && _me.CombatDistanceSqr(closerAggroTarget) < _me.CombatDistanceSqr(_me.CurrentTarget))
                {
                    Safe_StopMoving("aggro on different target");
                    Slog(Color.Orange, "^Switch from {0} @ {1:F1} yds to aggro {2} @ {3:F1} yds",
                        Safe_UnitName(_me.CurrentTarget),
                        _me.CurrentTarget.Distance,
                        Safe_UnitName(closerAggroTarget),
                        closerAggroTarget.Distance);
                    Safe_SetCurrentTarget(closerAggroTarget);
                }
            }

            if (!IsFacing( _me.CurrentTarget))
            {
                Safe_FaceTarget();
            }

            Safe_StopFace();

            /*            if (typeShaman != ShamanType.Enhance)
                            PullRanged();
                        else 
            */
            if (typeShaman == ShamanType.Enhance && IsPVP())
                PullFast();
            else
            {
                switch (cfg.PVE_PullType)
                {
                    case ConfigValues.TypeOfPull.Auto:
                        PullAuto();
                        break;
                    case ConfigValues.TypeOfPull.Body:
                        PullBody();
                        break;
                    case ConfigValues.TypeOfPull.Fast:
                        PullFast();
                        break;
                    case ConfigValues.TypeOfPull.Ranged:
                        PullRanged();
                        break;
                }
            }


            if (!_me.GotTarget)
                Dlog("PullLogic: leaving with no current target");
            else
                Dlog("PullLogic: distance after pull: {0:F2}", _me.CurrentTarget.Distance);
            // CheckForPlayers();
        }


        public void PullAuto()
        {
            Dlog("PullType Auto");
            if (typeShaman != ShamanType.Enhance)
                PullRanged();
            else if (!IsCaster(_me.CurrentTarget) && _me.CurrentTarget.Level < (_me.Level - 2))
            {
                Dlog("PullAuto: {0} Level={1} is a melee", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Level );
                PullRanged();
            }
            else
                PullFast();
        }

        public void PullBody()
        {
            Dlog("PullType Body");
            if (!CurrentTargetInMeleeDistance())
                MoveToCurrentTarget();
            else if (!ShockOpener())
            {
                if (_me.GotTarget && !_me.IsAutoAttacking && _me.CurrentTarget.IsAlive && CurrentTargetInMeleeDistance() && !IsPVP())
                {
                    Dlog("** Auto-Attack started in PullBody");
                    AutoAttack();
                }
            }
        }

        public void PullFast()
        {
            bool castSpell = false;

            Dlog("PullType Fast");
            if (typeShaman != ShamanType.Enhance)
            {
                if (!CurrentTargetInRangedDistance())
                    MoveToCurrentTarget();
                else
                {
                    Safe_StopMoving(String.Format("in combat range at {0:F1} yds", _me.CurrentTarget.Distance));
                    castSpell = SetTotemsAsNeeded();    // set totems for non-enhance (they do in combat)
                }
            }
            else
            {
                if (!CurrentTargetInMeleeDistance())
                {
                    if (_hasTalentAncestralSwiftness && !_me.Mounted && cfg.UseGhostWolfForm && !InGhostwolfForm() && _me.CurrentTarget.Distance >= cfg.DistanceForGhostWolf)
                        GhostWolf();

                    MoveToCurrentTarget();
                }
                else
                {
                    Safe_StopMoving(String.Format("in combat range at {0:F1} yds", _me.CurrentTarget.Distance));
                }

                castSpell = MaelstromCheck();
            }

            if (!castSpell)
                castSpell = UnleashElements();

            if (!castSpell ) // && _me.CurrentTarget.Distance < 25)
                castSpell = ShockOpener();

            if (_me.CurrentTarget.Distance < 10 && _me.Mounted)
                Safe_Dismount();

            if (_me.GotTarget && !_me.IsAutoAttacking && _me.CurrentTarget.IsAlive && CurrentTargetInRangedDistance()) // && !IsPVP())
            {
                Dlog("** Auto-Attack started in PullFast");
                AutoAttack();
            }

            // mostly for Ele/Resto:  use LB stopped and haven't cast an instant yet
            if (!castSpell && typeShaman != ShamanType.Enhance && _me.GotTarget && (!Safe_IsMoving() || _hasGlyphOfUnleashedLightning))
            {
                Dlog("PullFast:  stopped and no cast yet, so using LB (others must be on CD)");
                castSpell = LightningBolt();
            }
        }


        public WoWUnit PullRangedAggroCheck()
        {
            WoWUnit add =
                (from o in ObjectManager.ObjectList
                 where o is WoWUnit
                 let unit = o.ToUnit()
                 where unit.Distance <= Targeting.PullDistance
                     && unit.Attackable
                     && unit.IsAlive
                     && unit.Combat
                     && !IsMeOrMyStuff(unit)
                     && IsTargetingMeOrMyStuff(unit)
                     && !Blacklist.Contains(unit.Guid)
                 orderby unit.Distance ascending
                 select unit
                ).FirstOrDefault();
            return add;
        }

        /// <summary>
        /// PullRanged() moves within ranged distance and attacks until tagged
        /// Tries to leverage following Lightning Bolt with an Instant attack
        /// to increase initial damage at range
        /// </summary>
        public void PullRanged()
        {
            Dlog("PullType Ranged");
            while (!IsGameUnstable() && !_pullTimer.Done && _me.IsAlive && _me.GotTarget && _me.CurrentTarget.IsAlive && !_me.CurrentTarget.Aggro)
            {
                if (_me.CurrentTarget.TaggedByOther && !IsTargetingMeOrMyGroup(_me.CurrentTarget))
                {
                    if (!_me.CurrentTarget.Aggro && !_me.CurrentTarget.PetAggro )
                    {
                        WoWUnit targetTarget = _me.CurrentTarget.CurrentTarget;
                        if (targetTarget != null && targetTarget.IsPlayer)
                        {
                            Slog("Ranged Pull Target is tagged by another player -- let them have it");
                            AddToBlacklist(_me.CurrentTargetGuid);
                            Safe_SetCurrentTarget(null);
                            return;
                        }
                    }
                }

                if (0 == _pullAttackCount && _me.Combat)
                {
                    WoWUnit aggro = PullRangedAggroCheck();
                    if (aggro != null)
                    {
                        Slog(Color.Orange, "Cancel pull to fight aggro {0}", Safe_UnitName(aggro));
                        Safe_StopMoving( "halt movement to fight aggro mobs");
                        Safe_SetCurrentTarget(aggro);
                        return;
                    }
                }

                if (!CurrentTargetInRangedPullDistance())
                {
                    MoveToCurrentTarget();
                    continue;
                }

                if (Safe_IsMoving())
                {
                    Safe_StopMoving("stopping at ranged attack distance");
                }

                if (!IsFacing( _me.CurrentTarget))
                {
                    Safe_FaceTarget();
                }

                Safe_StopFace();

                WaitForCurrentCastOrGCD();

                // set totems now for all specs except Enhancement (who does in Combat)
                if (typeShaman != ShamanType.Enhance && !IsPVP() && SetTotemsAsNeeded())
                {
                    continue;
                }

                // use LB if first attempt and don't already have aggro or close
                if (FarmingAsElemental())
                    _pullAttackCount++;
                else if (_pullAttackCount == 0 && (_me.CurrentTarget.Distance > 10 || !_me.CurrentTarget.Aggro) && LightningBolt())
                    _pullAttackCount++;
                else if (IsWeaponImbuedWithDPS() && UnleashElements())
                    _pullAttackCount++;
                else if (_me.CurrentTarget.Distance < 25 && ShockOpener())
                    _pullAttackCount++;
                else if (LightningBolt())
                    _pullAttackCount++;

                if (_me.GotTarget && _me.CurrentTarget.IsPlayer)
                    break;
            }
        }



        /*
		 * Combat()
		 * 
		 */
        public override void Combat()
        {
            try
            {
                if (IsGameUnstable())
                    return;

                if (!_me.IsAlive || (_me.GotTarget && !_me.CurrentTarget.IsAlive))
                {
                    ReportBodyCount();
                    return;
                }

                if (!_me.IsInInstance)
                {
                    if (_me.IsFlying || _me.OnTaxi)
                        return;
#if DISABLE_ON_TRANSPORT
                    if (_me.IsOnTransport)
                        return;
#endif
                }

                if (IsCasting() )
                {
                    if (AllowCastToFinish())
                    {
                        return;
                    }
                }

                ShowStatus("COMBAT");
                // ShowStatus("Enter COMBAT");
                CombatLogicDelegate();
                // ShowStatus("Exit COMBAT");

#if PREVIOUS_DISPATCHER
                if (IsPVP())
                {
                    if (typeShaman == ShamanType.Enhance)
                    {
                        ShowStatus("Enter COMBAT");
                        CombatLogicEnhancePVP();
                        ShowStatus("Exit  COMBAT");
                        return;
                    }
 
                    /*
                                        if (typeShaman == ShamanType.Elemental )
                                        {
                                            CombatLogicElementalPVP();
                                            return;
                                        }
                     */
                }
#endif
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException)
            {
                Logging.WriteDebug("Combat: game unstable, so passing control to HonorBuddy");
                return;
            }
            catch (Exception e)
            {
                if (!_me.GotTarget)
                    Logging.WriteDebug("Exception:  target expired, out of range, or left group -- not a big deal");
                else
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("EXCEPTION in Combat() - HonorBuddy API or CC Error");
                }

                Logging.WriteException(e);
            }


#if COLLECT_NEW_PURGEABLES
            PurgeableSpellCollector();
#endif

            if (_isBotBGBuddy)
            {
                // this is a hack to fix problem where BGBuddy moves to next objective
                //  ..  before UI reflects that a cast is underway
                while (!IsGameUnstable() && _me.IsAlive && !_me.Combat && (GCD() || HandleCurrentSpellCast()))
                {
                    Dlog("Combat: waiting for GCD to pass");
                }
            }

        }

        private void CombatLogic()
        {
            if (UseTrinkets())
            {
                return;
            }

            WoWUnit raidTarget = null;
            if (IsPVP())
                raidTarget = FindRaidIconTarget(cfg.PVP_HexIcon);
            else if (IsRAF())
                raidTarget = FindRaidIconTarget(cfg.RAF_HexIcon);

            if (Hex(raidTarget))
                return;

            // if we dispel someone, done for now
            if (priorityCleanse == ConfigValues.SpellPriority.High && DispelRaid())
                return;

            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
                return;

            if (CombatResto())
                return;

            if (MeImmobilized())
            {
                if (typeShaman == ShamanType.Elemental && _me.Stunned && _count10YardEnemy > 0 && !_hasGlyphOfThunderstorm )
                {
                    Dlog("CombatLogic: {0} enemies within 10 yards while stunned", _count10YardEnemy );
                    Thunderstorm();
                }

                // use our Tremor if we have
                SetTotemsAsNeeded();
                return;
            }

            if (typeShaman == ShamanType.Enhance && IsSnared(_me) && _me.GotAlivePet && WolvesSpiritWalk())
            {
                return;
            }

            if ( IsFleeing(_me) && SpellManager.HasSpell((int)TotemId.TREMOR_TOTEM) && !TotemExist(TotemId.TREMOR_TOTEM))
            {
                if (SetTotemsAsNeeded())
                    return;
            }

            if (!combatChecks())
                return;

            // targeting a friendly -- which we never need to do
            // ... since BG Bot sometimes insists on this we'll just wait it out
            if (IsPVP() && _me.GotTarget && Safe_IsFriendly(_me.CurrentTarget))
            {
                // Dlog("CombatLogic:  targeting a friendly player, so just wait for another target to come");
                return;
            }

            CheckForAdds();

            if (!combatChecks())
                return;

            if ( _checkIfTagged && _me.CurrentTarget.TaggedByOther && !_me.CurrentTarget.TaggedByMe && !IsTargetingMeOrMyGroup(_me.CurrentTarget))
            {
                if (!_me.CurrentTarget.Aggro && !_me.CurrentTarget.PetAggro)
                {
                    WoWUnit targetTarget = _me.CurrentTarget.CurrentTarget;
                    if (targetTarget != null && targetTarget.IsPlayer )
                    {
                        Slog("Combat Target is tagged by another player -- let them have it");
                        Safe_SetCurrentTarget(null);
                        return;
                    }
                }
            }

            // check if we agroed a mob unintentionally
            if (_pullTargGuid != _me.CurrentTarget.Guid)
            {
                Safe_StopMoving("combat target changed");
                Slog(">>> ADD: " + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "] at " + _me.CurrentTarget.Distance.ToString("F1") + " yds");
                PullInitialize();
            }

            // if (!WoWMovement.IsFacing)
            if (!_me.IsAutoAttacking && _me.GotTarget && _me.CurrentTarget.IsAlive && CurrentTargetInRangedDistance())
            {
                Dlog("** Auto-Attack started in Combat");
                AutoAttack();
            }

            if (HealMySelfInstantOnly())
                return;

            // ok, reevaluate if still need healing in hopes we can attack
            if (IsSelfHealNeeded())
            {
                ShowStatus("COMBAT-HEAL=YES!!!!");
                HealMyself();
                // return;
            }

            InterruptEnemyCast();

            if (!pvpChecks())
            {
                if (!FindBestTarget())
                    return;
            }

            Safe_FaceTarget();             // hate to spam this, but appears .IsFacing=true doesn't mean that don't need to call .Face

#if false
            WoWSpell spellPending = _me.CurrentPendingCursorSpell;
            if (spellPending != null)
            {
                switch (spellPending.Id)
                {
                    case 73920:     // healing rain
                    case 77478:     // earthquake
                    case 61882:     // earthquake
                        break;
                    default:
                        if (!IsCastingOrGCD())
                        {
                            Dlog("@@@ CombatLogic: SPELL PENDING {0} #{1}", spellPending.Name, spellPending.Id);
                        }
                        break;
                }
            }
#endif

            if (typeShaman == ShamanType.Unknown)
                CombatUndefined();
            else if (typeShaman == ShamanType.Enhance)
                CombatMelee();
            else if (IsPVP())
                CombatElementalPVP();
            else
                CombatElemental();

            ShamanBuffs(false);
        }


        private readonly Stopwatch _timerCombatStats = new Stopwatch();
        
        /*
		 * CombatUndefined()
		 * 
		 * Rotation used prior to learning any talents.  Characteristics at this level are
		 * hi mana and health regen, with Lightning Bolt still having the best damage to mana
		 * ratio.  However, due to high regen will use instant attacks which aren't as 
		 * efficient but allow higher dps.
		 */
        private void CombatUndefined()
        {
            if (!_me.GotTarget )
            {
                return;
            }

#if MOVE_IN_COMBAT_HANDLER
            if (!CurrentTargetInRangedDistance() && _me.Rooted)
            {
                // options -- trinket, heal (anticipating being hit)
                if (!FindBestTarget())
                {
                    Slog("Rooted:  no target in range so waiting it out...");
                    return;
                }
            }

            if (!CurrentTargetInRangedDistance())
            {
                // chase until in ranged distance or for 8 seconds
                Dlog("running to mob " + _me.CurrentTarget.Distance.ToString("F2") + " away");
                MoveToCurrentTarget();
                BestInstantAttack();
                return;
            }

            if (Safe_IsMoving())
            {
                string sReason;
                if (!_me.GotTarget)
                    sReason = "no target?  stopping to get my bearings";
                else
                    sReason = String.Format("target {0:F1} yds away, stopping now...", _me.CurrentTarget.Distance);
                Safe_StopMoving(sReason);
            }
#else
            HandleMovement();
#endif
            FaceToUnit(_me.CurrentTarget);

            if (!combatChecks())
                return;

            // #1 Set Strength of Earth Totem if no buff
            if (!_me.HasAura("Strength of Earth") && SetTotemsAsNeeded())
                return;

            // #2 Cast Racial dps enhance abilitiy
            if (CastCombatSpecials())
                return;

            // #3 Instant (Ranged): Earth Shock
            if (EarthShock())
                return;

            // #4 Instant (Melee):  Primal Strike
            if (PrimalStrike())
                return;

            // #5 Lightning Bolt filler
            if (combatChecks())
                LightningBolt();
        }


        /*
         * CombatMelee()
         */
        private void CombatMelee()
        {
            if (!_me.GotTarget)
                return;

            HandleMovement();

            if (!CurrentTargetInMeleeDistance())
            {
                if (!_me.Rooted)
                {
                    if (!_me.CurrentTarget.IsPlayer)
                    {
                        WoWUnit closerAggroTarget = (from unit in AllEnemyMobs
                                             where unit.Aggro
                                                 && unit.Guid != _me.CurrentTargetGuid
                                                 && !unit.IsPet 
                                                 && _me.CombatDistanceSqr(unit) < (STD_MELEE_RANGE * STD_MELEE_RANGE)
                                                 && !Blacklist.Contains(unit.Guid)
                                             orderby unit.CurrentHealth ascending
                                             select unit).FirstOrDefault();
                        if (closerAggroTarget != null && _me.CombatDistanceSqr(closerAggroTarget) < _me.CombatDistanceSqr(_me.CurrentTarget))
                        {
                            Safe_StopMoving("aggro on different target");
                            Slog(Color.Orange, "^Switch from {0} @ {1:F1} yds to aggro {2} @ {3:F1} yds", 
                                Safe_UnitName(_me.CurrentTarget),
                                _me.CurrentTarget.Distance,
                                Safe_UnitName(closerAggroTarget),
                                closerAggroTarget.Distance);
                            Safe_SetCurrentTarget(closerAggroTarget);
                            return;
                        }
                    }
                }
                else
                {
                    Slog("Rooted...");
                    if (!FindBestMeleeTarget())      // check for weakest targets only first
                    {
                        if (!FindBestTarget())      // settle for weakest target within range
                        {
                            Dlog("CombatMelee:  rooted and no target in range so waiting it out...");
                            return;
                        }
                    }

                    if (IsCastingOrGCD())
                        return;

                    if ( FeralSpiritCheck())
                    {
                        return;
                    }
                }

                if (IsCastingOrGCD())
                    return;

                if (BestInstantAttack())
                    return;

                if (ChainLightning())
                    return;

                if (LightningBolt())
                    return;

                return;
            }

            if (IsFleeing(_me))
            {
                if (IsCastingOrGCD())
                    return;

                Slog("^FEAR: casting ");
                SetTotemsAsNeeded();
            }

#if MOVE_IN_COMBAT_HANDLER
            if (Safe_IsMoving())
            {
                if (IsMovementDisabled())
                {
                    Dlog("User is moving - waiting to cast");
                    return;
                }

                string sReason;
                if (!_me.GotTarget)
                    sReason = "moving and I don't have a target???";
                else
                    sReason = String.Format("target {0:F1} yds away, stopping now...", _me.CurrentTarget.Distance);

                Safe_StopMoving(sReason);
            }
#else
            HandleMovement();
#endif
            if (!combatChecks())
                return;

            // strafe away slightly from target if we are really close
            if (!IsMovementDisabled() && _me.GotTarget && _me.CurrentTarget.Distance < ConfigValues.TargetTooCloseDistance && !_me.CurrentTarget.IsPlayer)
            {
                double distFromMob = ConfigValues.TargetTooCloseDistance + ConfigValues.TargetTooCloseAdjust;
                WoWMovement.MovementDirection way = (Environment.TickCount & 1) == 0 ? WoWMovement.MovementDirection.StrafeLeft : WoWMovement.MovementDirection.StrafeRight;
                Dlog("Close to target @ {0:F2} yds - {1} small distance away!!!", _me.CurrentTarget.Distance, way);

                Safe_StopFace();

                Dlog("CombatMelee:  strafing {0} for max of 333 ms", way.ToString());
                WoWMovement.Move(way);
                Countdown stopStrafe = new Countdown(333);
                while (!IsGameUnstable() && _me.IsAlive && _me.GotTarget && _me.CurrentTarget.Distance < distFromMob && !stopStrafe.Done)
                {
                    Sleep(25);   // give a small timeslice back
                }

                WoWPoint lastPos = new WoWPoint();
                do
                {
                    WoWMovement.MoveStop(way);
                    Sleep(50);
                    if (_me.Location.Distance2D(lastPos) == 0)
                        break;
                    lastPos = _me.Location;
                } while (!IsGameUnstable() && _me.IsAlive);

                WoWMovement.MoveStop();
                Safe_FaceTarget();
                Dlog("Adjusted distance is {0:F2} yds", _me.CurrentTarget.Distance);
            }

            WaitForCurrentCastOrGCD();

            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
            {
                return;
            }

            // use Shamanistic Rage when our mana is low and mob we are fighting has 
            // .. a lot of health left.  Since it gives mana back for 
            if (_me.ManaPercent < cfg.ShamanisticRagePercent)
            {
                if (IsFightStressful() || (countEnemy > 1) || (_me.GotTarget && _me.CurrentTarget.HealthPercent >= 75))
                {
                    if (ShamanisticRage())
                    {
                        return;
                    }
                }
            }

            if (IsWeaponImbueNeeded())
            {
                ImbueWeapons();
                return;
            }

            // for Enhancement:  make first set of totems high priority
            if (!TotemsWereSet && combatChecks() && AllowNonHealSpells() && SetTotemsAsNeeded())
                return;

            if (CallForReinforcements())
                return;

            if (CastCombatSpecials())
                return;

            if (!IsRAF() && MaelstromCheck())
                return;

            if (CombatMeleeAOE())
                return;

            if (Stormstrike())
                return;

            if (LavaLash())
                return;

            if (MaelstromCheck())
                return;

            if (UnleashFlameCheck())
                return;

            if (UnleashElements())
                return;

            if (FireNova())
                return;

            if ((_me.Level < 81 || !SpellManager.HasSpell("Unleash Elements")) && FlameShock())
                return;

            if (EarthShock())
                return;

            if (FeralSpiritCheck())
                return;

            if (PrimalStrike())
                return;

            if (CleanseIfNeeded(_me))
                return;

            // now check to see if any totems still needed
            if (combatChecks() && AllowNonHealSpells())
            {
                if (countEnemy <= 1 && !IsFightStressful() && _me.CurrentTarget.HealthPercent < 20 && _me.HealthPercent > 40)
                {
                    if (SetTotemsAsNeeded())
                    {
                        return;
                    }
                }
            }

            if (HandleWaterBreathing())
            {
                Dlog("CombatMelee:  trying not to drown");
                return;
            }
        }

        /*
         * CombatMelee()
         */
        private bool CombatMeleeAOE()
        {
            if (_count10YardEnemy < 3 && _countFireNovaEnemy < 5)
            {
                Dlog("CombatMeleeAOE:  not enough aoe mobs, skipping");
                return false;
            }

            if (!SpellManager.HasSpell("Flame Shock"))
                return false;

            Dlog("CombatMeleeAOE:  see {0} in 10yds and {1} in firenova range", _count10YardEnemy, _countFireNovaEnemy);

            // FS on current
            WoWAura fs = _me.CurrentTarget.GetAuraCreatedByMe("Flame Shock");
            bool hasFlameShock = (fs != null && fs.TimeLeft.TotalMilliseconds > 3000);
            if (!hasFlameShock)
            {
                if (IsWeaponImbuedWithDPS() && UnleashElements())
                    return true;

                return FlameShock();
            }

            // FS on aoe mob
            WoWSpell spell = SpellManager.Spells["Flame Shock"];
            if (!SpellHelper.OnCooldown(spell))
            {
                int EnemyCastDistance = (int)spell.MaxRange - 2;
                WoWUnit target = (from unit in AllEnemyOrNeutralMobs 
                                  where unit.Distance <= EnemyCastDistance
                                      && !unit.IsPet
                                      && unit.HealthPercent > 1
                                      && !Blacklist.Contains(unit)
                                      && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                                      && null == _me.CurrentTarget.GetAuraCreatedByMe("Flame Shock")
                                      && unit.InLineOfSpellSight
                                      && FaceToUnit(unit)
                                  orderby unit.Distance descending
                                  select unit
                        ).FirstOrDefault();

                if (target != null)
                {
                    if (Safe_CastSpell(target, spell))
                    {
                        return true;
                    }
                }
            }

            if (hasFlameShock && LavaLash())
                return true;

            if (hasFlameShock && Safe_CanCastSpell(_me.CurrentTarget, "Fire Nova"))
            {
                if (Safe_CastSpell(_me.CurrentTarget, "Fire Nova"))
                {
                    return true;
                }
            }

            if (MaelstromCheck())
                return true;

            if (_countAoe12Enemy < 5)
            {
                if (!TotemExist(TOTEM_FIRE) && TotemCast(TotemId.SEARING_TOTEM))
                    return true;
            }
            else
            {
                if (!TotemExist(TotemId.MAGMA_TOTEM) && TotemCast(TotemId.MAGMA_TOTEM))
                    return true;

                if (ChainLightning())
                    return true;
            }

            return true;
        }

        /*
         * CombatMelee()
         */
        private bool CombatMeleeRaidAOE()
        {
            if (Safe_IsBoss(_me.CurrentTarget))
            {
                Dlog("CombatMeleeRaidAOE:  no AoE rotation when targeting boss");
                return false;
            }

            if (!SpellManager.HasSpell("Flame Shock"))
            {
                return false;
            }

            WoWSpell flameShock = SpellManager.Spells["Flame Shock"];
            int countMobs = AllEnemyOrNeutralMobs.Count( unit => unit.Distance < flameShock.MaxRange
                                                                && IsTargetingMeOrMyGroup(unit));
                                
            Dlog("CombatMeleeRaidAOE:  see {0} mobs hitting group within {1:F1}", countMobs, flameShock.MaxRange  );
            if (countMobs <= 1)
            {
                return false;
            }

            // FS on current
            uint timeFlameShock = _me.CurrentTarget.GetAuraTimeLeft("Flame Shock");
            bool hasFlameShock = timeFlameShock > 3000;

            if (!hasFlameShock && _me.CurrentTarget.HealthPercent > 5)
            {
                if ( IsWeaponImbuedWithDPS() && UnleashElements())
                    return true;

                return FlameShock();
            }

            // FS on aoe mob
            if (!SpellHelper.OnCooldown(flameShock))
            {
                WoWUnit target = (from unit in AllEnemyOrNeutralMobs
                                  where unit.Distance <= flameShock.MaxRange 
                                  where !unit.IsPet
                                      && unit.HealthPercent > 5
                                      && !Blacklist.Contains(unit)
                                      && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                                      && unit.GetAuraTimeLeft("Flame Shock") < 3000
                                      && unit.InLineOfSpellSight
                                      && IsFacing( unit)
                                  orderby unit.HealthPercent descending 
                                  select unit
                        ).FirstOrDefault();

                if (target != null)
                {
                    if (Safe_CastSpell(target, flameShock))
                    {
                        return true;
                    }
                }
            }

            const int LAVA_LASH = 0;
            WoWSpell lavaLash = WoWSpell.FromId( LAVA_LASH);
            if ( !SpellHelper.OnCooldown( LAVA_LASH ))
            {
                WoWUnit target = (from unit in AllEnemyOrNeutralMobs
                                  where !unit.IsPet
                                      && unit.IsWithinMeleeRange 
                                      && !Blacklist.Contains(unit)
                                      && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                                      && unit.GetAuraTimeLeft("Flame Shock") > 500
                                      && IsFacing( unit)
                                  orderby unit.HealthPercent descending 
                                  select unit
                        ).FirstOrDefault();

                if ( target != null && Safe_CastSpell( target, LAVA_LASH))
                {
                        return true;
                }
            }

            if (AllEnemyOrNeutralMobs.Where(u => u.DistanceSqr < 1600 && u.IsAuraPresent("Flame Shock")).Any() && SpellManager.HasSpell("Fire Nova") && Safe_CanCastSpell(_me.CurrentTarget, "Fire Nova"))
            {
                if (Safe_CastSpell(_me.CurrentTarget, "Fire Nova"))
                {
                    return true;
                }
            }

            if ( GetMaelstromCount() >= 7 )
            {
                WoWSpell chainLightning = SpellManager.Spells["Chain Lightning"];
                WoWUnit target = (from unit in AllEnemyOrNeutralMobs
                                  where !unit.IsPet
                                      && _me.IsUnitInRange( unit, chainLightning.MaxRange)
                                      && !Blacklist.Contains(unit)
                                      && WillChainLightningHop( unit )
                                      && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                                      && IsFacing( unit)
                                  orderby unit.HealthPercent descending 
                                  select unit
                        ).FirstOrDefault();

                return true;
            }

            if (_countAoe12Enemy < 5)
            {
                if (!TotemExist(TOTEM_FIRE))
                {
                    Dlog("CombatMeleeAoE:  less than 5 mobs and no fire totem, so setting Searing");
                    if (TotemCast(TotemId.SEARING_TOTEM))
                    {
                        return true;
                    }
                }
            }
            else 
            {
                if (!TotemExist( TotemId.MAGMA_TOTEM) && !TotemExist( TotemId.FIRE_ELEMENTAL_TOTEM ))
                {
                    Dlog("CombatMeleeAoE: {0} mobs so setting Magma Totem", _countAoe12Enemy);
                    if ( TotemCast(TotemId.MAGMA_TOTEM))
                        return true;
                }

                Dlog("CombatMeleeAoE: all important aoe spells on cd, so chain lightning used", _countAoe12Enemy);
                if (ChainLightning())
                    return true;
            }

            return true;
        }

        /*
         * CombatMelee()
         */
        private void CombatMeleeRaid()
        {
#if SLOW
            if (HandleBreakingCrowdControl())
            {
                Dlog("CombatMeleeRaid: handled cc break is true this pass");
                return;
            }
#endif

            #region Get Moving!!!! OR Stop if you should!!!
#if MOVE_IN_COMBAT_HANDLER
            if (!_me.GotTarget)
            {
                if (Safe_IsMoving())
                    Safe_StopMoving( "no current target");

                return;
            }

            if (_me.Rooted)
            {
                Slog("Rooted...");
                if (FeralSpiritCheck())
                    return;

                if (FindBestMeleeTarget())      // check for weakest targets only first
                    ;
                else if (FindBestTarget())      // settle for weakest target within range
                {
                    CombatElemental();
                    return;
                }
                else
                {
                    Dlog("CombatMeleeRaid:  rooted and no target in range so waiting it out...");
                    return;
                }
            }
            else 
            {
                if (!Safe_IsMoving()) //  || _me.CurrentTarget.IsMoving)
                {
                    if (IsCasting())
                    {
                        if (!CanMoveWhileCasting())
                        {
                            Dlog("CombatMeleeRaid: casting, so avoiding movement");
                            return;
                        }

                        Dlog("CombatMeleeRaid: casting but have SpiritWalkers Grace or Unleashed Lightning");
                    }
                }

                if (_me.CurrentTarget.Distance > 2.5 || (_me.CurrentTarget.IsMoving) || _me.CurrentTarget.IsSafelyBehind(_me))
                {
                    MoveToUnit(_me.CurrentTarget, 0.5);
                }
                else // if (_me.CurrentTarget.Distance < 1.5 || _me.CurrentTarget.IsSafelyBehind( _me))
                {
                    Safe_StopMoving(String.Format("Target {0:F1} yds away", _me.CurrentTarget.Distance));
                }
            }
#else
            HandleMovement();
#endif
            if (!IsFacing( _me.CurrentTarget))
            {
                FaceToUnit(_me.CurrentTarget);
                Dlog("CombatLogicEnhcPVP: trying to face current target");
                return;
            }

            if (IsCasting())
            {
                Dlog("CombatMeleeRaid: cast in progress");
                return;
            }

            if (GCD())
            {
                Dlog("CombatMeleeRaid: gcd in progress");
                return;
            }

            #endregion

#if SLOW
            WoWAura aura = GetCrowdControlledAura(_me);
            if (IsSilencedAura(aura))
            {
                Slog(Color.Orange, "{0} due to {1} -- unable to cast", aura.Spell.Mechanic.ToString(), aura.Name);
                return;
            }
#endif
            Dlog("CombatMeleeRaid: okay to cast");
#if SLOW
            if (HealMySelfInstantOnly())
            {
                Dlog("CombatLogicEnhcPVP: looks like we insta-healed, done");
                return;
            }

            if (!combatChecks())
                return;
#endif
            if (IsWeaponImbueNeeded())
            {
                ImbueWeapons();
                return;
            }

#if SLOW
            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
                return;
#endif
            // use Shamanistic Rage when our mana is low and mob we are fighting has 
            // .. a lot of health left.  Since it gives mana back for 
            if (_me.ManaPercent < cfg.ShamanisticRagePercent && ShamanisticRage())
                return;

            if (SetTotemsAsNeeded())
                return;
#if SLOW
            if (CallForReinforcements())
                return;

            CastCombatSpecials();
#endif
            UseItem(trinkCombat);

            if (Safe_CastSpell(_me.CurrentTarget, "Stormstrike"))
                return;

            if ( Safe_CastSpell( _me.CurrentTarget, "Lava Lash"))
                return;

            if (GetMaelstromCount() >= 5 && Safe_CastSpell(_me.CurrentTarget, "Lightning Bolt"))
                return;

            if ( _me.Auras.ContainsKey("Unleash Flame") && Safe_CastSpell( _me.CurrentTarget, "Flame Shock"))
                return;

            if ( Safe_CastSpell( _me.CurrentTarget, "Unleash Elements"))
                return;

            if ((_me.Level < 81 || !SpellManager.HasSpell("Unleash Elements")) && FlameShock())
                return;

            if (Safe_CastSpell(_me.CurrentTarget, "Earth Shock"))
                return;

            if (FeralSpiritCheck())
                return;

            if ((IsRAF() || IsRaidBehavior()) && FeralSpirit())
                return;

#if SLOW
            if (CleanseIfNeeded(_me))
                return;
#endif
        }

        private void CombatLogicEnhancePVP()
        {
            if (HandleBreakingCrowdControl())
            {
                Dlog("CombatLogicEnhcPVP: handled cc break is true this pass");
                return;
            }

            if (HandlePvpTargetChange())
            {
                Dlog("CombatLogicEnhcPVP: handled pvp target change is true this pass");
                return;
            }

            if (HandlePvpGroundingTotem())
            {
                Dlog("CombatLogicEnhcPVP: handled pvp grounding totem this pass");
                return;
            }

            HandleMovement();

            FaceToUnit(_me.CurrentTarget);
            if (IsCasting())
            {
                Dlog("CombatLogicEnhcPVP: cast in progress");
                return;
            }

            if (GCD())
            {
                Dlog("CombatLogicEnhcPVP: gcd in progress");
                return;
            }

            WoWAura aura = GetCrowdControlledAura(_me);
            if (IsSilencedAura(aura))
            {
                Slog(Color.Orange, "{0} due to {1} -- unable to cast", aura.Spell.Mechanic.ToString(), aura.Name);
                return;
            }

            Dlog("CombatLogicEnhcPVP: okay to cast");

            if (MaelstromCheckHealPriority())
            {
                Dlog("CombatLogicEnhcPVP: looks like we healed, done");
                return;
            }

            if (HealMySelfInstantOnly())
            {
                Dlog("CombatLogicEnhcPVP: looks like we insta-healed, done");
                return;
            }

            if (!IsFacing( _me.CurrentTarget))
            {
                Dlog("CombatLogicEnhcPVP: not facing current target");
                return;
            }

            double distTarget = _me.CurrentTarget.Distance;

            if (distTarget < 7 )
            {
                if (!TotemsWereSet && !InGhostwolfForm() && SetTotemsAsNeeded())
                {
                    return;
                }
            }

            if ( distTarget > 7 )
            {
                // Stay in Ghostwolf or Mounted if > 7 yds away
                if (_me.Mounted || InGhostwolfForm())
                {
                    Dlog("CombatLogicEnhcPVP: staying mounted/ghostwolf while moving {0:F1} yds from target", distTarget);
                    return;
                }

                const int FROST_SHOCK = 8056;
                // const int UNLEASH_ELEMENTS = 73680;

                if (_me.CurrentTarget.IsMoving)
                {
                    if (!_me.CurrentTarget.IsAuraPresent(FROST_SHOCK))
                    {
                        if (FrostShock())
                        {
                            Dlog("CombatLogicEnhcPVP: trying Frost Shock to slow moving target {0:F1} yds away", distTarget);
                            return;
                        }

                        if (!_me.CurrentTarget.IsAuraPresent("Unleash Frost") && IsWeaponImbuedWithFrostBrand())
                        {
                            if (UnleashElements())
                            {
                                Dlog("CombatLogicEnhcPVP: trying Unleash Frost to slow moving target {0:F1} yds away", distTarget);
                                return;
                            }
                        }
                    }
                }
            }

            if (distTarget > 10)
            {
                if (cfg.UseGhostWolfForm && !InGhostwolfForm())
                {
                    if (_hasTalentAncestralSwiftness || distTarget > 15)
                    {
                        Dlog("CombatLogicEnhcPVP: switching to ghostwolf moving {0:F1} yds from target", distTarget);
                        if (GhostWolf())
                            return;
                    }
                }
            }

            if (!_me.IsAutoAttacking && _me.CurrentTarget.IsAlive && _me.CurrentTarget.Distance < 10)
            {
                Dlog("CombatLogicEnhcPVP: ** Auto-Attack started in Combat");
                AutoAttack();
                return;
            }


            if (HandleHex())
            {
                return;
            }

            // dont waste Spirit Walk... if less than 4 secs and haven't used, cast it
            if (_me.GotAlivePet)
            {
                WoWAura fs = _me.GetAura("Feral Spirit");
                if (fs != null && fs.TimeLeft.TotalMilliseconds < 4000)
                {
                    if (WolvesSpiritWalk())
                    {
                        Dlog("CombatLogicEnhcPVP: cast Spirit Walk because wolves only have {0} ms left", fs.TimeLeft.TotalMilliseconds);
                        return;
                    }
                }

                if (_me.Pet.GotTarget)
                {
                    switch (_me.Pet.CurrentTarget.Class)
                    {
                        case WoWClass.Mage:
                        case WoWClass.Priest:
                        case WoWClass.Warlock:
                            if (_me.Pet.CurrentTarget.IsCasting && WolvesBash())
                                return;
                            break;

                        default:
                            if (WolvesBash())
                                return;
                            break;
                    }
                }
            }

            if (priorityCleanse == ConfigValues.SpellPriority.High && DispelRaid())
                return;
            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
                return;

            if (!Safe_IsMoving() && _me.GotTarget && IsCaster(_me.CurrentTarget) && _me.CombatDistanceSqr(_me.CurrentTarget) > 100)
            {
                if (Hex(_me.CurrentTarget))
                    return;
            }

            if (InterruptEnemyCast())
                return;

            if (FeralSpirit())
                return;

            if (UseTrinkets())
                return;

            if (FlameShock())
                return;

            if (Stormstrike())
                return;

            if (LavaLash())
                return;

            if (EarthShock())
                return;

            if (PrimalStrike())
                return;

            // for Enhancement:  make first set of totems high priority
            if (SetTotemsAsNeeded())
                return;

            if (CastCombatSpecials())
                return;

            if (CallForReinforcements())
                return;

            if (ShamanBuffs(false))
                return;

            if (!Safe_IsMoving() && BindWaterElemental())
                return;

            if (priorityCleanse == ConfigValues.SpellPriority.Low && DispelRaid())
                return;
            if (priorityPurge == ConfigValues.SpellPriority.Low && Purge(null))
                return;
            if (priorityPurge == ConfigValues.SpellPriority.LowCurrentTarget && _me.GotTarget && Purge(_me.CurrentTarget))
                return;

            Dlog("CombatLogicEnhcPVP:  no cast made this pass");


        }

        /*
         * CombatElemental()
         * 
         */
        private void CombatElemental()
        {
            if (IsPVP())
            {
                CombatElementalPVP();
                return;
            }

#if MOVE_IN_COMBAT_HANDLER
            if (!CurrentTargetInRangedDistance() && _me.Rooted)
            {
                // options -- trinket, heal (anticipating being hit)
                if (!FindBestTarget())
                {
                    Slog("Rooted:  no target in range so waiting it out...");
                    return;
                }
            }

            if (_me.GotTarget && !CurrentTargetInRangedDistance())
            {
                // chase until in ranged distance or for 8 seconds
                Dlog("CombatElemental: running to mob " + _me.CurrentTarget.Distance.ToString("F2") + " away");
                MoveToCurrentTarget();
                if (BestInstantAttack())
                {
                    _pullAttackCount++;
                    return;
                }

                return;
            }

            if (Safe_IsMoving())
            {
                if (CanMoveWhileCasting())
                {
                    Dlog("CombatElemental:  User is moving - waiting to cast");
                }
                else if (IsMovementDisabled())
                {
                    Dlog("CombatElemental:  User is moving - waiting to cast");
                    return;
                }
                else
                {
                    Safe_StopMoving("in range for Elemental combat");
                }
            }
#else
            HandleMovement();
#endif
            if (IsCastingOrGCD())
            {
                Dlog("CombatElem: casting or gcd active");
                return;
            }

            if (!combatChecks())
            {
                Dlog("CombatElem: failed combat checks so no attack cast this pass");
                return;
            }

            // cast totems in pull, but if using a Bot that only calls Combat we'll get it here
            if (AllowNonHealSpells())
            {
                // check for multiple targets or current has some life left
                if ((countEnemy > 1 || (_me.GotTarget && _me.CurrentTarget.HealthPercent > 25)))
                {
                    if (SetTotemsAsNeeded())
                    {
                        return;
                    }
                }
            }

            if (CallForReinforcements())
            {
                Dlog("CombatElem: failed CallForReinforcements() so no attack cast this pass");
                return;
            }

            if (CastCombatSpecials())
            {
                Dlog("CombatElem:  combat special used, so no more casts this pass");
                return;
            }

            if ((_count10YardEnemy > 0 && IsFightStressful())
                || (_count10YardEnemy > 0 && _me.ManaPercent <= cfg.ThunderstormPercent)
                || (IsRAF() && _me.ManaPercent <= cfg.ThunderstormPercent)
               )
            {
                Dlog("CombatElemental:  {0} enemies in 10 yards and stress={1}", _count10YardEnemy, IsFightStressful());
                if (Thunderstorm())
                {
                    Dlog("CombatElem: Thunderstorm cast so no further attacks this pass");
                    return;
                }
            }

            if (FarmingAsElemental())
            {
                return;
            }

            int aoeCountNeeded = _hasGlyphOfChainLightning ? 7 : 5;
            if (AllowNonHealSpells() && _me.GotTarget)
            {
                if (_me.ManaPercent > 50 && _me.IsAuraPresent("Clearcasting") && _countAoe8Enemy >= aoeCountNeeded && _me.CurrentTarget.Distance < 33 && SpellManager.HasSpell("Earthquake"))
                {
                    if (Safe_CastSpell(_me.CurrentTarget, "Earthquake"))
                    {
                        WaitForCurrentCastOrGCD();
                        if (!LegacySpellManager.ClickRemoteLocation(_me.CurrentTarget.Location))
                        {
                            Dlog("^Ranged AoE Click FAILED:  cancelling Earthquake");
                            SpellManager.StopCasting();
                        }
                        else
                        {
                            Dlog("^Ranged AoE Click successful:  EARTHQUAKE on {0} targets", _countAoe8Enemy);
                            SleepForLagDuration();
                        }

                        return;
                    }
                }
            }

            if (ReplenishTotem(TOTEM_FIRE))
                return;

            if (_pullAttackCount == 0 && FulminationCheck())
            {
                Dlog("CombatElem: fulmination used as OPENER");
                return;
            }

            _pullAttackCount++;

            if (FlameShockRenew())
            {
                Dlog("CombatElem: flame shock, so no more attacks cast this pass");
                return;
            }

            if (ElementalMastery())
            {
                Dlog("CombatElem: elemental mastery, so no more casts this pass");
                return;
            }

            if ((_countAoe12Enemy <= 2 || IsBossCurrentTarget()) && LavaBurst())
            {
                Dlog("CombatElem: lavaburst, so no more attacks cast this pass");
                return;
            }

            if (FulminationCheck())
            {
                Dlog("CombatElem: fulmination proc'd, so no more casts this pass");
                return;
            }

            if (_countAoe12Enemy > 1 && (!InGroup() || _me.ManaPercent > 40 || _me.IsAuraPresent("Clearcasting")))
            {
                Dlog("CombatElem: chain lightning - {0:F1}% mana, clearcasting={1}, group={2}", _me.ManaPercent, _me.IsAuraPresent("Clearcasting"), InGroup());
                if (ChainLightning())
                {
                    return;
                }
            }

#if PRE_430
            if (FireNova())
            {
                Dlog("CombatElem: firenova, so no more attacks cast this pass");
                return;
            }
#endif

            // earth shock now, but only if we don't have Fulmination talent and it won't interfere with Flame Shock DoT
            if (!_hasTalentFulmination && CanAnElemBuyAnEarthShock() && EarthShock())
            {
                Dlog("CombatElem: earth shock, so no more attacks cast this pass");
                return;
            }

            if (LightningBolt())
            {
                Dlog("CombatElem: lightningbolt, so no more attacks cast this pass");
                return;
            }

            if (CleanseIfNeeded(_me))
            {
                Dlog("CombatElem: cleanse, so no more attacks cast this pass");
                return;
            }

            if (HandleWaterBreathing())
            {
                Dlog("CombatMelee:  trying not to drown");
                return;
            }

            if (FrostShock())
            {
                Dlog("CombatElem: frost shock used because of targets ?Nature Immunity?");
                return;
            }

            if (IsWeaponImbuedWithDPS() && UnleashElements())
            {
                Dlog("CombatElem: unleash elements used because of targets ?Nature Immunity?");
                return;
            }

            Dlog("CombatElem: made it through entire pass without casting anything!!!!");
        }

        /*
         * CombatElemental()
         * 
         */
        private void CombatElementalRaid()
        {
#if SLOW
            if (HandleBreakingCrowdControl())
            {
                Dlog("CombatElementalRaid: handled cc break is true this pass");
                return;
            }
#endif

#if MOVE_IN_COMBAT_HANDLER
            if (!_me.GotTarget)
            {
                if (Safe_IsMoving())
                    Safe_StopMoving("no current target");

                return;
            }

            #region Get Moving!!!! OR Stop if you should!!!
            if (!CurrentTargetInRangedDistance() || !_me.CurrentTarget.InLineOfSpellSight)
            {
                if (_me.Rooted)
                {
                    Slog("Rooted...");
                    if (FindBestTarget())
                        return;

                    Dlog("CombatElementalRaid:  rooted and no target in range so waiting it out...");
                    return;
                }

                if (!Safe_IsMoving() && IsCasting())
                {
                    if (!CanMoveWhileCasting())
                    {
                        Dlog("CombatElementalRaid: casting, so avoiding movement");
                        return;
                    }

                    Dlog("CombatElementalRaid: casting but have SpiritWalkers Grace or Unleashed Lightning");
                }

                
                MoveToUnit(_me.CurrentTarget);
                return;
            }

            Safe_StopMoving(String.Format("Target {0:F1} yds away", _me.CurrentTarget.Distance));
            #endregion
#else
            HandleMovement();
#endif

            if (!IsFacing( _me.CurrentTarget))
            {
                Dlog("CombatElementalRaid: not facing current target");
                FaceToUnit(_me.CurrentTarget);
                return;
            }

            if (IsCasting())
            {
                Dlog("CombatElementalRaid: cast in progress");
                return;
            }

            if (GCD())
            {
                Dlog("CombatElementalRaid: gcd in progress");
                return;
            }

#if SLOW
            WoWAura aura = GetCrowdControlledAura(_me);
            if (IsSilencedAura(aura))
            {
                Slog(Color.Orange, "{0} due to {1} -- unable to cast", aura.Spell.Mechanic.ToString(), aura.Name);
                return;
            }
#endif

            Dlog("CombatElementalRaid: okay to cast");

#if SLOW
            if (HealMySelfInstantOnly())
            {
                Dlog("CombatLogicEnhcPVP: looks like we insta-healed, done");
                return;
            }

            ShamanBuffs(false);
#endif
            if (!_me.IsAuraPresent("Lightning Shield") && Safe_CastSpell(_me, "Lightning Shield"))
            {
                return;
            }

#if SLOW
            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
                return;
#endif

            // PRIORITY

            if (!TotemsWereSet && SetTotemsAsNeeded())
                return;

            if (TotemsWereSet && ReplenishTotem( TOTEM_FIRE))
                return;

#if SLOW
            if (CallForReinforcements())
                return;

            CastCombatSpecials();
#endif
            uint flameShockLeft = _me.CurrentTarget.GetAuraTimeLeft("Flame Shock");
            if (flameShockLeft < (6000 - (500 * _ptsTalentReverberation)))
            {
                if ( flameShockLeft < 2000 && Safe_CastSpell(_me.CurrentTarget, "Flame Shock"))
                {
                    return;
                }
            }
            else if ( GetLightningShieldCount() > 7 )
            {
                if (Safe_CastSpell(_me.CurrentTarget, "Earth Shock"))
                {
                    return;
                }
            }

            if (Safe_CastSpell(_me, "Elemental Mastery"))
                return;

            if (Safe_CastSpell( _me.CurrentTarget, "Lava Burst"))
                return;

            if (Safe_CastSpell( _me.CurrentTarget, "Lightning Bolt"))
                return;

            Dlog("CombatElementalRaid: made it through entire pass without casting anything!!!!"); 
        }

        private bool CombatElementalRaidAoE()
        {
            if (Safe_IsBoss(_me.CurrentTarget))
            {
                Dlog("CombatElementalAoE:  no AoE rotation when targeting boss");
                return false;
            }

            int eqCountNeeded = _hasGlyphOfChainLightning ? 7 : 5;
            if (_me.CurrentTarget.Distance < 33)
            {
                // mana contingent Earthquake
                if (_me.ManaPercent > 60 && _me.IsAuraPresent("Clearcasting"))
                {
                    if (SpellManager.HasSpell("Earthquake"))
                    {
                        int eqCount = 0;
                        int eqRadiusSqr = 8 * 8;

                        if (IsRAFandTANK())
                            eqCount = AllEnemyMobs.Count(u => u.CurrentTarget == GroupTank && u.Location.DistanceSqr(GroupTank.Location) < eqRadiusSqr);
                        else
                            eqCount = AllEnemyMobs.Count(u => u.Aggro && u.Location.DistanceSqr(GroupTank.Location) < eqRadiusSqr); 

                        if (eqCount >= eqCountNeeded && Safe_CastSpell(_me.CurrentTarget, "Earthquake"))
                        {
                            WaitForCurrentCastOrGCD();
                            if (LegacySpellManager.ClickRemoteLocation(_me.CurrentTarget.Location))
                            {
                                Dlog("^Ranged AoE Click successful:  EARTHQUAKE on {0} targets", _countAoe8Enemy);
                                return true;
                            }

                            Dlog("^Ranged AoE Click FAILED:  cancelling Earthquake");
                            SpellManager.StopCasting();
                        }
                    }
                }
            }

            if (_me.ManaPercent > 50 || _me.IsAuraPresent("Clearcasting"))
            {
                if (SpellManager.HasSpell("Chain Lightning"))
                {
                    bool clWillHop = AllEnemyMobs.Any(
                        u => u != _me.CurrentTarget 
                            && u.Combat 
                            && u.Location.DistanceSqr(_me.CurrentTarget.Location) < (12 * 12));

                    if ( clWillHop && Safe_CastSpell(_me.CurrentTarget, "Chain Lightning"))
                    {
                        WaitForCurrentCastOrGCD();
                        if (LegacySpellManager.ClickRemoteLocation(_me.CurrentTarget.Location))
                        {
                            Dlog("^Ranged AoE Click successful:  EARTHQUAKE on {0} targets", _countAoe8Enemy);
                            return true;
                        }

                        Dlog("^Ranged AoE Click FAILED:  cancelling Earthquake");
                        SpellManager.StopCasting();
                    }
                }
            }

            return false;
        }

        private bool FarmingAsElemental()
        {
            bool castSpell = false;
            if (!InGroup() && cfg.PVE_CombatStyle == ConfigValues.PveCombatStyle.FarmingLowLevelMobs)
            {
                int countMobs = (from u in AllEnemyOrNeutralMobs where _me.CurrentTarget.Location.Distance(u.Location) < 12 select u).Count();
                if (countMobs >= 2)
                    castSpell = ChainLightning();
            }

            return castSpell;
        }

        private void CombatElementalPVP()
        {
            if (HandleBreakingCrowdControl())
            {
                Dlog("CombatLogicEnhcPVP: handled cc break is true this pass");
                return;
            }

            if (HandlePvpTargetChange())
            {
                Dlog("CombatLogicEnhcPVP: handled pvp target change is true this pass");
                return;
            }

            if (HandlePvpGroundingTotem())
            {
                Dlog("CombatLogicEnhcPVP: handled pvp grounding totem this pass");
                return;
            }

#if MOVE_IN_COMBAT_HANDLER
            if (!CurrentTargetInRangedDistance())
            {
                if (_me.Rooted)
                {
                    // options -- trinket, heal (anticipating being hit)
                    if (!FindBestTarget())
                    {
                        Slog("Rooted:  no target in range so waiting it out...");
                        return;
                    }
                }

                if (!CurrentTargetInRangedDistance())
                {
                    MoveToCurrentTarget();
                    if (BestInstantAttack())
                        return;

                    if (_hasGlyphOfUnleashedLightning)
                        LightningBolt();
                    return;
                }
            }

            if (Safe_IsMoving())
            {
                if (IsMovementDisabled())
                {
                    Dlog("User moving - wait to cast non-instant spells");
                    return;
                }

                Safe_StopMoving( "in range for Elemental Combat");
            }
#else
            HandleMovement();
#endif
            if (!combatChecks())
            {
                Dlog("CombatElemPVP: failed combat checks so no attack cast this pass");
                return;
            }


#if NO_TOTEMS_IN_PVP
#else
            // we already cast totems in pull, so this is just to see if we need to replace any if in crisis
            if (AllowNonHealSpells() && _me.GotTarget && _me.CurrentTarget.HealthPercent > 35 && !_me.CurrentTarget.Mounted)
            {
                SetTotemsAsNeeded();
            }
#endif
            if (_countMeleeEnemy > 0 && CallForReinforcements())
            {
                Dlog("CombatElemPVP: CallForReinforcements() so no attack cast this pass");
                return;
            }

            if ((_count10YardEnemy > 1 && !_hasGlyphOfThunderstorm ) || _me.ManaPercent < cfg.EmergencyManaPercent)
            {
                Dlog("CombatElemPVP: {0} enemies within 10 yds", _count10YardEnemy);
                if (Thunderstorm())
                {
                    Dlog("CombatElemPVP: Thunderstorm cast so no further attacks this pass");
                    return;
                }
            }

            if (FulminationCheck())
                return;

            if (CallForReinforcements())
            {
                Dlog("CombatElem: failed CallForReinforcements() so no attack cast this pass");
                return;
            }

            CastCombatSpecials();

            if (FlameShock())
            {
                Dlog("CombatElemPVP: flame shock, so no more attacks cast this pass");
                return;
            }

            if (_me.GotTarget && _me.CurrentTarget.IsCasting && Hex(_me.CurrentTarget))
            {
                Dlog("CombatElemPVP: hex, so no more attacks cast this pass");
                return;
            }

            ElementalMastery();
            if (LavaBurst())
            {
                Dlog("CombatElemPVP: lavaburst, so no more attacks cast this pass");
                return;
            }

            if (ChainLightning())
            {
                Dlog("CombatElemPVP: chain lightning, so no more attacks cast this pass");
                return;
            }

            if (!Safe_IsMoving() && BindWaterElemental())
                return;

            if (HandleWaterBreathing())
            {
                Dlog("CombatMelee:  trying not to drown");
                return;
            }

            if (LightningBolt())
            {
                Dlog("CombatElemPVP: chain lightning, so no more attacks cast this pass");
                return;
            }

            Dlog("CombatElemPVP: made it through entire pass without casting anything!!!!");
        }


        private void CombatLogicElementalPVP()
        {
            if (IsGameUnstable())
                return;

            if (!_me.IsAlive || (_me.GotTarget && !_me.CurrentTarget.IsAlive))
            {
                ReportBodyCount();
                return;
            }

            if (HandleBreakingCrowdControl())
            {
                return;
            }

            if (HandlePvpTargetChange())
            {
                return;
            }

#if MOVE_IN_COMBAT_HANDLER
            if (!_me.Rooted)
            {
                if (!Safe_IsMoving()) //  || _me.CurrentTarget.IsMoving)
                {
                    if (IsCasting())
                    {
                        if (!CanMoveWhileCasting())
                        {
                            Dlog("CombatLogicElemPVP: casting, so avoiding movement");
                            return;
                        }

                        Dlog("CombatLogicElemPVP: casting but have SpiritWalkers Grace or Unleashed Lightning");
                    }

                    if ( !CurrentTargetInRangedDistance())
                    {
                        MoveToUnit(_me.CurrentTarget, 0);
                    }
                }

                if (!CurrentTargetInRangedDistance())
                {
                    MoveToUnit(_me.CurrentTarget, 0);
                }

                if (_me.CurrentTarget.Distance < (_maxDistForRangeAttack - 5))
                {
                    Safe_StopMoving(String.Format("Target {0:F1} yds away", _me.CurrentTarget.Distance));
                }
            }
#else
            HandleMovement();
#endif

            FaceToUnit(_me.CurrentTarget);
            if (IsCasting())
            {
                Dlog("CombatLogicElemPVP: cast in progress");
                return;
            }

            if (GCD())
            {
                Dlog("CombatLogicElemPVP: gcd in progress");
                return;
            }

            if (_me.Silenced)
            {
                Slog(Color.Orange, "You are silenced and unable to cast");
                return;
            }

            if (HealMySelfInstantOnly())
            {
                return;
            }

            if (!IsFacing( _me.CurrentTarget))
            {
                Dlog("CombatLogicElemPVP: not facing current target");
                return;
            }

            double distTarget = _me.CombatDistance(_me.CurrentTarget);

            // out of range?  if in Ghost Wolf or Mounted, return 
            if (distTarget > _maxDistForRangeAttack )
            {
                if ( _me.Mounted || InGhostwolfForm())
                {
                    Dlog("CombatLogicEnhcPVP: staying mounted/ghostwolf while moving {0:F1} yds from target", distTarget);
                    return;
                }

                // out of range by 10 or more?  go go Ghost Wolf
                if (distTarget > (_maxDistForRangeAttack + 7))
                {
                    if (cfg.UseGhostWolfForm && !InGhostwolfForm())
                    {
                        if (_hasTalentAncestralSwiftness || distTarget > (_maxDistForRangeAttack + 15))
                        {
                            Dlog("CombatLogicEnhcPVP: switching to ghostwolf moving {0:F1} yds from target", distTarget);
                            if (GhostWolf())
                            {
                                return;
                            }
                        }
                    }
                }

                // out of range, so return now  (you could purge, cleanse, interrupt here while moving possibly
                return;
            }

            Dlog("CombatLogicElemPVP: okay to cast");

            if (!combatChecks())
            {
                Dlog("CombatLogicElemPVP: failed combat checks so no attack cast this pass");
                return;
            }

            if (Safe_IsMoving() && !CanMoveWhileCasting())
            {
                if (FulminationCheck())
                {
                    return;
                }

                if (IsWeaponImbuedWithDPS() && UnleashElements())
                {
                    return;
                }

                if (FlameShock())
                {
                    return;
                }

                if (FrostShock())
                {
                    return;
                }

                if (_hasGlyphOfUnleashedLightning && LightningBolt())
                {
                    return;
                }

                if (InterruptEnemyCast())
                {
                    return;
                }

                if ( Purge(null))
                {
                    return;
                }
            }
            else
            {
                // we already cast totems in pull, so this is just to see if we need to replace any if in crisis
                if (AllowNonHealSpells() && _me.GotTarget && _me.CurrentTarget.HealthPercent > 35 && !_me.CurrentTarget.Mounted)
                {
                    if (SetTotemsAsNeeded())
                    {
                        return;
                    }
                }

                if (_countMeleeEnemy > 0 && CallForReinforcements())
                {
                    Dlog("CombatLogicElemPVP: CallForReinforcements() so no attack cast this pass");
                    return;
                }

                if ((_count10YardEnemy > 1 && !_hasGlyphOfThunderstorm) || _me.ManaPercent < cfg.EmergencyManaPercent)
                {
                    Dlog("CombatLogicElemPVP: {0} enemies within 10 yds", _count10YardEnemy);
                    if (Thunderstorm())
                    {
                        Dlog("CombatLogicElemPVP: Thunderstorm cast so no further attacks this pass");
                        return;
                    }
                }

                if (FulminationCheck())
                {
                    return;
                }

                if (CallForReinforcements())
                {
                    Dlog("CombatLogicElemPVP: failed CallForReinforcements() so no attack cast this pass");
                    return;
                }

                if (CastCombatSpecials())
                {
                    return;
                }

                if (FrostShock())
                {
                    return;
                }

                if (FlameShock())
                {
                    return;
                }


                if (HandleHex())
                {
                    return;
                }

                if (_me.GotTarget && _me.CurrentTarget.IsCasting && Hex(_me.CurrentTarget))
                {
                    return;
                }

                if (ElementalMastery())
                {
                    return;
                }

                if (LavaBurst())
                {
                    return;
                }

                if (_me.IsAuraPresent( "Clearcasting") && ChainLightning())
                {
                    return;
                }

                if (!Safe_IsMoving() && BindWaterElemental())
                {
                    return;
                }

                if (LightningBolt())
                {
                    Dlog("CombatLogicElemPVP: chain lightning, so no more attacks cast this pass");
                    return;
                }
            }

            Dlog("CombatLogicElemPVP: made it through entire pass without casting anything!!!!");

        }


        private bool NeedRestLogicResto()
        {
            // handle low mana situation
            if (_me.ManaPercent <= cfg.ManaTidePercent && _me.Combat) // (_me.Combat || (IsRAFandTANK() && GroupTank.Combat)))
            {
                if (Safe_IsMoving())
                    Dlog("NeedRestLogicResto:  moving, so waiting to cast Mana Tide");
                else if (_me.HasAura((int)TotemId.MANA_TIDE_TOTEM))
                    Dlog("NeedRestLogicResto:  mana tide totem aura present");
                else if (SpellManager.HasSpell((int)TotemId.MANA_TIDE_TOTEM)
                        && Safe_CanCastSpell(_me, (int)TotemId.MANA_TIDE_TOTEM)
                        && TotemCast(TotemId.MANA_TIDE_TOTEM))
                    return true;
                else if (_me.HasAura((int)TotemId.MANA_SPRING_TOTEM))
                    Dlog("NeedRestLogicResto:  mana spring totem aura present");
                else if (SpellManager.HasSpell((int)TotemId.MANA_SPRING_TOTEM)
                        && Safe_CanCastSpell(_me, (int)TotemId.MANA_SPRING_TOTEM)
                        && TotemCast(TotemId.MANA_SPRING_TOTEM))
                    return true;
            }

            // use potion if a key person is in combat
            if ( _me.ManaPercent <= cfg.TrinkAtMana && _me.Combat && UseManaPotionIfAvailable())
                return true;

            // if we dispel someone, done for now
            if ( priorityCleanse == ConfigValues.SpellPriority.High )
            {
                if (DispelRaid())
                    return true;
            }

            // if we heal someone, done for now
            if (HealRaid())
                return true;

            // if we dispel someone, done for now
            if ( priorityCleanse == ConfigValues.SpellPriority.Low )
            {
                if (DispelRaid())
                    return true;
            }

            // healers need to stay within range of Tank,
            // .. so do best to stay in range while heals aren't desparately needed
            if (IsPVP() && !Safe_IsMoving() && _me.Combat)
            {
                if (SetTotemsAsNeeded())
                    return true;
            }
            else if (IsRAF())
            {
                // if not in range move there
                if (!_me.IsUnitInRange(GroupTank, cfg.Party_FollowAtRange))
                {
                    if (!cfg.RAF_FollowClosely)
                        ;
                    else if (GroupTank == null)
                        Dlog("NeedRestLogicResto:  no tank identified or in range, so nobody to follow");
                    else
                    {
                        Dlog("NeedRestLogicResto:  no heals needed and tank {0:F1} yds away so moving in range", _me.CombatDistance(GroupTank));
                        MoveToHealTarget(GroupTank, cfg.Party_FollowAtRange * 0.88);
                    }
                }
                // else when in range see if fight started and position set
                else if (GroupTank.Combat && GroupTank.GotTarget && Safe_IsHostile(GroupTank.CurrentTarget))
                {
                    if (!GroupTank.IsMoving && 8 > GroupTank.CombatDistance(GroupTank.CurrentTarget))
                    {
                        if (SetTotemsAsNeeded())
                            return true;
                    }
                }
            }

            // if in a Healer Only role (not trying to DPS for anything other than mana regen)
            //  ..  then check buffs and stuff then return True so it doesn't do Combat()
            if (IsHealerOnly() && InterruptEnemyCast())
                return true;

            return false;
        }


        private void CombatRestoWrapper()
        {
            if (CombatResto())
                return;

            return;
        }

        Countdown restoMsg = new Countdown(1000);

        private bool CombatResto()
        {
            if (HandleBreakingCrowdControl())
            {
                Dlog("CombatResto: handled cc break is true this pass");
                return true;
            }

            if (IsPVP())
            {
                if (HandlePvpTargetChange())
                {
                    Dlog("CombatResto: handled pvp target change is true this pass");
                    return true;
                }

                if (HandlePvpGroundingTotem())
                {
                    Dlog("CombatResto: handled pvp grounding totem this pass");
                    return true;
                }
            }

            // purely a group activity, so bail if not in one
            // check if offhealing is needed when you aren't a healer
            if (!IsHealer())
            {
                if (!IsRAF() || cfg.RAF_CombatStyle == ConfigValues.RafCombatStyle.CombatOnly )
                    return false;

                WoWPlayer playerOffheal = GroupMembers.FirstOrDefault(p => p.CurrentHealth > 1 && cfg.RAF_GroupOffHeal > (int)p.HealthPercent && _me.CombatDistance(p) < cfg.GroupHeal.SearchRange);
                if (playerOffheal == null)
                    return false;

                Slog("Temporarily switching to OffHeal:  {0} at {1:F1}%", Safe_UnitName(playerOffheal), playerOffheal.HealthPercent);
                minGroupHealth = (int)playerOffheal.HealthPercent;
            }

            // handle replenishment of totems
            // handle totems for battlegrounds
            if (IsPVP())
            {
                if (!Safe_IsMoving() && _me.Combat)
                {
                    if (SetTotemsAsNeeded())
                    {
                        return true;
                    }
                }
            }
            // handle totems for RAF
            else if (IsRAF())
            {
                if (GroupTank == null)
                {
                    if ( _me.Combat && SetTotemsAsNeeded())
                    {
                        Dlog("CombatResto: totems set because no Tank established");
                        return true;
                    }
                }
                else if (GroupTank.CurrentHealth <= 1)
                {
                    if (_me.Combat)
                    {
                        if (SetTotemsAsNeeded())
                        {
                            return true;
                        }

                        if (EarthElementalTotem())
                        {
                            Dlog("CombatResto: tank is dead, so set Earth Elemental");
                            return true;
                        }
                    }
                }
                else if (!_me.IsUnitInRange(GroupTank, IsMovementDisabled() ? 40 : cfg.Party_FollowAtRange))
                {
                    if (!cfg.RAF_FollowClosely)
                        ;
                    else if (GroupTank == null)
                        Dlog("CombatResto:  no tank exists or in range, so nobody to move towards");
                    else
                    {
                        Dlog("CombatResto:  no heals needed and tank {0:F1} yds away so moving in range", _me.CombatDistance(GroupTank));
                        MoveToHealTarget(GroupTank, cfg.Party_FollowAtRange * 0.88);
                    }
                }
                // else when in range see if fight started and position set
                else if (GroupTank.Combat && GroupTank.GotTarget && Safe_IsHostile(GroupTank.CurrentTarget))
                {
                    double tankTargetMeleeRange = GroupTank.MeleeRange(GroupTank.CurrentTarget);
                    double tankDistToTarget = GroupTank.CombatDistance(GroupTank.CurrentTarget);
                    bool shouldTotemsBeCast = false;

                    if (!GroupTank.IsMoving && tankDistToTarget < (tankTargetMeleeRange + 15))
                        shouldTotemsBeCast = true;
                    else if ( _me.IsUnitInRange(GroupTank.CurrentTarget, _maxSpellRange))
                        shouldTotemsBeCast = true;

                    if (!TotemsWereSet || _ptTotems.Distance(_me.Location) > Shaman.cfg.DistanceForTotemRecall)
                    {
                        Dlog("CombatResto: RAF Totem Set Criteria: tkmove={0} tk2trg={1:F2} tkmelee={2:F2} allowCast={3} totemsSet={4}",
                            GroupTank.IsMoving,
                            tankDistToTarget,
                            tankTargetMeleeRange,
                            shouldTotemsBeCast ,
                            TotemsWereSet 
                            );
                    }

                    if ( TotemsWereSet || shouldTotemsBeCast )
                    {
                        if (SetTotemsAsNeeded())
                            return true;
                    }
                }
            }

            // handle low mana situation
            // use potion if a key person is in combat
            if (_me.ManaPercent <= cfg.TrinkAtMana && _me.Combat)
            {
                if (UseManaPotionIfAvailable())
                    return true;
            }

            if ( ShieldTwisting(false))
                return true;

            if (CastCombatSpecials())
                return true;

            // if we heal someone, done with processing for now
            if (HealRaid())
                return true;

            // if we dispel someone, done for now
            if ( priorityCleanse == ConfigValues.SpellPriority.Low && DispelRaid())
                return true;

            // if in a Healer Only role (not trying to DPS for anything other than mana regen)
            //  ..  then check buffs and stuff then return True so it doesn't do Combat()
            if (IsHealerOnly())
            {
#if HEALER_DONT_WINDSHEAR
#else
                if (InterruptEnemyCast())
                    return true;
#endif
                if (combatChecks()) // makes sure we have hostile target and are facing
                {
                    if (priorityPurge == ConfigValues.SpellPriority.Low && Purge(null))
                        return true;

                    if (_me.GotTarget && !IsFacing( _me.CurrentTarget))
                        Safe_FaceTarget();

                    if (_me.GotTarget && _me.CurrentTarget.IsAlive && _me.CurrentTarget.Combat && Safe_IsHostile(_me.CurrentTarget))
                    {
                        if (!_me.CurrentTarget.InLineOfSpellSight)
                        {
                            Dlog("CombatResto:  enemy target not in line of sight");
                        }
                        else if (!FaceToUnit(_me.CurrentTarget))
                        {
                            Dlog("CombatResto:  not facing enemy target");
                        }
                        else
                        {
                            bool castAttack = false;
#if HEALER_IGNORE_FOCUSED_INSIGHT
#else
                            if (IsPVP() && _me.ManaPercent > 40 && _hasTalentFocusedInsight && Safe_CanCastSpell(_me.CurrentTarget, "Earth Shock"))
                            {
                                const int FLAME_SHOCK = 8050;
                                const int EARTH_SHOCK = 8042;
                                const int FROST_SHOCK = 8056;

                                if (!_me.CurrentTarget.IsAuraPresent(FLAME_SHOCK) && (_me.CurrentTarget.Class == WoWClass.Rogue || _me.CurrentTarget.Class == WoWClass.Druid))
                                    castAttack = Safe_CastSpell(_me.CurrentTarget, FLAME_SHOCK);
                                else if (!_me.CurrentTarget.IsAuraPresent(EARTH_SHOCK) && _me.CurrentTarget.Class != WoWClass.Priest && _me.CurrentTarget.Class != WoWClass.Warlock)
                                    castAttack = Safe_CastSpell(_me.CurrentTarget, EARTH_SHOCK);
                                else if (!_me.CurrentTarget.IsAuraPresent(FROST_SHOCK))
                                    castAttack = Safe_CastSpell(_me.CurrentTarget, FROST_SHOCK);
                                else if (!_me.CurrentTarget.IsAuraPresent(FLAME_SHOCK))
                                    castAttack = Safe_CastSpell(_me.CurrentTarget, FLAME_SHOCK);
                                else
                                    Dlog("CombatResto: target has all needed shock debuffs - cooldown saved for later");
                            }
#endif
#if HEALER_IGNORE_TELLURIC_CURRENTS
#else
                            if (!castAttack && _hasTalentTelluricCurrents && Safe_CanCastSpell(_me.CurrentTarget, "Lightning Bolt"))
                            {
                                castAttack = Safe_CastSpell(_me.CurrentTarget, "Lightning Bolt");
                                if (castAttack)
                                {
                                    // wait for spell with check to cancel cast if group member needs heal
                                    WaitForHealerDamageSpell();
                                }
                            }
#endif
                            if (castAttack)
                                return true;
                        }
                    }
                }
            }

            // try to show some message indicating nothing was done without a lot of spam
            if (restoMsg.Done && !_me.IsMoving && !IsCastingOrGCD())
            {
                Log(Color.LightPink, "CombatResto:  Nothing Done!!!");
                restoMsg.Remaining = 1000;
            }

            return IsHealerOnly();
        }

        public bool HandleBreakingCrowdControl()
        {
            WoWAura aura = GetCrowdControlledAura(_me);
            if (aura == null)
                return false;

            TotemId totem = TotemId.NONE;

            Slog(Color.Orange, "Loss of control ({0}) due to {1}", aura.Spell.Mechanic.ToString(), aura.Name);
            if (IsCastingOrGCD())
            {
                Dlog("HandleBreakingCrowdControl: suppress cc break since Casting={0} GCD={1}", GCD(), IsCasting());
                return false;
            }

            switch (aura.Spell.Mechanic)
            {
                case WoWSpellMechanic.Fleeing:
                case WoWSpellMechanic.Charmed:
                case WoWSpellMechanic.Asleep:
                    totem = TotemId.TREMOR_TOTEM;
                    break;

                case WoWSpellMechanic.Snared:
                    if (_hasTalentEarthenPower )
                        totem = TotemId.EARTHBIND_TOTEM;
                    break;
            }

            if (typeShaman == ShamanType.Enhance && _me.GotAlivePet )
            {
                if ( IsSpellBlacklisted( PET_SPIRITWALK ))
                    return false;   // possibly broke CC, so allow other stuff

                if (IsPetSpellUsable(PET_SPIRITWALK))
                {
                    Log("^Attempting to break {0} with PET_SPIRITWALK", aura.Name);
                    if (CastPetAction(PET_SPIRITWALK))
                        return true;
                }
            }

            if (totem != TotemId.NONE && HasTotemSpell(totem))
            {
                if (IsSpellBlacklisted((int)totem))
                {
                    Dlog("HandleBreakingCrowdControl:  {0} is currently blacklisted", totem.ToString());
                    return false;   // possibly broken, so allow other stuff
                }

                if (!TotemExist(totem))
                {
                    WoWSpell totemSpell = SpellManager.Spells.First(t => t.Value.Id == (int)totem).Value;
                    if ( !SpellHelper.OnCooldown( totemSpell))
                    {
                        Log("^Attempting to break {0} with {1}", aura.Name, totem.ToString());
                        if ( TotemCast(totem))
                        {
                            return true;
                        }
                    }
                }
            }

            if ( aura.Spell.Mechanic == WoWSpellMechanic.Rooted
                || aura.Spell.Mechanic == WoWSpellMechanic.Snared
                || aura.Spell.Mechanic == WoWSpellMechanic.Slowed 
                || aura.Spell.Mechanic == WoWSpellMechanic.Stunned )
            {
                if (typeShaman == ShamanType.Enhance && ShamanisticRage())
                {
                    return true;
                }

                if (aura.Spell.Mechanic != WoWSpellMechanic.Stunned && IsFacing( _me.CurrentTarget))
                {
                    Dlog("HandleBreakingCrowdControl:  crowd controlled but still facing so try to fight on");
                    return false;
                }
            }

            if (aura.TimeLeft.TotalMilliseconds < 1000)
            {
                Dlog("HandleBreakingCrowdControl:  saving PVP Trink since {0} only has {1} ms left", aura.Name, aura.TimeLeft.TotalMilliseconds);
            }
            else if (UseItem(trinkPVP))
            {
                return true;
            }

            return !(aura.Spell.Mechanic == WoWSpellMechanic.Rooted
                || aura.Spell.Mechanic == WoWSpellMechanic.Snared
                || aura.Spell.Mechanic == WoWSpellMechanic.Slowed);
        }

        public bool HandlePvpTargetChange()
        {
            if (!_me.GotTarget)
            {
                Safe_StopMoving("No Current Combat Target");
                return true;
            }

            if (IsTargetingDisabled())
            {
                return false;
            }

#if MAKELOVE
            if ( _me.CurrentTarget.Dead && !_hasAchieveMakeLoveNotWarcraft && Safe_IsEnemyPlayer(_me.CurrentTarget))
            {
                _hasAchieveMakeLoveNotWarcraft = AchieveCompleted(247);
                if (!_hasAchieveMakeLoveNotWarcraft)
                {
                    Slog("/hug {0}", Safe_UnitName(_me.CurrentTarget));
                    RunLUA("RunMacroText(\"/hug\")");
                }
            }
#endif
            if (_pullTargGuid != _me.CurrentTarget.Guid)
            {
                Slog(">>> NEW TARGET: " + Safe_UnitName(_me.CurrentTarget) + "[" + _me.CurrentTarget.Level + "] at " + _me.CurrentTarget.Distance.ToString("F1") + " yds");
                PullInitialize();
            }

            WoWUnit newTarget = _me.CurrentTarget;
            WoWUnit chgTarget = null;
            while (chgTarget != newTarget)
            {
                chgTarget = newTarget;
                while (newTarget.CreatedByUnit != null)
                {
                    Dlog("HandlePvpTargetChange:  target {0} was created by {1}", Safe_UnitName(newTarget), Safe_UnitName(newTarget.CreatedByUnit));
                    newTarget = newTarget.CreatedByUnit;
                }

                while (newTarget.SummonedByUnit != null)
                {
                    Dlog("HandlePvpTargetChange:  target {0} was summoned by {1}", Safe_UnitName(newTarget), Safe_UnitName(newTarget.SummonedByUnit));
                    newTarget = newTarget.SummonedByUnit;
                }

                while (newTarget.OwnedByUnit != null)
                {
                    Dlog("HandlePvpTargetChange:  target {0} is owned by {1}", Safe_UnitName(newTarget), Safe_UnitName(newTarget.OwnedByUnit));
                    newTarget = newTarget.OwnedByUnit;
                }
            }

            if (newTarget == _me.CurrentTarget)
                return false;

            Safe_SetCurrentTarget(newTarget);
            return true;
        }

        public bool HandleHex()
        {
            if (!SpellManager.HasSpell("Hex"))
            {
                return false;
            }

            WoWSpell hex = SpellManager.Spells["Hex"];

            WoWUnit target = (from o in ObjectManager.ObjectList
                              let unit = o.ToUnit()
                              where unit != null 
                                  && unit.IsValid
                                  && (unit != _me.CurrentTarget || _me.Rooted)
                                  && unit.IsPlayer
                                  && Safe_IsHostile(unit)
                                  && !unit.IsPet
                                  && unit.HealthPercent > 1
                                  && (unit.IsCasting || unit.Class == WoWClass.Hunter)
                                  && 10 < _me.CombatDistance(unit) && _me.CombatDistance(unit) < hex.MaxRange 
                                  && !Blacklist.Contains(unit)
                                  && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                                  && unit.InLineOfSpellSight
                                  && FaceToUnit(unit)      //  && IsFacing( unit) 
                              orderby unit.Distance descending
                              select unit
                            ).FirstOrDefault();

            if (target == null)
            {
                return false;
            }

            return Safe_CastSpell(target, hex);
        }

        public static string BoolToYN(bool b)
        {
            return b ? "Y" : "N";
        }

        private void ShowStatus(string s)
        {
            try
            {
                _ShowStatusDetails(s);
            }
            catch
            {
                if ( IsGameUnstable())
                    Dlog("EXCEPTN {0}: <game unstable - exception in status>");
                else
                    Dlog("EXCEPTN {0}: h/m:{1:F1}%/{2:F1}%, combat:{3}, melee:{4}, range:{5}, rooted:{6}, immobile:{7}, silenced:{8}, fleeing:{9}",
                        s,
                        _me.HealthPercent,
                        _me.ManaPercent,
                        BoolToYN(_me.Combat),
                        _countMeleeEnemy,
                        _countRangedEnemy,
                        BoolToYN(_me.Rooted),
                        BoolToYN(_me.IsImmobilized()),
                        BoolToYN(_me.Silenced),
                        BoolToYN(IsFleeing(_me))
                        );
            }
        }

        private void _ShowStatusDetails(string s)
        {
#if     DEBUG_AURA_STACKS
            int buffIdx = 1;
            foreach (KeyValuePair<string,WoWAura> a in _me.ActiveAuras)
            {
                Dlog("   {0}: {1} ({2})", buffIdx, a.Key, a.Value.StackCount);
                buffIdx += 1;
            }
            /*
            Dlog("BUFFCHECK: LS:{0} WS:{1} ES:{2} MW:{3}", 
                _me.GetAuraStackCount( "Lightning Shield"),
                _me.GetAuraStackCount( "Water Shield"),
                _me.GetAuraStackCount( "Earth Shield"),
                _me.GetAuraStackCount( "Maelstrom Weapon")
                );
             */
#endif
            if (IsRAF())
            {
                Dlog("RAFSTAT {0} [-me-]: H={1:F1}% M={2:F1}% melee:{3},range:{4},mecombat:{5},memoving:{6},metarg:{7} at {8} yds with {9} box",
                    s,
                    _me.HealthPercent,
                    _me.ManaPercent,
                    _countMeleeEnemy,
                    _countRangedEnemy,
                    BoolToYN(_me.Combat),
                    BoolToYN(Safe_IsMoving()),
                    !_me.GotTarget ? "(null)" : Safe_UnitName(_me.CurrentTarget),
                    !_me.GotTarget ? "(null)" : _me.CurrentTarget.Distance.ToString("F1"),
                    !_me.GotTarget ? "(null)" : _me.CurrentTarget.CombatReach.ToString("F1")
                    );
                Dlog("RAFSTAT {0} [tank]: tnkH={1:F1}% tnkcombat:{2} tnkmoving:{3} at {4:F1} yds,tktarg:{5} at {6} yds with {7} box",
                    s,
                    GroupTank == null ? 0.0 : GroupTank.HealthPercent,
                    GroupTank == null ? "(null)" : BoolToYN(GroupTank.Combat),
                    GroupTank == null ? "(null)" : BoolToYN(GroupTank.IsMoving),
                    GroupTank == null ? "(null)" : _me.CombatDistance(GroupTank).ToString("F1"),
                    GroupTank == null || !GroupTank.GotTarget ? "(null)" : Safe_UnitName(GroupTank.CurrentTarget),
                    GroupTank == null || !GroupTank.GotTarget ? "(null)" : _me.CombatDistance(GroupTank.CurrentTarget).ToString("F1"),
                    GroupTank == null || !GroupTank.GotTarget ? "(null)" : GroupTank.CurrentTarget.CombatReach.ToString("F1")
                    );
            }
            else if (IsPVP())
            {
                Dlog("PVPSTAT {0} [me]:  h/m:{1:F1}%/{2:F1}%, combat:{3}, melee:{4}, range:{5}, rooted:{6}, immobile:{7}, silenced:{8}, fleeing:{9}",
                    s,
                    _me.HealthPercent,
                    _me.ManaPercent,
                    BoolToYN(_me.Combat),
                    _countMeleeEnemy,
                    _countRangedEnemy,
                    BoolToYN(_me.Rooted),
                    BoolToYN(_me.IsImmobilized()),
                    BoolToYN(_me.Silenced),
                    BoolToYN(IsFleeing(_me))
                    );
            }
            else
            {
                Dlog("GRDSTAT {0} [-me-]: h/m:{1:F1}%/{2:F1}%, combat:{3}, melee:{4}, range:{5}, rooted:{6}, immobile:{7}, silenced:{8}, fleeing:{9},memoving:{10},facing:{11},metarg:{12} at {13} yds with {14} box",
                    s,
                    _me.HealthPercent,
                    _me.ManaPercent,
                    BoolToYN(_me.Combat),
                    _countMeleeEnemy,
                    _countRangedEnemy,
                    BoolToYN(_me.Rooted),
                    BoolToYN(_me.IsImmobilized()),
                    BoolToYN(_me.Silenced),
                    BoolToYN(IsFleeing(_me)),
                    BoolToYN(Safe_IsMoving()),
                    BoolToYN(!_me.GotTarget ? false : IsFacing( _me.CurrentTarget)),
                    !_me.GotTarget ? "(null)" : Safe_UnitName(_me.CurrentTarget),
                    !_me.GotTarget ? "(null)" : _me.CurrentTarget.Distance.ToString("F1"),
                    !_me.GotTarget ? "(null)" : _me.CurrentTarget.CombatReach.ToString("F1")
                    );
            }

            if (!_me.GotTarget || IsHealer())
                ;
            else
                Dlog("        {0} [target]: {1} th:{2:F1}%, tdist:{3:F1} tlos:{4} tlosocd:{5} tcombat:{6} ttarget:{7} taggro:{8} tpetaggro:{9}",
                    s,
                    Safe_UnitName(_me.CurrentTarget),
                    _me.CurrentTarget.HealthPercent,
                    _me.CurrentTarget.Distance,
                    BoolToYN(_me.CurrentTarget.InLineOfSight),
                    BoolToYN(_me.CurrentTarget.InLineOfSpellSight),
                    BoolToYN(_me.CurrentTarget.Combat),
                    !_me.CurrentTarget.GotTarget ? "(null)" : Safe_UnitName(_me.CurrentTarget.CurrentTarget),
                    BoolToYN(_me.CurrentTarget.Aggro),
                    BoolToYN(_me.CurrentTarget.PetAggro)
                    );
        }

        private WoWPoint _pursuitStart;
        private readonly Stopwatch _pursuitTimer = new Stopwatch();

        public void PursuitBegin()
        {
            _pursuitTimer.Reset();
            _pursuitTimer.Start();
            _pursuitStart = _me.Location;
        }

        public bool InPursuit()
        {
            return _pursuitTimer.IsRunning;
        }

        public long PursuitTime
        { get { return _pursuitTimer.ElapsedMilliseconds; } }

        public WoWPoint PursuitOrigin
        { get { return _pursuitStart; } }



        /// <summary>
        /// combatChecks() verifies the minimum necessary elements for combat between 
        /// _me and _me.CurrentTarget.  This verifies:
        ///     _me is alive
        ///     != null
        ///     .CurrentTarget is alive
        ///     .CurrentTarget is not self
        ///     .CurrentTarget is not my pet/totems/etc
        ///     _me is facing the .CurrentTarget
        ///     
        /// if no current target OR if .CurrentTarget is dead and still in combat or in a battleground
        ///     switch to the best available target
        ///     
        /// </summary>
        /// <returns>true - combat can continue
        /// false - unable to fight current target</returns>
        private bool combatChecks()
        {
            WoWUnit add = null;

            if (IsGameUnstable())
                return false;

            // if I am dead
            if (!_me.IsAlive)
            {
                ReportBodyCount();
                return false;
            }

            // if my target is dead
            if (_me.GotTarget && !_me.CurrentTarget.IsAlive)
            {
                ReportBodyCount();
                if (!( _me.Combat || IsPVP()))
                    return false;
            }

            bool checkForTarget = true;
            // if no target, or target is dead, or targeting a friendly for some reason
            if (!_me.GotTarget)
                Dlog("combatChecks:  no current target, so checking for another");
            else if (!_me.CurrentTarget.IsAlive)
                Dlog("combatChecks:  target {0} is dead, so checking for another", Safe_UnitName(_me.CurrentTarget));
            else if (IsPVP() && !IsEnemy(_me.CurrentTarget))    // have to check for this rather IsFriendly due to same faction Arena fight bug
                Dlog("combatChecks:  pvp target {0} is not enemy, so checking for another", Safe_UnitName(_me.CurrentTarget));
            else if (!IsEnemy(_me.CurrentTarget))    // have to check for this rather IsFriendly due to same faction Arena fight bug
                Dlog("combatChecks:  target {0} is friendly, so checking for another", Safe_UnitName(_me.CurrentTarget));
            else
                checkForTarget = false;

            if ( checkForTarget )
            {
                if (_me.Combat || IsPVP() || IsRAF())
                {
                    if (_me.GotAlivePet && _me.Pet.GotTarget && _me.Pet.CurrentTarget.IsAlive && !Safe_IsFriendly(_me.Pet.CurrentTarget))
                    {
                        add = _me.Pet.CurrentTarget;
                        Slog(">>> SET PETS TARGET: {0}-{1}[{2}]", add.Class, Safe_UnitName(add), add.Level);
                    }
                    else if (FindBestTarget())
                    {
                        add = _me.CurrentTarget;
                    }
                    else if (!IsPVP() && FindAggroTarget())
                    {
                        add = _me.CurrentTarget;
                    }

                    if (add == null && !_me.IsInInstance)
                    {
                        // target an enemy totem (this just cleans up in some PVE fights)
                        List<WoWUnit> addList
                            = (from o in ObjectManager.ObjectList
                               where o is WoWUnit 
                               let unit = o.ToUnit()
                               where 
                                unit.Distance <= _maxDistForRangeAttack
                                && unit.Attackable
                                && unit.IsAlive
                                && !Safe_IsFriendly(unit)
                                && unit.InLineOfSpellSight
                                && unit.CreatedByUnitGuid != _me.Guid   // guard against my own totems being selected
                                && unit.CreatureType == WoWCreatureType.Totem
                               select unit
                                    ).ToList();

                        if (addList != null && addList.Any())
                        {
                            add = addList.First();
                            Slog("Setting to enemy totem: {0}-{1}[{2}]", add.Class, add.Name, add.Level);
                        }
                    }

                    if (add != null)
                    {
                        if (!Safe_SetCurrentTarget(add))
                            return false;
                    }
                }

                if (!_me.GotTarget)
                {
                    Dlog("No Current Target and can't find any nearby -- why still in Combat()");
                    return false;
                }
            }

            if (_me.GotTarget)
            {
                if (_me.CurrentTarget.IsMe)
                {
                    Dlog("Targeting myself -- clearing and bailing out of Combat()");
                    Safe_SetCurrentTarget(null);
                    return false;
                }

                if (_me.CurrentTarget.CreatedByUnitGuid == _me.Guid)
                {
                    Slog("? HB targeted my own: {0}, blacklisting ?", Safe_UnitName(_me.CurrentTarget));
                    Dlog("combatCheck:   targeted item   me.guid={0:X}   tgt:createdby={1:X}", _me.Guid, _me.CurrentTarget.CreatedByUnitGuid);
                    AddToBlacklist(_me.CurrentTarget.Guid);
                    Safe_SetCurrentTarget(null);
                    return false;
                }

                //				if (!WoWMovement.IsFacing)
                //					Safe_FaceTarget();
            }

            return true;
        }


        /// <summary>
        /// pvpChecks() 
        /// Inspects the current target and handles certain PvP specific issues
        ///     blacklists pet
        ///     purges player defensive ability (iceblock, divine shield)
        /// </summary>
        /// <returns>
        /// true - continue with fighting
        /// false- can't fight, find new target if needed
        /// </returns>
        private bool pvpChecks()
        {
            if (!_me.GotTarget)
                return false;

            if (IsPVP() || Safe_IsEnemyPlayer(_me.CurrentTarget))
            {
                // check for things we can't fight and should blacklist
                if (_me.CurrentTarget.IsPet)
                {
                    Slog("PVP: Blacklisting pet " + Safe_UnitName(_me.CurrentTarget));
                    Blacklist.Add(_me.CurrentTarget.Guid, TimeSpan.FromMinutes(5));
                    Safe_SetCurrentTarget(null);
                    return false;
                }

                // test, if in battleground and someone is out of line of sight, blacklist for 5 seconds
#if COMMENT
				if (!_me.CurrentTarget.InLineOfSight)
				{
					Slog("PVP: Target not in LoS, blacklisting for 2 seconds");
					Blacklist.Add(_me.CurrentTarget.Guid, TimeSpan.FromSeconds(2));
                    Safe_SetCurrentTarget(null);
                    return false;
				}
#endif

                // _me.CurrentTarget.GetBuffs(true);   // refresh buffs for checking need for blacklist or purge

                if (_me.CurrentTarget.HasAura("Divine Shield"))
                {
                    Slog("PVP: Palidan popped Divine Shield, blacklisted 10 secs");
                    Blacklist.Add(_me.CurrentTarget.Guid, TimeSpan.FromSeconds(10));
                    Safe_SetCurrentTarget(null);
                    return false;
                }

                if (_me.CurrentTarget.HasAura("Ice Block"))
                {
                    Slog("PVP: Mage popped Iceblock, blacklisted 10 secs");
                    Blacklist.Add(_me.CurrentTarget.Guid, TimeSpan.FromSeconds(10));
                    Safe_SetCurrentTarget(null);
                    return false;
                }

                if (_me.CurrentTarget.Shapeshift == ShapeshiftForm.SpiritOfRedemption)
                {
                    Slog("PVP: Priest died and popped Spirit of Redemption, blacklisted 15 secs");
                    Blacklist.Add(_me.CurrentTarget.Guid, TimeSpan.FromSeconds(15));
                    Safe_SetCurrentTarget(null);
                    return false;
                }

#if PURGE_IS_DIFFERENT_NOW
                if (SpellManager.HasSpell("Purge") && SpellManager.Spells["Purge"].Cooldown)
                {
                    Dlog("PVP:  Purge on cooldown, skipping buff tests");
                }
                else if (_me.CurrentTarget.Auras.ContainsKey("Presence of Mind") && Purge(null))
                {
                    Slog("PVP: mage had Presence of Mind, purging");
                }
                else if (_me.CurrentTarget.Auras.ContainsKey("Blessing of Protection") && Purge(null))
                {
                    Slog("PVP: target has Blessing of Protection, purging");
                }
                else if (_me.CurrentTarget.Auras.ContainsKey("Avenging Wrath") && Purge(null))
                {
                    Slog("PVP: paladin used Avenging Wrath, purging");
                }

                else if (_me.CurrentTarget.Auras.ContainsKey("Power Word: Shield") && Purge(null))
                {
                    Slog("PVP: priest used Power Word: Shield, purging");
                }
                else if (_me.CurrentTarget.Auras.ContainsKey("Fear Ward") && Purge(null))
                {
                    Slog("PVP: target has Fear Ward, purging");
                }
#endif
                if (typeShaman == ShamanType.Enhance && _me.CurrentTarget.Distance > (_maxDistForRangeAttack + 5))
                {
                    Dlog("pvpChecks:  target at range so checking if better target nearby");
                    if ( !FindBestTarget())
                    {
                        if (_me.Mounted)
                            Dlog("pvpChecks:  target at range and nothing better, so staying mounted and pursuing");
                        else
                        {
                            if (InGhostwolfForm())
                                Dlog("pvpChecks:  target at range and nothing better, so closing with Ghost Wolf");
                            else if ( cfg.UseGhostWolfForm )
                            {
                                Dlog("pvpChecks:  target at range and nothing better, so casting Ghost Wolf to close ground");
                                GhostWolf();
                            }
                        }
                    }
                }

            }

            return true;
        }


        private void AutoAttack()
        {
            Log(Color.DodgerBlue, "*Auto-Attack");
            // RunLUA("StartAttack()");
            Lua.DoString("StartAttack()");
        }

        private void StopAutoAttack()
        {
            Log(Color.DodgerBlue, "*Stop Auto-Attack");
            // RunLUA("StopAttack()");
            Lua.DoString("StopAttack()");
        }

        private bool CastCombatSpecials()
        {
            bool cast = false;

            if (AllowNonHealSpells() && _me.Combat )
            {
                if (IsPVP() || IsRAF() || IsFightStressful() )
                {
                    /*
                                        if (_me.Auras.ContainsKey("Elemental Mastery"))
                                            ;
                                        else 
                     */
                    if (_me.IsHorde && _me.Auras.ContainsKey("Bloodlust"))
                        ;
                    else if (_me.IsAlliance && _me.Auras.ContainsKey("Heroism"))
                        ;
                    else if (typeShaman == ShamanType.Resto && _tier13CountResto >= 4 && _me.Auras.ContainsKey("Spiritwalker's Grace"))
                        ;
                    else if (_me.Race == WoWRace.Troll && _me.Auras.ContainsKey("Berserking"))
                        ;
                    else if ( _me.Race == WoWRace.Orc && _me.Auras.ContainsKey("Blood Fury"))
                        ;
                    else if (_me.Auras.ContainsKey("Lifeblood"))
                        ;
                    else if (typeShaman == ShamanType.Elemental && _me.Auras.ContainsKey("Elemental Mastery"))
                        ;
                    else
                    {
                        // Elemental Mastery is cast a part of rotation
                        if (!cast)
                            cast = BloodlustHeroism();   // horde and alliance shaman
                        if (!cast && typeShaman == ShamanType.Resto && _tier13CountResto >= 4)
                            cast = SpiritwalkersGrace();
                        if (!cast)
                            cast = Berserking();  // trolls
                        if (!cast)
                            cast = BloodFury();   // orcs
                        if (!cast)
                            cast = Lifeblood();
                    }
                }
            }

            return cast;
        }

        private bool LightningBolt()
        {
            if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if (IsImmunneToNature(_me.CurrentTarget))
                Dlog("skipping Lightning Bolt since {0}[{1}] is immune to Nature damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else if (!Safe_IsMoving() || _hasGlyphOfUnleashedLightning || IsSpiritWalkersGraceActive())
                return Safe_CastSpell(_me.CurrentTarget, "Lightning Bolt");

            return false;
        }

        private static int EnemyCountInAOE(WoWUnit target, double distRadius)
        {
            int enemyCount = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            if (target != null)
            {
                try
                {
                    distRadius *= distRadius;
                    if (!IsPVP())
                    {
                        enemyCount = (from unit in AllEnemyMobs 
                                      where unit.Location.DistanceSqr(target.Location) <= distRadius 
                                            && unit.Combat
                                            && !IsMeOrMyGroup(unit)
                                            && IsTargetingMeOrMyGroup(unit)
                                            && !Blacklist.Contains(unit.Guid)
                                      orderby unit.CurrentHealth ascending
                                      select unit
                                     ).Count();
                    }
                    else
                    {
                        enemyCount = (from unit in AllEnemyMobs 
                                      where unit.Location.DistanceSqr(target.Location) <= distRadius 
                                            && unit.IsPlayer 
                                            && !unit.IsPet
                                            && !Blacklist.Contains(unit.Guid)
                                      orderby unit.CurrentHealth ascending
                                      select unit
                                     ).Count();
                    }

                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("HB EXCEPTION in EnemyCountInAOE()");
                    Logging.WriteException(e);
                }
            }

            Dlog("EnemyCountInAOE(): found {0} in {1} ms", enemyCount, timer.ElapsedMilliseconds);
            return enemyCount;
        }

		private static bool WillChainLightningHop(WoWUnit target)
		{
			return 2 >= EnemyCountInAOE(target, 12);
		}

#if NOT_RIGHT_NOW
		private static bool UseEarthquake(WoWUnit target)
		{
			return 3 >= EnemyCountInAOE(target, 8);
		}
#endif
        private bool ChainLightning()
        {
            if (!SpellManager.HasSpell("Chain Lightning"))
                ;
            else if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if ( !Safe_IsMoving() || IsSpiritWalkersGraceActive())
                return Safe_CastSpell( _me.CurrentTarget, "Chain Lightning" );

            return false;
        }

        private bool ShamanisticRage()
        {
            if (SpellManager.HasSpell("Shamanistic Rage"))
                return Safe_CastSpell(_me, "Shamanistic Rage");

            return false;
        }

        private bool EarthShock()
        {
            if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if (!SpellManager.HasSpell("Earth Shock"))
                ;
            else if (!_me.CurrentTarget.IsPlayer && IsImmunneToNature(_me.CurrentTarget))
                Dlog("skipping Earth Shock since {0}[{1}] is immune to Nature damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else
                return Safe_CastSpell(_me.CurrentTarget, "Earth Shock");

            return false;
        }

        private bool CanAnElemBuyAnEarthShock()
        {
            if (!_me.GotTarget)
                return false;

            bool permitEarthShock = false;
            WoWAura fsa = _me.CurrentTarget.GetAuraCreatedByMe( "Flame Shock");
            if (fsa == null)
                permitEarthShock = !SpellManager.HasSpell("Flame Shock");
            else
            {
                WoWSpell fss = SpellManager.Spells["Flame Shock"];
                permitEarthShock = fsa.TimeLeft.TotalMilliseconds > 6000;
                Dlog("CanBuyEarthShock:  flame shock DoT left={0}", fsa.TimeLeft.TotalMilliseconds);
            }

            return permitEarthShock;
        }

        private bool UnleashFlameCheck()
        {
            if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if (!SpellManager.HasSpell("Flame Shock"))
                ;
            else if (!_me.CurrentTarget.IsPlayer && IsImmunneToFire(_me.CurrentTarget))
                Dlog("UnleashFlameCheck:  skipping Flame Shock since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else if (!_me.Auras.ContainsKey("Unleash Flame"))
                Dlog("UnleashFlameCheck:  missing Unleash Flame debuff");
            else
                return Safe_CastSpell(_me.CurrentTarget, "Flame Shock");

            return false;
        }

        private bool UnleashElements()
        {
            if (!AllowNonHealSpells() || !SpellManager.HasSpell("Unleash Elements"))
                ;
            else if (!DoWeaponsHaveImbue())
                Dlog("UnleashElements:  skipping cast until weapons imbued");
            else
                return Safe_CastSpell(_me.CurrentTarget, "Unleash Elements");

            return false;
        }

        private bool FlameShock()
        {
            if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if (!SpellManager.HasSpell("Flame Shock"))
                ;
            else if (!_me.CurrentTarget.IsPlayer && IsImmunneToFire(_me.CurrentTarget))
                Dlog("skipping Flame Shock since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else if (_me.CurrentTarget.Auras.ContainsKey("Flame Shock"))
                Dlog("FlameShock:  target already has DoT");
            else
                return Safe_CastSpell(_me.CurrentTarget, "Flame Shock");

            return false;
        }

        private bool FlameShockRenew()
        {
            if (!SpellManager.HasSpell("Flame Shock"))
                ;
#if IMMUNITY_CHECKING
            else if (!_me.CurrentTarget.IsPlayer && IsImmunneToFire(_me.CurrentTarget))
                Dlog("skipping Flame Shock since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
#endif
            else
            {
                // following code checks to make sure that DoT won't 
                // ... fall off before Lava Burst cast completes
                WoWAura fs = _me.CurrentTarget.GetAuraCreatedByMe( "Flame Shock");
                if (fs != null)
                {
#if CLIP_USING_LvB_CAST_TIME
                    if (!SpellManager.HasSpell("Lava Burst"))
                        return false;

                    WoWSpell lvb = SpellManager.Spells["Lava Burst"];
                    if ((200 + lvb.CastTime) < fs.TimeLeft.TotalMilliseconds)
                        return false;
#else
                    if (fs.TimeLeft.TotalMilliseconds >= 2500)
                        return false;
#endif
                    Dlog("FlameShock:  DoT only has {0} ms left, so renewing", fs.TimeLeft.TotalMilliseconds);
                }

                return Safe_CastSpell(_me.CurrentTarget, "Flame Shock");
            }
            return false;
        }


        private bool FrostShock()
        {
            if (!HaveValidTarget() && !AllowNonHealSpells())
                ;
            else if (!SpellManager.HasSpell("Frost Shock"))
                ;
            else if (!_me.CurrentTarget.IsPlayer && IsImmunneToFrost(_me.CurrentTarget))
                Dlog("skipping Frost Shock since {0}[{1}] is immune to Frost damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else if (_me.CurrentTarget.Auras.ContainsKey("Frost Shock"))
                ;
            else
                return Safe_CastSpell(_me.CurrentTarget, "Frost Shock");

            return false;
        }

        /*
         * Summary:  determines the best Shock spell opener to
         * use.  This is for use during pull only.  
         */
        private bool ShockOpener()
        {
            bool bCast = false;

            if (_me.GotTarget && !IsHealerOnly())
            {
                if (!bCast)
                    bCast = UnleashElements();

                if (!bCast && (_me.CurrentTarget.IsPlayer && _me.CurrentTarget.Class != WoWClass.Rogue))
                    bCast = FrostShock();

                if (!bCast)
                    bCast = FlameShock();

                if (!bCast)
                    bCast = EarthShock();

                if (!bCast)
                    bCast = FrostShock();
            }

            return bCast;
        }

        private bool BestInstantAttack()
        {
            bool knowFrost = SpellManager.HasSpell("Frost Shock");
            bool knowEarth = SpellManager.HasSpell("Earth Shock");
            bool knowFire = SpellManager.HasSpell("Flame Shock");
            
            if (priorityPurge == ConfigValues.SpellPriority.High && Purge(null))
                return true;

            if (knowFire && _me.CurrentTarget.Class == WoWClass.Rogue && FlameShock())
                return true;

            if (knowFrost && _me.CurrentTarget.IsPlayer && FrostShock())
                return true;

            if (knowEarth && FulminationCheck())
                return true;

            if (IsPVP() && IsWeaponImbuedWithDPS() && UnleashElements())
                return true;

            if (knowFire && FlameShock())
                return true;

            if (knowEarth && EarthShock())
                return true;

            if (MaelstromCheck())
                return true;

            if (IsWeaponImbuedWithDPS() && UnleashElements())
                return true;

            if (RocketBarrage())
                return true;

            //      if (knowFrost && FrostShock())
            //      return true;

            return false;
        }

        // note:  unlike other offensive spells, this one works on a provided
        // target rather than .CurrentTarget so we can CC someone other than
        // person directly engaged in combat with
        private bool Hex(WoWUnit u)
        {
            //            if (!AllowNonHealSpells())        don't care since very low mana cost
            //                ;
            if (u == null)
                ;
            else if (!SpellManager.HasSpell("Hex"))
                ;
            else if (  u.IsPlayer && u.Class == WoWClass.Druid && u.Shapeshift != ShapeshiftForm.Normal )
                Dlog("Hex:  cannot hex {0} {1} in form={2}", u.Class, Safe_UnitName(u), u.Shapeshift.ToString());
            else if (!u.IsPlayer && u.CreatureType != WoWCreatureType.Humanoid && u.CreatureType != WoWCreatureType.Beast)
                Dlog("Hex:  cannot hex, target {0} is type={1}; npc must be a humanoid or beast", Safe_UnitName(u), u.CreatureType);
            else if (u.IsAuraPresent("Hex"))
                Dlog("Hex:  target {0} already a Frog", Safe_UnitName(u));
            else
                return Safe_CastSpell(u, "Hex");

            return false;
        }

        private bool BindElemental(WoWUnit u)
        {
            if (u == null)
                ;
            else if (u.IsPlayer)
                ;
            else if (u.CreatureType != WoWCreatureType.Elemental)
                ;
            else if (!SpellManager.HasSpell("Bind Elemental"))
                ;
            else if (u.IsAuraPresent("Bind Elemental"))
                Dlog("BindElemental:  target {0} already Bound", Safe_UnitName(u));
            else
                return Safe_CastSpell(u, "Bind Elemental");

            return false;
        }

        private bool BindWaterElemental()
        {
            if (!SpellManager.HasSpell("Bind Elemental"))
                return false;

            WoWUnit waterElem = 
                (from o in ObjectManager.ObjectList 
                 where o is WoWUnit && o.Entry == 510 && o.Distance < 30 
                 let unit = o.ToUnit() 
                 where unit.IsHostile 
                 select unit).FirstOrDefault();
            if (waterElem != null && FaceToUnit(waterElem))
                return BindElemental(waterElem);

            return false;
        }

        private bool PrimalStrike()
        {
            const int PRIMAL_STRIKE = 73899;

            if (!HaveValidTarget() || !AllowNonHealSpells())
                ;
            else if (SpellManager.HasSpell("Stormstrike"))      // never use if we know Stormstrike
                ;                                               // .. since they share a cooldown
            else if (!SpellManager.HasSpell(PRIMAL_STRIKE))
                ;
            else if (!CurrentTargetInMeleeDistance())
                ;
            else if (IsSpellBlacklisted(PRIMAL_STRIKE))
                ;
            else if ( Safe_CastSpell( _me.CurrentTarget, PRIMAL_STRIKE ))
                return true;

            return false;
        }

        private bool Stormstrike()
        {
            if (!SpellManager.HasSpell("Stormstrike"))
                ;
            else if (IsImmunneToNature(_me.CurrentTarget))
                Dlog("skipping Stormstrike since {0}[{1}] is immune to Nature damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else
                return Safe_CastSpell(_me.CurrentTarget, "Stormstrike");

            return false;
        }

        private bool IsStormstrikeNeeded()
        {
            if (SpellManager.HasSpell("Stormstrike") && AllowNonHealSpells())
            {
                if (HaveValidTarget() && !_me.CurrentTarget.HasAura("Stormstrike"))
                {
                    if (CurrentTargetInMeleeDistance())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool LavaLash()
        {
            if (!SpellManager.HasSpell("Lava Lash"))
                ;
            else if (IsImmunneToFire(_me.CurrentTarget))
                Dlog("skipping Lava Lash since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else 
                return Safe_CastSpell(_me.CurrentTarget, "Lava Lash");

            return false;
        }

        private bool LavaBurst()
        {
            if (!SpellManager.HasSpell("Lava Burst"))
                ;
            else if (IsImmunneToFire(_me.CurrentTarget))
                Dlog("skipping Lava Burst since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else if ( !Safe_IsMoving() || IsSpiritWalkersGraceActive())
                return Safe_CastSpell(_me.CurrentTarget, "Lava Burst");

            return false;
        }

        private bool ElementalMastery()
        {
            if (!HaveValidTarget() && !AllowNonHealSpells())
                ;
            else if (IsRAF() && !cfg.RAF_UseCooldowns)
                ;
            else if (IsPVP() && !cfg.PVP_UseCooldowns)
                ;
            else if (!SpellManager.HasSpell("Elemental Mastery"))
                ;
            else
                return Safe_CastSpell(_me, "Elemental Mastery");

            return false;
        }

        private bool Thunderstorm()
        {
            if (IsRAF() && !cfg.RAF_UseThunderstorm)
                ;
            else if (!SpellManager.HasSpell("Thunderstorm"))
                ;
            else
                return Safe_CastSpell( _me.CurrentTarget, "Thunderstorm" );

            return false;
        }

        private bool FireNova()
        {
            if (!HaveValidTarget() && !AllowNonHealSpells())
                ;
            else if (!SpellManager.HasSpell("Fire Nova"))
                ;
#if PRE_410_METHOD
            else if (!(TotemExist(TotemId.MAGMA_TOTEM) || TotemExist(TotemId.FLAMETONGUE_TOTEM) || TotemExist(TotemId.FIRE_ELEMENTAL_TOTEM)))
                Dlog("Magma/Flametongue/Fire elemental totem doesn't exist, Fire Nova not cast");
#else
//            else if ( null != GetAuraCreatedByMe( _me.CurrentTarget, "Flame Shock"))
//                Dlog("FireNova:  current target missing Flame Shock");
#endif
            else if (_countFireNovaEnemy < 3)
                Dlog("FireNova:  not cast, only {0} fire nova hits found", _countFireNovaEnemy );
//            else if (IsImmunneToFire(_me.CurrentTarget))
//                Dlog("FireNova:  skipping since {0}[{1}] is immune to Fire damage", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Entry);
            else
            {
                Dlog("FireNova:  found {0} fire nova hits", _countFireNovaEnemy );
                return Safe_CastSpell( _me.CurrentTarget, "Fire Nova");
            }

            return false;
        }

        const int MAELSTROM_WEAPON = 51530;

        private bool MaelstromCheck()
        {
            bool castSpell = false;

            if (!_hasTalentMaelstromWeapon)
                return castSpell;

            if (IsPVP() && cfg.PVP_HealOnMaelstrom )
            {
                return MaelstromCheckHealPriority();
            }
            else if (!IsRAF() && cfg.PVE_HealOnMaelstrom )
            {
                if (IsFightStressful())
                {
                    return MaelstromCheckHealPriority();
                }
            }
            else if (cfg.RAF_CombatStyle != ConfigValues.RafCombatStyle.CombatOnly && _me.HealthPercent < cfg.RAF_GroupOffHeal)
            {
                if ( MaelstromCheckHealPriority())
                    return true;
            }

            if ( IsSpellBlacklisted(MAELSTROM_WEAPON))
                return castSpell;

            // RAF specific behavior
            uint stacks = GetMaelstromCount();
            if (  stacks >= 5)
            {
                Log("^Maelstrom Attack @ {0} stks", stacks );
#if NO_SAFE_CAST_WAIT
                while (!IsGameUnstable() && _me.CurrentHealth > 1 && !SpellManager.CanCast("Lightning Bolt"))
                {
                    Dlog("MaelstromCheck:  waiting to cast some Lightning");
                    Sleep(25);
                }
#endif
                if (_countAoe12Enemy > 1 && SpellManager.HasSpell("Chain Lightning"))
                    castSpell = Safe_CastSpell(_me.CurrentTarget, "Chain Lightning");

                if (!castSpell)
                    castSpell = Safe_CastSpell(_me.CurrentTarget, "Lightning Bolt");

                if (castSpell)
                    AddSpellToBlacklist(MAELSTROM_WEAPON);
            }

            return castSpell;
        }

        private bool MaelstromCheckHealPriority()
        {
            bool castSpell = false;

            if ( !_hasTalentMaelstromWeapon)
                ;
            else if (Blacklist.Contains(MAELSTROM_WEAPON))
                ;
            else if (!IsCastingOrGCD())
            {
                uint timeLeft;
                uint stackCount = GetMaelstromCount( out timeLeft);
                if ( stackCount < 5 )
                {
                    if ( stackCount < 4 || _me.HealthPercent > cfg.EmergencyHealthPercent)
                    {
                        return false;
                    }
                }

                bool keepForHealing = false;
                if (IsPVP())
                    keepForHealing = cfg.PVP_HealOnMaelstrom;
                else if (!IsRAF())
                    keepForHealing = cfg.PVE_HealOnMaelstrom;

                WoWPlayer heal = null;
                if (!keepForHealing)
                {
                    heal = _me.HealthPercent < cfg.NeedHealHealthPercent ? _me.ToPlayer() : null;
                }
                else if (_me.HealthPercent < 75)
                {
                    heal = _me.ToPlayer();
                }
                else
                {
                    heal =
                        (from p in GroupMembers
                         where
                            _me.CombatDistance(p) < 38
                            && p.IsAlive
                            && p.HealthPercent < 75
                            && p.InLineOfSpellSight
                         orderby p.HealthPercent ascending
                         select p
                         ).FirstOrDefault();
                }

                if (heal != null)
                {

                    WaitForCurrentCastOrGCD();
                    if (!SpellManager.CanCast("Healing Wave"))
                        Dlog("MaelstromCheckHealPriority:  have a heal target but cant cast heal?");
                    else
                    {
                        Log("^Maelstrom Heal @ {0} stacks", stackCount);
                        if (heal.HealthPercent > 60 && SpellManager.HasSpell("Healing Rain") && SpellManager.CanCast("Healing Rain") && WillHealingRainCover(heal, 2))
                        {
                            if (Safe_CastSpell( heal, "Healing Rain"))
                            {
                                WaitForCurrentCastOrGCD();
                                if (LegacySpellManager.ClickRemoteLocation(heal.Location))
                                    return true;

                                Dlog("MaelstromCheckHealPriority:  ^Ranged AoE Click FAILED:  cancelling Healing Rain");
                                SpellManager.StopCasting();
                            }
                        }

                        if (SpellManager.HasSpell("Greater Healing Wave") )
                            castSpell = Safe_CastSpell(heal, "Greater Healing Wave");
                        if (!castSpell && SpellManager.HasSpell("Healing Surge") )
                            castSpell = Safe_CastSpell(heal, "Healing Surge");
                        if (!castSpell)
                            castSpell = Safe_CastSpell(heal, "Healing Wave");

                    if (castSpell)
                        AddSpellToBlacklist(MAELSTROM_WEAPON);

                    return castSpell;
                    }
                }

                if (IsImmunneToNature(_me.CurrentTarget))
                    return false;

                if (!keepForHealing)
                    Log("^Maelstrom Attack @ 5 stks");
                else if (timeLeft > 2500)
                    return false;   // save stacks
                else
                    Log("^Maelstrom Attack @ 5 stks since only {0} ms left", timeLeft);

                if ((IsPVP() || _countAoe12Enemy > 1) && SpellManager.HasSpell("Chain Lightning"))
                    castSpell = Safe_CastSpell(_me.CurrentTarget, "Chain Lightning");

                if (!castSpell)
                    castSpell = Safe_CastSpell(_me.CurrentTarget, "Lightning Bolt");
            }

            if (castSpell)
                AddSpellToBlacklist(MAELSTROM_WEAPON);

            return castSpell;
        }

        private bool HealMySelfInstantOnly()
        {
            if (_me.Stunned || _me.Silenced)
            {
                return false;
            }

            bool castSpell = false;
            if (_me.HealthPercent < cfg.InstantHealPercent)
            {
                castSpell = GiftOfTheNaaru();
            }

            if (!castSpell && _me.HealthPercent < cfg.TrinkAtHealth)
            {
                castSpell = UseItem(CheckForItem(ITEM_HEALTHSTONE));   // Healthstone
                if (!castSpell)
                    castSpell = UseHealthPotionIfAvailable();

                if (!castSpell && _hasGlyphOfStoneClaw)
                {
                    if (!TotemExist(TotemId.EARTH_ELEMENTAL_TOTEM) && !TotemExist(TotemId.STONECLAW_TOTEM))
                    {
                        if (!IsCastingOrGCD() && !SpellHelper.OnCooldown((int) TotemId.STONECLAW_TOTEM))
                        {
                            Log("^Shaman Bubble:  casting Glyphed Stoneclaw Totem");
                            castSpell = TotemCast(TotemId.STONECLAW_TOTEM);
                        }
                    }
                }
            }

            return castSpell;
        }

#if WAIT_FOR_SEARING_FLAMES
		private bool SearingFlamesCheck()
		{
			if (!_hasTalentImprovedLavaLash)
				;
			else if (!combatChecks())
				;
			else if (!HaveValidTarget())
				;
			else if (!SpellManager.HasSpell("Lava Lash"))
				;
			else if (!CurrentTargetInMeleeDistance())
				;
			else if (IsImmunneToFire(_me.CurrentTarget))
				Slog("SearingFlames: skipping Lava Lash because mob is Fire immune");
			else
			{
				uint stackCount = GetAuraStackCount( _me.CurrentTarget, "Searing Flames");
				Dlog( "SearingFlameCheck: found {0} stacks", stackCount);
				if (stackCount >= 5)
				{
					Slog("^Searing Flames @ " + stackCount + " stks");
					if (Safe_CastSpell("Lava Lash"))
					{
						Dlog("CombatElem: Lava Lash cast so no further attacks this pass");
						return true;
					}
				}
			}

			return false;
		}
#endif

        private bool CanMoveWhileCasting()
        {
            // const int LIGHTNING_BOLT = 403;
            return IsSpiritWalkersGraceActive(); // || (_hasGlyphOfUnleashedLightning && _me.CastingSpellId == LIGHTNING_BOLT);
        }

        private bool IsSpiritWalkersGraceActive()
        {
            const int SPIRITWALKERS_GRACE = 79206;
            return _me.IsAuraPresent( SPIRITWALKERS_GRACE);
        }

        private bool FulminationCheck()
        {
            const int EARTH_SHOCK = 8042;

            if (!_hasTalentFulmination)
                ;
            else if (!HaveValidTarget())
                ;
            else if (IsSpellBlacklisted(EARTH_SHOCK))
                Dlog("FulminationCheck:  spell blacklisted temporarily");
            else if (SpellManager.HasSpell(EARTH_SHOCK))
            {
                uint stackCount = GetLightningShieldCount();
                Dlog("FulminationCheck:  Lightning Shields stack count is {0}", stackCount);
                if (stackCount >= 7)
                {
                    if (IsImmunneToNature(_me.CurrentTarget))
                        Slog("FulminationCheck: skipping Earth Shock because mob is Nature immune");
                    else if (!SpellManager.CanCast(EARTH_SHOCK))
                        Dlog("Earth Shock on Cooldown... waiting on Fulmination cast", stackCount);
                    else
                    {
                        Slog("^Fulmination at {0} stacks", stackCount);
                        if (Safe_CastSpell(_me.CurrentTarget, EARTH_SHOCK))
                        {
                            AddSpellToBlacklist(EARTH_SHOCK);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool InterruptEnemyCast()
        {
            int MaxEnemyDistance = 0;
            int MaxNotFacingDistance = 0;
            WoWUnit target = null;
            WoWSpell windShear = null;
            WoWSpell warStomp = null;
            WoWSpell thunderStorm = null;

            if (cfg.InterruptStyle == ConfigValues.SpellInterruptStyle.None)
                return false;

            if (MeSilenced())
                return false;

#if WOLVES_BASH_FOR_INTERRUPT
            // currently not doing this because we have to use
            // .. LUA calls for pet spell management and its
            // .. and we want these to be fast and light

            if (typeShaman == ShamanType.Enhance && _me.GotAlivePet)
            {
                WoWUnit pt = _me.Pet.CurrentTarget;
                if (pt != null
                    && IsEnemy(pt)
                    && _me.Pet.Location.DistanceSqr(pt.Location) < 25
                    && pt.CreatedByUnit == null
                    && IsTargetCastInterruptible(pt))
                {
                    Log("^Interrupt {0} casting spell {1} #{2}", Safe_UnitName(target), target.CastingSpell.Name, target.CastingSpell.Id);
                    asa
                }
            }
#endif
            if (SpellManager.HasSpell("Wind Shear"))
            {
                windShear = SpellManager.Spells["Wind Shear"];
                if (!SpellHelper.OnCooldown(windShear))
                    MaxEnemyDistance = Math.Max(MaxEnemyDistance, (int)windShear.MaxRange);
                else
                    windShear = null;
            }

            if (!Safe_IsMoving() && !IsCasting() && SpellManager.HasSpell("War Stomp"))
            {
                warStomp = SpellManager.Spells["War Stomp"];
                if (!SpellHelper.OnCooldown(warStomp))
                {
                    MaxEnemyDistance = Math.Max(MaxEnemyDistance, 8);
                    MaxNotFacingDistance = Math.Max(MaxNotFacingDistance, 8);
                }
                else
                    warStomp = null;
            }

            if (IsPVP() && !IsCasting() && SpellManager.HasSpell("Thunderstorm"))
            {
                thunderStorm = SpellManager.Spells["Thunderstorm"];
                if (!SpellHelper.OnCooldown((thunderStorm)))
                {
                    MaxEnemyDistance = Math.Max(MaxEnemyDistance, 10);
                    MaxNotFacingDistance = Math.Max(MaxNotFacingDistance, 10);
                }
                else
                    thunderStorm = null;
            }

            if (MaxEnemyDistance == 0)
            {
                Dlog("InterruptEnemyCast:  both Wind Shear and War Stomp on cooldown, cannot cast");
                return false;
            }

            if (cfg.InterruptStyle == ConfigValues.SpellInterruptStyle.CurrentTarget)
            {
                if (_me.CurrentTarget != null && IsTargetCastInterruptible(_me.CurrentTarget) && IsEnemy(_me.CurrentTarget) && _me.CurrentTarget.Distance <= MaxEnemyDistance)
                {
                    target = _me.CurrentTarget;
                }
            }
            else if (cfg.InterruptStyle == ConfigValues.SpellInterruptStyle.FocusTarget)
            {
                if (_me.FocusedUnit != null && IsTargetCastInterruptible(_me.FocusedUnit) && IsEnemy(_me.FocusedUnit) && _me.FocusedUnit.Distance <= MaxEnemyDistance)
                {
                    target = _me.FocusedUnit;
                }
            }
            else // ConfigValues.SpellInterruptStyle.All
            {

                target = (from o in ObjectManager.ObjectList
                          where o is WoWUnit
                          let unit = o.ToUnit()
                          where
                                unit != null
                                && IsEnemy(unit)
                                && unit.Distance < MaxEnemyDistance
                                && unit.CreatedByUnit == null
                                && unit.HealthPercent > 5
                                && ((unit.Combat && IsTargetingMeOrMyGroup(unit)) || IsPVP())
                                && IsTargetCastInterruptible(unit)
                                && unit.InLineOfSpellSight
                                && (unit.Distance < MaxNotFacingDistance || IsFacing(unit))
                          orderby unit.CurrentHealth ascending
                          select unit
                         ).FirstOrDefault();
            }

            if (target != null && target.CastingSpell != null)
            {
                Log("^Interrupt {0} casting spell {1} #{2}", Safe_UnitName(target), target.CastingSpell.Name, target.CastingSpell.Id);
                if (target.Distance > MaxNotFacingDistance && windShear != null)
                {
                    if (!Safe_CanCastSpell( target, windShear ))  // !SpellManager.CanCast(windShear, target, false, false, cfg.AccountForLag))
                        Dlog("InterruptEnemyCast:  spellmanager says I cant cast Wind Shear right now");
                    else if (!IsFacing(target))
                        Dlog("InterruptEnemyCast:  not facing target for Wind Shear");
                    else if (!SpellManager.Cast(windShear, target))
                        Dlog("InterruptEnemyCast:  spell cast failed");
                    else
                    {
                        string info = string.Format("*{0} on {1} at {2:F1} yds at {3:F1}%", windShear.Name, Safe_UnitName(target), _me.CombatDistance(target), target.HealthPercent);
                        Log(Color.DodgerBlue, info);
                    }

                    return false;       // return false because Wind Shear doesn't have GCD
                }

                if (target.Distance <= 10 && thunderStorm != null && Safe_CastSpell(target, thunderStorm))
                {
                    return true;
                }

                if (target.Distance <= 8 && warStomp != null && Safe_CastSpell(target, warStomp))
                {
                    return true;
                }

                Dlog("InterruptEnemyCast:  failed to interrupt", Safe_UnitName(target), _me.CombatDistance(target), target.CastingSpell == null ? "(null)" : target.CastingSpell.Name);
            }

            return false;
        }

        public static bool HandlePvpGroundingTotem()
        {
            const int MaxEnemyRange = 50;   // spell range 40 + friendly possibly being away 10 

            var enemy = (from o in ObjectManager.ObjectList
                         where o is WoWPlayer && o.Distance <= MaxEnemyRange
                         let p = o.ToPlayer()
                         where IsPvpGroundingTotemNeeded(p)
                         select p).FirstOrDefault();
            if (enemy == null)
                return false;

            if (!HasTotemSpell(TotemId.GROUNDING_TOTEM))
                return false;

            if (IsCastingOrGCD())
            {
                Dlog("HandlePvpGroundingTotem: suppress grounding totem check Casting={0} GCD={1}", GCD(), IsCasting());
                return false;
            }

            if (!Safe_CanCastSpell(_me, (int)TotemId.GROUNDING_TOTEM))
            {
                Dlog("HandlePvpGroundingTotem:  spell manager says no Grounding Totem right now");
                return false;
            }

            if (enemy != null)
            {
                Log("^Grounding Totem because {0} casting {1} #{2} on {3}",
                    Safe_UnitName(enemy),
                    enemy.CastingSpell.Name,
                    enemy.CastingSpell.Id,
                    enemy.CurrentTarget.Name
                    );
                return TotemCast(TotemId.GROUNDING_TOTEM);
            }

            return false;
        }

        private static bool IsPvpGroundingTotemNeeded(WoWPlayer p)
        {
            const int MaxFriendlyRange = 10;
            return p.IsAlive
                && IsEnemy(p)
                && p.IsCasting && p.CastingSpell != null
                && p.GotTarget && p.CurrentTarget.IsAlive && Safe_IsFriendly(p.CurrentTarget)
                && p.CurrentTarget.Distance < MaxFriendlyRange
                && _hashPvpGroundingTotemWhitelist.Contains(p.CastingSpell.Id);
        }

        private bool Purge( WoWUnit unitFilter)
        {
            int EnemyCastDistance = 0;
            WoWUnit target = null;
            WoWSpell purge = null;

            if (!AllowNonHealSpells())
            {
                Dlog("Purge: non-healing spells blocked - purge not cast");
                return false;
            }

            if (SpellManager.HasSpell("Purge") && SpellManager.CanCast("Purge"))
                purge = SpellManager.Spells["Purge"];

            if (purge == null)
                return false;

            EnemyCastDistance = (int) purge.MaxRange -2;

            if (unitFilter == null)
            {
                target = (from o in ObjectManager.ObjectList
                          let unit = o.ToUnit()
                          where unit != null && unit.IsValid
                              && unit.Distance <= EnemyCastDistance
                              && unit.Attackable
                              && Safe_IsHostile(unit)
                              && !unit.IsPet
                              && unit.HealthPercent > 1
                              && !Blacklist.Contains(unit)
                              && (IsPVP() || (unit.Combat && IsTargetingMeOrMyGroup(unit)))
                              && (from dbf in unit.Buffs
                                  where IsPurgeWorthySpell(unit, dbf.Value)
                                  select dbf.Value
                                  ).Any()
                              && unit.InLineOfSpellSight
                          select unit
                        ).FirstOrDefault();
            }
            else 
            {
                if ( unitFilter.IsValid && unitFilter.Distance <= EnemyCastDistance && unitFilter.Attackable && Safe_IsHostile(unitFilter))
                {
                    if ( !unitFilter.IsPet && unitFilter.HealthPercent > 1 && !Blacklist.Contains(unitFilter) )
                    {
                        if ((IsPVP() || (unitFilter.Combat && IsTargetingMeOrMyGroup(unitFilter)) && unitFilter.InLineOfSpellSight))
                        {
                            if ((   from dbf in unitFilter.Buffs
                                    where IsPurgeWorthySpell(unitFilter, dbf.Value)
                                    select dbf.Value
                                    ).Any())
                            {
                                target = unitFilter;
                            }
                        }
                    }
                }
            }

            if (target != null)
            {
                // second lookup now that we char to display message about buff being removed
                var purgeBuff =
                        (from dbf in target.Buffs
                         where dbf.Value.Spell.DispelType == WoWDispelType.Magic && _hashPurgeWhitelist.Contains(dbf.Value.SpellId)
                         select dbf.Value
                        ).FirstOrDefault();
                if (purgeBuff != null && FaceToUnit(target))
                {
                    Log("^Purge {0} at {1:F1} yds has buff '{2}' with {3} ms remaining", Safe_UnitName(target), _me.CombatDistance(target), purgeBuff.Name, purgeBuff.TimeLeft.TotalMilliseconds  );
                    if (Safe_CastSpell(target, purge  ))
                        return true;

                    Dlog("Purge:  failed to cast purge on {0} @ {1:F1} yds with los={2}", Safe_UnitName(target), _me.CombatDistance(target), target.InLineOfSpellSight );
                }
            }

            return false;
        }

        private static bool IsPurgeWorthySpell(WoWUnit unit, WoWAura aura)
        {
            if ( aura.Spell.DispelType != WoWDispelType.Magic)
                return false;

            if (!_hashPurgeWhitelist.Contains(aura.SpellId))
                return false;

            if (unit.Class == WoWClass.Warrior || unit.Class == WoWClass.Rogue || unit.Class == WoWClass.DeathKnight)
            {
                if (aura.SpellId == 79058)  // ignore arcane brilliance on these melee
                {
                    return false;
                }
            }

            return true;
        }

        const int PET_BASH = 58861;
        const int PET_SPIRITWALK = 58875;
        const int PET_TWINHOWL = 58857;

        private bool FeralSpiritCheck()
        {
            bool castGood = false;

            if (cfg.FarmingLowLevel)
                return false;

            // see if a Boss is within 50 yds and in combat already
            if (IsRAF() || IsRaidBehavior())
            {
                if (IsTrainingDummy(_me.CurrentTarget))
                    ;
                else if (cfg.RAF_SaveFeralSpiritForBosses)
                {
                    if (!IsBossCurrentTarget())
                        return false;

                    if (_me.CurrentTarget.CurrentHealth <= 1)
                        return false;

                    Dlog("FeralSpirit: found boss {0}[{1}] at {2:F1} yds", Safe_UnitName(_me.CurrentTarget), _me.CurrentTarget.Level, _me.CombatDistance(_me.CurrentTarget));
                }
            }
            else if (!IsPVP() && !IsFightStressful() && cfg.PVE_SaveForStress_FeralSpirit)
            {
                // Dlog("Feral Spirit:  not cast because  InBattleground={0}, IsFightStressful()={1}, and SaveForStress={2}", IsPVP(), IsFightStressful(), cfg.PVE_SaveForStress_FeralSpirit );
                return false;
            }
            else
            {
                castGood = FeralSpirit();
            }

            return castGood;
        }

        private bool FeralSpirit()
        {
            bool castGood = false;

            if (SpellManager.HasSpell("Feral Spirit"))
            {
                castGood = Safe_CastSpell(_me, "Feral Spirit");
                if (castGood)
                {
                    WaitForCurrentCastOrGCD();

                    Dlog( "FeralSpirit: disable autocast - Bash");
                    RunLUA( "DisableSpellAutocast(\"Bash\")");

                    Dlog( "FeralSpirit: disable autocast - Spirit Walk");
                    RunLUA( "DisableSpellAutocast(\"Spirit Walk\")");

                    Dlog( "FeralSpirit: disable autocast - Twin Howl");
                    RunLUA( "DisableSpellAutocast(\"Twin Howl\")");

                    string sMode;
                    if (IsPVP())
                        sMode = "PetAssistMode";
                    else
                        sMode = "PetDefensiveMode";

                    Log(Color.MediumSpringGreen, "^" + sMode);
                    RunLUA( sMode + "()");       // turn on defensive mode

                    Log(Color.MediumSpringGreen, "^Pet Attack");
                    RunLUA("PetAttack()");              // now attack something hitting me

                    if (!IsPVP())
                    {
                        WolvesTwinHowl();
                    }
                    else if (_me.Rooted || MeImmobilized())
                    {
                        WolvesSpiritWalk();
                    }
                }
            }

            return castGood;
        }

        public static bool WolvesSpiritWalk()
        {
            if (!_me.GotAlivePet)
                return false;

            if (_me.Pet.Distance > 25)
            {
                Dlog("WolvesSpiritWalk:  wolves at {0:F1} yds away, cant cast if greater than 25 yds", _me.Pet.Distance);
                return false;
            }

            return CastPetAction(PET_SPIRITWALK);
        }

        public static bool WolvesBash()
        {
            if (!_me.GotAlivePet)
                return false;

            if (!_me.Pet.GotTarget)
                return false;

            if (_me.Pet.CombatDistance(_me.Pet.CurrentTarget) > 5)
                return false;

            return CastPetAction(PET_BASH );
        }

        public static bool WolvesTwinHowl()
        {
            if (!_me.GotAlivePet)
                return false;

            return CastPetAction(PET_TWINHOWL);
        }

        public static bool IsPetSpellUsable(int spellId)
        {
            if (!_me.GotAlivePet)
                return false;

            WoWPetSpell psp = _me.PetSpells.FirstOrDefault(s => s != null && s.Spell != null && s.Spell.Id == spellId);
            if (psp == null)
            {
                Dlog("IsPetSpellUsable:  cannot find pet spell {0}", spellId);
                return false;
            }

            string sCmd = String.Format("return GetPetActionSlotUsable({0})", psp.ActionBarIndex + 1);
            Dlog("IsPetSpellUsable:  lua='{0}'", sCmd);
            bool canUse = false;
            try
            {
                canUse = Lua.GetReturnVal<bool>(sCmd, 0);
            }
            catch
            {
                Dlog("IsPetSpellUsable:  error calling GetPetActionSlotUsable");
                return false;
            }

            if (!canUse || psp.Cooldown || ( psp.Spell != null && SpellHelper.OnCooldown(psp.Spell)))
            {
                Dlog("IsPetSpellUsable:  '{0}' not available yet", psp.ToString(), psp.SpellType.ToString());
                return false;
            }

            return true;
        }

        public static bool CastPetAction(int spellId )
        {
            if (!IsPetSpellUsable(spellId))
                return false;

            WoWPetSpell psp = _me.PetSpells.FirstOrDefault(s => s != null && s.Spell != null && s.Spell.Id == spellId);
            if (psp == null)
            {
                Dlog("CastPetAction:  cannot find pet spell {0}", spellId);
                return false;
            }

            string sCmd = String.Format("CastPetAction({0})", psp.ActionBarIndex + 1);
            Dlog("CastPetAction:  lua='{0}'", sCmd);
            RunLUA(sCmd);
            Slog(Color.MediumSpringGreen, "^Pet Spell:  {0}", psp.Spell.Name);
            return true;

        }

#if PET_SPELLS_FINALLY_WORK
        public static bool CastPetSpell(string spellName)
        {
            if ( !_me.GotAlivePet )
                return false;

            if (_me.PetSpells == null)
                return false;

            WoWPetSpell psp = _me.PetSpells.FirstOrDefault(s => s != null && s.Spell != null && s.Spell.Name == spellName);
            if (psp == null)
                Dlog("CastPetSpell:  cannot find pet spell '{0}'", spellName);

            return CastPetSpell( psp);
        }
/*
        public static bool CastPetSpell(int id)
        {
            if (!_me.GotAlivePet)
                return false;

            if (_me.PetSpells == null)
                return false;

            WoWPetSpell psp = _me.PetSpells.FirstOrDefault(s => s != null && s.Spell != null && s.Spell.Id == id);
            if (psp == null)
                Dlog("CastPetSpell:  cannot find pet spell [{0}]", id.ToString());
            return CastPetSpell(psp);
        }
*/
        public static bool CastPetSpell(WoWPetSpell psp)
        {
            if (psp == null)
            {
                Dlog("CastPetSpell:  error - attempt to cast 'null' spell");
                return false;
            }
/*
            if (psp.SpellType != WoWPetSpell.PetSpellType.Spell)
            {
                Dlog("CastPetSpell:  error - cannot cast [{0}] since its a '{1}'", psp.ToString(), psp.SpellType.ToString());
                return false;
            }
*/
            if (psp.Spell.CanCast)
            {
                Dlog("CastPetSpell:  '{0}' not available yet", psp.ToString(), psp.SpellType.ToString());
                return false;
            }

            psp.Spell.Cast();
            Slog(Color.MediumSpringGreen, "^Pet Spell:  {0}", psp.Spell.Name);
            return true;
        }
#endif

        private static bool IsBossCurrentTarget( )
        {
            WoWUnit tgt = _me.CurrentTarget;
            bool isBoss = Safe_IsBoss(tgt) && tgt.Combat;
            if (isBoss)
                Dlog("IsBossCurrentTarget: boss found - {0}[{1}] at {2:F1} yds", Safe_UnitName(tgt), tgt.Level, _me.CombatDistance(tgt));

            return isBoss;
        }

        private static bool IsBossTanksTarget()
        {
            if (!IsRAFandTANK())
                return false;

            WoWUnit tt = GroupTank.CurrentTarget;
            bool isBoss = Safe_IsBoss(tt) && tt.Combat;
            if (isBoss)
                Dlog("IsBossTanksTarget: boss found - {0}[{1}] at {2:F1} yds", Safe_UnitName(tt), tt.Level, _me.CombatDistance(tt));

            return isBoss;
        }

        private bool EarthElementalTotem()
        {
            bool castGood = false;

            if (!HasTotemSpell(TotemId.EARTH_ELEMENTAL_TOTEM))
                return false;

            if ( IsRAF() )
            {
                if (GroupTank == null)
                    return false;

                double tankHealth = GroupTank.HealthPercent;
                if (tankHealth > 15)
                    return false;

                if (!Safe_CanCastSpell(_me, (int)TotemId.EARTH_ELEMENTAL_TOTEM))
                {
                    Dlog("EarthElementalTotem: tank at {0:F1}% but unable to cast right now", tankHealth);
                    return false;
                }

                if (!cfg.RAF_UseCooldowns)
                    return false;

                if (cfg.RAF_SaveElementalTotemsForBosses && !IsBossCurrentTarget())
                    return false;

                if (ObjectManager.ObjectList.Any(o => o.Entry == 15430 && o.DistanceSqr < 2500 ))
                {
                    Dlog("EarthElementalTotem: found another Earth Elemental Totem nearby, not casting");
                    return false;
                }

                Dlog("EarthElementalTotem: tank at {0:F1}%, attempting to save group", tankHealth);
            }
            else if (IsPVP())
            {
                if (!cfg.PVP_UseCooldowns)
                    return false;   // currently always allow it to be cast
            }
            else if ((!IsFightStressful() && cfg.PVE_SaveForStress_ElementalTotems))
            {
                Dlog("Earth Elemental Totem:  not cast because not a stressful PVE situation");
                return false;
            }
            else if (cfg.PVE_CombatStyle == ConfigValues.PveCombatStyle.FarmingLowLevelMobs)
            {
                Dlog("Earth Elemental Totem:  not cast because farming lowlevel mobs");
                return false;
            }

            castGood = TotemCast(TotemId.EARTH_ELEMENTAL_TOTEM);
            return castGood;
        }

        private bool FireElementalTotem()
        {
            bool castGood = false;

            if (IsHealerOnly())
                return false;

            if (!HasTotemSpell(TotemId.FIRE_ELEMENTAL_TOTEM))
                return false;

            if (IsRAF() || IsRaidBehavior())
            {
                if (!cfg.RAF_UseCooldowns)
                    return false;

                // fire elemental is a DPS loss for Enhancement in Cata
                if (typeShaman == ShamanType.Enhance)
                    return false;

                if (cfg.RAF_SaveElementalTotemsForBosses && !IsBossCurrentTarget() && !IsBossTanksTarget())
                    return false;

                if (!SpellManager.CanCast((int)TotemId.FIRE_ELEMENTAL_TOTEM))
                {
                    Dlog("FireElementalTotem: unable to cast right now" );
                    return false;
                }

                Dlog("FireElementalTotem: about to cast dps elemental" );
            }
            else if (IsPVP())
            {
                if ( !cfg.PVP_UseCooldowns )
                    return false;   // currently always allow it to be cast
            }
            else if (cfg.PVE_SaveForStress_ElementalTotems && !IsFightStressful())
            {
                Dlog("Fire Elemental Totem:  not cast because not a stressful PVE situation");
                return false;
            }

            castGood = TotemCast(TotemId.FIRE_ELEMENTAL_TOTEM);
            return castGood;
        }

        private bool CallForReinforcements()
        {
            if (cfg.FarmingLowLevel)
                return false;

            if (_me.GotAlivePet)
                return false;

            if (TotemExist(TotemId.EARTH_ELEMENTAL_TOTEM))
                return false;

            if (TotemExist(TotemId.FIRE_ELEMENTAL_TOTEM))
                return false;

            if (FireElementalTotem())
                return true;

            if (EarthElementalTotem())
                return true;

            return false;
        }

        private static bool IsValidBloodlustTarget( WoWPartyMember pm)
        {
            if (pm != null )
            {
                WoWPlayer p = pm.ToPlayer();
                if (p != null && p.IsPlayer)
                {
                    if (p.Combat)
                    {
                        if ( pm.Location3D.Distance(_me.Location) < 100 )
                        {
                            if (p.HealthPercent > 50)
                            {
                                if ((_me.IsHorde && !p.Debuffs.ContainsKey("Sated")) || (_me.IsAlliance && !p.Debuffs.ContainsKey("Exhaustion")))
                                {
                                    if ( !p.Debuffs.ContainsKey("Temporal Displacement"))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool BloodlustHeroism()
        {
            if (IsPVP())
            {
                if ( !cfg.PVP_UseCooldowns )
                    return false;                       // okay to have a trigger finger here

                // if # of friendlies in combat within 100 yds with more than 50% health
                int cntFriendly = GroupMemberInfos.Count(pm => IsValidBloodlustTarget( pm));
                if (cntFriendly < cfg.PVP_BloodlustCount)     // for bg's only
                {
                    return false;
                }

                Dlog("BloodlustHeroism:  found {0} teammates in combat, in range, with no debuff", cntFriendly );
            }
            else if (IsRAF())
            {
                if (!cfg.RAF_UseCooldowns)
                    return false;
                if (!cfg.RAF_UseBloodlustOnBosses)
                    return false;
                if (GroupTank == null)
                    return false;
                WoWUnit target = GroupTank.CurrentTarget;
                if (target == null || !target.Combat)
                    return false;
                if ( !Safe_IsBoss( target ))
                    return false;
            }
            else if (cfg.FarmingLowLevel)
            {
                return false;
            }
            else if (cfg.PVE_SaveForStress_Bloodlust && !IsFightStressful())
            {
                return false;
            }

            bool knowBloodlust = _me.IsHorde && SpellManager.HasSpell("Bloodlust");
            bool knowHeroism = _me.IsAlliance && SpellManager.HasSpell("Heroism");
            if (!knowBloodlust && !knowHeroism)
                ;
            else if (_me.Debuffs.ContainsKey("Temporal Displacement"))
                ;
            else if (knowBloodlust && !_me.Debuffs.ContainsKey("Sated") && Safe_CastSpell(_me, "Bloodlust"))
            {
                Slog("Bloodlust: just broke out a major can of whoop a$$!");
                return true;
            }
            else if (knowHeroism && !_me.Debuffs.ContainsKey("Exhaustion") && Safe_CastSpell(_me, "Heroism"))
            {
                Slog("Heroism: just broke out a major can of whoop a$$!");
                return true;
            }

            return false;
        }

        private bool SpiritwalkersGrace()
        {
            if ( !_me.Combat )
                return false;

            return Safe_CastSpell(_me, "Spiritwalker's Grace");
        }

        private bool UseManaPotionIfAvailable()
        {
            // return UsePotion(CheckForItem(_potionManaEID));
            return UsePotion(FindPotion(WoWSpellEffectType.Energize));
        }

        private bool UseHealthPotionIfAvailable()
        {
            // return UsePotion(CheckForItem(_potionHealthEID));
            return UsePotion(FindPotion(WoWSpellEffectType.Heal));
        }

        private bool UsePotion(WoWItem potion)
        {
            if (potion != null)
            {
                if (_me.IsImmobilized())
                    Slog("Immobilized -- unable to use potion now");
                else
                {
                    if (CanUsePotion() )
                    {
                        Slog( Color.Pink, "^CONSUME:  /use '" + potion.Name + "'");
                        Dlog("{0} has a cooldown of {1:F1}", potion.Name, potion.Cooldown);
                        // RunLUA("UseItemByName(\"" + potion.Name + "\")");
                        potion.Use();

                        // SleepForLagDuration();
                        _potionCountdown = new Countdown(60 * 1000);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanUsePotion()
        {
            return _potionCountdown.Done;
        }

        public static bool IsPotion(WoWItem item, WoWSpellEffectType effectType)
        {
            bool isPot = item != null
                        && 0 != item.BaseAddress
                        && item.ItemInfo.ItemClass == WoWItemClass.Consumable
                        && item.ItemInfo.ContainerClass == WoWItemContainerClass.Potion;

            if (isPot)
            {
                isPot = item.ItemSpells.Any(s => s != null && s.ActualSpell.SpellEffects.Any(e => e != null && e.EffectType == effectType));
            }

            return isPot;
        }

        public static WoWItem FindPotion(WoWSpellEffectType effectType)
        {
            WoWItem pot = (from item in StyxWoW.Me.BagItems
                           where IsPotion(item, effectType)
                           && item.ItemInfo.RequiredLevel <= _me.Level
                           orderby item.ItemInfo.Level descending, item.ItemInfo.RequiredLevel descending 
                           select item).FirstOrDefault();
            return pot;
        }


        public static bool IsFlask(WoWItem item)
        {
            bool isFlask = item != null
                        && 0 != item.BaseAddress
                        && item.ItemInfo.ItemClass == WoWItemClass.Consumable
                        && item.ItemInfo.ContainerClass == WoWItemContainerClass.Flask;
            return isFlask;
        }

        public static WoWItem FindFlask()
        {
            WoWItem flask = (from item in StyxWoW.Me.BagItems
                           where IsFlask(item) && item.ItemInfo.RequiredLevel <= _me.Level 
                           orderby item.ItemInfo.Level descending, item.ItemInfo.RequiredLevel descending 
                           select item).FirstOrDefault();
            return flask;
        }


        private bool UseFlask(WoWItem flask)
        {
            if (flask != null)
            {
                if (!flask.ItemSpells.Any())
                {
                    Dlog("UseFlask:  item {0} does not have any item spells", flask.Name);
                }
                else if (flask.CooldownTimeLeft.TotalMilliseconds > 0)
                {
                    Dlog("UseFlask:  cannot use {0} for {1} ms", flask.Name, flask.CooldownTimeLeft.TotalMilliseconds);
                }
                else if (IsSpellBlacklisted(flask.ItemSpells[0].Id))
                {
                    Dlog("UseFlask:  temporarily blacklisted {0}", flask.Name);
                }
                else
                {
                    Slog("^FLASK:  /use '" + flask.Name + "'");
                    Dlog("{0} has a cooldown of {1:F1} and spell id #{2}", flask.Name, flask.Cooldown, flask.ItemSpells[0].Id);
                    flask.Use();
                    AddSpellToBlacklist(flask.ItemSpells[0].Id);
                    return true;
                }
            }
            return false;
        }

        private bool UseFlaskIfAvailable()
        {
            if (!cfg.UseFlasks)
                return false;

            WoWAura flaskAura = (from a in _me.GetAllAuras() 
                                  where a.Name.ToLower().IndexOf("flask") >= 0
                                  select a).FirstOrDefault();
            if (flaskAura != null)
            {
                Dlog("UseFlaskIfAvailable:  found flask buff {0} with {1:F0} seconds left", flaskAura.Name, flaskAura.TimeLeft.TotalSeconds);
                return false;
            }

            return UseFlask(FindFlask());
        }


        private static bool UseConsumeable(string sItem)
        {
            WoWItem item = CheckForItem(sItem);
            if (item != null)
            {
                item.Use();
                // SleepForLagDuration();
            }

            return item != null;
        }

        private bool UseBandageIfAvailable()
        {
            WoWItem bandage = CheckForItem(_bandageEID);
            if (cfg.UseBandages && !SpellManager.Spells.ContainsKey("First Aid"))
            {
                Wlog("Use Bandages ignored : your Shaman has not trained First Aid");
            }
            else if (bandage == null)
                Wlog("FIRST-AID:  no bandages in inventory");
            else if (_me.Debuffs.ContainsKey("Recently Bandaged"))
                Dlog("FIRST-AID:  can't bandage -- currently under 'Recently Bandaged' debuff");
            else if (!_me.IsImmobilized())
            {
                foreach (KeyValuePair<string, WoWAura> dbf in _me.Debuffs)
                {
                    if (!dbf.Value.IsHarmful)
                        continue;
                    Dlog("FIRST-AID:  can't bandage -- harmful debuff '{0}' active", dbf.Key);
                    return false;
                }

                Safe_StopMoving( "to use bandage");

                double healthStart = _me.HealthPercent;
                Stopwatch timeBandaging = new Stopwatch();
                Slog("FIRST-AID:  using '{0}' at {1:F0}%", bandage.Name, _me.HealthPercent);
                timeBandaging.Start();

                try
                {
                    bandage.Use();
                    // RunLUA("UseItemByName(\"" + bandage.Name + "\", \"player\")");
                    do
                    {
                        Sleep(100);
                        Dlog("dbg firstaid:  buff-present:{0}, casting:{1}, channeled:{2}",
                            _me.IsAuraPresent( "First Aid"),
                            IsCasting(),
                            _me.ChanneledCastingSpellId != 0);
                    } while (!IsGameUnstable() && _me.IsAlive && (_me.IsAuraPresent( "First Aid") || timeBandaging.ElapsedMilliseconds < 1000) && _me.HealthPercent < 100.0);
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("WOW LUA Call to UseItemByName() failed");
                    Logging.WriteException(e);
                }

                Dlog("FIRST-AID:  used {0} for {1:F1} secs ending at {2:F0}%", bandage.Name, timeBandaging.Elapsed.TotalSeconds, _me.HealthPercent);
                if (healthStart < _me.HealthPercent)
                    return true;
            }

            return false;
        }



        private bool IsShieldBuffNeeded(bool atRest)
        {
            ShieldType st = WhichShieldTypeNeeded(atRest);
            return st != ShieldType.None;
        }

        private ShieldType WhichShieldTypeNeeded(bool atRest)
        {
            if (IsAutoShieldApplyDisabled())
                return ShieldType.None;

            ShieldType shield = ShieldType.None;

            // RAF for any SHAMAN HEALER (can be any spec)
            //  :   Water Shield on self, Earth Shield (if available) on leader
            //  :   NOTE:  only do this when inside
            if (IsRAF() && IsHealer())
            {
                uint uWaterStacks = _me.GetAuraStackCount( "Water Shield");
                if ((uWaterStacks == 0 || (uWaterStacks < 3 && atRest)) && !IsSpellBlacklisted((int)ShieldType.Water) && SpellManager.HasSpell("Water Shield"))
                {
                    shield = ShieldType.Water;
                    Dlog("WhichShieldTypeNeeded:  {0} Shield on self due to RAF Healer or Lowbie farming", shield.ToString());
                }
                else if (IsRAFandTANK() && GroupTank.IsAlive && !GroupTank.IsMe && !IsSpellBlacklisted((int)ShieldType.Earth) && SpellManager.HasSpell("Earth Shield"))
                {
                    // check if tank needs Earth Shield and is within range
                    if (GroupTank.GetAuraStackCount("Earth Shield") < (atRest ? 4 : 1) && _me.IsUnitInRange(GroupTank, 39))
                    {
                        shield = ShieldType.Earth;
                        // dont set last shield used here since it was on someone else
                        Dlog("WhichShieldTypeNeeded:  {0} Shield on Tank due to RAF Healer", shield.ToString());
                    }
                }
            }
            // Battlegrounds/Arenas for RESTO SHAMAN ONLY
            //  :   Support Shield Twisting between Water Shield and Earth Shield
            else if (typeShaman == ShamanType.Resto && IsPVP() && SpellManager.HasSpell("Earth Shield"))
            {
                bool trainedWaterShield = SpellManager.HasSpell("Water Shield");
                bool trainedEarthShield = SpellManager.HasSpell("Earth Shield");
                if (!trainedEarthShield && !trainedWaterShield)
                    return shield;

                uint uWaterStacks = _me.GetAuraStackCount( "Water Shield");
                uint uEarthStacks = _me.GetAuraStackCount( "Earth Shield");

                if (trainedWaterShield && _me.ManaPercent <= cfg.TwistManaPercent && (uWaterStacks == 0 || (atRest && uWaterStacks < 3)))
                {
                    shield = ShieldType.Water;
                    Dlog("WhichShieldTypeNeeded:  {0} Shield due to PvP Resto and Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
                // check if Earth shield required
                else if (trainedEarthShield && _me.ManaPercent > cfg.TwistDamagePercent && (uEarthStacks == 0 || (atRest && uEarthStacks < 3)))
                {
                    shield = ShieldType.Earth;
                    Dlog("WhichShieldTypeNeeded:  {0} Shield due to PvP Resto and Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
                // now check if missing a shield and need to Twist
                else if ((uWaterStacks + uEarthStacks) == 0 || (atRest && (uWaterStacks + uEarthStacks) < 3))
                {
                    if (_lastShieldUsed == ShieldType.Water && trainedWaterShield)
                        shield = ShieldType.Water;
                    else
                        shield = ShieldType.Earth;

                    Dlog("WhichShieldTypeNeeded:  {0} Shield due to PvP Resto and Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
            }
            // Everything Else
            //  :   Shield Twist between Lightning Shield and Water Shield, but don't overwrite Earth Shield (if leading)
            else
            {
                uint uEarthStacks = _me.GetAuraStackCount( "Earth Shield");
                if (uEarthStacks > 0)
                {
                    Dlog("WhichShieldTypeNeeded:  detected {0} stacks of Earth Shield, so no shield applied", uEarthStacks );
                    return ShieldType.None;
                }

                bool trainedWaterShield = SpellManager.HasSpell("Water Shield");
                bool trainedLightningShield = SpellManager.HasSpell("Lightning Shield");
                if (!trainedLightningShield && !trainedWaterShield)
                    return shield;

                uint uWaterStacks = _me.GetAuraStackCount( "Water Shield");
                uint uLightningStacks = _me.GetAuraStackCount( "Lightning Shield");

                if (trainedWaterShield && _me.ManaPercent <= cfg.TwistManaPercent && (uWaterStacks == 0 || (atRest && uWaterStacks < 3)))
                {
                    shield = ShieldType.Water;
                    Dlog("WhichShieldTypeNeeded:  {0} Shield on self with Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
                // check if Lightning shield required
                else if (trainedLightningShield && _me.ManaPercent > cfg.TwistDamagePercent && (uLightningStacks == 0 || (atRest && uLightningStacks < 3)))
                {
                    shield = ShieldType.Lightning;
                    Dlog("WhichShieldTypeNeeded:  {0} Shield on self with Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
                // now check if missing a shield and need to Twist
                else if ((uWaterStacks + uLightningStacks) == 0 || (atRest && (uWaterStacks + uLightningStacks) < 3))
                {
                    if (_lastShieldUsed != ShieldType.Water && trainedWaterShield)
                        shield = ShieldType.Water;
                    else
                        shield = ShieldType.Lightning;

                    Dlog("WhichShieldTypeNeeded:  {0} Shield on self due to Twist with Mana at {1:F1}%", shield.ToString(), _me.ManaPercent);
                }
            }

            return shield;
        }

        /*
         * ShieldTwisting()
         * 
         * Implement a technique known as shield twisting where
         * you alternate between a damage shield and a mana restoration 
         * shield.  basically make sure one or the other is active.
         * 
         * Here is the general approach in priority order:
         * 
         * If Mana is low, then force Mana restoration
         * If Mana is full, then force Damage shield
         * Otherwise alterante between shield types
         * 
         * This is a Level 20 and Higher Technique
         */
        private bool ShieldTwisting(bool atRest)
        {
            ShieldType shield = WhichShieldTypeNeeded(atRest);
            bool castShield = false;

            if (shield == ShieldType.None)
                return castShield;

            if (IsSpellBlacklisted((int)shield))
            {
                Dlog("ShieldTwisting:  {0} Shield temporarily blacklisted waiting for client update", shield.ToString());  
                return castShield;
            }

            // if the shield type to use is Earth Shield and we are in RAF, it's meant for the tank
            if (IsRAF() && shield == ShieldType.Earth)
            {
                if (_me.IsUnitInRange(GroupTank, 39))
                {
                    castShield = Safe_CastSpell(GroupTank, shield.ToString() + " Shield");
                }
                else
                {
                    Dlog("ShieldTwisting:  tank needs earth shield but is out of range");
                }

                return castShield;
            }

            // otherwise, cast shield on me
            castShield = Safe_CastSpell(_me, shield.ToString() + " Shield");
            if (castShield)
            {
                _lastShieldUsed = shield;
            }

            return castShield;
        }

        public static bool IsWaterBreathingNeeded()
        {
            const int WATER_BREATHING = 131;

            if (cfg.WaterBreathing && _me.IsSwimming && _me.IsAlive)
            {
                var timeLeft = _me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime;
                if (timeLeft > 900001)  // Magic number from Anti Drown
                    timeLeft = 1;

                // anti drown plugin will fight bot and CC for movement control sometimes
                //  ..  so if active cast water breathing immediately when going underwater
                //  otherwise wait until last 5 seconds so we don't dismount, etc unnecessarily
                if ( 0 < timeLeft && ( timeLeft < 5000 || (!IsPVP() && _isPluginAntiDrown && timeLeft < 65000)))
                {
                    if (!_me.IsAuraPresent(WATER_BREATHING) && SpellManager.HasSpell(WATER_BREATHING))
                    {
                        if (_hasGlyphOfWaterBreathing || null != CheckForItem(SHINY_FISH_SCALES))
                        {
                            WoWSpell spell = SpellManager.Spells["Water Breathing"];
                            if (Safe_CanCastSpell(_me, spell))
                            {
                                return true;
                            }

                            Dlog("IsWaterBreathingNeeded:  we need it, but cannot cast for some reason");
                        }
                        else
                        {
                            Wlog("Cannot cast Water Breathing - missing Shiny Fish Scales");
                        }
                    }
                }
            }

            return false;
        }

        public static bool HandleWaterBreathing()
        {
            if (IsWaterBreathingNeeded())
            {
                if (Safe_CastSpell(_me, "Water Breathing"))
                {
                    return true;
                }
            }

            return false;
        }
        /*
         * Summary: inspects the list of WoWUnits for one that is within the
         *      maximum distance provided of the pt given.
         *          
         * Returns: true if clear for atleast that distance
         */
        private static bool CheckForSafeDistance(string reason, WoWPoint pt, double dist)
        {
            WoWUnit unitClose = null;
            Stopwatch timer = new Stopwatch();

            timer.Start();
            try
            {
                if (!IsPVP())
                    unitClose = (from o in ObjectManager.ObjectList
                                 where o is WoWUnit
                                 let unit = o.ToUnit()
                                 where pt.Distance(o.Location) < dist
                                     && unit.Attackable
                                     && !unit.IsCritter 
                                     && Safe_IsHostile(unit)
                                     && unit.IsAlive
                                 orderby unit.CurrentHealth ascending
                                 select unit
                                ).FirstOrDefault();
                else
                    unitClose = (from o in ObjectManager.ObjectList
                                 where o is WoWUnit
                                 let unit = o.ToUnit()
                                 where pt.Distance(unit.Location) < dist
                                     && Safe_IsEnemyPlayer( unit)
                                     && unit.IsAlive
                                 orderby unit.CurrentHealth ascending
                                 select unit
                                ).FirstOrDefault();

                if (unitClose == null)
                    Dlog("{0} CheckForSafeDistance({1:F1}): no hostiles/profile mobs in range - took {2} ms", reason, dist, timer.ElapsedMilliseconds);
                else
                    Dlog("{0} CheckForSafeDistance({1:F1}): saw {2}{3} - {4}[{5}] around {6:F1} yds away", // - took {6} ms",
                        reason,
                        dist,
                        (unitClose.IsTargetingMeOrPet ? "*" : ""),
                        unitClose.Class,
                        Safe_UnitName(unitClose),
                        unitClose.Level,
                        5 * Math.Round(pt.Distance(unitClose.Location) / 5)
                        //          , timer.ElapsedMilliseconds
                        );
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in CheckForSafeDistance()");
                Logging.WriteException(e);
            }

            return unitClose == null;
        }


        private static bool CheckForClearDistance(string reason, WoWPoint pt, double dist)
        {
            WoWUnit unitClose = null;
            Stopwatch timer = new Stopwatch();

            timer.Start();
            try
            {
                if (!IsPVP())
                    unitClose = (from o in ObjectManager.ObjectList
                                 where o is WoWUnit
                                 let unit = o.ToUnit()
                                 where pt.Distance(o.Location) < dist
                                     && unit.Attackable
                                     && Safe_IsHostile(unit)
                                     && unit.IsAlive
                                 orderby unit.CurrentHealth ascending
                                 select unit
                                ).FirstOrDefault();
                else
                    unitClose = (from o in ObjectManager.ObjectList
                                 where o is WoWUnit
                                 let unit = o.ToUnit()
                                 where pt.Distance(unit.Location) < dist
                                     && Safe_IsEnemyPlayer(unit)
                                     && unit.IsAlive
                                 orderby unit.CurrentHealth ascending
                                 select unit
                                ).FirstOrDefault();

                if (unitClose == null)
                    Dlog("{0} CheckForClearDistance({1:F1}): no hostiles/profile mobs in range - took {2} ms", reason, dist, timer.ElapsedMilliseconds);
                else
                    Dlog("{0} CheckForClearDistance({1:F1}): saw {2}{3} - {4}[{5}] around {6:F1} yds away", // - took {6} ms",
                        reason,
                        dist,
                        (unitClose.IsTargetingMeOrPet ? "*" : ""),
                        unitClose.Class,
                        Safe_UnitName(unitClose),
                        unitClose.Level,
                        5 * Math.Round(pt.Distance(unitClose.Location) / 5)
                        //          , timer.ElapsedMilliseconds
                        );
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in CheckForClearDistance()");
                Logging.WriteException(e);
            }

            return unitClose == null;
        }


        private static Countdown timerNextList = new Countdown(4000);

        public static void ListPlayerProps(WoWPlayer tgt)
        {
            Log("Attributes for {0}", Safe_UnitName(tgt).ToUpper());
            Log("---------------------------------------------");

            Log("{0} = {1}", "IsHorde", tgt.IsHorde );
            Log("{0} = {1}", "IsInMyPartyOrRaid", tgt.IsInMyPartyOrRaid);
            Log("{0} = {1}", "Class", tgt.Class);
            Log("{0} = {1}", "Attackable", tgt.Attackable);
            Log("{0} = {1:F2}", "Distance", tgt.Distance);           
            Log("{0} = {1:F2}", "CombatDistance", _me.CombatDistance(tgt));
            Log("{0} = {1:F2}", "CombatReach", tgt.CombatReach);
            Log("{0} = {1}", "FactionId", tgt.FactionId);
            Log("{0} = {1}", "IsFriendly", tgt.IsFriendly);
            Log("{0} = {1}", "IsHostile", tgt.IsHostile);
            Log("{0} = {1}", "IsNeutral", tgt.IsNeutral);
            Log("{0} = {1}", "IsPlayer", tgt.IsPlayer);
            Log("{0} = {1}", "MyReaction", tgt.MyReaction);
            Log("{0} = {1}", "PvpFlagged", tgt.PvpFlagged);
            Log("{0} = {1}", "PvPState", tgt.PvPState);
            Log("{0} = {1}", "ContestedPvPFlagged", tgt.ContestedPvPFlagged);
            Log("{0} = {1}", "DuelArbiterGuid", tgt.DuelArbiterGuid);
            Log("{0} = {1}", "DuelTeamId", tgt.DuelTeamId);
            Log("{0} = {1}", "IsFFAPvPFlagged", tgt.IsFFAPvPFlagged);
            Log("{0} = {1}", "BattlefieldArenaFaction", tgt.BattlefieldArenaFaction);

            Log("");
        }

        public static void ListSpecialInfoCollected()
        {
#if COLLECT_NEW_PURGEABLES
            PurgeableSpellReport();
#endif

#if DUMP_PLAYER_INFO
            List<WoWPlayer> PlayerList = (from o in ObjectManager.ObjectList
                        where o is WoWPlayer 
                        let p = o.ToPlayer()
                        where p.IsAlive && p.IsPlayer 
                        select p
                        ).ToList();

            Log( "");
            Log( "############ MY TEAM #############");
            foreach ( WoWPlayer tgt in PlayerList )
            {
                if ( tgt.IsInMyPartyOrRaid)
                    ListPlayerProps(tgt);
            }

            Log( "");
            Log( "############ OTHERS #############");
            foreach ( WoWPlayer tgt in PlayerList )
            {
                if ( !tgt.IsInMyPartyOrRaid)
                    ListPlayerProps(tgt);
            }

            Log( "");
#endif

            return;

            // save this code for debugging later if needed
            // ----------------------------------------------
            if (!timerNextList.Done)
                return;

            timerNextList.Remaining = 4000;

            List<WoWUnit> adds = (from o in ObjectManager.ObjectList
                                  where o is WoWUnit
                                  let unit = o.ToUnit()
                                  where unit.Distance < 50
                                  select unit
                                        ).ToList();

            Slog("ADDLST  -- CURRENT WOWUNIT LIST -- {0} ENTRIES", adds.Count());
            foreach (WoWUnit unit in adds)
            {
                LogWowUnit(unit);
            }

            adds = (from o in ObjectManager.ObjectList
                    where o is WoWUnit
                    let unit = o.ToUnit()
                    where unit.Distance < 50
                    select unit
                        ).ToList();
            Slog("STUFF   --GUID           Target           SummonedBy       CreatedBy        CharmedBy         RootOwner         Owner             Name [Type] -------");
            foreach (WoWUnit unit in adds)
            {
                DumpWowUnit(unit);
            }

            adds = (from o in ObjectManager.ObjectList
                    where o != null && o is WoWUnit
                    let unit = o.ToUnit()
                    where unit.Distance < 50
                    select unit
                        ).ToList();
            Slog("ALL<50  --GUID           Target           SummonedBy       CreatedBy        CharmedBy         RootOwner         Owner             Name [Type] -------");
            // foreach (WoWUnit unit in ObjectManager.GetObjectsOfType<WoWUnit>())
            foreach (WoWUnit unit in adds)
            {
                // if ( unit != null && (IsMeOrMyStuff(unit) || IsTargetingMeOrMyStuff(unit)))
                DumpWowUnit(unit);
            }

        }

        public static void LogWowUnit(WoWUnit unit)
        {
            try
            {
                Slog("        {0}{1:F2} yds hostile:{2} attackable:{3}  {4}-{5} {6}[{7}] -->{8} | {9} | {10}| {11}| {12}",
                    IsTargetingMeOrMyStuff(unit) ? "X " : "  ",
                    unit.Distance,
                    Safe_IsHostile(unit),
                    unit.Attackable,
                    unit.CreatureType,
                    unit.Class,
                    unit.Name,
                    unit.Level,
                    !unit.GotTarget ? "has no target" : unit.CurrentTargetGuid == ObjectManager.Me.Guid ? "TARGETTING ME" : unit.CurrentTarget.Name,
                    ObjectManager.Me.CurrentTarget.Guid == unit.Guid ? "MY TARGET<---" : " ",
                    unit.Aggro ? "AGGRO<---" : "         ",
                    ObjectManager.Me.GotAlivePet && ObjectManager.Me.Pet.GotTarget && ObjectManager.Me.Pet.CurrentTargetGuid == unit.Guid ? "PET TARGET<---" : " ",
                    unit.PetAggro ? "PETAGGRO!" : "         "
                    );
            }
            catch
            {
            }
        }


        public static void DumpWowUnit(WoWUnit unit)
        {
            try
            {
                Slog("        {0:X16} {1:X16} {2:X16} {3:X16} {4:X16} {5:X16}  {6} [{7}]",
                    unit.Guid,
                    unit.GotTarget ? unit.CurrentTargetGuid : 0,
                    unit.SummonedByUnitGuid,
                    unit.CreatedByUnitGuid,
                    unit.CharmedByUnitGuid,
                    unit.SummonedUnitGuid,
                    unit.Name,
                    unit.CreatureType
                    );
            }
            catch { }
        }

        /*
         * Summary: inspects the objects within ranged to check if they are targeting me
         *          and hostile.  breaks down counts between ranged and melee targets
         *          
         * Returns: total number of hostiles fighting me
         */
        private readonly Stopwatch _addsTimer = new Stopwatch();
        public static List<WoWUnit> mobList = new List<WoWUnit>();


        private static bool IsPveAdd(WoWUnit unit)
        {
            if (!unit.Attackable)
                return false;
            if ( !unit.IsAlive)
                return false;
            if (!unit.Combat && !unit.Aggro && !unit.PetAggro)
                return false;
            if (!IsTargetingMeOrMyGroup(unit) && unit.CreatureType != WoWCreatureType.Totem)
                return false;

            return true;
        }

        private void CheckForAdds()
        {
            // ListWowUnitsInRange();

            Stopwatch timerCFA = new Stopwatch();
            timerCFA.Start();

            _countMeleeEnemy = 0;
            _count8YardEnemy = 0;
            _count10YardEnemy = 0;
            _countRangedEnemy = 0;
            _countAoe8Enemy = 0;
            _countAoe12Enemy = 0;
            _countFireNovaEnemy = 0;
            _countMobs = 0;

            // _distClosestEnemy = 9999.99;
            _OpposingPlayerGanking = false;
            _BigScaryGuyHittingMe = false;

            try
            {
                // List<WoWObject> longList = ObjectManager.ObjectList;
                // List<WoWUnit> mobList = ObjectManager.GetObjectsOfType<WoWUnit>(false);
                // if (_mobList == null || (_addsTimer.ElapsedMilliseconds > 5000 && !_me.Combat ) ) 
                {
                    if (IsPVP())
                    {

                        mobList = (from o in ObjectManager.ObjectList
                                   where o is WoWUnit 
                                   let unit = o.ToUnit()
                                   where 
                                    unit.Distance <= _maxSpellRange 
                                    && unit.IsAlive && Safe_IsEnemyPlayer(unit) && !unit.IsPet
                                   // orderby o.Distance ascending
                                   select unit
                                    ).ToList();
                        Dlog("CheckForAdds():  PvP list built has {0} entries within {1:F1} yds", mobList.Count, _maxDistForRangeAttack);
                    }
                    else
                    {
                        mobList = (from o in ObjectManager.ObjectList
                                   where o is WoWUnit 
                                   let unit = o.ToUnit()
                                   where 
                                        unit.Distance <= _maxSpellRange 
#if NORMAL
                                   unit.Attackable
                                       && unit.IsAlive
                                       && unit.Combat
                                       && !Safe_IsFriendly(unit)
                                       && (IsTargetingMeOrMyGroup(unit) || unit.CreatureType == WoWCreatureType.Totem)
#else
                                        && HasAggro( unit )
#endif
                                   select unit
                                    ).ToList();
                        Dlog("CheckForAdds():  PVE list built has {0} entries within {1:F1} yds", mobList.Count, _maxDistForRangeAttack);
                    }
                }

                if (mobList != null && mobList.Any())
                {
                    Dlog("CheckForAdds() can see:");
                    try
                    {
                        foreach (WoWUnit unit in mobList)
                        {
                            if (unit == null || !unit.IsAlive)  // check again incase one died since making list
                                continue;

                            if (unit.Distance < STD_MELEE_RANGE )
                                _countMeleeEnemy++;
                            else
                                _countRangedEnemy++;

                            if ( unit.Distance < 8)     // special case for 8 yard checks
                                _count8YardEnemy++;

                            if ( unit.Distance < 10)     // special case for 10 yard checks
                                _count10YardEnemy++;

                            if (_me.GotTarget)
                            {
                                if ( unit.CombatDistance( _me.CurrentTarget) <= 12)
                                {
                                    _countAoe12Enemy++;
                                    if ( unit.CombatDistance( _me.CurrentTarget) <= 8)
                                        _countAoe8Enemy++;
                                }
                            }

#if PRE_410_METHOD
                            if (TotemExist(TOTEM_FIRE) && _totem[TOTEM_FIRE].Location.Distance(unit.Location) < 10)
                                _countFireNovaEnemy++;
#elif PRE_430_METHOD
                            bool nearFlameShock = (from mob in mobList
                                     where mob.Location.Distance(unit.Location) <= _rangeFireNova && mob.HasAura("Flame Shock")
                                     select mob).Any();
                            if (nearFlameShock && !IsImmunneToFire(unit))
                                _countFireNovaEnemy++;
#else
                            if (unit.HasAura("Flame Shock"))
                            {
                                int countHitByUnit = (from mob in mobList
                                                       where mob.Location.Distance(unit.Location) <= _radiusFireNova 
                                                            && !IsImmunneToFire(mob)
                                                       select mob).Count();
                                _countFireNovaEnemy += countHitByUnit;
                            }
#endif
                            if (unit.IsPlayer)
                            {
                                Dlog("  "
                                    + (!unit.GotTarget ? " " : unit.CurrentTarget.IsMe ? "*" : unit.IsTargetingPet ? "+" : IsTargetingMeOrMyStuff(unit) ? "@" : " ")
                                    + "PLAYER: (" + (unit.ToPlayer().IsHorde ? "H" : "A") + ") " + unit.Race + " " + unit.Class + " - " + Safe_UnitName(unit) + "[" + unit.Level + "]  dist: " + unit.Distance.ToString("F2"));
                                _OpposingPlayerGanking = !IsPVP();
                                if (_OpposingPlayerGanking && (!_me.GotTarget || !_me.CurrentTarget.IsPlayer))
                                {
                                    Safe_SetCurrentTarget(unit);
                                }
                            }
                            else
                            {
                                string sType = "NPC ";
                                if (Safe_IsProfileMob(unit))
                                {
                                    _countMobs++;
                                    sType = "MOB ";
                                }

                                if (unit.CreatureRank == WoWUnitClassificationType.WorldBoss)
                                    sType = "BOSS";

                                Dlog("  "
                                    + (!unit.GotTarget ? " " : unit.CurrentTarget.IsMe ? "*" : unit.IsTargetingPet ? "+" : IsTargetingMeOrMyStuff(unit) ? "@" : " ")
                                    + sType + ": " + unit.Class + " - " + Safe_UnitName(unit) + "[" + unit.Level + "]  dist: " + unit.Distance.ToString("F2"));

                                if (Safe_IsElite( unit))
                                    _BigScaryGuyHittingMe = true;
                            }
                        }
                    }
                    catch (ThreadAbortException) { throw; }
                    catch (GameUnstableException) { throw; }
                    catch (Exception e)
                    {
                        Log(Color.Red, "An Exception occured. Check debug log for details.");
                        Logging.WriteDebug("EXCEPTION in code doing CheckForAdds(1)");
                        Logging.WriteException(e);
                    }
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (GameUnstableException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in CheckForAdds(2)");
                Logging.WriteException(e);
            }

            Dlog("Count8={0}  Count10={1}  Aoe8={2}  AoE12={3}  FireNova={4}", _count8YardEnemy, _count10YardEnemy, _countAoe8Enemy, _countAoe12Enemy, _countFireNovaEnemy );
            Dlog("   ## Total  {0}/{1} melee/ranged in Combat - CheckForAdds took {2} ms", _countMeleeEnemy, _countRangedEnemy, timerCFA.ElapsedMilliseconds);
            if (!IsRAF())
            {
                if (countEnemy > 1)
                    Slog(">>> MULTIPLE TARGETS:  " + _countMeleeEnemy + " melee,  " + _countRangedEnemy + " ranged");
                if (_BigScaryGuyHittingMe)
                    Slog(">>> BIG Scary Guy Hitting Me (elite or {0}+ levels)", cfg.PVE_LevelsAboveAsElite);
                if (_OpposingPlayerGanking)
                    Slog(">>> Opposing PLAYER is Attacking!!!!");
            }

            return;
        }

        private WoWUnit CheckForTotems()
        {
            Stopwatch timerCFA = new Stopwatch();
            timerCFA.Start();
            WoWUnit totem = null;

            totem = ObjectManager.GetObjectsOfType<WoWUnit>(false).Find(
            unit => unit != null
                && Safe_IsHostile(unit)
                && unit.IsAlive
                && unit.Distance <= Targeting.PullDistance
                && unit.CreatureType == WoWCreatureType.Totem
                );

            return totem;
        }

        public static bool IsCrowdControlledAura(WoWAura aura)
        {
            if (aura != null)
            {
                switch (aura.Spell.Mechanic)
                {
                    case WoWSpellMechanic.Charmed:
                    case WoWSpellMechanic.Disoriented:
                    case WoWSpellMechanic.Distracted:
                    case WoWSpellMechanic.Fleeing:
                    case WoWSpellMechanic.Gripped:
                    case WoWSpellMechanic.Rooted:
                    case WoWSpellMechanic.Asleep:
                    case WoWSpellMechanic.Snared:
                    case WoWSpellMechanic.Stunned:
                    case WoWSpellMechanic.Frozen:
                    case WoWSpellMechanic.Incapacitated:
                    case WoWSpellMechanic.Polymorphed:
                    case WoWSpellMechanic.Banished:
                    case WoWSpellMechanic.Shackled:
                    // case WoWSpellMechanic.Turned:    // not needed
                    case WoWSpellMechanic.Horrified:
                    case WoWSpellMechanic.Invulnerable2:
                    case WoWSpellMechanic.Sapped:
                        return true;
                }
            }

            return false;
        }

        public static bool IsImmobilizedAura(WoWAura aura)
        {
            if (aura == null)
                return false;

            return IsCrowdControlledAura(aura) && aura.Spell.Mechanic != WoWSpellMechanic.Rooted && aura.Spell.Mechanic != WoWSpellMechanic.Snared;
        }

        public static bool IsSilencedAura(WoWAura aura)
        {
            return aura != null && (IsImmobilizedAura(aura) || aura.Spell.Mechanic == WoWSpellMechanic.Silenced);
        }

        public static WoWAura GetCrowdControlledAura( WoWUnit u)
        {
            WoWAura aura = null;

            if (u != null)
            {
                aura = (from a in u.GetAllAuras() where IsCrowdControlledAura(a) select a).FirstOrDefault();
                if (aura != null)
                {
                    Dlog("GetCrowdControlledAura: {0} is {1} due to aura {2}#{3}", Safe_UnitName(u), aura.Spell.Mechanic.ToString(), aura.Name, aura.SpellId);
                }
            }

            return aura;
        }


        public static bool IsFleeing( IEnumerable<WoWUnit> ul )
        {
            return ul != null && ul.Any(u => IsFleeing(u));
        }

        public static bool IsFleeing( WoWUnit u)
        {
            bool isFleeing = false;
            if ( u != null)
            {
                WoWAura aura = 
                    (from a in u.GetAllAuras()
                     where a.Spell.Mechanic == WoWSpellMechanic.Fleeing 
                     select a ).FirstOrDefault();
                if (aura != null)
                {
                    isFleeing = true;
                    Dlog("IsFleeing:  {0} is Fleeing due to aura {1}#{2}", Safe_UnitName(u), aura.Name, aura.SpellId);
                }
            }
            if (!!isFleeing != !!u.Fleeing)
                Dlog("IsFleeing:  IsFleeing={0} while .Fleeing={1} for {2}", isFleeing, u.Fleeing, Safe_UnitName(u));
            return isFleeing || u.Fleeing;
        }

        public static bool IsSnared(IEnumerable<WoWUnit> ul)
        {
            return ul != null && ul.Any(u => IsSnared(u));
        }

        public static bool IsSnared(WoWUnit u)
        {
            bool IsSnared = false;

            if (u != null)
            {
                WoWAura aura =
                    (from a in u.GetAllAuras()
                     where a.Spell.Mechanic == WoWSpellMechanic.Snared 
                     select a).FirstOrDefault();
                if (aura != null)
                {
                    IsSnared = true;
                    Dlog("IsSnared:  {0} is Snared due to aura {1}#{2} with {3} ms remaining", Safe_UnitName(u), aura.Name, aura.SpellId, aura.TimeLeft.TotalMilliseconds );
                }
            }

            return IsSnared;
        }

        public static bool NeedsTremor(WoWUnit u)
        {
            bool needsTremor = false;
            if (u != null)
            {
                WoWAura aura =
                    (from a in u.GetAllAuras()
                     where
                           a.Spell.Mechanic == WoWSpellMechanic.Asleep
                        || a.Spell.Mechanic == WoWSpellMechanic.Charmed 
                        || a.Spell.Mechanic == WoWSpellMechanic.Fleeing
                     select a).FirstOrDefault();
                if (aura != null)
                {
                    needsTremor = true;
                    Dlog("NeedsTremor:  {0} needs due to aura {1}#{2}", Safe_UnitName(u), aura.Name, aura.SpellId);
                }
            }

            return needsTremor;
        }

        private bool Warstomp()
        {
            if (!SpellManager.HasSpell("War Stomp"))
                ;
            else if (_count8YardEnemy < 1)
                ;
            else if (Safe_CastSpell(_me, "War Stomp"))
            {
                Slog("War Stomp: BOOM!");
                return true;
            }

            return false;
        }

        private bool Stoneform()
        {

            if (IsPVP() || IsFightStressful())
            {
                if (!SpellManager.HasSpell("Stoneform"))
                    ;
                else if (Safe_CastSpell(_me, "Stoneform"))
                {
                    Slog("Stoneform: just put on some body armor!");
                    return true;
                }
            }

            return false;
        }

        private bool Berserking()
        {
            if (IsPVP() || IsFightStressful() || !cfg.PVE_SaveForStress_DPS_Racials)
            {
                if (!SpellManager.HasSpell("Berserking"))
                    ;
                else if (Safe_CastSpell(_me, "Berserking"))
                {
                    Slog("Berserking: just broke out a can of whoop a$$!");
                    return true;
                }
            }

            return false;
        }

        private bool BloodFury()
        {
            if (IsPVP() || IsFightStressful() || !cfg.PVE_SaveForStress_DPS_Racials)
            {
                if (!SpellManager.HasSpell("Blood Fury"))
                    ;
                else if (Safe_CastSpell(_me, "Blood Fury"))
                {
                    Slog("Blood Fury: just broke out a can of whoop a$$!");
                    return true;
                }
            }

            return false;
        }

        private bool RocketBarrage()
        {
            if (_me.Race == WoWRace.Goblin && _me.GotTarget)
                return Safe_CastSpell(_me.CurrentTarget, "Rocket Barrage");

            return false;
        }

        private bool RocketJump()
        {
            if (_me.Race == WoWRace.Goblin)
                return Safe_CastSpell(_me, "Rocket Jump");

            return false;
        }

        private bool GiftOfTheNaaru()
        {
            if (!SpellManager.HasSpell("Gift of the Naaru"))
                ;
            else if (Safe_CastSpell(_me, "Gift of the Naaru"))
            {
                Slog("Gift of the Naaru: it's good to be Draenei!");
                return true;
            }

            return false;
        }

        private bool Lifeblood()
        {
            if (!SpellManager.HasSpell("Lifeblood"))
                ;
            else if (Safe_CastSpell(_me, "Lifeblood"))
            {
                Slog("Lifeblood: the benefit of being a flower picker!");
                return true;
            }

            return false;
        }


        /*
		 * Totem Manager Declarations -- slot numbers correlate to LUA usage
		 */

#if COLLECT_NEW_PURGEABLES
        static Dictionary<int, string> dictPurgeables = new Dictionary<int, string>();

        private static void PurgeableSpellCollector()
        {
            List<int> l =
               (from o in ObjectManager.ObjectList
                where o is WoWPlayer && o.DistanceSqr <= 2500
                let p = o.ToPlayer()
                where
                    Safe_IsEnemyPlayer(p)
                    && p.HealthPercent > 1
                from dbf in p.Buffs
                where
                    dbf.Value.Spell.DispelType == WoWDispelType.Magic
                    && !dictPurgeables.ContainsKey(dbf.Value.SpellId)
                    && !_hashPurgeWhitelist.Contains(dbf.Value.SpellId)
                select dbf.Value.SpellId
                ).ToList();

            foreach (int id in l)
            {
                dictPurgeables.Add(id, WoWSpell.FromId(id).Name);
            }
        }

        private static void PurgeableSpellReport()
        {
            Slog(" ");
            Slog("\nID  DEBUFF-NAME\n-----------------------");
            foreach (KeyValuePair<int, string> dbf in dictPurgeables)
            {
                Slog("    <Spell Id=\"{0}\" Name=\"{1}\" />", dbf.Key, dbf.Value);
            }

            Slog(" ");
        }
#endif


        /*
		 * Following list of id's come from:
		 * 
		 *      http://www.wowhead.com
		 * 
		 */
#if AUTO_DETECT_POTIONS
        // Potion EntryId's taken from WoWHead
        public static readonly List<uint> _potionHealthEID = new List<uint>()
		{
			//=== RESTORATION POTIONS (HEALTH AND MANA)
			40077,  // Crazy Alchemist's Potion 3500 (Alchemist)
			34440,  // Mad Alchemist's Potion 2750   (Alchemist)
			40087,  // Powerful Rejuvenation Potion 4125
			22850,  // Super Rejuvenation Potion 2300
			18253,  // Major Rejuvenation Potion 1760
			9144,   // Wildvine Potion 1500
			2456,   // Minor Rejuvenation Potion 150

			//=== HEALTH POTIONS 
			33447,  // Runic Healing Potion 4500

			43569,  // Endless Healing Potion  2500
			43531,  // Argent Healing Potion  2500
			32947,  // Auchenai Healing Potion  2500
			39671,  // Resurgent Healing Potion 2500
			22829,  // Super Healing Potion 2500
			33934,  // Crystal Healing Potion 2500
			23822,  // Healing Potion Injector 2500
			33092,  // Healing Potion Injector 2500

			31852,  // Major Combat Healing Potion 1750
			31853,  // Major Combat Healing Potion 1750
			31839,  // Major Combat Healing Potion 1750
			31838,  // Major Combat Healing Potion 1750
			13446,  // Major Healing Potion 1750
			28100,  // Volatile Healing Potion 1750 

			18839,  // Combat Healing Potion 900 
			3928,   // Superior Healing Potion 900

			1710,   // Greater Healing Potion  585

			929,    // Healing Potion  360

			4596,   // Discolored Healing Potion 180
			858,    // Lesser Healing Potion 180

			118     // Minor Healing Potion 90
			
		};

        // Mana Potion EntryId's taken from WoWHead
        public static readonly List<uint> _potionManaEID = new List<uint>()
		{
			//=== RESTORATION POTIONS (HEALTH AND MANA)
			40077,  // Crazy Alchemist's Potion 4400 (Alchemist)
			34440,  // Mad Alchemist's Potion 2750   (Alchemist)
			40087,  // Powerful Rejuvenation Potion 4125
			22850,  // Super Rejuvenation Potion 2300
			18253,  // Major Rejuvenation Potion 1760
			9144,   // Wildvine Potion 1500
			2456,   // Minor Rejuvenation Potion 150

			//=== MANA POTIONS 
		43570, // 3000 Endless Mana Potion 
		33448, // 4400 Runic Mana Potion
		40067, // 3000 Icy Mana Potion
		31677, // 3200 Fel Mana Potion
		33093, // 3000 Mana Potion Injector
		43530, // 3000 Argent Mana Potion
		32948, // 3000 Auchenai Mana Potion
		22832, // 3000 Super Mana Potion
		28101, // 2250 Unstable Mana Potion
		13444, // 2250 Major Mana Potion
		13443, // 1500 Superior Mana Potion
		 6149, // 900 Greater Mana Potion
		 3827, // 585 Mana Potion
		 3385, // 360 Lesser Mana Potion
		 2455, // 180 Minor Mana Potion
	};
#endif

        // Bandage EntryId's taken from WoWHead
        public static readonly List<uint> _bandageEID = new List<uint>()
		{
			// ID,  BANDAGE NAME,         (Level, Healing)
		34722,  // Heavy Frostweave,    (400, 5800)
		34721,  // Frostweave,      (350, 4800)
		21991,  // Heavy Netherweave,   (325, 3400)
		21990,  // Netherweave,     (300, 2800)
		14530,  // Heavy Runecloth, (225, 2000)
		14529,  // Runecloth,       (200, 1360)
		8545,   // Heavy Mageweave, (175, 1104)
		8544,   // Mageweave        (150, 800)
		6451,   // Heavy Silk,      (125, 640)
		6450,   // Silk         (100, 400)
		3531,   // Heavy Wool,      ( 75, 301)
			3530,   // Wool         ( 50, 161)
		2581,   // Heavy Linen,     ( 20, 114)
		1251,   // Linen        (  1, 66)
			
		};

#if SUPPORT_IMMUNITY_DETECTION
        public static readonly HashSet<uint> _listFrostImmune = new HashSet<uint>()
	{
		24601,  // Steam Rager (65-71)
		17358,  // Fouled Water Spirit (18-19)
		3950,  // Minor Water Guardian (25)
		14269,  // Seeker Aqualon (21)
		3917,  // Befouled Water Elemental (23-25)
		10757,  // Boiling Elemental (27-28)
		10756,  // Scalding Elemental (28-29)
		2761,  // Cresting Exile (38-39)
		691,  // Lesser Water Elemental (35-37)
		5461,  // Sea Elemental (46-49)
		5462,  // Sea Spray (45-48)
		8837,  // Muck Splash (47-49))
		7132,  // Toxic Horror(53-54)
		14458,  // Watery Invader (56-58)
		20792,  // Bloodscale Elemental (62-63) 
		20090,  // Bloodscale Sentry (62-63)
		20079,  // Darkcrest Sentry (61-62)
		17153,  // Lake Spirit (64-65)
		17155,  // Lake Surger (64-66)
		17154,  // Muck Spawn (63-66)
		21059,  // Enraged Water Spirit (68-69)
		25419,  // Boiling Spirit (68-70)
		25715,  // Frozen Elemental (65-71)
		23919,  // Ice Elemental (64-69)
		24228,  // Iceshard Elemental (70-71)   
		26316,  // Crystalline Ice Elemental (73-74)
		16570,  // Crazed Water Spirit (71-76)
		28411,  // Frozen Earth (76-77)
		29436,  // Icetouched Earthrager (69-75)
		29844,  // Icebound Revenant (78-80)
		30633,  // Water Terror (77-78)
	};

        public static readonly HashSet<uint> _listFireImmune = new HashSet<uint>()
	{
		6073, // Searing Infernal
		4038, // Burning Destroyer
		4037, // Burning Ravager
		4036, // Rogue Flame Spirit
		2760, // Burning Exile
		5850, // Blazing Elemental
		5852, // Inferno Elemental
		5855, // Magma Elemental
		9878, // Entropic Beast
		9879, // Entropic Horror
		14460, // Blazing Invader
		6521, // Living Blaze
		6520, // SScorching Elemental       
		20514, // Searing Elemental
		21061, // Enraged Fire Spirit   
		29504, // Seething Revenant 
		6073, // Searing Infernal
		7136, // Infernal Sentry
		7135, // Infernal Bodyguard
		21419, // Infernal Attacker

		19261, // Infernal Warbringer
        25417, // Raging Boiler
	};

        public static readonly HashSet<uint> _listNatureImmune = new HashSet<uint>()
	{
		18062, // Enraged Crusher
		11577, // Whirlwind Stormwalker
		11578, // Whirlwind Shredder
		11576, // Whirlwind Ripper
		4661, // Gelkis Rumbler
		832, // Dust Devil      
		4034, // Enraged Stone Spirit
		4035, // Furious Stone Spirit
		4499, // Rok'Alim the Pounder
		9377, // Swirling Vortex
		4120, // Thundering Boulderkin
		2258, // Stone Fury
		2592, // Rumbling Exile
		2762, // Thundering Exile
		2791, // Enraged Rock Elemental
		2919, // Fam'retor Guardian
		2736, // GGreater Rock Elemental    
		2735, // Lesser Rock Elemental
		92, // Rock Elemental   
		8667, // Gusting Vortex
		9396, // Ground Pounder
		5465, // Land Rager
		9397, // Living Storm
		14462, // Thundering Invader
		11745, // Cyclone Warrior   
		11746, // Desert Rumbler
		11744, // Dust Stormer
		14455, // Whirling Invader  
		17158, // Dust Howler   
		18062, // Enraged Crusher
		17160, // Living Cyclone    
		17157, // Shattered Rumbler
		17159, // Storm Rager
		17156, // Tortured Earth Spirit
		18882, // Sundered Thunderer
		20498, // Sundered Shard
		21060, // Enraged Air Spirit
		22115, // Enraged Earth Shard
		21050, // Enraged Earth Spirit      
		25415, // Enraged Tempest
		24229, // Howling Cyclone   
		24340, // Rampaging Earth Elemental     
		26407, // Lightning Sentry
		28784, // Altar Warden
		29124, // Lifeblood Elemental   
		28858, // Storm Revenant
	};
#endif

#if OLD_STYLE_FEAR_CHECK
        public static readonly HashSet<uint> _hashTremorTotemMobs = new HashSet<uint>()
	{
		// NPC Abilities Fear Abilities:  http://www.wowhead.com/spells=-8?filter=me=5;dt=1

		30284,  //  Ahn'kahet: Old Kingdom,   Bonegrinder
		2256,   //  Alterac Mountains,     Crushridge Enforcer
		19906,  //  Alterac Mountains,    Usha Eyegouge
		11947,  //  Alterac Valley,   Captain Galvangar
		30231,  //  Arathi Highlands,     Radulf Leder
		19905,  //  Arathi Highlands,     The Black Bride
		19908,  //  Ashenvale,    Su'ura Swiftarrow
		6116,   //  Azshara,      Highborne Apparition
		22855,  //  Black Temple,     Illidari Nightlord
        39700,  //  Blackrock Caverns, Beauty
		9018,   //  Blackrock Depths,      High Interrogator Gerstahn <Twilight's Hammer Interrogator>
		16059,  //  Blackrock Depths,     Theldren
		10162,  //  Blackrock Spire/Blackwing Lair,   Lord Victor Nefarius
		11583,  //  Blackrock Spire/Blackwing Lair,   Nefarian
		23353,  //  Blade's Edge Mountains,    Braxxus
		20735,  //  Blade's Edge Mountains,   Dorgok
		20889,  //  Blade's Edge Mountains,   Ethereum Prisoner (Group Energy Ball)
		22204,  //  Blade's Edge Mountains,   Fear Fiend
		23055,  //  Blade's Edge Mountains,   Felguard Degrader
		8716,   //  Blasted Lands,    Dreadlord
		17664,  //  Bloodmyst Isle,   Matis the Cruel <Herald�of�Sironas>
		32322,  //  Dalaran,      Gold Warrior
		32321,  //  Dalaran,      Green Warrior
		34988,  //  Darnassus,    Landuen Moonclaw
		34989,  //  Darnassus,    Rissa Shadeleaf
		14325,  //  Dire Maul,    Captain Kromcrush
		11455,  //  Dire Maul,    Wildspawn Felsworn
		14324,  //  Dire Maul North,      Cho'Rush the Observer
		27483,  //  Drak'Tharon Keep,     King Dred
		26830,  //  Drak'Tharon Keep,     Risen Drakkari Death Knight
		40195,  //  Durotar,      Mindless Troll
		1200,   //  Duskwood,      Morbent Fel
		202,    //  Duskwood,      Skeletal Horror
		12339,  //  Eastern Plaguelands,       Demetria <The Scarlet Oracle>
		8521,   //  Eastern Plaguelands,      Blighted Horror
		8542,   //  Eastern Plaguelands,      Death Singer
		8528,   //  Eastern Plaguelands,      Dread Wearer
		8600,   //  Eastern Plaguelands,      Plaguebat
		10938,  //  Eastern Plaguelands,      Redpath the Corrupted
		113,    //  Elwynn Forest,    Stonetusk Boar
		16329,  //  Ghostlands,   Dar'khan Drathir
		11445,  //  Gordok Captain,   
		21350,  //  Gruul's Lair,     Gronn-Priest
		28961,  //  Halls of Lightning,   Titanium Siegebreaker
		17000,  //  Hellfire Peninsula,   Aggonis
		17478,  //  Hellfire Peninsula,   Bleeding Hollow Scryer
		19424,  //  Hellfire Peninsula,   Bleeding Hollow Tormenter
		17014,  //  Hellfire Peninsula,   Collapsing Voidwalker
		2215,   //  Hillsbrad Foothills,      High Executor Darthalia
		17968,  //  Hyjal Summit,     Archimonde
		32278,  //  Icecrown,     Harbinger of Horror
		31222,  //  Icecrown,     Khit'rix the Dark Master
		31775,  //  Icecrown,     Thexal Deathchill
		37955,  //  Icecrown Citidel,      Blood-Queen Lana'thel
		34991,  //  Ironforge,    Borim Goldhammer
		17521,  //  Karazhan,     The Big Bad Wolf
		24558,  //  Magister's Terrace,   Ellrys Duskhallow
		24559,  //  Magister's Terrace,   Warlord Salaris
		11982,  //  Molten Core,       Magmadar
		17152,  //  Nagrand,       Felguard Legionnaire
		18870,  //  Netherstorm�,   Voidshrieker
		17833,  //  Old Hillsbrad Foothills,      Durnholde Warden
		34955,  //  Orgrimmar,    Karg Skullgore
		30610,  //  Orgrimmar,    War-Hunter Molog
		10508,  //  Ras Frostwhisperer,   Scholomance
		15391,  //  Ruins of Ahn'Qiraj,   Captain Qeez
		6490,   //  Scarlet Monestary,     Azshir the Sleepless
		4542,   //  Scarlet Monestary,    High Inquisitor Fairbanks
		10502,  //  Scholomance,      Lady Illucia Barov
		10470,  //  Scholomance,      Scholomance Neophyte
		8280,   //  Searing Gorge,     Shleipnarr
		18325,  //  Sethekk Halls,    Sethekk Prophet
		18796,  //  Shadow Labryinth,     Fel Overseer
		18731,  //  Shadow Labyrinth,     Ambassador Hellmaw
		19826,  //  Shadowmoon Valley,    Dark Conclave Shadowmancer
		21166,  //  Shadowmoon Valley,    Illidari Dreadlord
		22074,  //  Shadowmoon Valley,    Illidari Mind Breaker <The�Crimson�Sigil>
		22006,  //  Shadowmoon Valley,    Shadlowlord Deathwill
		21314,  //  Shadowmoon Valley,    Terrormaster
		15200,  //  Silithus�,      Twilight Keeper Mayna <Twilight's�Hammer>
		15308,  //  Silithus�,      Twilight Prophet <Twilight's�Hammer>
		40413,  //  Silvermoon City,      Alenjon Sunblade
		34998,  //  Stormwind City,   Alison Devay
		30578,  //  Stormwind City,   Bethany Aldire
		34997,  //  Stormwind City,   Devin Fardale
		20381,  //  Stormwind City,   Jovil
		1559,   //  Stranglethorn Vale,    King Mukla
		680,    //  Stranglethorn Vale,    Mosh'Ogg Lord
		2464,   //  Stranglethorn Vale,   Commander Aggro'gosh
		469,    //  Stranglethorn Vale,   Lieutenant Doren
		10812,  //  Stratholme,   Grand Crusader Dathrohan
		11143,  //  Stratholme,   Postmaster Malown
		16102,  //  Stratholme,   Sothos
		5271,   //  Sunken Temple,    Atal'ai Deathwalker
		25370,  //  Sunwell Plateau,      Sunblade Dusk Priest
		15311,  //  Temple of Ahn'Qiraj,      Anubisath Warder
		15543,  //  Temple of Ahn'Qiraj,      Princess Yaui
		15252,  //  Temple of Ahn'Qiraj,      Qiraji Champion
		23067,  //  Terokkar Forest,      Talonpriest Skizzik
		21200,  //  Terokkar Forrest,     Screeching Spirit
		18686,  //  Terrokar Forest,      Doomsayer Jurim
		20912,  //  The Arcatraz,     Harbinger Skyriss
		20875,  //  The Arcatraz,     Negaton Screamer
		3393,   //  The Barrens,      Captain Fairmount
		14781,  //  The Barrens,      Captain Shatterskull
		3338,   //  The Barrens,      Sergra Darkthorn
		21104,  //  The Black Morass,     Rift Keeper
		642,    //  The Deadmines,     Sneed's Shredder <Lumbermaster>
		30581,  //  The Exodar,   Buhurda
		34987,  //  The Exodar,   Hunara
		20118,  //  The Exodar,   Jihi
		34986,  //  The Exodar,   Liedel the Just
		20119,  //  The Exodar,   Mahul
		20382,  //  The Exodar,   Mitia
		35027,  //  The Exotar,   Erutor
		36497,  //  The Forge of Souls,   Bronjahm
		12496,  //  The Hinterlands,      Dreamtracker
		26798,  //  The Nexus,    Commander Kolurg
		26796,  //  The Nexus,    Commander Stoutbeard
		17694,  //  The Shattered Halls,      Shadowmoon Darkcaster
		16809,  //  The Shattered Halls,      Warbringer O'mrogg
		17957,  //  The Slave Pens,   Coilfang Champion
		17801,  //  The Steamvault,   Coilfang Siren
		1663,   //  The Stockade,     Dextren Ward
		34466,  //  Trial of the Crusader,    Anthar Forgemender <Priest>
		34473,  //  Trial of the Crusader,    Brienna Nightfell <Priest>
		34447,  //  Trial of the Crusader,    Caiphus the Stern <Priest>
		34450,  //  Trial of the Crusader,    Harkzog
		34474,  //  Trial of the Crusader,    Serissa Grimdabbler
		34441,  //  Trial of the Crusader,    Vivienne Blackwhisper <Priest>
		33515,  //  Ulduar,   Auriaya
		33818,  //  Ulduar,   Twilight Adherent
		34983,  //  Undercity,    Deathstalker Fane
		347,    //  Undercity,    Grizzle Halfmane
		2804,   //  Undercity,    Kurden Bloodclaw
		20386,  //  Undercity,    Lyrlia Blackshield
		35021,  //  Undercity,    Marog
		31531,  //  Undercity,    Perfidious Dreadlord
		32391,  //  Undercity,    Perfidious Dreadlord
		30583,  //  Undercity,    Sarah Forthright
		9167,   //  Un'Goro Crater,    Frenzied Pterrordax
		9166,   //  Un'Goro Crater,   Pterrordax
		26696,  //  Utgarde Pinnacle,      Ymirjar Berserker
		5056,   //  Wailing Caverns,       Deviate Dreadfang
		3654,   //  Wailing Caverns,       Mutanus the Devourer
		1785,   //  Western Plaguelands,      Skeletal Terror
		10200,  //  Winterspring,      Rak'shiri
		24246,  //  Zul'Aman,     Darkheart
		24239,  //  Zul'Aman,     Hex Lord Malacrass
		7275,   //  Zul'Farrak,    Shadowpriest Sezz'ziz
		11830,  //  Zul'Gurub,    Hakkari Priest
		14517,  //  Zul'Gurub,    High Priestess Jeklik
		11359,  //  Zul'Gurub,    Soulflayer
	};
#endif

        #region BOSS_INITIALIZATION_LIST
        public static readonly HashSet<uint> _hashBossList = new HashSet<uint>()
        {
    // <!-- "Ragefire Chasm" -->
    11517,   // <!--Oggleflint" -->
    11520,   // <!--Taragaman the Hungerer" -->
    11518,   // <!--Jergosh the Invoker" -->
    11519,   // <!--Bazzalan" -->
    17830,   // <!--Zelemar the Wrathful" -->

    // <!-- "The Deadmines" -->
    644,   // <!--Rhahk'Zor" -->
    3586,   // <!--Miner Johnson" -->
    643,   // <!--Sneed" -->
    642,   // <!--Sneed's Shredder" -->
    1763,  // <!--Gilnid" -->
    646,   // <!--Mr. Smite" -->
    645,   // <!--Cookie" -->
    647,   // <!--Captain Greenskin" -->
    639,   // <!--Edwin VanCleef" -->
    596,   // <!--Brainwashed Noble, outside" -->
    626,   // <!--Foreman Thistlenettle, outside" -->
    599,   // <!--Marisa du'Paige, outside" -->
    47162,   // <!--Glubtok" -->
    47296,   // <!--Helix Gearbreaker" -->
    43778,   // <!--Foe Reaper 5000" -->
    47626,   // <!--Admiral Ripsnarl" -->
    47739,   // <!--Captain Cookie" -->
    49541,   // <!--Vanessa VanCleef" -->

    // <!-- "Wailing Caverns" -->
    5775,   // <!--Verdan the Everliving" -->
    3670,   // <!--Lord Pythas" -->
    3673,   // <!--Lord Serpentis" -->
    3669,   // <!--Lord Cobrahn" -->
    3654,   // <!--Mutanus the Devourer" -->
    3674,   // <!--Skum" -->
    3653,   // <!--Kresh" -->
    3671,   // <!--Lady Anacondra" -->
    5912,   // <!--Deviate Faerie Dragon" -->
    3672,   // <!--Boahn, outside" -->
    3655,   // <!--Mad Magglish, outside" -->
    3652,   // <!--Trigore the Lasher, outside" -->

    // <!-- "Shadowfang Keep" -->
    3914,   // <!--Rethilgore" -->
    3886,   // <!--Razorclaw the Butcher" -->
    4279,   // <!--Odo the Blindwatcher" -->
    3887,   // <!--Baron Silverlaine" -->
    4278,   // <!--Commander Springvale" -->
    4274,   // <!--Fenrus the Devourer" -->
    3927,   // <!--Wolf Master Nandos" -->
    14682,   // <!--Sever (Scourge invasion only)" -->
    4275,   // <!--Archmage Arugal" -->
    3872,   // <!--Deathsworn Captain" -->
    46962,   // <!--Baron Ashbury" -->
    46963,   // <!--Lord Walden" -->
    46964,   // <!--Lord Godfrey" -->

    // <!-- "Blackfathom Deeps" -->
    4887,   // <!--Ghamoo-ra" -->
    4831,   // <!--Lady Sarevess" -->
    12902,   // <!--Lorgus Jett" -->
    6243,   // <!--Gelihast" -->
    12876,   // <!--Baron Aquanis" -->
    4830,   // <!--Old Serra'kis" -->
    4832,   // <!--Twilight Lord Kelris" -->
    4829,   // <!--Aku'mai" -->

    // <!-- "Stormwind Stockade" -->
    1716,   // <!--Bazil Thredd" -->
    1663,   // <!--Dextren Ward" -->
    1717,   // <!--Hamhock" -->
    1666,   // <!--Kam Deepfury" -->
    1696,   // <!--Targorr the Dread" -->
    1720,   // <!--Bruegal Ironknuckle" -->

    // <!-- "Razorfen Kraul" -->
    4421,   // <!--Charlga Razorflank" -->
    4420,   // <!--Overlord Ramtusk" -->
    4422,   // <!--Agathelos the Raging" -->
    4428,   // <!--Death Speaker Jargba" -->
    4424,   // <!--Aggem Thorncurse" -->
    6168,   // <!--Roogug" -->
    4425,   // <!--Blind Hunter" -->
    4842,   // <!--Earthcaller Halmgar" -->

    // <!-- "Gnomeregan" -->
    7800,   // <!--Mekgineer Thermaplugg" -->
    7079,   // <!--Viscous Fallout" -->
    7361,   // <!--Grubbis" -->
    6235,   // <!--Electrocutioner 6000" -->
    6229,   // <!--Crowd Pummeler 9-60" -->
    6228,   // <!--Dark Iron Ambassador" -->
    6231,   // <!--Techbot, outside" -->

    // <!-- "Scarlet Monastery: The Graveyard" -->
    3983,   // <!--Interrogator Vishas" -->
    6488,   // <!--Fallen Champion" -->
    6490,   // <!--Azshir the Sleepless" -->
    6489,   // <!--Ironspine" -->
    14693,   // <!--Scorn" -->
    4543,   // <!--Bloodmage Thalnos" -->
    23682,   // <!--Headless Horseman" -->
    23800,   // <!--Headless Horseman" -->

    // <!-- "Scarley Monastery: Library" -->
    3974,   // <!--Houndmaster Loksey" -->
    6487,   // <!--Arcanist Doan" -->

    // <!-- "Scarley Monastery: Armory" -->
    3975,   // <!--Herod" -->

    // <!-- "Scarley Monastery: Cathedral" -->
    4542,   // <!--High Inquisitor Fairbanks" -->
    3976,   // <!--Scarlet Commander Mograine" -->
    3977,   // <!--High Inquisitor Whitemane" -->

    // <!-- "Razorfen Downs" -->
    7355,   // <!--Tuten'kash" -->
    14686,   // <!--Lady Falther'ess" -->
    7356,   // <!--Plaguemaw the Rotting" -->
    7357,   // <!--Mordresh Fire Eye" -->
    8567,   // <!--Glutton" -->
    7354,   // <!--Ragglesnout" -->
    7358,   // <!--Amnennar the Coldbringer" -->

    // <!-- "Uldaman" -->
    7057,   // <!--Digmaster Shovelphlange" -->
    6910,   // <!--Revelosh" -->
    7228,   // <!--Ironaya" -->
    7023,   // <!--Obsidian Sentinel" -->
    7206,   // <!--Ancient Stone Keeper" -->
    7291,   // <!--Galgann Firehammer" -->
    4854,   // <!--Grimlok" -->
    2748,   // <!--Archaedas" -->
    6906,   // <!--Baelog" -->

    // <!-- "Zul'Farrak" -->
    10082,   // <!--Zerillis" -->
    10080,   // <!--Sandarr Dunereaver" -->
    7272,   // <!--Theka the Martyr" -->
    8127,   // <!--Antu'sul" -->
    7271,   // <!--Witch Doctor Zum'rah" -->
    7274,   // <!--Sandfury Executioner" -->
    7275,   // <!--Shadowpriest Sezz'ziz" -->
    7796,   // <!--Nekrum Gutchewer" -->
    7797,   // <!--Ruuzlu" -->
    7267,   // <!--Chief Ukorz Sandscalp" -->
    10081,   // <!--Dustwraith" -->
    7795,   // <!--Hydromancer Velratha" -->
    7273,   // <!--Gahz'rilla" -->
    7608,   // <!--Murta Grimgut" -->
    7606,   // <!--Oro Eyegouge" -->
    7604,   // <!--Sergeant Bly" -->

    // <!-- "Maraudon" -->
    13718,   // <!--The Nameless Prophet (Pre-instance)" -->
    13742,   // <!--Kolk [The First Khan]" -->
    13741,   // <!--Gelk [The Second Khan]" -->
    13740,   // <!--Magra [The Third Khan]" -->
    13739,   // <!--Maraudos [The Fourth Khan]" -->
    12236,   // <!--Lord Vyletongue" -->
    13738,   // <!--Veng [The Fifth Khan]" -->
    13282,   // <!--Noxxion" -->
    12258,   // <!--Razorlash" -->
    12237,   // <!--Meshlok the Harvester" -->
    12225,   // <!--Celebras the Cursed" -->
    12203,   // <!--Landslide" -->
    13601,   // <!--Tinkerer Gizlock" -->
    13596,   // <!--Rotgrip" -->
    12201,   // <!--Princess Theradras" -->

    // <!-- "Temple of Atal'Hakkar" -->
    1063,   // <!--Jade" -->
    5400,   // <!--Zekkis" -->
    5713,   // <!--Gasher" -->
    5715,   // <!--Hukku" -->
    5714,   // <!--Loro" -->

    5717,   // <!--Mijan" -->
    5712,   // <!--Zolo" -->
    5716,   // <!--Zul'Lor" -->
    5399,   // <!--Veyzhak the Cannibal" -->
    5401,   // <!--Kazkaz the Unholy" -->
    8580,   // <!--Atal'alarion" -->
    8443,   // <!--Avatar of Hakkar" -->
    5711,   // <!--Ogom the Wretched" -->
    5710,   // <!--Jammal'an the Prophet" -->
    5721,   // <!--Dreamscythe" -->
    5720,   // <!--Weaver" -->
    5719,   // <!--Morphaz" -->
    5722,   // <!--Hazzas" -->
    5709,   // <!--Shade of Eranikus" -->

    // <!-- "The Blackrock Depths: Detention Block" -->
    9018,   // <!--High Interrogator Gerstahn" -->

    // <!-- "The Blackrock Depths: Halls of the Law" -->
    9025,   // <!--Lord Roccor" -->
    9319,   // <!--Houndmaster Grebmar" -->

    // <!-- "The Blackrock Depths: Ring of Law (Arena)" -->
    9031,   // <!--Anub'shiah" -->
    9029,   // <!--Eviscerator" -->
    9027,   // <!--Gorosh the Dervish" -->
    9028,   // <!--Grizzle" -->
    9032,   // <!--Hedrum the Creeper" -->
    9030,   // <!--Ok'thor the Breaker" -->
    16059,   // <!--Theldren" -->

    // <!-- "The Blackrock Depths: Outer Blackrock Depths" -->
    9024,   // <!--Pyromancer Loregrain" -->
    9041,   // <!--Warder Stilgiss" -->
    9042,   // <!--Verek" -->

    9476,   // <!--Watchman Doomgrip" -->
    9056,   // <!--Fineous Darkvire" -->
    9017,   // <!--Lord Incendius" -->
    9016,   // <!--Bael'Gar" -->
    9033,   // <!--General Angerforge" -->
    8983,   // <!--Golem Lord Argelmach" -->

    // <!-- "The Blackrock Depths: Grim Guzzler" -->
    9543,   // <!--Ribbly Screwspigot" -->
    9537,   // <!--Hurley Blackbreath" -->
    9502,   // <!--Phalanx" -->
    9499,   // <!--Plugger Spazzring" -->
    23872,   // <!--Coren Direbrew" -->

    // <!-- "The Blackrock Depths: Inner Blackrock Depths" -->
    9156,   // <!--Ambassador Flamelash" -->
    8923,   // <!--Panzor the Invincible" -->
    17808,   // <!--Anger'rel" -->
    9039,   // <!--Doom'rel" -->
    9040,   // <!--Dope'rel" -->
    9037,   // <!--Gloom'rel" -->
    9034,   // <!--Hate'rel" -->
    9038,   // <!--Seeth'rel" -->
    9036,   // <!--Vile'rel" -->
    9938,   // <!--Magmus" -->
    10076,   // <!--High Priestess of Thaurissan" -->
    8929,   // <!--Princess Moira Bronzebeard" -->
    9019,   // <!--Emperor Dagran Thaurissan" -->

    // <!-- "Dire Maul: Arena" -->
    11447,   // <!--Mushgog" -->
    11498,   // <!--Skarr the Unbreakable" -->
    11497,   // <!--The Razza" -->

    // <!-- "Dire Maul: East" -->
    14354,   // <!--Pusillin" -->
    14327,   // <!--Lethtendris" -->
    14349,   // <!--Pimgib" -->
    13280,   // <!--Hydrospawn" -->
    11490,   // <!--Zevrim Thornhoof" -->
    11492,   // <!--Alzzin the Wildshaper" -->
    16097,   // <!--Isalien" -->

    // <!-- "Dire Maul: North" -->
    14326,   // <!--Guard Mol'dar" -->
    14322,   // <!--Stomper Kreeg" -->
    14321,   // <!--Guard Fengus" -->
    14323,   // <!--Guard Slip'kik" -->
    14325,   // <!--Captain Kromcrush" -->
    14324,   // <!--Cho'Rush the Observer" -->
    11501,   // <!--King Gordok" -->

    // <!-- "Dire Maul: West" -->
    11489,   // <!--Tendris Warpwood" -->
    11487,   // <!--Magister Kalendris" -->
    11467,   // <!--Tsu'zee" -->
    11488,   // <!--Illyanna Ravenoak" -->
    14690,   // <!--Revanchion (Scourge Invasion)" -->
    11496,   // <!--Immol'thar" -->
    14506,   // <!--Lord Hel'nurath" -->
    11486,   // <!--Prince Tortheldrin" -->

    // <!-- "Lower Blackrock Spire" -->
    10263,   // <!--Burning Felguard" -->
    9218,   // <!--Spirestone Battle Lord" -->
    9219,   // <!--Spirestone Butcher" -->
    9217,   // <!--Spirestone Lord Magus" -->
    9196,   // <!--Highlord Omokk" -->

    9236,   // <!--Shadow Hunter Vosh'gajin" -->
    9237,   // <!--War Master Voone" -->
    16080,   // <!--Mor Grayhoof" -->
    9596,   // <!--Bannok Grimaxe" -->
    10596,   // <!--Mother Smolderweb" -->
    10376,   // <!--Crystal Fang" -->
    10584,   // <!--Urok Doomhowl" -->
    9736,   // <!--Quartermaster Zigris" -->
    10220,   // <!--Halycon" -->
    10268,   // <!--Gizrul the Slavener" -->
    9718,   // <!--Ghok Bashguud" -->
    9568,   // <!--Overlord Wyrmthalak" -->

    // <!-- "Stratholme: Scarlet Stratholme" -->
    10393,   // <!--Skul" -->
    14684,   // <!--Balzaphon (Scourge Invasion)" -->

    // <!-- "11082, Name="Stratholme Courier" -->
    11058,   // <!--Fras Siabi" -->
    10558,   // <!--Hearthsinger Forresten" -->
    10516,   // <!--The Unforgiven" -->
    16387,   // <!--Atiesh" -->
    11143,   // <!--Postmaster Malown" -->
    10808,   // <!--Timmy the Cruel" -->
    11032,   // <!--Malor the Zealous" -->
    11120,   // <!--Crimson Hammersmith" -->
    10997,   // <!--Cannon Master Willey" -->
    10811,   // <!--Archivist Galford" -->
    10813,   // <!--Balnazzar" -->
    16101,   // <!--Jarien" -->
    16102,   // <!--Sothos" -->

    // <!-- "Stratholme: Undead Stratholme" -->
    10809,   // <!--Stonespine" -->
    10437,   // <!--Nerub'enkan" -->
    10436,   // <!--Baroness Anastari" -->
    11121,   // <!--Black Guard Swordsmith" -->
    10438,   // <!--Maleki the Pallid" -->
    10435,   // <!--Magistrate Barthilas" -->
    10439,   // <!--Ramstein the Gorger" -->
    10440,   // <!--Baron Rivendare (Stratholme)" -->

    // <!-- "Stratholme: Defenders of the Chapel" -->
    17913,   // <!--Aelmar the Vanquisher" -->
    17911,   // <!--Cathela the Seeker" -->
    17910,   // <!--Gregor the Justiciar" -->
    17914,   // <!--Vicar Hieronymus" -->
    17912,   // <!--Nemas the Arbiter" -->

    // <!-- "Scholomance" -->
    14861,   // <!--Blood Steward of Kirtonos" -->
    10506,   // <!--Kirtonos the Herald" -->
    14695,   // <!--Lord Blackwood (Scourge Invasion)" -->
    10503,   // <!--Jandice Barov" -->
    11622,   // <!--Rattlegore" -->
    14516,   // <!--Death Knight Darkreaver" -->
    10433,   // <!--Marduk Blackpool" -->
    10432,   // <!--Vectus" -->
    16118,   // <!--Kormok" -->
    10508,   // <!--Ras Frostwhisper" -->
    10505,   // <!--Instructor Malicia" -->
    11261,   // <!--Doctor Theolen Krastinov" -->
    10901,   // <!--Lorekeeper Polkelt" -->
    10507,   // <!--The Ravenian" -->
    10504,   // <!--Lord Alexei Barov" -->
    10502,   // <!--Lady Illucia Barov" -->
    1853,   // <!--Darkmaster Gandling" -->

    // <!-- "Upper Blackrock Spire" -->
    9816,   // <!--Pyroguard Emberseer" -->
    10264,   // <!--Solakar Flamewreath" -->
    10509,   // <!--Jed Runewatcher" -->
    10899,   // <!--Goraluk Anvilcrack" -->
    10339,   // <!--Gyth" -->
    10429,   // <!--Warchief Rend Blackhand" -->
    10430,   // <!--The Beast" -->
    16042,   // <!--Lord Valthalak" -->
    10363,   // <!--General Drakkisath" -->

    // <!-- "Zul'Gurub" -->
    14517,   // <!--High Priestess Jeklik" -->
    14507,   // <!--High Priest Venoxis" -->
    14510,   // <!--High Priestess Mar'li" -->
    11382,   // <!--Bloodlord Mandokir" -->
    15114,   // <!--Gahz'ranka" -->
    14509,   // <!--High Priest Thekal" -->
    14515,   // <!--High Priestess Arlokk" -->
    11380,   // <!--Jin'do the Hexxer" -->
    14834,   // <!--Hakkar" -->
    15082,   // <!--Gri'lek" -->
    15083,   // <!--Hazza'rah" -->
    15084,   // <!--Renataki" -->
    15085,   // <!--Wushoolay" -->

    // <!-- "Onyxia's Lair" -->
    10184,   // <!--Onyxia" -->

    // <!-- "Molten Core" -->
    12118,   // <!--Lucifron" -->
    11982,   // <!--Magmadar" -->
    12259,   // <!--Gehennas" -->
    12057,   // <!--Garr" -->
    12056,   // <!--Baron Geddon" -->
    12264,   // <!--Shazzrah" -->
    12098,   // <!--Sulfuron Harbinger" -->
    11988,   // <!--Golemagg the Incinerator" -->
    12018,   // <!--Majordomo Executus" -->
    11502,   // <!--Ragnaros" -->

    // <!-- "Blackwing Lair" -->
    12435,   // <!--Razorgore the Untamed" -->
    13020,   // <!--Vaelastrasz the Corrupt" -->
    12017,   // <!--Broodlord Lashlayer" -->
    11983,   // <!--Firemaw" -->
    14601,   // <!--Ebonroc" -->
    11981,   // <!--Flamegor" -->
    14020,   // <!--Chromaggus" -->
    11583,   // <!--Nefarian" -->
    12557,   // <!--Grethok the Controller" -->
    10162,   // <!--Lord Victor Nefarius [Lord of Blackrock] (Also found in Blackrock Spire)" -->

    // <!-- "Ruins of Ahn'Qiraj" -->
    15348,   // <!--Kurinnaxx" -->
    15341,   // <!--General Rajaxx" -->
    15340,   // <!--Moam" -->
    15370,   // <!--Buru the Gorger" -->
    15369,   // <!--Ayamiss the Hunter" -->
    15339,   // <!--Ossirian the Unscarred" -->

    // <!-- "Temple of Ahn'Qiraj" -->
    15263,   // <!--The Prophet Skeram" -->
    15511,   // <!--Lord Kri" -->
    15543,   // <!--Princess Yauj" -->
    15544,   // <!--Vem" -->
    15516,   // <!--Battleguard Sartura" -->
    15510,   // <!--Fankriss the Unyielding" -->
    15299,   // <!--Viscidus" -->
    15509,   // <!--Princess Huhuran" -->
    15276,   // <!--Emperor Vek'lor" -->
    15275,   // <!--Emperor Vek'nilash" -->
    15517,   // <!--Ouro" -->
    15727,   // <!--C'Thun" -->
    15589,   // <!--Eye of C'Thun" -->

    // <!-- "Naxxramas" -->
    30549,   // <!--Baron Rivendare (Naxxramas)" -->
    16803,   // <!--Death Knight Understudy" -->
    15930,   // <!--Feugen" -->
    15929,   // <!--Stalagg" -->

    // <!-- "Naxxramas: Spider Wing" -->
    15956,   // <!--Anub'Rekhan" -->
    15953,   // <!--Grand Widow Faerlina" -->
    15952,   // <!--Maexxna" -->

    // <!-- "Naxxramas: Abomination Wing" -->
    16028,   // <!--Patchwerk" -->
    15931,   // <!--Grobbulus" -->
    15932,   // <!--Gluth" -->
    15928,   // <!--Thaddius" -->

    // <!-- "Naxxramas: Plague Wing" -->
    15954,   // <!--Noth the Plaguebringer" -->
    15936,   // <!--Heigan the Unclean" -->
    16011,   // <!--Loatheb" -->

    // <!-- "Naxxramas: Deathknight Wing" -->
    16061,   // <!--Instructor Razuvious" -->
    16060,   // <!--Gothik the Harvester" -->

    // <!-- "Naxxramas: The Four Horsemen" -->
    16065,   // <!--Lady Blaumeux" -->
    16064,   // <!--Thane Korth'azz" -->
    16062,   // <!--Highlord Mograine" -->
    16063,   // <!--Sir Zeliek" -->

    // <!-- "Naxxramas: Frostwyrm Lair" -->
    15989,   // <!--Sapphiron" -->
    15990,   // <!--Kel'Thuzad" -->
    25465,   // <!--Kel'Thuzad" -->


    // <!-- "Hellfire Citadel: Hellfire Ramparts" -->
    17306,   // <!--Watchkeeper Gargolmar" -->
    17308,   // <!--Omor the Unscarred" -->
    17537,   // <!--Vazruden" -->
    17307,   // <!--Vazruden the Herald" -->
    17536,   // <!--Nazan" -->

    // <!-- "Hellfire Citadel: The Blood Furnace" -->
    17381,   // <!--The Maker" -->
    17380,   // <!--Broggok" -->
    17377,   // <!--Keli'dan the Breaker" -->

    // <!-- "Coilfang Reservoir: Slave Pens" -->
    25740,   // <!--Ahune" -->
    17941,   // <!--Mennu the Betrayer" -->
    17991,   // <!--Rokmar the Crackler" -->
    17942,   // <!--Quagmirran" -->

    // <!-- "Coilfang Reservoir: The Underbog" -->
    17770,   // <!--Hungarfen" -->
    18105,   // <!--Ghaz'an" -->
    17826,   // <!--Swamplord Musel'ek" -->
    17827,   // <!--Claw [Swamplord Musel'ek's Pet]" -->
    17882,   // <!--The Black Stalker" -->

    // <!-- "Auchindoun: Mana-Tombs" -->
    18341,   // <!--Pandemonius" -->
    18343,   // <!--Tavarok" -->
    22930,   // <!--Yor (Heroic)" -->
    18344,   // <!--Nexus-Prince Shaffar" -->

    // <!-- "Auchindoun: Auchenai Crypts" -->
    18371,   // <!--Shirrak the Dead Watcher" -->
    18373,   // <!--Exarch Maladaar" -->

    // <!-- "Caverns of Time: Escape from Durnholde Keep" -->
    17848,   // <!--Lieutenant Drake" -->
    17862,   // <!--Captain Skarloc" -->
    18096,   // <!--Epoch Hunter" -->
    28132,   // <!--Don Carlos" -->

    // <!-- "Auchindoun: Sethekk Halls" -->
    18472,   // <!--Darkweaver Syth" -->
    23035,   // <!--Anzu (Heroic)" -->
    18473,   // <!--Talon King Ikiss" -->

    // <!-- "Coilfang Reservoir: The Steamvault" -->
    17797,   // <!--Hydromancer Thespia" -->
    17796,   // <!--Mekgineer Steamrigger" -->
    17798,   // <!--Warlord Kalithresh" -->

    // <!-- "Auchindoun: Shadow Labyrinth" -->
    18731,   // <!--Ambassador Hellmaw" -->
    18667,   // <!--Blackheart the Inciter" -->
    18732,   // <!--Grandmaster Vorpil" -->
    18708,   // <!--Murmur" -->

    // <!-- "Hellfire Citadel: Shattered Halls" -->
    16807,   // <!--Grand Warlock Nethekurse" -->
    20923,   // <!--Blood Guard Porung (Heroic)" -->
    16809,   // <!--Warbringer O'mrogg" -->
    16808,   // <!--Warchief Kargath Bladefist" -->

    // <!-- "Caverns of Time: Opening the Dark Portal" -->
    17879,   // <!--Chrono Lord Deja" -->
    17880,   // <!--Temporus" -->
    17881,   // <!--Aeonus" -->

    // <!-- "Tempest Keep: The Mechanar" -->
    19218,   // <!--Gatewatcher Gyro-Kill" -->
    19710,   // <!--Gatewatcher Iron-Hand" -->
    19219,   // <!--Mechano-Lord Capacitus" -->
    19221,   // <!--Nethermancer Sepethrea" -->
    19220,   // <!--Pathaleon the Calculator" -->

    // <!-- "Tempest Keep: The Botanica" -->
    17976,   // <!--Commander Sarannis" -->
    17975,   // <!--High Botanist Freywinn" -->
    17978,   // <!--Thorngrin the Tender" -->
    17980,   // <!--Laj" -->
    17977,   // <!--Warp Splinter" -->

    // <!-- "Tempest Keep: The Arcatraz" -->
    20870,   // <!--Zereketh the Unbound" -->
    20886,   // <!--Wrath-Scryer Soccothrates" -->
    20885,   // <!--Dalliah the Doomsayer" -->
    20912,   // <!--Harbinger Skyriss" -->
    20904,   // <!--Warden Mellichar" -->

    // <!-- "Magisters' Terrace" -->
    24723,   // <!--Selin Fireheart" -->
    24744,   // <!--Vexallus" -->
    24560,   // <!--Priestess Delrissa" -->
    24664,   // <!--Kael'thas Sunstrider" -->

    // <!-- "Karazhan" -->
    15550,   // <!--Attumen the Huntsman" -->
    16151,   // <!--Midnight" -->
    28194,   // <!--Tenris Mirkblood (Scourge invasion)" -->
    15687,   // <!--Moroes" -->
    16457,   // <!--Maiden of Virtue" -->
    15691,   // <!--The Curator" -->
    15688,   // <!--Terestian Illhoof" -->
    16524,   // <!--Shade of Aran" -->
    15689,   // <!--Netherspite" -->
    15690,   // <!--Prince Malchezaar" -->
    17225,   // <!--Nightbane" -->
    17229,   // <!--Kil'rek" -->
    // <!-- "Chess event" -->

    // <!-- "Karazhan: Servants' Quarters Beasts" -->
    16179,   // <!--Hyakiss the Lurker" -->
    16181,   // <!--Rokad the Ravager" -->
    16180,   // <!--Shadikith the Glider" -->

    // <!-- "Karazhan: Opera Event" -->
    17535,   // <!--Dorothee" -->
    17546,   // <!--Roar" -->
    17543,   // <!--Strawman" -->
    17547,   // <!--Tinhead" -->
    17548,   // <!--Tito" -->
    18168,   // <!--The Crone" -->
    17521,   // <!--The Big Bad Wolf" -->
    17533,   // <!--Romulo" -->
    17534,   // <!--Julianne" -->

    // <!-- "Gruul's Lair" -->
    18831,   // <!--High King Maulgar" -->
    19044,   // <!--Gruul the Dragonkiller" -->

    // <!-- "Gruul's Lair: Maulgar's Ogre Council" -->
    18835,   // <!--Kiggler the Crazed" -->
    18836,   // <!--Blindeye the Seer" -->
    18834,   // <!--Olm the Summoner" -->
    18832,   // <!--Krosh Firehand" -->

    // <!-- "Hellfire Citadel: Magtheridon's Lair" -->
    17257,   // <!--Magtheridon" -->

    // <!-- "Zul'Aman: Animal Bosses" -->
    29024,   // <!--Nalorakk" -->
    28514,   // <!--Nalorakk" -->
    23576,   // <!--Nalorakk" -->
    23574,   // <!--Akil'zon" -->
    23578,   // <!--Jan'alai" -->
    28515,   // <!--Jan'alai" -->
    29023,   // <!--Jan'alai" -->
    23577,   // <!--Halazzi" -->
    28517,   // <!--Halazzi" -->
    29022,   // <!--Halazzi" -->
    24239,   // <!--Malacrass" -->

    // <!-- "Zul'Aman: Final Bosses" -->
    24239,   // <!--Hex Lord Malacrass" -->
    23863,   // <!--Zul'jin" -->

    // <!-- "Coilfang Reservoir: Serpentshrine Cavern" -->
    21216,   // <!--Hydross the Unstable" -->
    21217,   // <!--The Lurker Below" -->
    21215,   // <!--Leotheras the Blind" -->
    21214,   // <!--Fathom-Lord Karathress" -->
    21213,   // <!--Morogrim Tidewalker" -->
    21212,   // <!--Lady Vashj" -->
    21875,   // <!--Shadow of Leotheras" -->

    // <!-- "Tempest Keep: The Eye" -->
    19514,   // <!--Al'ar" -->
    19516,   // <!--Void Reaver" -->
    18805,   // <!--High Astromancer Solarian" -->
    19622,   // <!--Kael'thas Sunstrider" -->
    20064,   // <!--Thaladred the Darkener" -->
    20060,   // <!--Lord Sanguinar" -->
    20062,   // <!--Grand Astromancer Capernian" -->
    20063,   // <!--Master Engineer Telonicus" -->
    21270,   // <!--Cosmic Infuser" -->
    21269,   // <!--Devastation" -->
    21271,   // <!--Infinity Blades" -->
    21268,   // <!--Netherstrand Longbow" -->
    21273,   // <!--Phaseshift Bulwark" -->
    21274,   // <!--Staff of Disintegration" -->
    21272,   // <!--Warp Slicer" -->

    // <!-- "Caverns of Time: Battle for Mount Hyjal" -->
    17767,   // <!--Rage Winterchill" -->
    17808,   // <!--Anetheron" -->
    17888,   // <!--Kaz'rogal" -->
    17842,   // <!--Azgalor" -->
    17968,   // <!--Archimonde" -->

    // <!-- "Black Temple" -->
    22887,   // <!--High Warlord Naj'entus" -->
    22898,   // <!--Supremus" -->
    22841,   // <!--Shade of Akama" -->
    22871,   // <!--Teron Gorefiend" -->
    22948,   // <!--Gurtogg Bloodboil" -->
    23420,   // <!--Essence of Anger" -->
    23419,   // <!--Essence of Desire" -->
    23418,   // <!--Essence of Suffering" -->
    22947,   // <!--Mother Shahraz" -->
    23426,   // <!--Illidari Council" -->
    22917,   // <!--Illidan Stormrage -- Not adding solo quest IDs for now" -->
    22949,   // <!--Gathios the Shatterer" -->
    22950,   // <!--High Nethermancer Zerevor" -->
    22951,   // <!--Lady Malande" -->
    22952,   // <!--Veras Darkshadow" -->

    // <!-- "Sunwell Plateau" -->
    24891,   // <!--Kalecgos" -->
    25319,   // <!--Kalecgos" -->
    24850,   // <!--Kalecgos" -->
    24882,   // <!--Brutallus" -->
    25038,   // <!--Felmyst" -->
    25165,   // <!--Lady Sacrolash" -->
    25166,   // <!--Grand Warlock Alythess" -->
    25741,   // <!--M'uru" -->
    25315,   // <!--Kil'jaeden" -->
    25840,   // <!--Entropius" -->
    24892,   // <!--Sathrovarr the Corruptor" -->

    // <!-- "Utgarde Keep: Main Bosses" -->
    23953,   // <!--Prince Keleseth (Utgarde Keep)" -->
    27390,   // <!--Skarvald the Constructor" -->
    24200,   // <!--Skarvald the Constructor" -->
    23954,   // <!--Ingvar the Plunderer" -->
    23980,   // <!--Ingvar the Plunderer" -->

    // <!-- "Utgarde Keep: Secondary Bosses" -->
    27389,   // <!--Dalronn the Controller" -->
    24201,   // <!--Dalronn the Controller" -->

    // <!-- "The Nexus" -->
    26798,   // <!--Commander Kolurg (Heroic)" -->
    26796,   // <!--Commander Stoutbeard (Heroic)" -->
    26731,   // <!--Grand Magus Telestra" -->
    26832,   // <!--Grand Magus Telestra" -->
    26928,   // <!--Grand Magus Telestra" -->
    26929,   // <!--Grand Magus Telestra" -->
    26930,   // <!--Grand Magus Telestra" -->
    26763,   // <!--Anomalus" -->
    26794,   // <!--Ormorok the Tree-Shaper" -->
    26723,   // <!--Keristrasza" -->

    // <!-- "Azjol-Nerub" -->
    28684,   // <!--Krik'thir the Gatewatcher" -->
    28921,   // <!--Hadronox" -->
    29120,   // <!--Anub'arak" -->

    // <!-- "Ahn'kahet: The Old Kingdom" -->
    29309,   // <!--Elder Nadox" -->
    29308,   // <!--Prince Taldaram (Ahn'kahet: The Old Kingdom)" -->
    29310,   // <!--Jedoga Shadowseeker" -->
    29311,   // <!--Herald Volazj" -->
    30258,   // <!--Amanitar (Heroic)" -->

    // <!-- "Drak'Tharon Keep" -->
    26630,   // <!--Trollgore" -->
    26631,   // <!--Novos the Summoner" -->
    27483,   // <!--King Dred" -->
    26632,   // <!--The Prophet Tharon'ja" -->
    27696,   // <!--The Prophet Tharon'ja" -->

    // <!-- "The Violet Hold" -->
    29315,   // <!--Erekem" -->
    29313,   // <!--Ichoron" -->
    29312,   // <!--Lavanthor" -->
    29316,   // <!--Moragg" -->
    29266,   // <!--Xevozz" -->
    29314,   // <!--Zuramat the Obliterator" -->
    31134,   // <!--Cyanigosa" -->

    // <!-- "Gundrak" -->
    29304,   // <!--Slad'ran" -->
    29305,   // <!--Moorabi" -->
    29307,   // <!--Drakkari Colossus" -->
    29306,   // <!--Gal'darah" -->
    29932,   // <!--Eck the Ferocious (Heroic)" -->

    // <!-- "Halls of Stone" -->
    27977,   // <!--Krystallus" -->
    27975,   // <!--Maiden of Grief" -->
    28234,   // <!--The Tribunal of Ages" -->
    27978,   // <!--Sjonnir The Ironshaper" -->

    // <!-- "Halls of Lightning" -->
    28586,   // <!--General Bjarngrim" -->
    28587,   // <!--Volkhan" -->
    28546,   // <!--Ionar" -->
    28923,   // <!--Loken" -->

    // <!-- "The Oculus" -->
    27654,   // <!--Drakos the Interrogator" -->
    27447,   // <!--Varos Cloudstrider" -->
    27655,   // <!--Mage-Lord Urom" -->
    27656,   // <!--Ley-Guardian Eregos" -->

    // <!-- "Caverns of Time: Culling of Stratholme" -->
    26529,   // <!--Meathook" -->
    26530,   // <!--Salramm the Fleshcrafter" -->
    26532,   // <!--Chrono-Lord Epoch" -->
    32273,   // <!--Infinite Corruptor" -->
    26533,   // <!--Mal'Ganis" -->
    29620,   // <!--Mal'Ganis" -->

    // <!-- "Utgarde Pinnacle" -->
    26668,   // <!--Svala Sorrowgrave" -->
    26687,   // <!--Gortok Palehoof" -->
    26693,   // <!--Skadi the Ruthless" -->
    26861,   // <!--King Ymiron" -->

    // <!-- "Trial of the Champion: Alliance" -->
    35617,   // <!--Deathstalker Visceri [Grand Champion of Undercity]" -->
    35569,   // <!--Eressea Dawnsinger [Grand Champion of Silvermoon]" -->
    35572,   // <!--Mokra the Skullcrusher [Grand Champion of Orgrimmar]" -->
    35571,   // <!--Runok Wildmane [Grand Champion of the Thunder Bluff]" -->
    35570,   // <!--Zul'tore [Grand Champion of Sen'jin]" -->

    // <!-- "Trial of the Champion: Horde" -->
    34702,   // <!--Ambrose Boltspark [Grand Champion of Gnomeregan]" -->
    34701,   // <!--Colosos [Grand Champion of the Exodar]" -->
    34705,   // <!--Marshal Jacob Alerius [Grand Champion of Stormwind]" -->
    34657,   // <!--Jaelyne Evensong [Grand Champion of Darnassus]" -->
    34703,   // <!--Lana Stouthammer [Grand Champion of Ironforge]" -->

    // <!-- "Trial of the Champion: Neutral" -->
    34928,   // <!--Argent Confessor Paletress" -->
    35119,   // <!--Eadric the Pure" -->
    35451,   // <!--The Black Knight" -->

    // <!-- "Forge of Souls" -->
    36497,   // <!--Bronjahm" -->
    36502,   // <!--Devourer of Souls" -->

    // <!-- "Pit of Saron" -->
    36494,   // <!--Forgemaster Garfrost" -->
    36477,   // <!--Krick" -->
    36476,   // <!--Ick [Krick's Minion]" -->
    36658,   // <!--Scourgelord Tyrannus" -->

    // <!-- "Halls of Reflection" -->
    38112,   // <!--Falric" -->
    38113,   // <!--Marwyn" -->
    37226,   // <!--The Lich King" -->
    38113,   // <!--Marvyn" -->

    // <!-- "Obsidian Sanctum" -->
    30451,   // <!--Shadron" -->
    30452,   // <!--Tenebron" -->
    30449,   // <!--Vesperon" -->
    28860,   // <!--Sartharion" -->

    // <!-- "Vault of Archavon" -->
    31125,   // <!--Archavon the Stone Watcher" -->
    33993,   // <!--Emalon the Storm Watcher" -->
    35013,   // <!--Koralon the Flamewatcher" -->
    38433,   // <!--Toravon the Ice Watcher" -->

    // <!-- "The Eye of Eternity" -->
    28859,   // <!--Malygos" -->

    // <!-- "Ulduar: The Siege of Ulduar" -->
    33113,   // <!--Flame Leviathan" -->
    33118,   // <!--Ignis the Furnace Master" -->
    33186,   // <!--Razorscale" -->
    33293,   // <!--XT-002 Deconstructor" -->
    33670,   // <!--Aerial Command Unit" -->
    33329,   // <!--Heart of the Deconstructor" -->
    33651,   // <!--VX-001" -->

    // <!-- "Ulduar: The Antechamber of Ulduar" -->
    32867,   // <!--Steelbreaker" -->
    32927,   // <!--Runemaster Molgeim" -->
    32857,   // <!--Stormcaller Brundir" -->
    32930,   // <!--Kologarn" -->
    33515,   // <!--Auriaya" -->
    34035,   // <!--Feral Defender" -->
    32933,   // <!--Left Arm" -->
    32934,   // <!--Right Arm" -->
    33524,   // <!--Saronite Animus" -->

    // <!-- "Ulduar: The Keepers of Ulduar" -->
    33350,   // <!--Mimiron" -->
    32906,   // <!--Freya" -->
    32865,   // <!--Thorim" -->
    32845,   // <!--Hodir" -->

    // <!-- "Ulduar: The Descent into Madness" -->
    33271,   // <!--General Vezax" -->
    33890,   // <!--Brain of Yogg-Saron" -->
    33136,   // <!--Guardian of Yogg-Saron" -->
    33288,   // <!--Yogg-Saron" -->
    32915,   // <!--Elder Brightleaf" -->
    32913,   // <!--Elder Ironbranch" -->
    32914,   // <!--Elder Stonebark" -->
    32882,   // <!--Jormungar Behemoth" -->
    33432,   // <!--Leviathan Mk II" -->
    34014,   // <!--Sanctum Sentry" -->

    // <!-- "Ulduar: The Celestial Planetarium" -->
    32871,   // <!--Algalon the Observer" -->

    // <!-- "Trial of the Crusader" -->
    34796,   // <!--Gormok" -->
    35144,   // <!--Acidmaw" -->
    34799,   // <!--Dreadscale" -->
    34797,   // <!--Icehowl" -->

    34780,   // <!--Jaraxxus" -->

    34461,   // <!--Tyrius Duskblade [Death Knight]" -->
    34460,   // <!--Kavina Grovesong [Druid]" -->
    34469,   // <!--Melador Valestrider [Druid]" -->
    34467,   // <!--Alyssia Moonstalker [Hunter]" -->
    34468,   // <!--Noozle Whizzlestick [Mage]" -->
    34465,   // <!--Velanaa [Paladin]" -->
    34471,   // <!--Baelnor Lightbearer [Paladin]" -->
    34466,   // <!--Anthar Forgemender [Priest]" -->
    34473,   // <!--Brienna Nightfell [Priest]" -->
    34472,   // <!--Irieth Shadowstep [Rogue]" -->
    34470,   // <!--Saamul [Shaman]" -->
    34463,   // <!--Shaabad [Shaman]" -->
    34474,   // <!--Serissa Grimdabbler [Warlock]" -->
    34475,   // <!--Shocuul [Warrior]" -->

    34458,   // <!--Gorgrim Shadowcleave [Death Knight]" -->
    34451,   // <!--Birana Stormhoof [Druid]" -->
    34459,   // <!--Erin Misthoof [Druid]" -->
    34448,   // <!--Ruj'kah [Hunter]" -->
    34449,   // <!--Ginselle Blightslinger [Mage]" -->
    34445,   // <!--Liandra Suncaller [Paladin]" -->
    34456,   // <!--Malithas Brightblade [Paladin]" -->
    34447,   // <!--Caiphus the Stern [Priest]" -->
    34441,   // <!--Vivienne Blackwhisper [Priest]" -->
    34454,   // <!--Maz'dinah [Rogue]" -->
    34444,   // <!--Thrakgar	[Shaman]" -->
    34455,   // <!--Broln Stouthorn [Shaman]" -->
    34450,   // <!--Harkzog [Warlock]" -->
    34453,   // <!--Narrhok Steelbreaker [Warrior]" -->

    35610,   // <!--Cat [Ruj'kah's Pet / Alyssia Moonstalker's Pet]" -->
    35465,   // <!--Zhaagrym [Harkzog's Minion / Serissa Grimdabbler's Minion]" -->

    34497,   // <!--Fjola Lightbane" -->
    34496,   // <!--Eydis Darkbane" -->
    34564,   // <!--Anub'arak (Trial of the Crusader)" -->

    // <!-- "Icecrown Citadel" -->
    36612,   // <!--Lord Marrowgar" -->
    36855,   // <!--Lady Deathwhisper" -->

    // <!-- "Gunship Battle" -->
    37813,   // <!--Deathbringer Saurfang" -->
    36626,   // <!--Festergut" -->
    36627,   // <!--Rotface" -->
    36678,   // <!--Professor Putricide" -->
    37972,   // <!--Prince Keleseth (Icecrown Citadel)" -->
    37970,   // <!--Prince Valanar" -->
    37973,   // <!--Prince Taldaram (Icecrown Citadel)" -->
    37955,   // <!--Queen Lana'thel" -->
    36789,   // <!--Valithria Dreamwalker" -->
    37950,   // <!--Valithria Dreamwalker (Phased)" -->
    37868,   // <!--Risen Archmage, Valitrhia Add" -->
    36791,   // <!--Blazing Skeleton, Valithria Add" -->
    37934,   // <!--Blistering Zombie, Valithria Add" -->
    37886,   // <!--Gluttonous Abomination, Valithria Add" -->
    37985,   // <!--Dream Cloud , Valithria Add" -->
    36853,   // <!--Sindragosa" -->
    36597,   // <!--The Lich King (Icecrown Citadel)" -->
    37217,   // <!--Precious" -->
    37025,   // <!--Stinki" -->
    36661,   // <!--Rimefang [Drake of Tyrannus]" -->

    // <!-- "Ruby Sanctum (PTR 3.3.5)" -->
    39746,   // <!--Zarithrian" -->
    39747,   // <!--Saviana" -->
    39751,   // <!--Baltharus" -->
    39863,   // <!--Halion" -->
    39899,   // <!--Baltharus (Copy has an own id apparently)" -->
    40142,   // <!--Halion (twilight realm)" -->

    // <!-- "Blackrock Mountain: Blackrock Caverns" -->
    39665,   // <!--Rom'ogg Bonecrusher" -->
    39679,   // <!--Corla, Herald of Twilight" -->
    39698,   // <!--Karsh Steelbender" -->
    39700,   // <!--Beauty" -->
    39705,   // <!--Ascendant Lord Obsidius" -->

    // <!-- "Abyssal Maw: Throne of the Tides" -->
    40586,   // <!--Lady Naz'jar" -->
    40765,   // <!--Commander Ulthok" -->
    40825,   // <!--Erunak Stonespeaker" -->
    40788,   // <!--Mindbender Ghur'sha" -->
    42172,   // <!--Ozumat" -->

    // <!-- "The Stonecore" -->
    43438,   // <!--Corborus" -->
    43214,   // <!--Slabhide" -->
    42188,   // <!--Ozruk" -->
    42333,   // <!--High Priestess Azil" -->

    // <!-- "The Vortex Pinnacle" -->
    43878,   // <!--Grand Vizier Ertan" -->
    43873,   // <!--Altairus" -->
    43875,   // <!--Asaad" -->

    // <!-- "Grim Batol" -->
    39625,   // <!--General Umbriss" -->
    40177,   // <!--Forgemaster Throngus" -->
    40319,   // <!--Drahga Shadowburner" -->
    40484,   // <!--Erudax" -->

    // <!-- "Halls of Origination" -->
    39425,   // <!--Temple Guardian Anhuur" -->
    39428,   // <!--Earthrager Ptah" -->
    39788,   // <!--Anraphet" -->
    39587,   // <!--Isiset" -->
    39731,   // <!--Ammunae" -->
    39732,   // <!--Setesh" -->
    39378,   // <!--Rajh" -->

    // <!-- "Lost City of the Tolvir" -->
    44577,   // <!--General Husam" -->
    43612,   // <!--High Prophet Barim" -->
    43614,   // <!--Lockmaw" -->
    49045,   // <!--Augh" -->
    44819,   // <!--Siamat" -->

    // <!-- "Baradin Hold" -->
    47120,   // <!--Argaloth" -->
    52363,   // <!--Occu'thar" -->

    // <!-- "Blackrock Mountain: Blackwing Descent" -->
    41570,   // <!--Magmaw" -->
    42180,   // <!--Toxitron" -->
    41378,   // <!--Maloriak" -->
    41442,   // <!--Atramedes" -->
    43296,   // <!--Chimaeron" -->
    41376,   // <!--Nefarian" -->

    // <!-- "Throne of the Four Winds" -->
    45871,   // <!--Nezir" -->
    46753,   // <!--Al'Akir" -->

    // <!-- "The Bastion of Twilight" -->
    45992,   // <!--Valiona" -->
    45993,   // <!--Theralion" -->
    44600,   // <!--Halfus Wyrmbreaker" -->
    43735,   // <!--Elementium Monstrosity" -->
    43324,   // <!--Cho'gall" -->
    45213,   // <!--Sinestra (heroic)" -->

    // <!-- "World Dragons" -->
    14889,   // <!--Emeriss" -->
    14888,   // <!--Lethon" -->
    14890,   // <!--Taerar" -->
    14887,   // <!--Ysondre" -->

    // <!-- "Azshara" -->
    14464,   // <!--Avalanchion" -->
    6109,   // <!--Azuregos" -->

    // <!-- "Un'Goro Crater" -->
    14461,   // <!--Baron Charr" -->

    // <!-- "Silithus" -->
    15205,   // <!--Baron Kazum [Abyssal High Council]" -->
    15204,   // <!--High Marshal Whirlaxis [Abyssal High Council]" -->
    15305,   // <!--Lord Skwol [Abyssal High Council]" -->
    15203,   // <!--Prince Skaldrenox [Abyssal High Council]" -->
    14454,   // <!--The Windreaver" -->

    // <!-- "Searing Gorge" -->
    9026,   // <!--Overmaster Pyron" -->

    // <!-- "Winterspring" -->
    14457,   // <!--Princess Tempestria" -->

    // <!-- "Hellfire Peninsula" -->
    18728,   // <!--Doom Lord Kazzak" -->
    12397,   // <!--Lord Kazzak" -->

    // <!-- "Shadowmoon Valley" -->
    17711,   // <!--Doomwalker" -->

    // <!-- "Nagrand" -->
    18398,   // <!--Brokentoe" -->
    18069,   // <!--Mogor [Hero of the Warmaul], friendly" -->
    18399,   // <!--Murkblood Twin" -->
    18400,   // <!--Rokdar the Sundered Lord" -->
    18401,   // <!--Skra'gath" -->
    18402,   // <!--Warmaul Champion" -->

    // <!-- " Cata Zul'gurub" -->
    52155,   // <!--High Priest Venoxis" -->
    52151,   // <!--Bloodlord Mandokir" -->
    52271,   // <!--Hazza'ra" -->
    52059,   // <!--High Priestess Kilnara" -->
    52053,   // <!--Zanzil" -->
    52148,   // <!--Jin'do the Godbreaker" -->

    // <!-- "Firelands" -->
    53691,   // <!--Shannox" -->
    52558,   // <!--Lord Rhyolith" -->
    52498,   // <!--Beth'tilac" -->
    52530,   // <!--Alysrazor" -->
    53494,   // <!--Baleroc" -->
    52571,   // <!--Majordomo Staghelm" -->
    52409,   // <!--Ragnaros" -->

    // <!-- "Dragon Soul" -->
    55265,   // <!--Morchok" -->
    57773,   // <!--Kohcrom (Heroic Morchok encounter)" -->
    55308,   // <!--Zon'ozz" -->
    55312,   // <!--Yor'sahj" -->
    55689,   // <!--Hagara" -->
    55294,   // <!--Ultraxion" -->
    56427,   // <!--Blackhorn" -->
    56846,   // <!--Arm Tentacle -- Madness of DW" -->
    56167,   // <!--Arm Tentacle -- Madness of DW" -->
    56168,   // <!--Wing Tentacle - Madness of DW" -->
    57962    // <!--Deathwing ----- Madness of DW (his head)" -->
    };
        #endregion 



        public static readonly HashSet<int> _hashTrinkCombat = new HashSet<int>()
        {
            99711	, // 	call of conquest
            92226	, // 	call of conquest
            84969	, // 	call of conquest
            99739	, // 	call of conquest
            105132  , //    call of conquest 11
            102441  , //    call of conquest 11

            99741	, // 	Call of Dominance
            92225	, // 	Call of Dominance
            84968	, // 	Call of Dominance
            99712	, // 	Call of Dominance
            105134  , //    Call of Dominance 11
            102437  , //    call of dominance 11

            92224	, // 	call of victory
            99740	, // 	call of victory
            99713	, // 	call of victory
            84966	, // 	call of victory
            105133  , //    call of victory 11
            102434  , //    call of victory 11

            67683	, // 	celerity 
            91173	, // 	celerity 
            91136	, // 	Leviathan
            91135	, // 	Leviathan
            92071	, // 	Nimble
            91352	, // 	Polarization
            91351	, // 	Polarization
            91828	, // 	thrill of victory
            91340	, // 	Typhoon
            91341	, // 	Typhoon
            67684   , //    Hospitality
            60521   , //    Winged Talisman
            60305   , //    Heart of a Dragon
            73551   , //    Figurine Jewel Serpent
            46567   , //    Goblin Rocket Launcher
            55039   , //    Gnomish Lightning Generator
            82645   , //    Elementium Dragonling
            92357   , //    Memory of Invincibility
            92200   , //    Blademaster
            92199   , //    Blademaster

            92123   , //    Enigma
            73549   , //    Demon Panther Figurine
            73550   , //    Earthen Guardian - Figurine
            73522   , //    King of Boars
            91019   , //    Soul Power

            91374   , //    Battle Prowess
            91376   , //    Battle Prowess
            92098   , //    Speed of Thought
            92099   , //    Speed of Thought
            92336   , //    Summon Fallen Footman
            92337   , //    Summon Fallen Grunt
            90900   , //    Focus
            92188   , //    Master Tactician
            95875   , //    Heartsparked
            90889   , //    fury of the earthen
            91345   , //    favored
            95874   , //    searing words
            95870   , //    lightning in a bottle
            95877   , //    la-la's song
            91344   , //    battle!

            9515    , //    summon tracking hound - dog whistle, loot from Scarlet Monestary
            93742   , //    death by peasent - barov servant caller, quest reward from Scholomance
            93740   , //    poison cloud - Essence of Eranikus' Shade, drop in Sunken Temple
            17668   , //    Ramstein's Lightning Bolts - Ramstein's Lightning Bolts, drop in Stratholme

            109908  , //    Foul Gift of the Demon Lord (stealth break / vanity)
            107947  , //    Kiroptyric Sigil
            107948  , //    Reflection of the Light
            91173   , //    Shard of Woe
            97009   , //    Ancient Petrified Seed
            96934   , //    Apparatus of Khaz'goroth
            97008   , //    Fiery Quintessence
            92328   , //    Heart of Ignacious
            109888  , //    Varo'then's Brooch
            97007   , //    Rune of Zeth
            110008  , //    Rosary of Light
            97121   , //    Jaws of Defeat
            96908   , //    Jaws of Defeat
            100612  , //    Moonwell Chalice
            101515  , //    Ricket's Magnetic Fireball
            91041   , //    Heart of Ignacious
            91322   , //    Jar of Ancient Remedies
            91019   , //    Soul Casket
            92123   , //    Unsolvable Riddle
            98275   , //    Miniature Voodoo Mask
            101285  , //    Bitterer Balebrew Charm
            101286  , //    Bubblier Brightbrew Charm

            33667   , //    Bladefist's Breadth
            33662   , //    Vengeance of the Illidari

            90842   , //    Mindfletcher Talisman
            95879   , //    Devourer's Stomach
            95880   , //    Emissary's Watch
            62984   , //    Omarion's Gift
            93248   , //    Horn of the Traitor
            93248   , //    Horn of the Traitor
            78830   , //    Chelsea's Nightmare
            95185   , //    Rhea's Last Egg
            95227   , //    Tosselwrench's Shrinker
            93225   , //    Toy Windmill
            95224   , //    Rainbow Generator ( vanity - light damage )

            107948  , //    Bottled Wishes
            107947  , //    Kiroptyric Sigil
            107948  , //    Reflection of the Light
        };

        public static readonly HashSet<int> _hashTrinkPVP = new HashSet<int>()
        {
            42292	, // 	PVP Trinket
        };

        public static readonly HashSet<int> _hashTrinkMana = new HashSet<int>()
        {
            91155	, // 	Expansive Soul = Core of Ripeness
            73552   , //    Figurine Jewel Owl
            92272   , //    Collecting Mana - Tyrande's Favorite Doll
            92601   , //    Detonate Mana - Tyrande's Favorite Doll
            95872   , //    undying flames

            92331   , //    Jar of Ancient Remedies
        };

        public static readonly HashSet<int> _hashTrinkHealth = new HashSet<int>()
        {
            44055	, // 	tremendous fortitude
            55915	, // 	tremendous fortitude
            55917	, // 	tremendous fortitude
            67596   , //    tremendous fortitude
            84960	, // 	tremendous fortitude
            92223	, // 	tremendous fortitude
            99714	, // 	tremendous fortitude
            99737	, // 	tremendous fortitude
            105144  , //    tremendous fortitude 11
            99737   , //    tremendous fortitude 11

            89181	, // 	mighty earthquake
            92186   , //    amazing fortitude
            92187   , //    amazing fortitude
            92172   , //    great fortitude
            33828   , //    talisman of the alliance
            32140   , //    Talisman of the Horde

            96880   , //    Scales of Life
            96988   , //    Stay of Execution

            33668   , //    Regal Protectorate

            23991   , //    Damage Absorb - Defiler's Talisman
            25746   , //    Damage Absorb - Defiler's Talisman
            25747   , //    Damage Absorb - Defiler's Talisman
            25750   , //    Damage Absorb - Defiler's Talisman

            93749   , //    Lunk's Special Gear
            25750   , //    Ancient Seed Casing
            23506   , //    Arena Grand Master

            93229   , //    Dying Breath
            95216   , //    Light's Embrace
        };

        public static readonly HashSet<string> _hashTinkerCombat = new HashSet<string>()
        {
            "Hyperspeed Accelerators",
            "Hand-Mounted Pyro Rocket",
            "Reticulated Armor Webbing",
            "Quickflip Deflection Plates",
            "Synapse Springs",
            "Tazik Shocker",
            "Grounded Plasma Shield"
        };

        public static readonly List<string> _enchantElemental = new List<string>
	    {
		    "Flametongue Weapon",
		    "Windfury Weapon",
		    "Rockbiter Weapon",
		    "Frostbrand Weapon",
	    };

        public static readonly List<string> _enchantEnhancementPVE_Mainhand = new List<string>
	    {
		    "Windfury Weapon",
		    "Flametongue Weapon",
		    "Rockbiter Weapon",
		    "Frostbrand Weapon"
	    };

        public static readonly List<string> _enchantEnhancementPVE_Offhand = new List<string>
	    {
		    "Flametongue Weapon",
		    "Windfury Weapon",
		    "Rockbiter Weapon",
		    "Frostbrand Weapon"
	    };

        public static readonly List<string> _enchantEnhancementPVP_Mainhand = new List<string>
	    {
		    "Windfury Weapon",
		    "Flametongue Weapon",
		    "Rockbiter Weapon",
		    "Frostbrand Weapon"
	    };

        public static readonly List<string> _enchantEnhancementPVP_Offhand = new List<string>
	    {
		    "Frostbrand Weapon",
		    "Flametongue Weapon",
		    "Windfury Weapon",
		    "Rockbiter Weapon"
	    };

        public static readonly List<string> _enchantResto = new List<string>
	    {
		    "Earthliving Weapon",
		    "Flametongue Weapon",
		    "Windfury Weapon",
		    "Rockbiter Weapon",
		    "Frostbrand Weapon"
	    };

        public static HashSet<int> _hashCleanseBlacklist = new HashSet<int>();
        public static HashSet<int> _hashPurgeWhitelist = new HashSet<int>();
        public static HashSet<int> _hashPvpGroundingTotemWhitelist = new HashSet<int>();

        // public override bool WantButton { get { return true; } }
        public override bool WantButton
        { get { return true; } }

#if    BUNDLED_WITH_HONORBUDDY

		public override void OnButtonPress()
		{
            DialogResult rc = MessageBox.Show(
                "This CC does not have a configuration"     +Environment.NewLine +
                "window, but you can modify the .config"      +Environment.NewLine +
                "file to change settings."                  +Environment.NewLine +
                ""                                           +Environment.NewLine +
                "Click OK and Windows will open the file"   +Environment.NewLine +
                "containing the settings for this Shaman"    +Environment.NewLine +
                "using the program setup as the default"  + Environment.NewLine +
                "for .config files on your system."       + Environment.NewLine +
                ""   + Environment.NewLine +
                "If none is setup, Windows will display"   + Environment.NewLine +
                "a Window letting you choose one.  Just"   + Environment.NewLine +
                "select Notepad.exe or a text editor.",
                Name, 
                MessageBoxButtons.OKCancel, 
                MessageBoxIcon.Information
                );

			if ( rc == DialogResult.OK )
                Process.Start("\"" + ConfigFilename + "\"");
		}
  
#else
        private ConfigForm _frm;

        public override void OnButtonPress()
        {
            Log(">>> LOADING OPTIONS (may take a few seconds first time)");
            if (_frm == null)
                _frm = new ConfigForm();

            Dlog(" About to show dialog");
            System.Windows.Forms.DialogResult rc = _frm.ShowDialog();
            if (rc == System.Windows.Forms.DialogResult.OK)
            {
                lastCheckConfig = true;
                cfg.DebugDump();
                try
                {
                    cfg.Save(ConfigFilename);
                    Log(">>> OPTIONS SAVED to ShamWOW-realm-char.config");
                    hsm = new HealSpellManager();
                    hsm.Dump();
                }
                catch (ThreadAbortException) { throw; }
                catch (GameUnstableException) { throw; }
                catch (Exception e)
                {
                    Log(Color.Red, "An Exception occured. Check debug log for details.");
                    Logging.WriteDebug("EXCEPTION Saving to ShamWOW-realm-char.config");
                    Logging.WriteException(e);
                }

                // TotemSetupBar(); // just for debug atm
                _needTotemBarSetup = true;
            }
        }
#endif

        private BadInstallForm _frmbad;

        public void ShowBadInstallMessage()
        {
            if (_frmbad == null)
                _frmbad = new BadInstallForm();

            Dlog(" About to show bad install dialog");
            _frmbad.ShowDialog();
        }


        public void SetFocus(WoWUnit unit)
        {
            if (unit != null)
                Lua.DoString("FocusUnit(\"" + unit.Name + "\")");
            else
                Lua.DoString("ClearFocus()");
        }

        public static WoWUnit GetFocusedFriendlyUnit()
        {
            string sUnitNameInfo = Lua.GetReturnVal<string>("return GetUnitName(\"focus\", 0)", 0);
            if (String.IsNullOrEmpty(sUnitNameInfo))
                return null;

            string[] parts = sUnitNameInfo.Split(' ');
            string sUnitName = parts[0];
            
            WoWUnit unitByName =
               (from o in ObjectManager.ObjectList
                where o is WoWUnit 
                let u = o.ToUnit()
                where Safe_IsFriendly(u) && u.Attackable && (u.Name == sUnitName || u.Name == sUnitNameInfo)
                select u
               ).FirstOrDefault();

            return unitByName;
        }


        public static bool IsEnemy(WoWUnit u)
        {

            return u != null 
                && u.CanSelect 
                && u.Attackable 
                && u.IsAlive 
                && (Safe_IsHostile(u) || IsTrainingDummy(u) || Safe_IsBoss(u))
                && !u.IsNonCombatPet 
                && !u.IsCritter;
        }

        public static bool IsEnemyOrNeutral(WoWUnit u)
        {
            return u != null && u.IsValid && u.Attackable && u.IsAlive && (u.IsNeutral || Safe_IsHostile(u));
        }

        private static IEnumerable<WoWUnit> AllEnemyMobs
        {
            get
            {
                return (from o in ObjectManager.ObjectList
                        where o is WoWUnit 
                        let u = o.ToUnit()
                        where IsEnemy(u)
                        select u);
            }
        }

        private static IEnumerable<WoWUnit> AllEnemyOrNeutralMobs
        {
            get
            {
                return (from o in ObjectManager.ObjectList
                        where o is WoWUnit 
                        let u = o.ToUnit()
                        where IsEnemyOrNeutral(u)
                        select u);
            }
        }

        private static List<WoWPoint> AllMobLocations
        {
            get
            {
                return (from u in AllEnemyMobs
                        where u.DistanceSqr < (80 * 80)
                        select u.Location).ToList();
            }
        }

        private static WoWPoint NearestMobLoc(WoWPoint p, IEnumerable<WoWPoint> mobLocs)
        {
            if (!mobLocs.Any())
                return WoWPoint.Empty;
            
            return mobLocs.OrderBy(u => u.Distance2DSqr(p)).First();
        }

        public static WoWPoint FindSafeLocation(double minSafeDist)
        {
            return FindSafeLocation(_me.Location, minSafeDist);
        }

        public static WoWPoint FindSafeLocation(WoWPoint ptOrigin, double minSafeDist)
        {
            WoWPoint destinationLocation = new WoWPoint();
            List<WoWPoint> mobLocations = new List<WoWPoint>();

            mobLocations = AllMobLocations;

            double minSafeDistSqr = minSafeDist * minSafeDist;

            for (int arcIndex = 0; arcIndex < 36; arcIndex++)
            {
                float degreesFrom = 180;
                if ((arcIndex & 1) == 0)
                    degreesFrom += (arcIndex >> 1) * 10;
                else
                    degreesFrom -= (arcIndex >> 1) * 10;

                for (float distFromOrigin = 0f; distFromOrigin <= 35f; distFromOrigin += 5f)
                {
                    if (degreesFrom != 0f && distFromOrigin == 0f)
                        continue;

                    destinationLocation = ptOrigin.RayCast((float)(degreesFrom * Math.PI / 180f), distFromOrigin);
                    double mobDistSqr = destinationLocation.Distance2DSqr(NearestMobLoc(destinationLocation, mobLocations));

                    if (mobDistSqr <= minSafeDistSqr) 
                        continue;

                    if (Navigator.GeneratePath(_me.Location, destinationLocation).Length <= 0)
                    {
                        Dlog("Mob-free location failed path generation check for degrees={0:F1} dist={1:F1}", degreesFrom, distFromOrigin );
                        continue;
                    }

                    if (!Styx.WoWInternals.World.GameWorld.IsInLineOfSight(_me.Location, destinationLocation))
                    {
                        Dlog("Mob-free location failed line of sight check for degrees={0:F1} dist={1:F1}", degreesFrom, distFromOrigin);
                        continue;
                    }

                    Dlog( "Mob-free location found for degrees={0:F1} dist={1:F1}", degreesFrom, distFromOrigin);

                    // We pass all checks. This is a good location 
                    // Make it so 'Number 1', "Engage"
                    return destinationLocation;
                }
            }

            Dlog("No mob-free location found with {0:F1} yds of {1}", minSafeDist, ptOrigin );
            return WoWPoint.Empty;
        }

        public static void Sleep(int ms)
        {
            // Thread.Sleep(ms);
        }

        public static void SleepForLagDuration()
        {
            // StyxWoW.SleepForLagDuration();
        }
    }

    /// <summary>
    ///  class Countdown
    ///  
    ///  provides a Countdown timer for easily checking whether a specified number of
    ///  milliseconds has elapsed.
    /// </summary>
    public class Countdown
    {
        private Stopwatch s = new Stopwatch();
        private int timeExpire;

        public Countdown()
        {
            timeExpire = 0;
        }
        public Countdown(int ms)
        {
            StartTimer(ms);
        }

        public bool Done
        {
            get { return timeExpire <= s.ElapsedMilliseconds; }
        }

        public int Remaining
        {
            get
            {
                return Math.Max(0, timeExpire - (int)s.ElapsedMilliseconds);
            }
            set
            {
                StartTimer(value);
            }
        }

        public void StartTimer(int ms)
        {
            timeExpire = ms;
            s.Reset();
            s.Start();
        }

        public int ElapsedMilliseconds
        {
            get { return (int)s.ElapsedMilliseconds; }
        }
    }

    class Mob
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int HitBox { get; set; }

        public Mob(int id, string name, int melee)
        {
            Id = id;
            Name = name;
            HitBox = melee;       
        }
    }

    public class GameUnstableException : System.Exception 
    {
        public GameUnstableException()
        {
            Logging.WriteDebug("GameUnstableException: game unstable, so passing control to HonorBuddy");
            // Sleep(1500);
        }

        public GameUnstableException(string message)
            : base(message)
        {
            Logging.WriteDebug("GameUnstableException: {0}", message);
            Shaman.Sleep(1500);
        }

        public GameUnstableException(string message, Exception innerException)
            : base(message, innerException )
        {
            Logging.WriteDebug("GameUnstableException(inr): {0}", message);
            Shaman.Sleep(1500);
        }

        protected GameUnstableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Logging.WriteDebug("GameUnstableException(ser): game unstable, so passing control to HonorBuddy");
            Shaman.Sleep(1500);
        }
    }

    public static class MyExtensions
    {
        public static int WordCount(this String str)
        {
            return str.Split(new char[] { ' ', '.', '?' },
                                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static double HitBoxRange(this WoWUnit x, WoWUnit y)
        {
            float combinedReach = x.CombatReach + y.CombatReach;
            return combinedReach;
        }

        public static double MeleeRange(this WoWUnit x, WoWUnit y)
        {
            return Math.Max(Shaman.STD_MELEE_RANGE, x.HitBoxRange(y) + 1.333f);
        }

        public static bool IsWithinMelee(this WoWUnit x, WoWUnit y)
        {
            if (x == null || y == null)
                return false;

            return x.Location.Distance(y.Location) < x.MeleeRange(y);
        }

        public static bool IsUnitInRange(this WoWUnit x, WoWUnit y, double range)
        {
            if (x == null || y == null)
                return false;
            double combatDistance = x.CombatDistance(y);
            return (combatDistance < range && y.InLineOfSpellSight);
        }

        public static double CombatDistance(this WoWUnit x, WoWUnit y)
        {
            double hitboxDistance = x.HitBoxRange(y);
            double combatDistance = x.Location.Distance(y.Location);
            combatDistance -= hitboxDistance;
            return combatDistance;
        }

        public static double CombatDistanceSqr(this WoWUnit x, WoWUnit y)
        {
            double hitboxDistance = x.HitBoxRange(y);
            double combatDistance = x.Location.DistanceSqr(y.Location);
            hitboxDistance *= hitboxDistance;
            combatDistance -= hitboxDistance;
            return combatDistance;
        }

        public static bool IsImmobilized(this WoWUnit unit)
        {
            WoWAura aura = Shaman.GetCrowdControlledAura(unit);
            if (aura == null)
                return false;

            return Shaman.IsImmobilizedAura(aura);
        }

        public static bool IsAuraPresent(this WoWUnit unit, string sAura)
        {
            uint stackCount;
            return IsAuraPresent(unit, sAura, out stackCount);
        }

        public static bool IsAuraPresent(this WoWUnit unit, string sAura, out uint stackCount)
        {
            uint timeLeft;
            return IsAuraPresent(unit, sAura, out stackCount, out timeLeft);
        }

        public static bool IsAuraPresent(this WoWUnit unit, string sAura, out uint stackCount, out uint timeLeft)
        {
            stackCount = 0;
            timeLeft = 0;

            if (unit == null)
                return false;
#if AURA_CHECK_VIA_LUA
            // HonorBuddy has a bug which mishandles stack count when a buff has the
            // .. same name as a talent.  maelstrom weapon and tidal waves are only ones for Shaman
            if (unit.IsMe && (sAura.ToLower() == "maelstrom weapon" || sAura.ToLower() == "tidal waves"))
            {
                List<string> myAuras = Lua.GetReturnValues("return UnitAura(\"player\",\"" + sAura + "\")");
                if (Equals(null, myAuras))
                    return false;

                stackCount = (uint)Convert.ToInt32(myAuras[3]);
                return true;
            }
#else
            // otherwise, use more efficient aura retrieval
            WoWAura aura = unit.GetAuraByName(sAura);
            if (aura == null)
                return false;

            stackCount = aura.StackCount;
            timeLeft = (uint) aura.TimeLeft.TotalMilliseconds;
            return true;
#endif

        }

        public static bool IsAuraPresent(this WoWUnit unit, int spellId)
        {
            uint stackCount;
            return IsAuraPresent(unit, spellId, out stackCount);
        }

        public static bool IsAuraPresent(this WoWUnit unit, int spellId, out uint stackCount)
        {
            stackCount = 0;
            if (unit == null)
                return false;

            WoWAura aura = unit.GetAuraById(spellId);
            if (aura == null)
                return false;

            stackCount = aura.StackCount;
            return true;
        }

        public static uint GetAuraStackCount(this WoWUnit unit, string auraName)
        {
            uint stackCount = 0;
            bool isPresent = IsAuraPresent(unit, auraName, out stackCount);
            return stackCount;
        }

        public static uint GetAuraTimeLeft(this WoWUnit unit, string auraName)
        {
            uint stackCount;
            uint timeLeft;
            IsAuraPresent(unit, auraName, out stackCount, out timeLeft);
            return timeLeft;
        }

        public static WoWAura GetAura(this WoWUnit unit, string auraName)
        {
            if (unit == null)
                return null;

            return unit.GetAuraByName(auraName);
        }

        public static WoWAura GetAura(this WoWUnit unit, int spellId)
        {
            if (unit != null)
            {
                WoWAura aura = unit.GetAllAuras().Where(a => a.SpellId == spellId).FirstOrDefault();
                if (aura != null)
                {
                    return aura;
                }
            }

            return null;
        }

        public static WoWAura GetAuraCreatedByMe(this WoWUnit unit, string auraName)
        {
            if (unit != null)
            {
                WoWAura aura = unit.GetAllAuras().Where(a => a.Name == auraName && (a.CreatorGuid == 0 || a.CreatorGuid == StyxWoW.Me.Guid)).FirstOrDefault();
                if (aura != null)
                {
                    return aura;
                }
            }

            return null;
        }

    }
}

