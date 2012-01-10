using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TreeSharp;

namespace Durid
{
    partial class DuridRoutine
    {
        private Composite _pullBehavior;

        public override Composite PullBehavior
        {
            get { return _pullBehavior ?? (_pullBehavior = CreatePullBehavior()); }
        }

        private Composite CreatePullBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => Me.CurrentTarget.IsFlying && Me.CurrentTarget.Distance < 29f,
                    CreateCast("Faerie Fire (Feral)")),
                CreateCombatBehavior());
        }
    }
}
