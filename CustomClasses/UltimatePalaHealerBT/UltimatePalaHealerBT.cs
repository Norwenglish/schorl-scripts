//Inspired by Sm0k3d CC for paladin healer
//Inspired by Sm0k3d and Gilderoy UpaCC
//UPaCCBT the BehaviourTree Ultimate Paladin Healer Custom Class
//A bilion thanks go to Sm0k3d for his exellent work on the paladin healing class

//aggiungere lifeblood
//addon trinket?
//mount up dopo buff in pvp
//FIXME se compagno muore in arena arrendersi o passare a solo mode
//FIXME non si possono passare argomenti, solo usare variabili globali!
//FIXME Cambio del tank in arena
//FIXME per il PVP potrei fare se sono in combattimento e non ho nessuna bless casta la king (contro i dispell)
//FIXME riaggiungere Hand of Freedom anche per slow oltre che per snare
//FIXME se sto in Solo e me.inparty || me.inraid richiamare create behaviour, stessacosa al contrartio
//dal nome ottenere la posizione nell'array interno dei raid memeber di wow
//Quando uso un trinket devo controllare: che non sia passivo, che sia utilizzabile, che non sia in cooldown, che sia del tipo giusto

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
using TreeSharp;
using Action = TreeSharp.Action;
namespace UltimatePaladinHealerBT
{
    public partial class UltimatePalaHealerBT : CombatRoutine
    {
        public static UltimatePalaHealerBT Instance = new UltimatePalaHealerBT();
        private WoWUnit lastCast = null;
        private WoWPlayer fallbacktank = null;
        private WoWPlayer tank;
        private WoWUnit Enemy;
        private Random rng;
        private WoWPlayer x;
        private WoWPlayer tar;
        private WoWPlayer mtank;
        private WoWUnit Epet;
        private WoWPlayer BlessTarget;
        private WoWPlayer RessTarget;
        private WoWPlayer CleanseTarget;
        private WoWPlayer UrgentCleanseTarget;
        private WoWPlayer UrgentHoFTarget;
        private WoWPlayer luatank;
        private WoWPlayer focustank;
        private WoWPlayer focus;
        public List<string> GUIDlist;
        public string FormattedGUID;
        public ulong FinalGuid;

        //From here for the gui

