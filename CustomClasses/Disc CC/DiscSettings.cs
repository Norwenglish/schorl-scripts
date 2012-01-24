using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Styx;
using Styx.Helpers;

namespace Disc
{
    class DiscSettings : Settings
    {
        public static DiscSettings Instance = new DiscSettings();
        
        public DiscSettings()
            : base(Path.Combine(Logging.ApplicationPath, string.Format(@"CustomClasses\Config\Disc CC-Settings-{0}.xml", StyxWoW.Me.Name)))
        {
        }

        
        [Setting, DefaultValue(false)]
        public bool Stop_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool TankHealing_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int Mana_Percent { get; set; }

        [Setting, DefaultValue(15)]
        public int HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool PrayerMending_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool InnerFocus_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int Heal_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int FlashHeal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int Penance_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int PWShield_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(2)]
        public int PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(0)]
        public int Renew_SET { get; set; }

        //Selective Healing
        [Setting, DefaultValue(false)]
        public bool SelectiveHealing_SET { get; set; }

        public List<String> HealBlackList;

        public System.ComponentModel.BindingList<Dispels> UrgentDispelList; 

        //New Settings 3.0
        [Setting, DefaultValue(false)]
        public bool ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal Only")]
        public String DPS_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool FaceTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool Smite_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool HolyFire_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DevPlague_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool SWPain_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int DPSmana_SET { get; set; }

        [Setting, DefaultValue("Dungeon")]
        public String CurrentProfile_SET { get; set; }



        /*************************************************************************/
        //Raid Settings
        [Setting, DefaultValue(true)]
        public bool RAID_TankHealing_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String RAID_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int RAID_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int RAID_Mana_Percent { get; set; }

        [Setting, DefaultValue(10)]
        public int RAID_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int RAID_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int RAID_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int RAID_Heal_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int RAID_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int RAID_GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int RAID_Penance_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int RAID_PWShield_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int RAID_BindHeal_SET { get; set; }

        [Setting, DefaultValue(15)]
        public int RAID_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int RAID_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int RAID_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int RAID_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(2)]
        public int RAID_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(0)]
        public int RAID_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal Only")]
        public String RAID_DPS_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool RAID_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_Smite_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_HolyFire_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_DevPlague_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_SWPain_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool RAID_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int RAID_DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int RAID_DPSmana_SET { get; set; }

        /*************************************************************************/
        //Dungeon Settings
        [Setting, DefaultValue(true)]
        public bool DUNG_TankHealing_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String DUNG_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int DUNG_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int DUNG_Mana_Percent { get; set; }

        [Setting, DefaultValue(20)]
        public int DUNG_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int DUNG_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int DUNG_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int DUNG_Heal_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int DUNG_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int DUNG_GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int DUNG_Penance_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int DUNG_PWShield_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int DUNG_BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int DUNG_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int DUNG_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int DUNG_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int DUNG_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(2)]
        public int DUNG_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(85)]
        public int DUNG_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal Only")]
        public String DUNG_DPS_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool DUNG_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_Smite_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_HolyFire_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_DevPlague_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_SWPain_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool DUNG_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int DUNG_DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int DUNG_DPSmana_SET { get; set; }

        /*************************************************************************/
        //BG Settings
        [Setting, DefaultValue(true)]
        public bool BG_TankHealing_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String BG_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int BG_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int BG_Mana_Percent { get; set; }

        [Setting, DefaultValue(10)]
        public int BG_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int BG_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(20)]
        public int BG_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int BG_Heal_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int BG_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int BG_GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int BG_Penance_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int BG_PWShield_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int BG_BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int BG_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int BG_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int BG_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int BG_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int BG_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(85)]
        public int BG_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal First")]
        public String BG_DPS_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_Smite_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_HolyFire_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_DevPlague_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_SWPain_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool BG_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int BG_DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int BG_DPSmana_SET { get; set; }

        /*************************************************************************/
        //Arena Settings
        [Setting, DefaultValue(true)]
        public bool ARENA_TankHealing_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String ARENA_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int ARENA_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int ARENA_Mana_Percent { get; set; }

        [Setting, DefaultValue(10)]
        public int ARENA_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int ARENA_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(20)]
        public int ARENA_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int ARENA_Heal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int ARENA_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int ARENA_GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int ARENA_Penance_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int ARENA_PWShield_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int ARENA_BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int ARENA_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int ARENA_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int ARENA_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int ARENA_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int ARENA_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(85)]
        public int ARENA_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal First")]
        public String ARENA_DPS_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_Smite_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_HolyFire_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_DevPlague_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_SWPain_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool ARENA_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int ARENA_DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int ARENA_DPSmana_SET { get; set; }

        /*************************************************************************/
        //Solo Settings
        [Setting, DefaultValue(true)]
        public bool SOLO_TankHealing_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Will")]
        public String SOLO_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int SOLO_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int SOLO_Mana_Percent { get; set; }

        [Setting, DefaultValue(10)]
        public int SOLO_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int SOLO_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(20)]
        public int SOLO_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int SOLO_Heal_SET { get; set; }

        [Setting, DefaultValue(85)]
        public int SOLO_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int SOLO_GHeal_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int SOLO_Penance_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int SOLO_PWShield_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int SOLO_BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int SOLO_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int SOLO_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int SOLO_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int SOLO_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int SOLO_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(85)]
        public int SOLO_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal First")]
        public String SOLO_DPS_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_Smite_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_HolyFire_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_DevPlague_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_SWPain_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool SOLO_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(65)]
        public int SOLO_DPShealth_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int SOLO_DPSmana_SET { get; set; }

        /*************************************************************************/
        //Custom Settings
        [Setting, DefaultValue(true)]
        public bool CUSTOM_TankHealing_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_WeakenedSoul_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_FearWard_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_PWFort_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_ShadProt_SET { get; set; }

        [Setting, DefaultValue("Inner Fire")]
        public String CUSTOM_FireOrWill_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_Dispel_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_DispelUrgent_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_LevitateFallers_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int CUSTOM_Health_Percent { get; set; }

        [Setting, DefaultValue(50)]
        public int CUSTOM_Mana_Percent { get; set; }

        [Setting, DefaultValue(10)]
        public int CUSTOM_HymnHope_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_PowerInfusion_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_PrayerMending_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int CUSTOM_PainSuppression_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_Fade_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_InnerFocus_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int CUSTOM_ShadowFiend_SET { get; set; }

        //Heals
        [Setting, DefaultValue(95)]
        public int CUSTOM_Heal_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int CUSTOM_FlashHeal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int CUSTOM_GHeal_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int CUSTOM_Penance_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int CUSTOM_PWShield_SET { get; set; }

        [Setting, DefaultValue(80)]
        public int CUSTOM_BindHeal_SET { get; set; }

        [Setting, DefaultValue(12)]
        public int CUSTOM_DivineHymnHealth_SET { get; set; }

        [Setting, DefaultValue(3)]
        public int CUSTOM_DivHymnNum_SET { get; set; }

        [Setting, DefaultValue(35)]
        public int CUSTOM_PrayerHealingMin_SET { get; set; }

        [Setting, DefaultValue(75)]
        public int CUSTOM_PrayerHealingMax_SET { get; set; }

        [Setting, DefaultValue(2)]
        public int CUSTOM_PrayerHealingNum_SET { get; set; }

        [Setting, DefaultValue(0)]
        public int CUSTOM_Renew_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_ShieldAggro_Heal_SET { get; set; }

        [Setting, DefaultValue("Heal Only")]
        public String CUSTOM_DPS_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_FaceTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_ShieldAggroed_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_UseTankTarget_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_Smite_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_HolyFire_SET { get; set; }

        [Setting, DefaultValue(true)]
        public bool CUSTOM_DevPlague_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_SWPain_SET { get; set; }

        [Setting, DefaultValue(false)]
        public bool CUSTOM_PenanceDPS_SET { get; set; }

        [Setting, DefaultValue(50)]
        public int CUSTOM_DPShealth_SET { get; set; }

        [Setting, DefaultValue(0)]
        public int CUSTOM_DPSmana_SET { get; set; }
        
    }
}
