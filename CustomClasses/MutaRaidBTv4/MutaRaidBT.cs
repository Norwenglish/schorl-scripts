//////////////////////////////////////////////////
//              MutaRaidBT.cs                   //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System;
using System.Drawing;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using TreeSharp;

namespace MutaRaidBT
{
    class MutaRaidBT : CombatRoutine
    {
        public Version mCurVersion = new Version(4, 1);

        public override string Name { get { return "MutaRaidBT v" + mCurVersion; } }
        public override WoWClass Class { get { return WoWClass.Rogue; } }
        public override bool WantButton { get { return true; } }

        public override Composite CombatBehavior { get { return Composites.Composites.BuildCombatBehavior(); } }
        public override Composite PullBehavior { get { return Composites.Composites.BuildPullBehavior(); } }
        public override Composite PreCombatBuffBehavior { get { return Composites.Composites.BuildBuffBehavior(); } }
        public override Composite RestBehavior { get { return Composites.Rest.CreateRestBehavior(); } }

        public override void Initialize()
        {
            Logging.Write(Color.Orange, "");
            Logging.Write(Color.Orange, "MutaRaidBT v" + mCurVersion + " is now operational.");
            Logging.Write(Color.Orange, "");
            Logging.Write(Color.Orange, "Your feedback is appreciated. Please leave some in the forum thread at:");
            Logging.Write(Color.Orange, "http://www.thebuddyforum.com/honorbuddy-forum/classes/rogue/32282-release-mutaraid-cc.html");
            Logging.Write(Color.Orange, "");
            Logging.Write(Color.Orange, "Enjoy topping the DPS meters!");
            Logging.Write(Color.Orange, "");
        }

        public override void OnButtonPress()
        {
            var configUi = new UI.Config();
            configUi.Show();
        }

        public override void Pulse()
        {
            using (new FrameLock())
            {
                Helpers.Target.Pulse();
                Helpers.Area.Pulse();
                Helpers.Rogue.Pulse();
                Helpers.Focus.Pulse();
                Helpers.Specials.Pulse();

                Helpers.Target.EnsureValidTarget();
            }
        }
    }
}
