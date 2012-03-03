using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = System.Action;
using Sequence = Styx.Logic;
using Styx.WoWInternals.World;



namespace Amplify
{
    public partial class Amplify
    {
        internal class Spells
        {

            private static Spells _instance;

            public static Spells Instance
            {
                get { return _instance ?? (_instance = new Spells()); }
            }

            public void StopCasting()
            {
                Lua.DoString("SpellStopCasting()");
            }




            public void SellItem(string Itemname)
            {
                foreach (WoWItem item in Me.BagItems)
                {
                    if (item.Name == Itemname)
                    {
                        Logging.Write("Selling Item {0}", item.Name);
                        item.UseContainerItem();
                        item.PickUp();
                    }
                }
            }

            private static bool GotPet()
            {
                List<string> HasPet = Lua.GetReturnValues("return HasPetUI(1)", "hawker.lua");
                if (HasPet[0] == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}

