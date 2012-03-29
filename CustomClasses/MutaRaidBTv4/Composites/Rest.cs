//////////////////////////////////////////////////
//                  Rest.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System.Drawing;
using CommonBehaviors.Actions;
using Styx;
using Styx.Helpers;
using Styx.Logic.Inventory;
using Styx.Logic.Pathing;
using TreeSharp;

namespace MutaRaidBT.Composites
{
    static class Rest
    {
        static public Composite CreateRestBehavior()
        {
            return new Decorator(ret => !StyxWoW.Me.IsSwimming && !StyxWoW.Me.IsGhost && 
                                        StyxWoW.Me.IsAlive && !StyxWoW.Me.Mounted &&
                                        Helpers.Area.mLocation != Helpers.Enumeration.LocationContext.Raid &&
                                        Helpers.Area.mLocation != Helpers.Enumeration.LocationContext.HeroicDungeon,
                new PrioritySelector(
                    new Decorator(ret => Helpers.Spells.IsAuraActive(StyxWoW.Me, "Food") && StyxWoW.Me.HealthPercent <= 90,
                        new ActionAlwaysSucceed()),

                    Helpers.Spells.CastSelf("Recuperate", ret => StyxWoW.Me.RawComboPoints >= 1 && 
                                                                 !Helpers.Spells.IsAuraActive(StyxWoW.Me, "Recuperate")),

                    new Decorator(ret => Consumable.GetBestFood(true) != null && StyxWoW.Me.HealthPercent <= 75,
                        new PrioritySelector(

                            new Decorator(ret => StyxWoW.Me.IsMoving,
                                new Action(ret => Navigator.PlayerMover.MoveStop())
                            ),

                            Helpers.Spells.CastSelf("Stealth", ret => !StyxWoW.Me.HasAura("Stealth")),

                            new Action(ret => Styx.Logic.Common.Rest.FeedImmediate())
                        )
                    ),

                    new Decorator(ret => StyxWoW.Me.HealthPercent <= 30,
                        new PrioritySelector(
                            Helpers.Spells.CastSelf("Stealth",  ret => !StyxWoW.Me.HasAura("Stealth")),
                            new Action(ret => Logging.Write(Color.Orange, "No food, waiting to heal!"))
                        )
                    )
                )
            );
        }
    }
}
