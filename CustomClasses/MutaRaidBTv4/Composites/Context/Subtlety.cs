//////////////////////////////////////////////////
//                Subtlety.cs                   //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites.Context
{
    static class Subtlety
    {
        static public Composite BuildCombatBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Subtlety.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Subtlety.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Subtlety.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Subtlety.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Subtlety.BuildCombatBehavior()
                    )
                )
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Subtlety.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Subtlety.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Subtlety.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Subtlety.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Subtlety.BuildPullBehavior()
                    )
                )
            );
        }

        static public Composite BuildBuffBehavior()
        {
            return new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Subtlety.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Subtlety.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Subtlety.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Subtlety.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Subtlety.BuildBuffBehavior()
                    )
            );
        }
    }
}
