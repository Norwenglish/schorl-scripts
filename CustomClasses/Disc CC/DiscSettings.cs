using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Styx;
using Styx.Helpers;
using System.ComponentModel;
using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Disc
{
    class DiscSettings : Settings
    {
        public static DiscSettings Instance = new DiscSettings();
        
        public DiscSettings()
            : base(Path.Combine(Logging.ApplicationPath, string.Format(@"CustomClasses\Config\Disc CC-Settings-{0}.xml", StyxWoW.Me.Name)))
        {
        }

        public System.ComponentModel.BindingList<SelectiveHealName> SHRaidMembers;
        public System.ComponentModel.BindingList<SelectiveHealName> SHBlackListNames;
        public System.ComponentModel.BindingList<Dispels> UrgentDispelList;
        public bool Stop_SET = false;

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Shield tank")]
        [Description("Keep PW: Shield on tank")]
        public bool TankHealing_SET { get; set; }
        
        [Setting]
        [DefaultValue(true)]
        [Category("Misc")]
        [DisplayName("Dismount")]
        [Description("Dismount to heal")]
        public bool Dismount_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Discipline (Non-AA)")]
        [DisplayName("Clear W. Soul")]
        [Description("Clears Weakened Soul if doing nothing else")]
        public bool WeakenedSoul_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Misc")]
        [DisplayName("Fear Ward")]
        [Description("Use Fear Ward on self")]
        public bool FearWard_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Misc")]
        [DisplayName("PW: Fortitude")]
        [Description("Power Word: Fortitude")]
        public bool PWFort_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Misc")]
        [DisplayName("Shadow Protection")]
        [Description("Dismount to heal")]
        public bool ShadProt_SET { get; set; }

        [Setting]
        [DefaultValue("Inner Will or Fire")]
        [Category("Misc")]
        [DisplayName("Inner Will")]
        [Description("Inner Will or Inner Fire")]
        public String FireOrWill_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Dispel")]
        [Description("Dispel stuff")]
        public bool Dispel_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Dispel Urgent")]
        [Description("Dispel Urgent stuff")]
        public bool DispelUrgent_SET { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Misc")]
        [DisplayName("Rest Health")]
        [Description("Rest Health")]
        public int Health_Percent { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Misc")]
        [DisplayName("Rest Mana")]
        [Description("Rest Mana")]
        public int Mana_Percent { get; set; }

        [Setting]
        [DefaultValue(15)]
        [Category("Common")]
        [DisplayName("Hymn of Hope")]
        [Description("Cast Hymn of Hope at this mana%")]
        public int HymnHope_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Power Infusion")]
        [Description("Use Power Infusion")]
        public bool PowerInfusion_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Prayer of Mending")]
        [Description("Use Prayer of Mending")]
        public bool PrayerMending_SET { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Common")]
        [DisplayName("Pain Suppression")]
        [Description("Use Pain Suppression at this Health Percent")]
        public int PainSuppression_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Misc")]
        [DisplayName("Fade")]
        [Description("Fade when aggro")]
        public bool Fade_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Inner Focus")]
        [Description("Use Inner Focus")]
        public bool InnerFocus_SET { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Misc")]
        [DisplayName("Shadow Fiend")]
        [Description("Use Shadow Fiend at this Mana%")]
        public int ShadowFiend_SET { get; set; }

        //Heals
        [Setting]
        [DefaultValue(95)]
        [Category("Discipline (Non-AA)")]
        [DisplayName("Heal")]
        [Description("Heal at this Health%")]
        public int Heal_SET { get; set; }

        [Setting]
        [DefaultValue(35)]
        [Category("Common")]
        [DisplayName("Flash Heal")]
        [Description("Flash Heal at this Health%")]
        public int FlashHeal_SET { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Discipline (Non-AA)")]
        [DisplayName("Greater Heal")]
        [Description("Greater Heal at this Health%")]
        public int GHeal_SET { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Common")]
        [DisplayName("Penance")]
        [Description("Penance at this Health%")]
        public int Penance_SET { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Common")]
        [DisplayName("PW: Shield")]
        [Description("Power Word: Shield at this Health%")]
        public int PWShield_SET { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Discipline (Non-AA)")]
        [DisplayName("Binding Heal")]
        [Description("Binding Heal at this Health%")]
        public int BindHeal_SET { get; set; }

        [Setting]
        [DefaultValue(12)]
        [Category("Common")]
        [DisplayName("Divine Hymn")]
        [Description("Divine Hymn at this Health%")]
        public int DivineHymnHealth_SET { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Common")]
        [DisplayName("Divine Hymn Count")]
        [Description("Divine Hymn at this number of people")]
        public int DivHymnNum_SET { get; set; }

        [Setting]
        [DefaultValue(35)]
        [Category("Common")]
        [DisplayName("Prayer of Healing Min")]
        [Description("Prayer of Healing min Health%")]
        public int PrayerHealingMin_SET { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Common")]
        [DisplayName("Prayer of Healing Max")]
        [Description("Prayer of Healing max Health%")]
        public int PrayerHealingMax_SET { get; set; }

        [Setting]
        [DefaultValue(2)]
        [Category("Common")]
        [DisplayName("Prayer of Healing Count")]
        [Description("Prayer of Healing Count")]
        public int PrayerHealingNum_SET { get; set; }

        [Setting]
        [DefaultValue(0)]
        [Category("Discipline (Non-AA)")]
        [DisplayName("Renew")]
        [Description("Renew at this Health%")]
        public int Renew_SET { get; set; }
     
        //New Settings 3.0
        [Setting]
        [DefaultValue(true)]
        [Category("Common")]
        [DisplayName("Shield Aggroed")]
        [Description("If people have aggro, shield them")]
        public bool ShieldAggro_Heal_SET { get; set; }

        [Setting]
        [DefaultValue("Heal Only")]
        [Category("Misc")]
        [DisplayName("DPS")]
        [Description("Heal Only, Heal First, DPS First")]
        public String DPS_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Face target")]
        [Description("Face target")]
        public bool FaceTarget_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Shield Aggroed")]
        [Description("If people have aggro, shield them")]
        public bool ShieldAggroed_SET { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("DPS")]
        [DisplayName("Use tank target")]
        [Description("Use the tank's target")]
        public bool UseTankTarget_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Smite")]
        [Description("Smite")]
        public bool Smite_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Holy Fire")]
        [Description("Holy Fire")]
        public bool HolyFire_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Dev Plague")]
        [Description("Devouring Plague")]
        public bool DevPlague_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("SW: Pain")]
        [Description("Shadow Word: Pain")]
        public bool SWPain_SET { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("DPS")]
        [DisplayName("Penance")]
        [Description("Penance in dps rotation")]
        public bool PenanceDPS_SET { get; set; }

        [Setting]
        [DefaultValue(65)]
        [Category("DPS")]
        [DisplayName("DPS Health")]
        [Description("Min Health to DPS")]
        public int DPShealth_SET { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("DPS")]
        [DisplayName("DPS Mana")]
        [Description("Min Mana to DPS")]
        public int DPSmana_SET { get; set; }
        
    }
}
