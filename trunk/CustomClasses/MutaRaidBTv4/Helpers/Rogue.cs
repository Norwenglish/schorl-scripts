//////////////////////////////////////////////////
//                 Rogue.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Drawing;
using System.Linq;
using System;
using System.Collections.Generic;
using CommonBehaviors.Actions;
using Styx;
using Styx.Helpers;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MutaRaidBT.Helpers
{
    static class Rogue
    {
        static public int mCurrentEnergy { get; private set; }
        static public Enumeration.TalentTrees mCurrentSpec { get; private set; }

        static Rogue()
        {
            Lua.Events.AttachEvent("CHARACTER_POINTS_CHANGED", delegate
                {
                    Logging.Write(Color.Orange, "Your spec has been updated. Rebuilding behaviors...");
                    mCurrentSpec = GetCurrentSpecLua();
                }
            );

            Lua.Events.AttachEvent("ACTIVE_TALENT_GROUP_CHANGED", delegate
                {
                    Logging.Write(Color.Orange, "Your spec has changed. Rebuilding behaviors...");
                    mCurrentSpec = GetCurrentSpecLua();
                }
            );

            mCurrentSpec = GetCurrentSpecLua();
        }

        static public void Pulse()
        {
            mCurrentEnergy = GetCurrentEnergyLua();
        }

        static public bool IsInterruptUsable()
        {
            return StyxWoW.Me.CurrentTarget.IsCasting && StyxWoW.Me.CurrentTarget.CurrentCastTimeLeft.TotalSeconds <= 0.5;
        }

        static public bool IsBehindUnit(WoWUnit unit)
        {
            return Settings.Mode.mForceBehind || unit.MeIsBehind;
        }

        static public bool IsAoeUsable()
        {
            return Settings.Mode.mUseAoe && IsThrowingItemEquipped();
        }

        static public bool IsCloakUsable()
        {
            return Target.mNearbyEnemyUnits.Any(unit => unit.CurrentTarget != null &&
                                                        unit.CurrentTarget.Guid == StyxWoW.Me.Guid &&
                                                        unit.IsCasting &&
                                                        unit.CurrentCastTimeLeft.TotalSeconds <= 0.5 &&
                                                        (!unit.IsWithinMeleeRange ||
                                                        (Spells.GetSpellCooldown("Kick") > 0 && 
                                                        Spells.GetSpellCooldown("Kidney Shot") > 0)));
        }

        static public bool IsCooldownsUsable()
        {
            if (Settings.Mode.mUseCooldowns)
            {
                switch (Settings.Mode.mCooldownUse)
                {
                    case Enumeration.CooldownUse.Always:

                        return true;

                    case Enumeration.CooldownUse.ByFocus:

                        return Focus.mFocusTarget != null && Focus.mFocusTarget.Guid == StyxWoW.Me.CurrentTarget.Guid &&
                               !Focus.mFocusTarget.IsFriendly;

                    case Enumeration.CooldownUse.OnlyOnBosses:

                        return Area.IsCurTargetSpecial();
                }
            }

            return false;
        }

        static public Composite ApplyPosions()
        {
            return new Decorator(ret => Settings.Mode.mUsePoisons[(int) Area.mLocation],
                new PrioritySelector(
                    ApplyPoisonToItem(StyxWoW.Me.Inventory.Equipped.MainHand, ret => (uint) Settings.Mode.mPoisonsMain[(int) Area.mLocation]),
                    ApplyPoisonToItem(StyxWoW.Me.Inventory.Equipped.OffHand,  ret => (uint) Settings.Mode.mPoisonsOff[(int) Area.mLocation])
                )
            );
        }

        static private Composite ApplyPoisonToItem(WoWItem item, PoisonDelegate poison)
        {
            return new Decorator(ret => !IsPoisonApplied(item) && IsPoisonInInventory(poison(ret)),
                new Sequence(
                    new Action(ret =>
                        {
                            Logging.Write(Color.Orange, "Applying " + (Enumeration.PoisonSpellId) poison(ret) + " to " + item.Name);
                            Navigator.PlayerMover.MoveStop();
                        }
                    ),

                    new WaitContinue(TimeSpan.FromSeconds(0.5), ret => false, new ActionAlwaysSucceed()),

                    new Action(ret =>
                        {
                            var thePoison = StyxWoW.Me.BagItems.First(inventoryItem => inventoryItem.ItemInfo.Id == poison(ret));

                            thePoison.Interact();
                            item.Interact();          
                        }),

                    new WaitContinue(TimeSpan.FromSeconds(5), ret => false, new ActionAlwaysSucceed())
                )
            );

        }

        static private bool IsPoisonApplied(WoWItem item)
        {
            return item != null && item.TemporaryEnchantment.Id != 0;
        }

        static private bool IsPoisonInInventory (uint poisonId)
        {
            return StyxWoW.Me.BagItems.Any(inventoryItem => inventoryItem != null && inventoryItem.ItemInfo.Id == poisonId);
        }

        static private bool IsThrowingItemEquipped()
        {
            return true; //StyxWoW.Me.Inventory.Equipped.Ranged.IsThrownWeapon;
        }
        
        // Used the fix the slow resource updating in Honorbuddy.
        static private int GetCurrentEnergyLua()
        {
            return Lua.GetReturnVal<int>("return UnitMana(\"player\");", 0);
        }

        // Returns the index of the current active dual spec -- first or second.
        static private int GetSpecGroupLua()
        {
            return Lua.GetReturnVal<int>("return GetActiveTalentGroup(false, false)", 0);
        }

        static private Enumeration.TalentTrees GetCurrentSpecLua()
        {
            int group = GetSpecGroupLua();

            var pointsSpent = new int[3];
           
            for (int tab = 1; tab <= 3; tab++)
            {
                List<string> talentTabInfo = Lua.GetReturnValues("return GetTalentTabInfo(" + tab + ", false, false, " + group + ")");
                pointsSpent[tab - 1] = Convert.ToInt32(talentTabInfo[4]);
            }

            if (pointsSpent[0] > (pointsSpent[1] + pointsSpent[2]))
            {
                return Enumeration.TalentTrees.Assassination;
            }

            if (pointsSpent[1] > (pointsSpent[0] + pointsSpent[2])) 
            {
                return Enumeration.TalentTrees.Combat;
            }

            if (pointsSpent[2] > (pointsSpent[0] + pointsSpent[1]))
            {
                return Enumeration.TalentTrees.Subtlety;
            }

            return Enumeration.TalentTrees.None;
        }
    }
}
