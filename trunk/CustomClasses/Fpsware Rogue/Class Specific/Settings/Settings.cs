using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hera
{
    public static class Settings
    {
        internal static List<string> IgnoreSettings = new List<string>(new[] { "ConfigFile", "DirtyData", "Environment", "EnvironmentLoading","_debugKey" });
        public static string ConfigFolder = @"CustomClasses\Fpsware Rogue\Class Specific\Settings\";
        public static string ConfigFile = @"CustomClasses\Fpsware Rogue\Class Specific\Settings\Settings.xml";
        private static string _debugKey = "nothing has been ready yet";

        // Backing fields
        private static string _lazyRaider;
        private static int _maximumPullDistance;
        private static int _minimumPullDistance;
        private static int _combatTimeout;
        private static string _mainHandPoison;
        private static string _offHandPoison;

        public static string LazyRaider { get { return _lazyRaider; } set { _lazyRaider = value; Target.LazyRaider = value; Movement.LazyRaider = value; } }

        // Common settings template
        //public static string Environment { get; set; }
        //public static string EnvironmentLoading { get; set; }

        public static string LowLevelCheck { get; set; }
        public static bool DirtyData { get; set; }
        public static int RestHealth { get; set; }
        public static int GougeHealthPercent { get; set; }
        public static int VendettaHealthPercent { get; set; }
        public static int RuptureHealthPercent { get; set; }
        public static int FinishingMoveHealthPercent { get; set; }
        public static string SprintDistance { get; set; }
        public static string StealthDistance { get; set; }
        public static string StealthAlways { get; set; }
        public static int RestMana { get; set; }
        public static string Debug { get; set; }
        public static string RAFTarget { get; set; }
        public static string ShowUI { get; set; }
        public static string SmartEatDrink { get; set; }
        public static int HealthPotion { get; set; }
        public static int ManaPotion { get; set; }
        public static int LifebloodHealth{ get; set; }
        public static string RacialSpell { get; set; }
        public static string RacialUseWhen { get; set; }
        public static int SinisterStrikeEnergy { get; set; }
        public static int EvasionHealth { get; set; }
        public static int RecuperateHealth { get; set; }
        public static string RecuperateCombo{ get; set; }
        public static int VanishHealth { get; set; }
        public static string PickPocket { get; set; }
        public static string PullSpell { get; set; }
        public static string SliceAndDice { get; set; }
        public static string Backstab { get; set; }
        public static string BlindAdd { get; set; }
        public static string Sap { get; set; }
        public static string BladeFlurry { get; set; }
        public static string StealthBehind { get; set; }
        public static string AdrenalineRush { get; set; }
        public static string KillingSpree { get; set; }
        public static string Vendetta{ get; set; }
        public static string ShadowDance { get; set; }
        public static string FinishingMove { get; set; }
        public static string FinisherComboPoints { get; set; }
        public static string Rupture { get; set; }
        public static string StealthOverride { get; set; }
        public static string IgnoreRunners { get; set; }
        public static string RevealingStrike { get; set; }
        public static int RevealingStrikeHealth { get; set; }
        public static string PickPocketOnly { get; set; }

        public static string MainHandPoison { get { return _mainHandPoison; } set { _mainHandPoison = value; ClassHelpers.Rogue.Poisons.MainHandPoison = value; } }
        public static string OffHandPoison { get { return _offHandPoison; } set { _offHandPoison = value; ClassHelpers.Rogue.Poisons.OffHandPoison = value; } }
        public static int MaximumPullDistance { get { return _maximumPullDistance; } set { _maximumPullDistance = value; Movement.MaximumDistance = _maximumPullDistance; } }
        public static int MinimumPullDistance { get { return _minimumPullDistance; } set { _minimumPullDistance = value; Movement.MinimumDistance = _minimumPullDistance; } }
        public static int CombatTimeout { get { return _combatTimeout; } set { _combatTimeout = value; Target.CombatTimeout = _combatTimeout; } }



        #region Save and load settings
        public static void Save()
        {
            ConfigSettings.FileName = Settings.ConfigFile;

            if (ConfigSettings.Open())
            {
                foreach (PropertyInfo p in typeof(Settings).GetProperties())
                {
                    if (p.Name.StartsWith("_") || IgnoreSettings.Contains(p.Name)) continue;

                    object propValue = typeof(Settings).GetProperty(p.Name).GetValue(p.Name, null);
                    ConfigSettings.SetProperty(String.Format("//{0}/{1}", Fpsware.CCClass, p.Name), propValue.ToString());
                }

                ConfigSettings.Save();
            }
        }

        public static void Load()
        {
            ConfigSettings.FileName = Settings.ConfigFile;

            try
            {
                if (ConfigSettings.Open())
                {
                    foreach (PropertyInfo p in typeof(Settings).GetProperties())
                    {
                        if (p.Name.StartsWith("_") || IgnoreSettings.Contains(p.Name)) continue;
                        _debugKey = p.Name;

                        switch (typeof(Settings).GetProperty(p.Name).PropertyType.Name)
                        {
                            case "Boolean": { p.SetValue(typeof(Settings), Convert.ToBoolean(ConfigSettings.GetBoolProperty(String.Format("//{0}/{1}", Fpsware.CCClass, p.Name))), null); } break;
                            case "String": { p.SetValue(typeof(Settings), Convert.ToString(ConfigSettings.GetStringProperty(String.Format("//{0}/{1}", Fpsware.CCClass, p.Name))), null); } break;
                            case "Int32": { p.SetValue(typeof(Settings), Convert.ToInt16(ConfigSettings.GetIntProperty(String.Format("//{0}/{1}", Fpsware.CCClass, p.Name))), null); } break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Log("************ something went wrong ready the config file");
                Utils.Log(e.Message);
                Utils.Log("*********** Last key attempted to be read was: " + _debugKey);
            }

        }
        #endregion


    }
}