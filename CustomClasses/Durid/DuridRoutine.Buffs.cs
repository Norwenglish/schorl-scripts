using System;
using System.Linq;

using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;

using Action = TreeSharp.Action;

namespace Durid
{
    partial class DuridRoutine
    {
        private Composite _buffComposite;

        private Composite _combatBuffComposite;

        private Composite CreateBuffComposite()
        {
            return new Decorator(
                ret => !Me.Mounted,
                new PrioritySelector(
                    CreateSelfBuff("Mark of the Wild"),
                    CreateEnhancementFlaskBuff(),
                    new Decorator(
                        ret => Me.Shapeshift != CurrentShapeshift,
                        new Action(ret => EnsureCurrentForm()))));
        }

        private Composite CreateCombatBuffComposite()
        {
            // Cast thorns when we have more mobs on us. Kthx.
            // It's cheap, and adds a little extra damage.
            return new Decorator(
                ret => Adds.Count > 2,
                new PrioritySelector(
                    CreateSelfBuff("Thorns", ret => !Battlegrounds.IsInsideBattleground),
                    CreateSelfBuff("Barkskin", ret => Adds.Count > 2)));
        }

        private bool _hasFlask;
        WaitTimer _flaskWait = new WaitTimer(TimeSpan.FromMinutes(5));
        private bool HasFlaskOfEnhancement
        {
            get
            {
                if (_flaskWait.IsFinished)
                {
                    _hasFlask = ObjectManager.GetObjectsOfType<WoWItem>(false, false).Any(i => i.Entry == 58149);
                    _flaskWait.Reset();
                }
                return _hasFlask;
            }
        }

        private Composite CreateEnhancementFlaskBuff()
        {
            // Basically, this uses [Flask of Enhancement] (http://www.wowhead.com/item=58149)
            // Which is an alchemist item that gives an hourly buff. It gives one of 3 buffs, hence why we check the spell ID range.
            // Yes, this is a bit 'hackish' but oh well.
            return
                new Decorator(
                    ret => HasFlaskOfEnhancement && !Me.GetAllAuras().Any(b => b.SpellId >= 79638 && b.SpellId <= 79640),
                    new Sequence(
                        new ActionLog("Alchemist detected, and have Flask of Enhancement. Getting a free hourly buff!"),
                        new Action(ret => ObjectManager.GetObjectsOfType<WoWItem>(false, false).First(i => i.Entry == 58149).Use())));
        }

        public override Composite CombatBuffBehavior { get { return _combatBuffComposite ?? (_combatBuffComposite = CreateCombatBuffComposite()); } }
        public override Composite PreCombatBuffBehavior  { get { return _buffComposite ?? (_buffComposite = CreateBuffComposite()); } }
    }
}
