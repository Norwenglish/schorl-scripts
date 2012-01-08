using System;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Logic;
using Styx.WoWInternals;

namespace Hera
{
    public partial class Fpsware
    {
        // ************************************************************************************
        //
        public const string CCName = "Fpsware Rogue";                                   // Name of the CC displayed to the user
        public const string AuthorName = "Fpsware";                                     // Part of the string used in the CC name
        private readonly Version _versionNumber = new Version(0, 1, 7);                 // Part of the string used in the CC name
        public const WoWClass CCClass = WoWClass.Rogue;                                 // The class this CC will support
        // ************************************************************************************

        #region HB Start Up
        void BotEvents_OnBotStarted(EventArgs args)
        {
            // Finds the spec of your class: 0,1,2,3 and uses an enum to return something more logical
            ClassHelpers.Rogue.ClassSpec = (ClassHelpers.Rogue.ClassType)Talents.Spec;
            Utils.Log(string.Format("You are a level {0} {1} {2}", Me.Level, ClassHelpers.Rogue.ClassSpec, Me.Class));

            // Do important stuff on LUA events
            Lua.Events.AttachEvent("COMBAT_LOG_EVENT", EventHandlers.CombatLogEventHander);
            Lua.Events.AttachEvent("CHARACTER_POINTS_CHANGED", EventHandlers.TalentPointEventHander);
            Lua.Events.AttachEvent("PLAYER_TALENT_UPDATE", EventHandlers.TalentPointEventHander);

            Timers.Add("Environment");
            Timers.Add("Pulse");
            Timers.Add("PulseOther");
            Timers.Add("Interact");
            Timers.Add("DistanceCheck");
            Timers.Add("PickPocket");
            Timers.Add("PoisonCheck");
            Timers.Add("Kick");
            Timers.Add("KidneyShot");
            Timers.Add("Vanish");
            Timers.Add("Backstab");
            Timers.Add("Distract");
            Timers.Add("AlwaysStealth");

            LoadSettings(true);

            ClassHelpers.Rogue.CanShoot = true;
            ClassHelpers.Rogue.CanThrow = true;
        }

        // This event is fired each time you hit the Stop button in HB
        // Currently its only asigning FALSE to a variable, but you go do anything you want in here
        void BotEvents_OnBotStopped(EventArgs args)
        {
            //
        }
        #endregion

        #region Pulse
        public override void Pulse()
        {
            // HB runs this as frequenty as possible. I don't know the exact frequency but its supposed to be 5-10 times per second
            // Anything you want checked on a regular basis you may want to add here. 
            // For example buffing / healing random players

            base.Pulse();

            int lootableMobs = LootTargeting.Instance.LootingList.Count;

            
            if (!_isCCLoaded) { _isCCLoaded = true; Settings.DirtyData = true; }
            if (Settings.DirtyData) LoadSettings(true);

            // So we don't overload HB the below code is only run once per second
            if (!Timers.Expired("Pulse", 1000)) return;
            Timers.Reset("Pulse");

            // Stealth always - if selected
            if (!Me.IsMoving) Timers.Reset("AlwaysStealth");
            if (Settings.StealthAlways.Contains("always") && Timers.Expired("AlwaysStealth", 4000) && !Me.IsFlying && !Me.Mounted && !Self.IsBuffOnMe("Stealth") && !Me.Combat && Self.IsHealthPercentAbove(Settings.RestHealth) && lootableMobs <= 0)
            {
                Timers.Reset("AlwaysStealth");
                Spell.Cast("Stealth");
            }

            // Poisons
            if (Timers.Expired("PoisonCheck", 5000))
            {
                Timers.Reset("PoisonCheck");
                if (!Me.Dead && !Me.IsGhost && !Me.Combat && !Me.IsFlying) ClassHelpers.Rogue.Poisons.ApplyPoisons();
            }

            // Out of combat Recuperate
            if (!Me.Combat && !Self.IsBuffOnMe("Recuperate") && !Self.IsHealthPercentAbove(95) && Spell.CanCast("Recuperate")) Spell.Cast("Recuperate");
        }


        #endregion
    }
}
