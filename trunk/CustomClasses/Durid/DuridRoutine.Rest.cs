using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Styx.Logic;
using Styx.Logic.Inventory;

using TreeSharp;

using Action = TreeSharp.Action;

namespace Durid
{
    partial class DuridRoutine
    {
        private Composite _restBehavior;

        private Composite _healBehavior;

        public override Composite RestBehavior
        {
            get { return _restBehavior ?? (_restBehavior = CreateRestBehavior()); }
        }

        public override Composite HealBehavior
        {
            get { return _healBehavior ?? (_healBehavior = CreateHealBehavior()); }
        }

        private Composite CreateHealBehavior()
        {
            // Don't heal in BGs. Kthx.
            return new Decorator(
                ret => !Battlegrounds.IsInsideBattleground && !Me.IsCasting,
                new PrioritySelector(
                    CreateSelfBuff("Healing Touch", ret => Me.HealthPercent < 45 && Me.HasAura("Predator's Swiftness")),
                    //CreateSelfBuff("Nourish", ret => Me.HealthPercent < 55 && (Me.HasAura("Rejuvenation") || Me.HasAura("Regrowth"))),
                    CreateSelfBuff("Regrowth", ret => Me.HealthPercent < 50),
                    CreateSelfBuff("Rejuvenation", ret => Me.HealthPercent <= 60),

                    // War stomp for healing touch. Kthx.
                    new Decorator(
                        ret => Me.HealthPercent < 40,
                        new PrioritySelector(
                            CreateCast("War Stomp"),
                            CreateSelfBuff("Healing Touch"))),

                    CreateSelfBuff("Innervate", ret => Me.ManaPercent < 30)
                    ));
        }

        private Composite CreateRestBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => !Me.HasAura("Food") && !Me.HasAura("Drink"),
                    CreateHealBehavior()),
                new Decorator(
                    ret => Me.ManaPercent < 20 && Consumable.GetBestDrink(false) != null,
                    new Action(ret => Styx.Logic.Common.Rest.DrinkImmediate())),
                new Decorator(
                    ret => Me.HealthPercent < 50 && Consumable.GetBestFood(false) != null,
                    new Action(ret => Styx.Logic.Common.Rest.FeedImmediate())));
        }
    }
}
