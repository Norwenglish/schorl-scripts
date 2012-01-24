using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace Disc
{
    public partial class settingsForm : Form
    {
        //DiscPriest dp = new DiscPriest();
        private string[] RaidRoster;

        System.Windows.Forms.Label[] RaidNameLabels;
        System.Windows.Forms.Label[] HealStatusLabels;
        System.Windows.Forms.CheckBox[] Phase1Boxs;
        System.Windows.Forms.CheckBox[] Phase2Boxs;
        System.Windows.Forms.CheckBox[] Phase3Boxs;
        
        public settingsForm()
        {            
            InitializeComponent();

            DiscSettings.Instance.HealBlackList = new List<string>();
            DiscSettings.Instance.HealBlackList.Clear();
            DiscSettings.Instance.HealBlackList.Add("Blank");
            DiscSettings.Instance.UrgentDispelList = new System.ComponentModel.BindingList<Dispels>();
            ImportDispels();
            dataGridView1.DataSource = DiscSettings.Instance.UrgentDispelList;
            dataGridView1.Columns[0].HeaderText = "Urgent Dispels";
            dataGridView1.RowHeadersVisible = false;
            
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
            this.RaidNameLabels = new System.Windows.Forms.Label[40];
            this.HealStatusLabels = new System.Windows.Forms.Label[40];
            this.Phase1Boxs = new System.Windows.Forms.CheckBox[40];
            this.Phase2Boxs = new System.Windows.Forms.CheckBox[40];
            this.Phase3Boxs = new System.Windows.Forms.CheckBox[40];
            int m = 0;
            for (int i = 0; i < 40; i++)
            {                
                if ((i % 5) == 0)
                {
                    m = ((7*i) / 5);
                }                
                //Raid Name Labels
                this.RaidNameLabels[i] = new Label();
                this.RaidNameLabels[i].AutoSize = true;
                this.RaidNameLabels[i].Location = new System.Drawing.Point(60, 8+(16*i) + m);
                this.RaidNameLabels[i].Margin = new System.Windows.Forms.Padding(1);
                this.RaidNameLabels[i].Size = new System.Drawing.Size(41, 13);
                this.RaidNameLabels[i].TabIndex = 6;
                this.RaidNameLabels[i].Text = "";
                this.tabPage2.Controls.Add(RaidNameLabels[i]);
                //Healing Status Labels
                this.HealStatusLabels[i] = new Label();
                this.HealStatusLabels[i].AutoSize = true;
                this.HealStatusLabels[i].Location = new System.Drawing.Point(200, 8 + (16 * i) + m);
                this.HealStatusLabels[i].Margin = new System.Windows.Forms.Padding(1);
                this.HealStatusLabels[i].Size = new System.Drawing.Size(41, 13);
                this.HealStatusLabels[i].TabIndex = 6;
                this.HealStatusLabels[i].Text = "";
                this.tabPage2.Controls.Add(HealStatusLabels[i]);
                //Phase 1 Boxes
                this.Phase1Boxs[i] = new CheckBox();
                this.Phase1Boxs[i].AutoSize = true;
                this.Phase1Boxs[i].Checked = true;
                this.Phase1Boxs[i].CheckState = System.Windows.Forms.CheckState.Checked;
                this.Phase1Boxs[i].Location = new System.Drawing.Point(2, 8 + (16 * i) + m);
                this.Phase1Boxs[i].Margin = new System.Windows.Forms.Padding(1);
                this.Phase1Boxs[i].Size = new System.Drawing.Size(15, 14);
                this.Phase1Boxs[i].TabIndex = 0;
                this.Phase1Boxs[i].UseVisualStyleBackColor = true;
                this.tabPage2.Controls.Add(Phase1Boxs[i]);
                //Phase 2 Boxes
                this.Phase2Boxs[i] = new CheckBox();
                this.Phase2Boxs[i].AutoSize = true;
                this.Phase2Boxs[i].Checked = true;
                this.Phase2Boxs[i].CheckState = System.Windows.Forms.CheckState.Checked;
                this.Phase2Boxs[i].Location = new System.Drawing.Point(19, 8 + (16 * i) + m);
                this.Phase2Boxs[i].Margin = new System.Windows.Forms.Padding(1);
                this.Phase2Boxs[i].Size = new System.Drawing.Size(15, 14);
                this.Phase2Boxs[i].TabIndex = 0;
                this.Phase2Boxs[i].UseVisualStyleBackColor = true;
                this.tabPage2.Controls.Add(Phase2Boxs[i]);
                //Phase 3 Boxes
                this.Phase3Boxs[i] = new CheckBox();
                this.Phase3Boxs[i].AutoSize = true;
                this.Phase3Boxs[i].Checked = true;
                this.Phase3Boxs[i].CheckState = System.Windows.Forms.CheckState.Checked;
                this.Phase3Boxs[i].Location = new System.Drawing.Point(36, 8 + (16 * i) + m);
                this.Phase3Boxs[i].Margin = new System.Windows.Forms.Padding(1);
                this.Phase3Boxs[i].Size = new System.Drawing.Size(15, 14);
                this.Phase3Boxs[i].TabIndex = 0;
                this.Phase3Boxs[i].UseVisualStyleBackColor = true;
                this.tabPage2.Controls.Add(Phase3Boxs[i]);

            }
            
            loadSettings();
            DiscSettings.Instance.Stop_SET = false;
        }

        private void loadSettings()
        {
            DiscSettings.Instance.Load();
            FillSettings();
        }

        private void ImportDispels()
        {
            try
            {
                StreamReader s = new StreamReader("DiscCC_dispels.ini");
                String str;
                while ((str = s.ReadLine()) != null)
                {
                    Logging.Write("Importing Urgent Dispel: " + str);
                    DiscSettings.Instance.UrgentDispelList.Add(new Dispels(str));
                }
                s.Close();
            }
            catch (Exception e)
            {
                Logging.Write("Unable to read dispels.ini");
                Logging.Write(e.Message);
            }
        }

        private void PrintDispels()
        {
            StreamWriter o = new StreamWriter("DiscCC_dispels.ini");
            foreach(Dispels d in DiscSettings.Instance.UrgentDispelList)
            {
                o.WriteLine(d.ListItem.ToString());
            }
            o.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                DiscSettings.Instance.UrgentDispelList.Add(new Dispels(textBox1.Text));
                dataGridView1.DataSource = DiscSettings.Instance.UrgentDispelList;
                textBox1.Text = "";
            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                int i = -1;
                foreach (Dispels d in DiscSettings.Instance.UrgentDispelList)
                {
                    if (d.ListItem.ToString().Equals(textBox1.Text))
                    {
                        i=DiscSettings.Instance.UrgentDispelList.IndexOf(d);
                        //DiscSettings.Instance.UrgentDispelList.Remove(d);
                    }
                }
                if(i!=-1)
                {
                    DiscSettings.Instance.UrgentDispelList.RemoveAt(i);
                }
                
                dataGridView1.DataSource = DiscSettings.Instance.UrgentDispelList;                
                textBox1.Text = "";
            }
            dataGridView1.Columns[0].HeaderText = "Urgent Dispels";
            dataGridView1.RowHeadersVisible = false;
        }

        private void FillSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.WeakenedSoul_SET);

            //Turn off Selective Healing at Startup
            DiscSettings.Instance.SelectiveHealing_SET = false;
            CheckTheBox(checkBox88, DiscSettings.Instance.SelectiveHealing_SET);

            //New 3.0 Stuff
            setCB2Index();
            CheckTheBox(checkBox6, DiscSettings.Instance.ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }
            if (String.Equals(Convert.ToString(DiscSettings.Instance.DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillDungeonSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.DUNG_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.DUNG_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.DUNG_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.DUNG_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.DUNG_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.DUNG_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.DUNG_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.DUNG_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.DUNG_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.DUNG_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.DUNG_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.DUNG_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.DUNG_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.DUNG_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.DUNG_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.DUNG_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.DUNG_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.DUNG_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.DUNG_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.DUNG_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.DUNG_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.DUNG_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.DUNG_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.DUNG_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.DUNG_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.DUNG_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.DUNG_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.DUNG_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.DUNG_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.DUNG_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.DUNG_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.DUNG_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.DUNG_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.DUNG_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.DUNG_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.DUNG_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.DUNG_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.DUNG_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }

            if (String.Equals(Convert.ToString(DiscSettings.Instance.DUNG_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.DUNG_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillRaidSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.RAID_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.RAID_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.RAID_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.RAID_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.RAID_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.RAID_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.RAID_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.RAID_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.RAID_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.RAID_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.RAID_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.RAID_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.RAID_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.RAID_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.RAID_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.RAID_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.RAID_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.RAID_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.RAID_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.RAID_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.RAID_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.RAID_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.RAID_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.RAID_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.RAID_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.RAID_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.RAID_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.RAID_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.RAID_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.RAID_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.RAID_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.RAID_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.RAID_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.RAID_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.RAID_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.RAID_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.RAID_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.RAID_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }

            if (String.Equals(Convert.ToString(DiscSettings.Instance.RAID_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.RAID_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillBGSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.BG_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.BG_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.BG_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.BG_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.BG_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.BG_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.BG_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.BG_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.BG_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.BG_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.BG_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.BG_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.BG_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.BG_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.BG_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.BG_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.BG_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.BG_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.BG_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.BG_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.BG_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.BG_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.BG_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.BG_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.BG_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.BG_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.BG_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.BG_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.BG_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.BG_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.BG_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.BG_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.BG_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.BG_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.BG_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.BG_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.BG_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.BG_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }

            if (String.Equals(Convert.ToString(DiscSettings.Instance.BG_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.BG_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillArenaSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.ARENA_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.ARENA_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.ARENA_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.ARENA_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.ARENA_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.ARENA_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.ARENA_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.ARENA_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.ARENA_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.ARENA_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.ARENA_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.ARENA_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.ARENA_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.ARENA_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.ARENA_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.ARENA_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.ARENA_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.ARENA_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.ARENA_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.ARENA_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.ARENA_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.ARENA_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.ARENA_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.ARENA_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.ARENA_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.ARENA_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.ARENA_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.ARENA_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.ARENA_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.ARENA_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.ARENA_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.ARENA_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.ARENA_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.ARENA_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.ARENA_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.ARENA_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.ARENA_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.ARENA_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }
            if (String.Equals(Convert.ToString(DiscSettings.Instance.ARENA_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.ARENA_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillSoloSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.SOLO_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.SOLO_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.SOLO_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.SOLO_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.SOLO_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.SOLO_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.SOLO_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.SOLO_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.SOLO_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.SOLO_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.SOLO_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.SOLO_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.SOLO_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.SOLO_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.SOLO_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.SOLO_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.SOLO_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.SOLO_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.SOLO_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.SOLO_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.SOLO_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.SOLO_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.SOLO_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.SOLO_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.SOLO_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.SOLO_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.SOLO_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.SOLO_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.SOLO_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.SOLO_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.SOLO_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.SOLO_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.SOLO_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.SOLO_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.SOLO_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.SOLO_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.SOLO_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.SOLO_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }
            if (String.Equals(Convert.ToString(DiscSettings.Instance.SOLO_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.SOLO_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void FillCustomSettings()
        {
            numericUpDown1.Value = DiscSettings.Instance.CUSTOM_Heal_SET;
            numericUpDown2.Value = DiscSettings.Instance.CUSTOM_FlashHeal_SET;
            numericUpDown3.Value = DiscSettings.Instance.CUSTOM_GHeal_SET;
            numericUpDown4.Value = DiscSettings.Instance.CUSTOM_Penance_SET;
            numericUpDown5.Value = DiscSettings.Instance.CUSTOM_PWShield_SET;
            numericUpDown6.Value = DiscSettings.Instance.CUSTOM_BindHeal_SET;
            numericUpDown7.Value = DiscSettings.Instance.CUSTOM_ShadowFiend_SET;
            numericUpDown8.Value = DiscSettings.Instance.CUSTOM_HymnHope_SET;
            numericUpDown9.Value = DiscSettings.Instance.CUSTOM_Renew_SET;
            numericUpDown10.Value = DiscSettings.Instance.CUSTOM_PrayerHealingMin_SET;
            numericUpDown11.Value = DiscSettings.Instance.CUSTOM_PrayerHealingNum_SET;
            numericUpDown12.Value = DiscSettings.Instance.CUSTOM_PrayerHealingMax_SET;
            numericUpDown13.Value = DiscSettings.Instance.CUSTOM_DivineHymnHealth_SET;
            numericUpDown14.Value = DiscSettings.Instance.CUSTOM_DivHymnNum_SET;
            numericUpDown15.Value = DiscSettings.Instance.CUSTOM_PainSuppression_SET;

            CheckTheBox(checkBox1, DiscSettings.Instance.CUSTOM_PWFort_SET);
            CheckTheBox(checkBox2, DiscSettings.Instance.CUSTOM_ShadProt_SET);
            CheckTheBox(checkBox3, DiscSettings.Instance.CUSTOM_FearWard_SET);
            CheckTheBox(checkBox4, DiscSettings.Instance.CUSTOM_PowerInfusion_SET);
            CheckTheBox(checkBox5, DiscSettings.Instance.CUSTOM_PrayerMending_SET);
            CheckTheBox(checkBox87, DiscSettings.Instance.CUSTOM_TankHealing_SET);
            CheckTheBox(checkBox89, DiscSettings.Instance.CUSTOM_Fade_SET);
            CheckTheBox(checkBox90, DiscSettings.Instance.CUSTOM_Dispel_SET);
            CheckTheBox(checkBox91, DiscSettings.Instance.CUSTOM_InnerFocus_SET);
            CheckTheBox(checkBox92, DiscSettings.Instance.CUSTOM_WeakenedSoul_SET);

            //New 3.0 Stuff
            CheckTheBox(checkBox6, DiscSettings.Instance.CUSTOM_ShieldAggro_Heal_SET);
            CheckTheBox(checkBox7, DiscSettings.Instance.CUSTOM_ShieldAggroed_SET);
            CheckTheBox(checkBox8, DiscSettings.Instance.CUSTOM_UseTankTarget_SET);
            CheckTheBox(checkBox9, DiscSettings.Instance.CUSTOM_Smite_SET);
            CheckTheBox(checkBox10, DiscSettings.Instance.CUSTOM_HolyFire_SET);
            CheckTheBox(checkBox11, DiscSettings.Instance.CUSTOM_DevPlague_SET);
            CheckTheBox(checkBox12, DiscSettings.Instance.CUSTOM_SWPain_SET);
            CheckTheBox(checkBox13, DiscSettings.Instance.CUSTOM_PenanceDPS_SET);
            CheckTheBox(checkBox14, DiscSettings.Instance.CUSTOM_DispelUrgent_SET);
            CheckTheBox(checkBox15, DiscSettings.Instance.CUSTOM_FaceTarget_SET);
            numericUpDown16.Value = DiscSettings.Instance.CUSTOM_DPShealth_SET;
            numericUpDown17.Value = DiscSettings.Instance.CUSTOM_DPSmana_SET;


            //Old Dont feel like changing
            if (String.Equals(Convert.ToString(DiscSettings.Instance.CUSTOM_FireOrWill_SET), "Inner Will"))
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                comboBox1.SelectedIndex = 1;
            }

            if (String.Equals(Convert.ToString(DiscSettings.Instance.CUSTOM_DPS_SET), "Heal First"))
            {
                comboBox3.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.CUSTOM_DPS_SET), "DPS First"))
            {
                comboBox3.SelectedIndex = 1;
            }
            else
            {
                comboBox3.SelectedIndex = 2;
            }
        }

        private void setCB2Index()
        {
            if (String.Equals(Convert.ToString(DiscSettings.Instance.CurrentProfile_SET), "Dungeon"))
            {
                comboBox2.SelectedIndex = 0;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.CurrentProfile_SET), "Raid"))
            {
                comboBox2.SelectedIndex = 1;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.CurrentProfile_SET), "Battleground"))
            {
                comboBox2.SelectedIndex = 2;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.CurrentProfile_SET), "Arena"))
            {
                comboBox2.SelectedIndex = 3;
            }
            else if (String.Equals(Convert.ToString(DiscSettings.Instance.CurrentProfile_SET), "Solo"))
            {
                comboBox2.SelectedIndex = 4;
            }
            else
            {
                comboBox2.SelectedIndex = 5;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetCurrentSettings();
            if (comboBox2.SelectedIndex == 0)
            {
                SetCurrentSettings();
                SaveDungeonSettings();
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                SetCurrentSettings();
                SaveRaidSettings();
            }
            else if (comboBox2.SelectedIndex == 2)
            {
                SetCurrentSettings();
                SaveBGSettings();
            }
            else if (comboBox2.SelectedIndex == 3)
            {
                SetCurrentSettings();
                SaveArenaSettings();
            }
            else if (comboBox2.SelectedIndex == 4)
            {
                SetCurrentSettings();
                SaveSoloSettings();
            }
            else
            {
                SetCurrentSettings();
                SaveCustomSettings();
                
            }
            PrintDispels();
            DiscSettings.Instance.Save();
            Logging.Write("Settings Saved");
        }

        private void SetCurrentSettings()
        {
            DiscSettings.Instance.Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.SelectiveHealing_SET = BoxChecked(checkBox88);
            DiscSettings.Instance.FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.DPS_SET = comboBox3.Text;

            //3.0 Stuff            
            DiscSettings.Instance.ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.DPS_SET = "Heal Only";

            } 
        }

        private void SaveDungeonSettings()
        {
            DiscSettings.Instance.DUNG_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.DUNG_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.DUNG_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.DUNG_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.DUNG_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.DUNG_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.DUNG_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.DUNG_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.DUNG_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.DUNG_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.DUNG_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.DUNG_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.DUNG_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.DUNG_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.DUNG_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.DUNG_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.DUNG_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.DUNG_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.DUNG_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.DUNG_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.DUNG_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.DUNG_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.DUNG_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.DUNG_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.DUNG_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.DUNG_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.DUNG_FireOrWill_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.DUNG_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.DUNG_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.DUNG_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.DUNG_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.DUNG_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.DUNG_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.DUNG_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.DUNG_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.DUNG_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.DUNG_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.DUNG_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.DUNG_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.DUNG_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.DUNG_FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.DUNG_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.DUNG_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.DUNG_DPS_SET = "Heal Only";

            } 
        }

        private void SaveRaidSettings()
        {
            DiscSettings.Instance.RAID_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.RAID_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.RAID_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.RAID_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.RAID_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.RAID_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.RAID_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.RAID_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.RAID_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.RAID_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.RAID_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.RAID_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.RAID_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.RAID_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.RAID_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.RAID_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.RAID_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.RAID_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.RAID_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.RAID_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.RAID_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.RAID_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.RAID_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.RAID_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.RAID_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.RAID_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.RAID_DPS_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.RAID_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.RAID_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.RAID_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.RAID_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.RAID_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.RAID_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.RAID_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.RAID_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.RAID_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.RAID_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.RAID_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.RAID_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.RAID_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.RAID_FireOrWill_SET = "Inner Fire";

            }

            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.RAID_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.RAID_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.RAID_DPS_SET = "Heal Only";

            } 
        }

        private void SaveBGSettings()
        {
            DiscSettings.Instance.BG_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.BG_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.BG_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.BG_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.BG_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.BG_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.BG_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.BG_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.BG_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.BG_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.BG_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.BG_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.BG_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.BG_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.BG_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.BG_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.BG_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.BG_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.BG_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.BG_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.BG_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.BG_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.BG_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.BG_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.BG_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.BG_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.BG_DPS_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.BG_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.BG_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.BG_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.BG_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.BG_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.BG_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.BG_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.BG_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.BG_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.BG_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.BG_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.BG_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.BG_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.BG_FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.BG_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.BG_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.BG_DPS_SET = "Heal Only";

            } 
        }

        private void SaveArenaSettings()
        {
            DiscSettings.Instance.ARENA_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.ARENA_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.ARENA_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.ARENA_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.ARENA_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.ARENA_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.ARENA_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.ARENA_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.ARENA_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.ARENA_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.ARENA_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.ARENA_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.ARENA_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.ARENA_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.ARENA_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.ARENA_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.ARENA_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.ARENA_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.ARENA_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.ARENA_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.ARENA_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.ARENA_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.ARENA_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.ARENA_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.ARENA_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.ARENA_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.ARENA_DPS_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.ARENA_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.ARENA_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.ARENA_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.ARENA_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.ARENA_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.ARENA_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.ARENA_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.ARENA_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.ARENA_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.ARENA_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.ARENA_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.ARENA_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.ARENA_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.ARENA_FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.ARENA_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.ARENA_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.ARENA_DPS_SET = "Heal Only";

            }
        }

        private void SaveSoloSettings()
        {
            DiscSettings.Instance.SOLO_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.SOLO_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.SOLO_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.SOLO_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.SOLO_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.SOLO_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.SOLO_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.SOLO_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.SOLO_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.SOLO_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.SOLO_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.SOLO_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.SOLO_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.SOLO_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.SOLO_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.SOLO_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.SOLO_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.SOLO_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.SOLO_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.SOLO_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.SOLO_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.SOLO_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.SOLO_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.SOLO_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.SOLO_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.SOLO_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.SOLO_DPS_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.SOLO_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.SOLO_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.SOLO_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.SOLO_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.SOLO_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.SOLO_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.SOLO_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.SOLO_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.SOLO_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.SOLO_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.SOLO_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.SOLO_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.SOLO_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.SOLO_FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.SOLO_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.SOLO_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.SOLO_DPS_SET = "Heal Only";

            }
        }

        private void SaveCustomSettings()
        {
            DiscSettings.Instance.CUSTOM_Heal_SET = Convert.ToInt16(numericUpDown1.Value);
            DiscSettings.Instance.CUSTOM_FlashHeal_SET = Convert.ToInt16(numericUpDown2.Value);
            DiscSettings.Instance.CUSTOM_GHeal_SET = Convert.ToInt16(numericUpDown3.Value);
            DiscSettings.Instance.CUSTOM_Penance_SET = Convert.ToInt16(numericUpDown4.Value);
            DiscSettings.Instance.CUSTOM_PWShield_SET = Convert.ToInt16(numericUpDown5.Value);
            DiscSettings.Instance.CUSTOM_BindHeal_SET = Convert.ToInt16(numericUpDown6.Value);
            DiscSettings.Instance.CUSTOM_ShadowFiend_SET = Convert.ToInt16(numericUpDown7.Value);
            DiscSettings.Instance.CUSTOM_HymnHope_SET = Convert.ToInt16(numericUpDown8.Value);
            DiscSettings.Instance.CUSTOM_Renew_SET = Convert.ToInt16(numericUpDown9.Value);
            DiscSettings.Instance.CUSTOM_PrayerHealingMin_SET = Convert.ToInt16(numericUpDown10.Value);
            DiscSettings.Instance.CUSTOM_PrayerHealingNum_SET = Convert.ToInt16(numericUpDown11.Value);
            DiscSettings.Instance.CUSTOM_PrayerHealingMax_SET = Convert.ToInt16(numericUpDown12.Value);
            DiscSettings.Instance.CUSTOM_DivineHymnHealth_SET = Convert.ToInt16(numericUpDown13.Value);
            DiscSettings.Instance.CUSTOM_PainSuppression_SET = Convert.ToInt16(numericUpDown15.Value);
            DiscSettings.Instance.CUSTOM_DivHymnNum_SET = Convert.ToInt16(numericUpDown14.Value);

            DiscSettings.Instance.CUSTOM_PWFort_SET = BoxChecked(checkBox1);
            DiscSettings.Instance.CUSTOM_ShadProt_SET = BoxChecked(checkBox2);
            DiscSettings.Instance.CUSTOM_FearWard_SET = BoxChecked(checkBox3);
            DiscSettings.Instance.CUSTOM_PowerInfusion_SET = BoxChecked(checkBox4);
            DiscSettings.Instance.CUSTOM_PrayerMending_SET = BoxChecked(checkBox5);
            DiscSettings.Instance.CUSTOM_TankHealing_SET = BoxChecked(checkBox87);
            DiscSettings.Instance.CUSTOM_Fade_SET = BoxChecked(checkBox89);
            DiscSettings.Instance.CUSTOM_Dispel_SET = BoxChecked(checkBox90);
            DiscSettings.Instance.CUSTOM_InnerFocus_SET = BoxChecked(checkBox91);
            DiscSettings.Instance.CUSTOM_WeakenedSoul_SET = BoxChecked(checkBox92);
            DiscSettings.Instance.CUSTOM_FireOrWill_SET = comboBox1.Text;
            DiscSettings.Instance.CUSTOM_DPS_SET = comboBox3.Text;

            //3.0 Stuff
            DiscSettings.Instance.CUSTOM_ShieldAggro_Heal_SET = BoxChecked(checkBox6);
            DiscSettings.Instance.CUSTOM_DispelUrgent_SET = BoxChecked(checkBox14);
            DiscSettings.Instance.CUSTOM_FaceTarget_SET = BoxChecked(checkBox15);
            DiscSettings.Instance.CUSTOM_ShieldAggroed_SET = BoxChecked(checkBox7);
            DiscSettings.Instance.CUSTOM_UseTankTarget_SET = BoxChecked(checkBox8);
            DiscSettings.Instance.CUSTOM_Smite_SET = BoxChecked(checkBox9);
            DiscSettings.Instance.CUSTOM_HolyFire_SET = BoxChecked(checkBox10);
            DiscSettings.Instance.CUSTOM_DevPlague_SET = BoxChecked(checkBox11);
            DiscSettings.Instance.CUSTOM_SWPain_SET = BoxChecked(checkBox12);
            DiscSettings.Instance.CUSTOM_PenanceDPS_SET = BoxChecked(checkBox13);
            DiscSettings.Instance.CUSTOM_DPShealth_SET = Convert.ToInt16(numericUpDown16.Value);
            DiscSettings.Instance.CUSTOM_DPSmana_SET = Convert.ToInt16(numericUpDown17.Value);

            if (comboBox1.SelectedIndex == 0)
            {
                DiscSettings.Instance.CUSTOM_FireOrWill_SET = "Inner Will";
            }
            else
            {
                DiscSettings.Instance.CUSTOM_FireOrWill_SET = "Inner Fire";

            }
            if (comboBox3.SelectedIndex == 0)
            {
                DiscSettings.Instance.CUSTOM_DPS_SET = "Heal First";
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                DiscSettings.Instance.CUSTOM_DPS_SET = "DPS First";
            }
            else
            {
                DiscSettings.Instance.CUSTOM_DPS_SET = "Heal Only";

            }
        }

        private bool BoxChecked(System.Windows.Forms.CheckBox box)
        {
            if (!box.Checked)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
        private void CheckTheBox(System.Windows.Forms.CheckBox box, bool b)
        {
            if (b)
            {
                box.Checked = true;
            }
            else
            {
                box.Checked = false;
            }
        }

        private void fillRaidNames()
        {
            RaidRoster = new String[40];
            int i = 0;
            foreach (WoWPlayer p in Disc.DiscPriest.Me.RaidMembers)
            {
                RaidNameLabels[i].Text = p.Name;
                i++;
            }        
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                DiscSettings.Instance.PWFort_SET = true;
            }
            else
            {
                DiscSettings.Instance.PWFort_SET = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                DiscSettings.Instance.ShadProt_SET = true;
            }
            else
            {
                DiscSettings.Instance.ShadProt_SET = false;
            }
        }

        private void settingsForm_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("Click to Stop"))
            {
                button2.Text = "Click to Start";
                button2.ForeColor = System.Drawing.Color.Green;
                DiscSettings.Instance.Stop_SET = true;
                
            }
            else if (button2.Text.Equals("Click to Start"))
            {
                button2.Text = "Click to Stop";
                button2.ForeColor = System.Drawing.Color.Red;
                DiscSettings.Instance.Stop_SET = false;
            }
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void setSelectiveButtons(int phase)
        {
            if (phase == 1)
            {
                button3.ForeColor = System.Drawing.Color.Blue;
                button3.Font = new Font(button3.Font.FontFamily, 10,FontStyle.Bold);
                button4.ForeColor = System.Drawing.Color.Black;
                button4.Font = new Font(button4.Font.FontFamily, 8, FontStyle.Regular);
                button5.ForeColor = System.Drawing.Color.Black;
                button5.Font = new Font(button5.Font.FontFamily, 8, FontStyle.Regular);
            }
            else if (phase == 2)
            {
                button4.ForeColor = System.Drawing.Color.Blue;
                button4.Font = new Font(button4.Font.FontFamily, 10, FontStyle.Bold);
                button3.ForeColor = System.Drawing.Color.Black;
                button3.Font = new Font(button3.Font.FontFamily, 8, FontStyle.Regular);
                button5.ForeColor = System.Drawing.Color.Black;
                button5.Font = new Font(button5.Font.FontFamily, 8, FontStyle.Regular);
            }
            else
            {
                button5.ForeColor = System.Drawing.Color.Blue;
                button5.Font = new Font(button5.Font.FontFamily, 10, FontStyle.Bold);
                button4.ForeColor = System.Drawing.Color.Black;
                button4.Font = new Font(button4.Font.FontFamily, 8, FontStyle.Regular);
                button3.ForeColor = System.Drawing.Color.Black;
                button3.Font = new Font(button3.Font.FontFamily, 8, FontStyle.Regular);
            }
        }

        private void BlacklistPerson(int i)
        {
            DiscSettings.Instance.HealBlackList.Add(RaidNameLabels[i].Text);
            HealStatusLabels[i].ForeColor = System.Drawing.Color.Red;
            HealStatusLabels[i].Text = "Blacklisted";
        }

        private void resetBlacklistStatus(int i)
        {
            HealStatusLabels[i].ForeColor = System.Drawing.Color.Green;
            HealStatusLabels[i].Text = "Healing";
        }

        private void checkBox88_CheckedChanged(object sender, EventArgs e)
        {
            DiscSettings.Instance.HealBlackList.Clear();
            if (checkBox88.Checked)
            {
                fillRaidNames();
                setSelectiveButtons(1);                
                for (int i = 0; i < 40; i++)
                {
                    if (!Phase1Boxs[i].Checked)
                    {
                        BlacklistPerson(i);
                    }
                    else
                    {
                        resetBlacklistStatus(i);
                    }
                }
            }           

        }

        private void button6_Click(object sender, EventArgs e)
        {
            fillRaidNames();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            setSelectiveButtons(1);
            DiscSettings.Instance.HealBlackList.Clear();
            for (int i = 0; i < 40; i++)
            {
                if (!Phase1Boxs[i].Checked)
                {
                    BlacklistPerson(i);
                }
                else
                {
                    resetBlacklistStatus(i);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            setSelectiveButtons(2);
            DiscSettings.Instance.HealBlackList.Clear();            
            for (int i = 0; i < 40; i++)
            {
                if (!Phase2Boxs[i].Checked)
                {
                    BlacklistPerson(i);
                }
                else
                {
                    resetBlacklistStatus(i);
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            setSelectiveButtons(3);
            DiscSettings.Instance.HealBlackList.Clear();
            for (int i = 0; i < 40; i++)
            {
                if (!Phase3Boxs[i].Checked)
                {
                    BlacklistPerson(i);
                }
                else
                {
                    resetBlacklistStatus(i);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                FillDungeonSettings();
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                FillRaidSettings();
            }
            else if (comboBox2.SelectedIndex == 2)
            {
                FillBGSettings();
            }
            else if (comboBox2.SelectedIndex == 3)
            {
                FillArenaSettings();
            }
            else if (comboBox2.SelectedIndex == 4)
            {
                FillSoloSettings();
            }
            else
            {
                FillCustomSettings();
            }
        }
        

    }
}
