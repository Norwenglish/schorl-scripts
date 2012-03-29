//////////////////////////////////////////////////
//                 Combat.cs                    //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites.Context
{
    static class Combat
    {
        static public Composite BuildCombatBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Combat.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Combat.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Combat.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Combat.BuildCombatBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Combat.BuildCombatBehavior()
                    )
                )
            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Decorator(ret => Settings.Mode.mUseCombat,
                new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Combat.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Combat.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Combat.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Combat.BuildPullBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Combat.BuildPullBehavior()
                    )
                )
            );
        }

        static public Composite BuildBuffBehavior()
        {
            return new Switch<Helpers.Enumeration.LocationContext>(ret => Helpers.Area.mLocation,

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Raid,
                        Raid.Combat.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.HeroicDungeon,
                        Raid.Combat.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Dungeon,
                        Level.Combat.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.Battleground,
                        Battleground.Combat.BuildBuffBehavior()
                    ),

                    new SwitchArgument<Helpers.Enumeration.LocationContext>(Helpers.Enumeration.LocationContext.World,
                        Level.Combat.BuildBuffBehavior()
                    )
            );
        }
    }
}