        /*
        private int Judgment_range = 0; //for me..
        private bool PVE_want_urgent_cleanse = true;    //Do you want to cleanse critical stuff like Static Cling or Polymorph? (used)
        private bool PVP_want_urgent_cleanse = true;    //Do you want to cleanse critical stuff like Static Cling or Polymorph? (used)
        private bool PVE_want_cleanse = true;           //Do you want to cleanse debuff?
        private bool PVP_want_cleanse = true;           //Do you want to cleanse debuff?
        private int PVE_max_healing_distance = 40;      //Ignore people more far away
        private int PVE_ohshitbutton_activator = 40;    //Percentage to click 1 OhShitButton!
        private int PVP_ohshitbutton_activator = 40;    //Percentage to click 1 OhShitButton!
        private bool PVE_wanna_LoH = true;              //wanna LoH?
        private int PVE_min_LoH_hp = 15;                  //LoH will be cast on target below this
        private bool PVE_wanna_HoP = true;              //wanna HoP?
        private int PVE_min_HoP_hp = 25;                //HoP will be cast on target below this that are not the tank
        private bool PVE_wanna_HoS = true;              //wanna HoS?
        private int PVE_min_HoS_hp = 65;                //HoS will be cast on the tank if he drop below this
        private bool PVE_want_HR = true;                //wanna HR at all?
        private int PVE_min_player_inside_HR = 3;       //HR will be cast when we have that many player that need heals inside it
        private bool PVE_Inf_of_light_wanna_DL = true;  //when we have infusion of light do we wanna DL? if false will HL instead
        private int PVE_Inf_of_light_min_DL_hp = 70;    //max HP to cast divine light with infusion of light buff
        private int PVE_min_FoL_hp = 35;                //will cast FoL un unit below that value
        private int PVE_min_DL_hp = 70;                 //same, with DL
        private int PVE_min_HL_hp = 85;                 //same, with HL
        private bool PVE_wanna_DF = true;               //do we want to use Divine favor?
        private bool PVE_wanna_AW = true;               //do we want to use Awenging Wrath?
        private bool PVE_wanna_GotAK = true;            //do we want to use Guardian of ancient king?
        private int PVE_do_not_heal_above = 95;         //at how much health we ignore people
        private int rest_if_mana_below = 60;            //if mana is this low and out of combat then drink
        private int use_mana_rec_trinket_every = 60;    //use the trinket to rec mana every 60 sec (for now only support Tyrande's Favorite Doll couse i have it :P)
        private int use_mana_rec_trinket_on_mana_below = 40;  //will use the trinket only if mana is below that
        private bool use_mana_potion = true;            //will use a mana potion? (not yet implemented)
        private int use_mana_potion_below = 20;         //% of mana where to use the potion
        private bool PVE_wanna_DP = true;               //do we wanna use Divine Protection?
        private int PVE_DP_min_hp = 85;                 //max hp to use divine protection at (will use on lower hp)
        private bool PVE_wanna_DS = true;                      //wanna use Divine Shield?
        private int PVE_DS_min_hp = 35;                 //at witch hp wanna use Divine Shield?
        private bool PVE_wanna_everymanforhimself = true; //wanna use Every Man For Himself?
        private bool PVP_wanna_DP = true;
        private int PVP_DP_min_hp = 85;
        private bool PVP_wanna_DS = true;
        private int PVP_DS_min_hp = 50;
        private bool PVP_wanna_everymanforhimself = true;
        private bool PVE_wanna_Judge = true;
        private bool PVP_wanna_Judge = true;
        private int PVE_min_Divine_Plea_mana = 70;
        private int PVP_min_Divine_Plea_mana = 70;
        private bool PVE_wanna_HoW = true;
        private bool PVP_wanna_HoW = true;
        private bool PVE_wanna_Denunce = true;
        private bool PVP_wanna_Denunce = true;
        private bool PVE_wanna_CS = true;
        private bool PVP_wanna_CS = true;
        private bool PVE_wanna_buff = true;
        private bool PVP_wanna_buff = true;
        private bool PVE_wanna_mount = true;
        private bool PVP_wanna_mount = true;
        private bool PVE_wanna_HoJ = false;
        private bool PVE_wanna_rebuke = false;
        private bool PVE_wanna_move_to_HoJ = false;
        private bool PVP_wanna_HoJ = true;
        private bool PVP_wanna_rebuke = true;
        private bool PVP_wanna_move_to_HoJ = false;
        private bool ARENA_wanna_move_to_HoJ = true;
        private bool PVP_want_HR = true;
        private bool PVP_wanna_move_to_heal = false;
        private bool ARENA_wanna_move_to_heal = true;
        private bool PVE_wanna_move_to_heal = false;
        private bool PVP_wanna_LoH = true;              //wanna LoH?
        private int PVP_min_LoH_hp = 15;                  //LoH will be cast on target below this
        private bool PVP_wanna_HoP = true;              //wanna HoP?
        private int PVP_min_HoP_hp = 25;                //HoP will be cast on target below this that are not the tank
        private bool PVP_wanna_HoS = true;              //wanna HoS?
        private int PVP_min_HoS_hp = 65;                //HoS will be cast on the tank if he drop below this
        private int PVP_min_player_inside_HR = 2;       //HR will be cast when we have that many player that need heals inside it
        private bool PVP_Inf_of_light_wanna_DL = true;
        private int PVP_min_FoL_hp = 70;
        private int PVP_min_FoL_on_tank_hp = 80;
        private int PVP_min_HL_hp = 85;
        private bool PVP_wanna_DF = true;
        private bool PVP_wanna_AW = true;
        private bool PVP_wanna_GotAK = true;
        private int PVP_do_not_heal_above = 95;
        private bool PVP_wanna_HoF = true;
        private bool PVE_wanna_target = true;           //do you want the cc to target something if you do not have any target?
        private bool PVP_wanna_target = true;           //do you want the cc to target something if you do not have any target?
        private bool wanna_face = true;                 //do you want to face enemy when needed?
        private float PVP_min_run_to_HoF = 5.05f;         //if target speed drop below this, HoF
        private float PVE_min_run_to_HoF = 5.05f;         //if target speed drop below this, HoF
        private bool PVE_wanna_HoF = false;             //wanna HoF in PVE
        private int PVE_HR_how_far = 12;
        private int PVP_HR_how_far = 20;
        private int PVE_HR_how_much_health = 70;
        private int PVP_HR_how_much_health = 85;
        private int PVP_mana_judge = 50;                //will start judging on cooldwn at this mana
        private int PVE_mana_judge = 70;
        private bool tank_healer = false;
        private bool debug = false;
        private bool PVP_wanna_crusader = true;         //wanna switch to crusader aura in pvp when mounted?
        private bool PVE_wanna_crusader = false;        //wanna switch to crusader aura in pve when mounted?
        private int last_word = 1;                      //0 1 or 2 point in the talent Last Word
        private bool Solo_wanna_move = true;
        private int Solo_mana_judge = 100;
        private bool Solo_wanna_rebuke = true;
        private bool Solo_wanna_HoJ = true;
        private bool Solo_wanna_move_to_HoJ = true;
        private bool Solo_wanna_crusader = true;
        private bool Solo_wanna_buff = true;
        private bool Solo_wanna_face = true;
        private bool PVE_use_stoneform = true;
        private bool PVP_use_stoneform = true;
        private int stoneform_perc = 80;
        private bool PVP_use_escapeartist = true;
        private bool PVE_use_escapeartist = true;
        private bool PVE_use_gift = true;
        private bool PVP_use_gift = true;
        private int PVE_min_gift_hp = 40;
        private int PVP_min_gift_hp = 40;
        private bool PVE_use_bloodfury = true;
        private bool PVP_use_bloodfury = true;
        private int PVP_bloodfury_min_hp = 40;
        private int PVE_bloodfury_min_hp = 40;
        private bool PVP_use_warstomp = true;
        private bool PVE_use_warstomp = false;
        private int PVP_min_warstomp_hp = 50;
        private int PVE_min_warstomp_hp = 50;
        private bool PVE_use_bersekering = true;
        private bool PVP_use_bersekering = true;
        private int PVE_min_bersekering_hp = 40;
        private int PVP_min_bersekering_hp = 40;
        private bool PVP_use_will_forsaken = true;
        private bool PVE_use_will_forsaken = true;
        private bool PVE_use_torrent = true;
        private bool PVP_use_torrent = true;
        private int PVE_min_torrent_hp = 50;
        private int PVP_min_torrent_hp = 50;
        private bool ARENA_wanna_taunt = true;
        private int PVP_min_DL_hp = 0;
        private int ARENA_min_FoL_hp = 95;
        private int ARENA_min_DL_hp = 0;
        private int ARENA_min_HL_hp = 95;
        private bool chimaeron = false;
        private bool chimaeron_p1 = false;
        private int aura_type = 0; //0 for concentration 1 for resistance
        private int PVE_torrent_mana_perc = 80;
        */



