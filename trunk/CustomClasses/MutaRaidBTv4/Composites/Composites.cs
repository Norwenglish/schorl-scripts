//////////////////////////////////////////////////
//               Composites.cs                  //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using TreeSharp;

namespace MutaRaidBT.Composites
{
    class Composites
    {
        static public Composite BuildCombatBehavior()
        {
            return new Switch<Helpers.Enumeration.TalentTrees>(ret => Helpers.Rogue.mCurrentSpec,

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.None,
                    Context.None.BuildCombatBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Assassination,
                    Context.Assassination.BuildCombatBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Combat,
                    Context.Combat.BuildCombatBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Subtlety,
                    Context.Subtlety.BuildCombatBehavior()
                )

            );
        }

        static public Composite BuildPullBehavior()
        {
            return new Switch<Helpers.Enumeration.TalentTrees>(ret => Helpers.Rogue.mCurrentSpec,

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.None,
                    Context.None.BuildPullBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Assassination,
                    Context.Assassination.BuildPullBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Combat,
                    Context.Combat.BuildPullBehavior()
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Subtlety,
                    Context.Subtlety.BuildPullBehavior()
                )

            );
        }

        static public Composite BuildBuffBehavior()
        {
            return new Switch<Helpers.Enumeration.TalentTrees>(ret => Helpers.Rogue.mCurrentSpec,

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.None,
                    new Action(ret => RunStatus.Failure)
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Assassination,
                    new PrioritySelector(
                        Helpers.Rogue.ApplyPosions(),
                        Context.Assassination.BuildBuffBehavior()
                    )
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Combat,
                    new PrioritySelector(
                        Helpers.Rogue.ApplyPosions(),
                        Context.Combat.BuildBuffBehavior()
                    )
                ),

                new SwitchArgument<Helpers.Enumeration.TalentTrees>(Helpers.Enumeration.TalentTrees.Subtlety,
                    new PrioritySelector(
                        Helpers.Rogue.ApplyPosions(),
                        Context.Subtlety.BuildBuffBehavior()
                    )
                )

            );
        }
    }
}
