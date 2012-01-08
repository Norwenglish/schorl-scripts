#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: apoc $
// $Date: 2011-12-11 01:22:31 -0800 (Sun, 11 Dec 2011) $
// $HeadURL: http://svn.apocdev.com/singular/trunk/Singular/Settings/HunterSettings.cs $
// $LastChangedBy: apoc $
// $LastChangedDate: 2011-12-11 01:22:31 -0800 (Sun, 11 Dec 2011) $
// $LastChangedRevision: 450 $
// $Revision: 450 $

#endregion

using System.ComponentModel;

using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    internal class HunterSettings : Styx.Helpers.Settings
    {
        public HunterSettings()
            : base(SingularSettings.SettingsPath + "_Hunter.xml")
        {
        }

        #region Category: Pet
        [Setting]
        [DefaultValue("1")]
        [Category("Pet")]
        [DisplayName("Pet Slot")]
        public string PetSlot { get; set; }
        #endregion
    }
}