        ////////////////////////////////////////////////////////////////////////////////////////////



        private bool gottank;
        private bool isinterrumpable = false;
        private string lastBehaviour = null;
        private string actualBehaviour = null;
        private string usedBehaviour = null;
        private double maxAOEhealth = 85;
        private double dontHealAbove = 95;
        private bool castedDL = false;
        private string lastbless = null;
        private bool Global_chimaeron_p1 = false;
        private bool Global_chimaeron = false;
        private int Global_Judgment_range = 0; //for me..
        private bool Global_debug = false;
        private int Talent_last_word = 1;                      //0 1 or 2 point in the talent Last Word
        private bool _should_king;
        private string fallbacktankname;
        private string urgentdebuff;
        private int tryes;
        private bool specialhealing_warning=false;
        int i,j;
        private bool selective_resetted = false;
        public string[] NameorRM = new string[41];
        public string[] OrganizedNames = new string[41];
        public string[] WoWnames = new string[41];
        public int[] Raidsbugroup = new int[41];
        public int[] Raidorder = new int[41];
        public bool[] healornot = new bool[41];
        public int[] check_aoe = new int[41];
        public int SB1C;
        public int SB2C;
        public int SB3C;
        public int SB4C;
        public int SB5C;
        public int myclass;

        public WoWUnit CastingSpellTarget { get; set; }
        public string LastSpellCast { get; set; }
        public string LastSpell { get; set; }

        public WoWItem Trinket1;
        public WoWItem Trinket2;


        public Composite _combatBehavior;

        private Composite _combatBuffsBehavior;

        private Composite _healBehavior;

        private Composite _preCombatBuffsBehavior;

        private Composite _pullBehavior;

        private Composite _pullBuffsBehavior;

        public Composite _restBehavior;

        public override Composite CombatBehavior { get { return _combatBehavior; } }

        public override Composite CombatBuffBehavior { get { return _combatBehavior;/*_combatBuffsBehavior;*/ } }

