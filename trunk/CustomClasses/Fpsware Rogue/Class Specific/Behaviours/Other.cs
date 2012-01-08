using System.Threading;
using Styx.WoWInternals;
using TreeSharp;

namespace Hera
{
    public partial class Fpsware
    {
        #region Heal Behaviour

        private Composite _healBehavior;

        public override Composite HealBehavior
        {
            get { if (_healBehavior == null) { Utils.Log("Creating 'Heal' behavior");  _healBehavior = CreateHealBehavior(); }  return _healBehavior; }
        }

        private static Composite CreateHealBehavior()
        {
            return new PrioritySelector(

                // Lifeblood
                new NeedToLifeblood(new Lifeblood()),

                // Use a health potion if we need it
                new NeedToUseHealthPot(new UseHealthPot())

                );
        }

        #endregion

        #region Rest Behaviour
        private Composite _restBehavior;
        public override Composite RestBehavior
        {
            get { if (_restBehavior == null) { Utils.Log("Creating 'Rest' behavior"); _restBehavior = CreateRestBehavior(); } return _restBehavior; }
        }

        private Composite CreateRestBehavior()
        {
            return new PrioritySelector(

                // Vanish Rest - Trying to fix Vanish, HB leaves combat as soon as we vanish.
                new NeedToVanishRest(new VanishRest()),

                // Recuperate
                new NeedToRecuperateRest(new RecuperateRest()),
                
                // We're full. Stop eating/drinking
                // No point sitting there doing nothing wating for the Eat/Drink buff to disapear
                new NeedToCancelFoodDrink(new CancelFoodDrink()),

                // Eat and Drink
                new NeedToEatDrink(new EatDrink())

                );
        }

        #endregion


        #region Recuperate Rest
        public class NeedToRecuperateRest : Decorator
        {
            public NeedToRecuperateRest(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                const string dpsSpell = "Recuperate";

                if (Self.IsHealthPercentAbove(99)) return false;
                if (!Utils.CombatCheckOk(dpsSpell, false)) return false;

                return (Spell.CanCast(dpsSpell));
            }
        }

        public class RecuperateRest : Action
        {
            protected override RunStatus Run(object context)
            {
                const string dpsSpell = "Recuperate";
                bool result = Spell.Cast(dpsSpell);

                return result ? RunStatus.Success : RunStatus.Failure;
            }
        }
        #endregion

        #region VanishRest
        public class NeedToVanishRest : Decorator
        {
            public NeedToVanishRest(Composite child) : base(child) { }

            protected override bool CanRun(object context)
            {
                if (!Self.IsBuffOnMe("Vanish")) return false;
                bool vanishStillRunning = !Timers.Expired("Vanish", 5000);
                
                
                return (vanishStillRunning);
            }
        }

        public class VanishRest : Action
        {
            protected override RunStatus Run(object context)
            {
                // If we are here its because Vanish was just used. 
                // Just do nothing until the Vanish timer is > 5 seconds
                while (Me.IsMoving && Self.IsBuffOnMe("Vanish"))
                {
                    Utils.Log("** running away and cowering in the corner");
                    Thread.Sleep(150);
                    ObjectManager.Update();
                }

                return RunStatus.Success;
            }
        }
        #endregion

       
    }
}
