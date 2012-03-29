//////////////////////////////////////////////////
//              Assassination.cs                //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites.Context
{
    static class Assassination
    {
        static public Composite BuildCombatBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Assassination.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Assassination.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Assassination.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Assassination.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Assassination.BuildCombatBehavior()
                    )
                )
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Assassination.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Assassination.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Assassination.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Assassination.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Assassination.BuildPullBehavior()
                    )
                )
            );
        }

        static public Composite BuildBuffBehavior()
        {
            return new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Assassination.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Assassination.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Assassination.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Assassination.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Assassination.BuildBuffBehavior()
                    )
            );
        }
    }
}