        public override Composite HealBehavior { get { return _combatBehavior;/*_healBehavior;*/ } }

        public override Composite PreCombatBuffBehavior { get { return _combatBehavior;/*_preCombatBuffsBehavior;*/ } }

        public override Composite PullBehavior { get { return _pullBehavior; } }

        public override Composite PullBuffBehavior { get { return _combatBehavior;/*_pullBuffsBehavior*/ } }

        public override Composite RestBehavior { get { return _restBehavior; } }

        public string[] RaidNames = new string[41];
        public bool[] Raidrole = new bool[41];
        public int[] Subgroup = new int[41];

        public List<WoWPlayer> NearbyFriendlyPlayers { get { return ObjectManager.GetObjectsOfType<WoWPlayer>(true, true).Where(p => p.DistanceSqr <= 40 * 40 && p.IsFriendly).ToList(); } }
        public List<WoWPlayer> NearbyFarFriendlyPlayers { get { return ObjectManager.GetObjectsOfType<WoWPlayer>(true, true).Where(p => p.DistanceSqr <= 70 * 70 && p.IsFriendly && p.IsInMyPartyOrRaid).ToList(); } }
        public List<WoWPlayer> NearbyUnFriendlyPlayers { get { return ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).Where(p => p.DistanceSqr <= 40 * 40 && !p.IsInMyPartyOrRaid && !p.Dead).ToList(); } }

