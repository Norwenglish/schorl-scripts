//////////////////////////////////////////////////
//               Trinkets.cs                    //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////
// Credit where credit is due:                  //
//  Thanks to Apoc and Singular for an example  //
//  of automagically determining if a trinket   //
//  is usable or not.                           //
//////////////////////////////////////////////////

using System.Drawing;
using Styx;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace MutaRaidBT.Helpers
{
    public delegate WoWItem WoWItemDelegate(object context);

    static class Specials
    {
        static public WoWItem mTrinket1 { get; private set; }
        static public bool mTrinket1Usable { get; private set; }

        static public WoWItem mTrinket2 { get; private set; }
        static public bool mTrinket2Usable { get; private set; }

        static public WoWItem mGloves { get; private set; }
        static public bool mGlovesUsable { get; private set; }

        public static string mRacialName { get; private set; }

        static Specials()
        {
            switch (StyxWoW.Me.Race)
            {
                case WoWRace.Orc:
                    mRacialName = "Blood Fury";
                    break;

                case WoWRace.Troll:
                    mRacialName = "Berserking";
                    break;

                case WoWRace.BloodElf:
                    mRacialName = "Arcane Torrent";
                    break;
            }
        }

        static public void Pulse()
        {
            var trinket1 = GetFirstTrinket();
            var trinket2 = GetSecondTrinket();
            var gloves = GetGloves();

            if (trinket1 != null && (mTrinket1 == null ||
                mTrinket1.Guid != trinket1.Guid))
            {
                mTrinket1 = trinket1;
                mTrinket1Usable = ItemHasUseEffectLua(mTrinket1);

                Logging.Write(Color.Orange, "");
                Logging.Write(Color.Orange, "" + mTrinket1.Name + " detected in Trinket Slot 1.");
                Logging.Write(Color.Orange, " Usable spell: " + mTrinket1Usable);
                Logging.Write(Color.Orange, "");
            }

            if (trinket2 != null && (mTrinket2 == null ||
                mTrinket2.Guid != trinket2.Guid))
            {
                mTrinket2 = trinket2;
                mTrinket2Usable = ItemHasUseEffectLua(mTrinket2);

                Logging.Write(Color.Orange, "" + mTrinket2.Name + " detected in Trinket Slot 2.");
                Logging.Write(Color.Orange, " Usable spell: " + mTrinket2Usable);
                Logging.Write(Color.Orange, "");
            }

            if (gloves != null && (mGloves == null ||
                mGloves.Guid != gloves.Guid))
            {
                mGloves = gloves;
                mGlovesUsable = ItemHasUseEffectLua(mGloves);

                Logging.Write(Color.Orange, "" + mGloves.Name + " detected in Gloves Slot.");
                Logging.Write(Color.Orange, " Usable spell: " + mGlovesUsable);
                Logging.Write(Color.Orange, "");
            }
        }

        static public Composite UseSpecialAbilities(CanRunDecoratorDelegate cond)
        {
            return new PrioritySelector(
                UseItem(ret => mTrinket1, ret => cond(ret) && mTrinket1Usable),
                UseItem(ret => mTrinket2, ret => cond(ret) && mTrinket2Usable),
                UseItem(ret => mGloves, ret => cond(ret) && mGlovesUsable),

                new Decorator(ret => mRacialName != null,
                    Spells.Cast(mRacialName))
            );
        }

        static public Composite UseSpecialAbilities()
        {
            return UseSpecialAbilities(ret => true);
        }

        static public Composite UseItem(WoWItemDelegate item, CanRunDecoratorDelegate cond)
        {
            return new Decorator(ret => item(ret) != null && cond(ret) && ItemUsable(item(ret)),
                new Action(ret =>
                    {
                        item(ret).Use();
                        Logging.Write(Color.Red, item(ret).Name);
                    }
                )
            );
        }

        static private bool ItemUsable(WoWItem item)
        {
            return item.Usable && item.Cooldown == 0;
        }

        static private bool ItemHasUseEffectLua(WoWItem item)
        {
            var itemSpell = Lua.GetReturnVal<string>("return GetItemSpell(" + item.Entry + ")", 0);

            if (itemSpell != null)
            {
                return true;
            }

            return false;
        }

        static private WoWItem GetFirstTrinket()
        {
            return StyxWoW.Me.Inventory.Equipped.Trinket1;
        }

        static private WoWItem GetSecondTrinket()
        {
            return StyxWoW.Me.Inventory.Equipped.Trinket2;
        }

        static private WoWItem GetGloves()
        {
            return StyxWoW.Me.Inventory.Equipped.Hands;
        }
    }
}
