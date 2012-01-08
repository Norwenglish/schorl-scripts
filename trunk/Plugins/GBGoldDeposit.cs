// Behavior originally contributed by Unknown.
//
// DOCUMENTATION:
//     http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Custom_Behavior:_UseGameObject
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Croga
{
    public class GBGoldDeposit : CustomForcedBehavior
    {
        public GBGoldDeposit(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                
                DepositAmount = GetAttributeAsNullable<int>("DepositAmount", false, ConstrainAs.AuraId, null) ?? 0;
                KeepAmount = GetAttributeAsNullable<int>("KeepAmount", false, ConstrainAs.AuraId, null) ?? 0;

                LogMessage("debug", "Deposit " + DepositAmount.ToString());
                LogMessage("debug", "Keep " + KeepAmount.ToString());

            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }

        // Attributes provided by caller
        public int DepositAmount { get; private set; }
        public int KeepAmount { get; private set; }

        public static LocalPlayer Me = ObjectManager.Me;

        // Private variables for internal state
        private int _counter;
        private bool _isDisposed;
        private bool _isBehaviorDone;
        private Composite _root;

        ~GBGoldDeposit()
        {
            Dispose(false);
        }


        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    // empty, for now
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }

            _isDisposed = true;
        }


        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(

                    

                    // Open the Guild Bank
                    // Calculate how much to deposit
                    // Deposit money

                ));
        }


        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();


            // If KeepAmount was specified, calculate DepositAmount
            int Deposit = 0;
            if (DepositAmount == 0 || DepositAmount == null)
            {
                ulong gotMoney = Me.Gold * 10000;
                Deposit = (int)gotMoney - KeepAmount;
            }
            else
            {
                Deposit = DepositAmount;
            }

            // Message
            LogMessage("debug", "Depositing " + Deposit.ToString() + " copper.");

            // Open the Guild Bank
            List<WoWGameObject> banks = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.Entry == 205104).OrderBy(o => o.Distance).ToList();
            WoWGameObject bank = banks[0];
            bank.Interact();

            // Deposit money
            String script = "RunMacroText \"/run DepositGuildBankMoney("+Deposit.ToString()+")\"";
            LogMessage("debug", script);
            Styx.WoWInternals.Lua.DoString(script);

            _isBehaviorDone = true;
        }

        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone);
            }
        }

        #endregion
    }
}
