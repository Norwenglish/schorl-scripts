using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Styx;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;
using Styx.Logic.Inventory.Frames.LootFrame;

// Support plugin for all Cataclysm Dailies
// Written by: Croga
// Inspired by: Katzerle

// Contents:
// Override of Pulse() to check for any of the supported events
// clearGyreworm()( will clear the huge Gyreworm in Deepholme as target
// findAndPickup() will pickup items for specific quests. It will pick up kegs and food in Twilight Highlands or Siege scrap in Tol Barad
// UseMagnet() will use the Magnet in Tol Barad to enable pickup of scrap metal

namespace croga
{
	class CrogaDailies: HBPlugin
	{       
        // ***** anything below here isn't meant to be modified *************
		public static string name { get { return "CrogaDailies " + _version.ToString(); } }
		public override string Name { get { return name; } }
		public override string Author { get { return "croga"; } }
		private readonly static Version _version = new Version(0, 7);
		public override Version Version { get { return _version; } }
		public override string ButtonText { get { return "No Settings"; } }
		public override bool WantButton { get { return false; } }
		public static LocalPlayer Me = ObjectManager.Me;
        
		public override void Pulse()
		{
				try
				{
                    if (Me.ZoneId == 5389)
                        UseMagnet();
                    if (Me.ZoneId == 5042)
                    {
                        clearGyreworm();
                        findAndPickup(40);
                    }
                    if (Me.ZoneId == 5389 || Me.ZoneId == 4922)
                    	findAndPickup(20);
				}
				catch (Exception e)
                {
                    Log("Exception in Pulse:{0}", e);
                }
		}

        public static void clearGyreworm()
        {
            if (Me.CurrentTarget != null)
            {
                if (Me.CurrentTarget.Name == "Colossal Gyreworm" || Me.CurrentTarget.Entry == 44258)
                    Me.ClearTarget();
            }
        }

        static public void findAndPickup( int pickupRange)
		{
			ObjectManager.Update();
			List<WoWGameObject> objectList = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.Entry == 6948).ToList();

            // Filling the objectList with whatever it is we're looking for.
            if (Me.ZoneId == 4922) // We're in Twilight Highlands
            {
                if (HasQuest(28861)&&!IsQuestCompleted(28861)) // We still need kegs!
                    objectList.AddRange(ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Where(o => o.Entry == 206195)
                        .OrderBy(o => o.Distance));
                if (HasQuest(28862)&&!IsQuestCompleted(28862)) // We still need food!
                    objectList.AddRange(ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => ((o.Entry ==206289)				// Wildhammer Food Stores
				    || (o.Entry ==206291)				// Wildhammer Food Stores
				    || (o.Entry ==206290)))				// Wildhammer Food Stores
				    .OrderBy(o => o.Distance).ToList());
            }
            else if (Me.ZoneId == 5389) // We're in Tol Barad
            {
                if ((HasQuest(27922) && !IsQuestCompleted(27992))||(HasQuest(28692)&&!IsQuestCompleted(28692))) // We're doing the Scrap quest
                objectList.AddRange(ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => ((o.Entry == 206644)	// Siege Scrap
                        || (o.Entry == 206652)
                        || (o.Entry == 206651)))
                    .OrderBy(o => o.Distance).ToList());
            }
            else if (Me.ZoneId == 5042) // We're in Deepholme
            {
                if (HasQuest(27050) && !IsQuestCompleted(27050)) // We're doing the Shroom quest
                    objectList.AddRange(ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => ((o.Entry == 205151)	// Shrooms
                        || (o.Entry == 205152)
                        || (o.Entry == 205146)
                        || (o.Entry == 205147)))
                    .OrderBy(o => o.Distance).ToList());
            }

            // Picking up all selected objects
			foreach (WoWGameObject o in objectList)
			{
                if (o.Entry != 6948)
                {
                    if (o.Location.Distance(Me.Location) < pickupRange)
                    {
                        movetoLoc(o.Location);
                        if (inCombat) return;
                        if (Me.Mounted)
                            Mount.Dismount();
                        Thread.Sleep(2000);
                        o.Interact();
                        Thread.Sleep(3000);
                    }
                }
			}
		}

        static public void UseMagnet()
        {
            WoWPoint Magnets = new WoWPoint(-416.3247, 1627.922, 18.6334);
            WoWItem Magnet;

            if ((Me.Location.Distance(Magnets) < 100) && HasQuest(27992) && !IsQuestCompleted(27992))
            {
                ObjectManager.Update();
                Magnet = ObjectManager.GetObjectsOfType<WoWItem>()
                    .Where(u => (u.Entry == 62829)).First();
                if (Magnet.Cooldown == 0)
                {
                    if (inCombat) return;
                    WoWMovement.MoveStop();
                    if (Me.Mounted)
                        Mount.Dismount();
                    Magnet.Use();
                }
            }
        }
        
        public static void movetoLoc(WoWPoint loc)
        {
            while (loc.Distance(Me.Location) > 10)
            {
                Navigator.MoveTo(loc);
                Thread.Sleep(100);
                if (inCombat) return;
            }
        }

		private static bool IsQuestCompleted(uint ID)
        {
            //to make sure every header is expanded in quest log
            Lua.DoString("ExpandQuestHeader(0)");
            //number of values in quest log (includes headers like "Durator")
            int QuestCount = Lua.GetReturnVal<int>("return select(1, GetNumQuestLogEntries())", 0);
            for (int i = 1; i <= QuestCount; i++)
            {
                List<string> QuestInfo = Lua.GetReturnValues("return GetQuestLogTitle(" + i + ")", "raphus.lua");

                //pass if the index isHeader or isCollapsed
                if (QuestInfo[4] == "1" || QuestInfo[5] == "1")
                    continue;

                string QuestStatus = null;
                if (QuestInfo[6] == "1")
                    QuestStatus = "completed";
                else if (QuestInfo[6] == "-1")
                    QuestStatus = "failed";
                else
                    QuestStatus = "in progress";
                if (QuestInfo[8] == Convert.ToString(ID) && QuestStatus == "completed")
                {
                    return true;
                }
            }
            return false;
        }
		
		private static bool HasQuest(uint ID)
        {
            //to make sure every header is expanded in quest log
            Lua.DoString("ExpandQuestHeader(0)");
            //number of values in quest log (includes headers like "Durator")
            int QuestCount = Lua.GetReturnVal<int>("return select(1, GetNumQuestLogEntries())", 0);
            for (int i = 1; i <= QuestCount; i++)
            {
                List<string> QuestInfo = Lua.GetReturnValues("return GetQuestLogTitle(" + i + ")", "raphus.lua");

                //pass if the index isHeader or isCollapsed
                if (QuestInfo[4] == "1" || QuestInfo[5] == "1")
                    continue;

                if (QuestInfo[8] == Convert.ToString(ID))
                {
                    return true;
                }
            }
            return false;
        }

        static public bool inCombat
        {
            get
            {
                if (Me.Combat || Me.Dead || Me.IsGhost) return true;
                return false;
            }
        }

        static public void Log(string msg, params object[] args) { Logging.Write(msg, args); }
        static public void Log(System.Drawing.Color c, string msg, params object[] args) { Logging.Write(c, msg, args); }		
		
	}
}

