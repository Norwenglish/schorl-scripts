using Styx;
using Styx.Logic.Combat;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class CombatAction : Action
    {
        protected override RunStatus Run(object context)
        {
            if (BotPoi.Current != null && BotPoi.Current.Type == PoiType.Harvest)
            {
                MoveToPoolAction.MoveToPoolSW.Reset();
                MoveToPoolAction.MoveToPoolSW.Start();
            }
            bool is2Hand = false;
            // equip right hand weapon
            uint mainHandID = AutoAngler.Instance.MySettings.MainHand;
            WoWItem mainHand = ObjectManager.Me.Inventory.Equipped.MainHand;

            if (mainHand == null || (mainHand.Entry != mainHandID && Util.IsItemInBag(mainHandID)))
            {
                is2Hand = Util.GetIteminBag(AutoAngler.Instance.MySettings.MainHand).ItemInfo.InventoryType ==
                          InventoryType.TwoHandWeapon;
                Util.EquipItemByID(AutoAngler.Instance.MySettings.MainHand);
            }

            // equip left hand weapon
            uint offhandID = AutoAngler.Instance.MySettings.OffHand;
            WoWItem offhand = ObjectManager.Me.Inventory.Equipped.OffHand;

            if ((!is2Hand && offhandID > 0 &&
                 (offhand == null || (offhand.Entry != offhandID && Util.IsItemInBag(offhandID)))))
            {
                Util.EquipItemByID(AutoAngler.Instance.MySettings.OffHand);
            }

            if (RoutineManager.Current.CombatBehavior != null) // this check doesn't have any effect. anymore...
            {
                try
                {
                    if (!RoutineManager.Current.CombatBehavior.IsRunning)
                        RoutineManager.Current.CombatBehavior.Start(null);
                    return RoutineManager.Current.CombatBehavior.Tick(null);
                }
                catch
                {
                    RoutineManager.Current.Combat();
                    return RunStatus.Success;
                }
            }
            RoutineManager.Current.Combat();
            return RunStatus.Success;
        }
    }
}