        public List<WoWUnit> NearbyUnfriendlyUnits
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Where(p => /*p.IsHostile && */!p.IsFriendly && !p.Dead && /*!p.IsPet &&*/ p.DistanceSqr <= 40 * 40).
                        ToList();
            }
        }
        public List<WoWPlayer> PartyorRaid { get { if (InParty()) { return Me.PartyMembers; } else if (InRaid()) { return Me.RaidMembers; } else { return null; } } }
        
        private static Stopwatch sw = new Stopwatch();
        private static Stopwatch Trinket1_sw = new Stopwatch();
        private static Stopwatch Trinket2_sw = new Stopwatch();
        private static Stopwatch select_heal_watch = new Stopwatch();
        private static Stopwatch combatfrom = new Stopwatch();
        private static Stopwatch noncombatfrom = new Stopwatch();
        private static Stopwatch subgroupSW = new Stopwatch();

        private string version = "1.4";
        private string revision = "220";

        public override sealed string Name
        {
            get
            {
                if (Me.Class == WoWClass.Priest)
                {
                    return "UltimateHolyPriestHealerBT v " + version + " revision " + revision;
                }
                else
                {
                    return "UltimatePalaHealerBT v " + version + " revision " + revision;
                }

            }
        }

        public override WoWClass Class
        {
            get
            {
                if (Me.Class == WoWClass.Priest)
                {
                    return WoWClass.Priest;
                }
                else
                {
                    return WoWClass.Paladin;
                }
            }
        }

        public int setmyclass()
        {
            if (Me.Class == WoWClass.Priest)
            {
                return 1;
            }
            else if (Me.Class == WoWClass.Paladin)
            {
                return 0;
            }
            else
            {
                slog("my class is {0}", Me.Class);
                return -1;
            }
        }

        public static LocalPlayer Me { get { return ObjectManager.Me; } }

        public override void Initialize()
        {
            tryes2 = 0;
            Instance = this;
            myclass = setmyclass();
            while (myclass < 0)
            {
                slog("Your class is not pala nor priest, insteas is {0} tryes {1} \n I'm retyng to undersand your class but if you are not a pala or priest you should not be here", Me.Class,tryes2);
                tryes2++;
                Thread.Sleep(1000);
                ObjectManager.Update();
                myclass = setmyclass();
            }
            if (myclass == 0)
            {
                slog(Color.Orange, "Hello Executor!\n I\'m UPaHCCBT and i\'m here to assist you keeping your friend alive\n You are using UPaHCCBT version {0} revision {1}", version, revision);
                Global_Judgment_range = (int)SpellManager.Spells["Judgement"].MaxRange;
                slog(Color.Orange, "Your Judgment range is {0} yard! will use this value", Global_Judgment_range);
                _can_dispel_disease = true;
                _can_dispel_magic = true;
                _can_dispel_poison = true;
            }
            else if (myclass == 1)
            {
                slog(Color.Orange, "Hello Executor!\n I\'m UPrHCCBT and i\'m here to assist you keeping your friend alive\n You are using UPrHCCBT version {0} revision {1}", version, revision);
                _can_dispel_disease = false;
                _can_dispel_magic = true;
                _can_dispel_poison = false;
            }

            AttachEventHandlers();
            //slog("attach event ha funzionato");
            if (!CreateBehaviors())
            {
                //slog("create beah ha fallito");
                return;
            }

            //slog("create beah ha funzionato");
        }       //if you need to run something just once do it here (will put up talent check in here)

        public bool CreateBehaviors()
        {
            tryes = 0;
            //Beahviour();
            //while (!unitcheck(ObjectManager.Me))
            while (Me==null || !Me.IsValid || Me.Dead)
            {
                slog("i'm not valid, still on loading schreen {0}", tryes);
                tryes++;
                Thread.Sleep(1000);
                ObjectManager.Update();
            }
            if (!unitcheck(Me) && !Me.IsGhost)
            {
                if (unitcheck(ObjectManager.Me))
                {
                    slog("Me is not valid but OBJM.Me yes, we have a problem here..");
                }
                else
                {
                    slog("nor me nor OBJ.Me are valid, i shouldn't be here..");
                }
            }
            else if(!Me.IsGhost)
            {
                slog("All green! building behaviours now!");
            } else if (Me.IsGhost)
            {
                slog("So, i'm a Ghost, let's wait..");
            }
            Beahviour();
            tryes = 0;
            while (usedBehaviour == "WTF are you doing?")
            {
                tryes++;
                slog("No Valid Behaviour found, tryand again! that's try number {0}", tryes);
                if (unitcheck(Me))
                {
                    slog("party {0} raid {1} instance {2} pvpstatus {3} valid {4} behaviour {5}", Me.IsInParty, Me.IsInRaid, Me.IsInInstance, actualBehaviour, Me.IsValid, usedBehaviour);
                }
                else
                {
                    slog("i'm not valid, still on loading schreen and behaviour=WTF");
                }
                Thread.Sleep(1000);
                ObjectManager.Update();
                Beahviour();
            }
            slog(Color.HotPink, "{0}", usedBehaviour);
            if (myclass == 0)
            {
                UPaHBTSetting.Instance.Load();
                if (!selective_resetted) { UPaHBTSetting.Instance.Selective_Healing = false; selective_resetted = true; slog("Starting up, resetting selective healing to false"); }
                UPaHBTSetting.Instance.General_Stop_Healing = false;
                Load_Trinket();
                Inizialize_Trinket();
                Variable_inizializer();
                _combatBehavior = Composite_Selector();
                //Select_composite(out _combatBehavior);
                //Select_rest_composite(out _restBehavior);
                _restBehavior = Composite_Rest_Selector();
                _pullBehavior = Composite_Pull_Selector();
                Variable_Printer();
                slog(Color.HotPink, "{0}", usedBehaviour);
                UPaHBTSetting.Instance.Save();
            }
            else if (myclass == 1)
            {
                UPrHBTSetting.Instance.Load();
                if (!selective_resetted) { UPrHBTSetting.Instance.Selective_Healing = false; selective_resetted = true; slog("Starting up, resetting selective healing to false"); }
                //Load_Trinket();
                //Inizialize_Trinket();
                Priest_Variable_inizializer();                              //priest
                _combatBehavior = Composite_Priest_Selector();              //priest
                _restBehavior = Composite_Priest_Rest_Selector();           //priest
                _pullBehavior = Composite_Priest_Pull_Selector();           //priest
                Priest_Variable_Printer();                                  //priest
                slog(Color.HotPink, "{0}", usedBehaviour);
                UPrHBTSetting.Instance.Save();
            }
            lastBehaviour = usedBehaviour;
            return true;
        }
        public override bool WantButton { get { return true; } }

        private Form _configForm;
        public override void OnButtonPress()
        {
            Inizialize_variable_for_GUI();
            if (usedBehaviour == "Raid" && _selective_healing) { BuildSubGroupArray();  }
            if (_configForm == null || _configForm.IsDisposed || _configForm.Disposing)
            {
                if (Me.Class == WoWClass.Paladin)
                {
                    _configForm = new UPaHCCBTConfigForm();
                }
                else if (Me.Class == WoWClass.Priest)
                {
                    _configForm = new UPrHCCBTConfigForm();
                }
            }
            _configForm.ShowDialog();
        }

    }
}
