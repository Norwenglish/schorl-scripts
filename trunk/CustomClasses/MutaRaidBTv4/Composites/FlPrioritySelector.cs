//////////////////////////////////////////////////
//           FlPrioritySelector.cs              //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using Styx;
using TreeSharp;

namespace MutaRaidBT.Composites
{
    class FlPrioritySelector : PrioritySelector
    {
        public FlPrioritySelector(params Composite[] children) : base(children)
        {
        }

        public override RunStatus Tick(object context)
        {
            using (new FrameLock())
            {
                return base.Tick(context);
            }
        }
    }
}
