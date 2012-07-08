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

            DiscSettings.Instance.UrgentDispelList = new System.ComponentModel.BindingList<Dispels>();
            ImportDispels();
            dataGridView1.DataSource = DiscSettings.Instance.UrgentDispelList;
            dataGridView1.Columns[0].HeaderText = "Urgent Dispels";
            dataGridView1.RowHeadersVisible = false;
            
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);

            if (DiscSettings.Instance.Stop_SET)
            {
                button2.Text = "Click to Start";
                button2.ForeColor = System.Drawing.Color.Green;
                DiscSettings.Instance.Stop_SET = true;

            }
            else if (!DiscSettings.Instance.Stop_SET)
            {
                button2.Text = "Click to Stop";
                button2.ForeColor = System.Drawing.Color.Red;
            }
            SetSHGridViews();
            propertyGrid1.SelectedObject = DiscSettings.Instance;
          
        }

        private void loadSettings()
        {
            DiscSettings.Instance.Load();
            DiscSettings.Instance.Stop_SET = false;
        }

        private void ImportDispels()
        {
            try
            {
                StreamReader s = new StreamReader(Path.Combine(Logging.ApplicationPath, string.Format(@"CustomClasses\Config\DiscCC_dispels.ini")));
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
            StreamWriter o = new StreamWriter(Path.Combine(Logging.ApplicationPath, string.Format(@"CustomClasses\Config\DiscCC_dispels.ini")));
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

        private void FillSHRaidMemberNames()
        {
            if (!SHContainsName(ObjectManager.Me.Name))
            {
                DiscSettings.Instance.SHRaidMembers.Add(new SelectiveHealName(ObjectManager.Me.Name));
            }
            foreach (WoWPlayer p in ObjectManager.Me.PartyMembers)
            {
                if (!SHContainsName(p.Name))
                {
                    DiscSettings.Instance.SHRaidMembers.Add(new SelectiveHealName(p.Name));
                }
            }
            foreach (WoWPlayer p in ObjectManager.Me.RaidMembers)
            {
                if (!SHContainsName(p.Name))
                {
                    DiscSettings.Instance.SHRaidMembers.Add(new SelectiveHealName(p.Name));
                }
            }
        }

        public bool SHBlackListContainsName(String str)
        {
            foreach (SelectiveHealName n in DiscSettings.Instance.SHBlackListNames)
            {
                if (n.ListItem.ToString().Equals(str))
                {
                    return true;
                }
            }
            return false;
        }

        private bool SHContainsName(String str)
        {
            foreach (SelectiveHealName n in DiscSettings.Instance.SHRaidMembers)
            {
                if (n.ListItem.ToString().Equals(str))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetSHGridViews()
        {
            FillSHRaidMemberNames();
            SHRaidMembersGrid.DataSource = DiscSettings.Instance.SHRaidMembers;
            SHRaidMembersGrid.Columns[0].HeaderText = "Raid Members";
            SHRaidMembersGrid.RowHeadersVisible = false;
            SHRaidMembersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SHBlackListGrid.DataSource = DiscSettings.Instance.SHBlackListNames;
            SHBlackListGrid.Columns[0].HeaderText = "Blacklisted Players";
            SHBlackListGrid.RowHeadersVisible = false;
            SHBlackListGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void SHRaidMembersGrid_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (!SHBlackListContainsName(SHRaidMembersGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()))
            {
                DiscSettings.Instance.SHBlackListNames.Add(new SelectiveHealName(SHRaidMembersGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()));
            }
            SHBlackListGrid.DataSource = DiscSettings.Instance.SHBlackListNames;
        }

        private void SHBlackListGrid_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            int removeBLindex = -1;
            foreach (SelectiveHealName n in DiscSettings.Instance.SHBlackListNames)
            {
                if (n.ListItem.ToString().Equals(SHBlackListGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()))
                {
                    removeBLindex = DiscSettings.Instance.SHBlackListNames.IndexOf(n);
                }
            }
            if (removeBLindex != -1)
            {
                DiscSettings.Instance.SHBlackListNames.RemoveAt(removeBLindex);
            }
            SHBlackListGrid.DataSource = DiscSettings.Instance.SHBlackListNames;
        }

        private void button1_Click(object sender, EventArgs e)
        {            
            PrintDispels();
            DiscSettings.Instance.Save();
            Logging.Write("Settings Saved");
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
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
        }

        private void BlacklistPerson(int i)
        {
        }

        private void resetBlacklistStatus(int i)
        {
        }

        private void checkBox88_CheckedChanged(object sender, EventArgs e)
        { 
        }

        private void button6_Click(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
        }

        private void button5_Click(object sender, EventArgs e)
        {          
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            DiscSettings.Instance.SHRaidMembers.Clear();
            DiscSettings.Instance.SHBlackListNames.Clear();
            SetSHGridViews();
        }

        private void SHRaidMembersGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!SHBlackListContainsName(SHRaidMembersGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()))
            {
                DiscSettings.Instance.SHBlackListNames.Add(new SelectiveHealName(SHRaidMembersGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()));
            }
            SHBlackListGrid.DataSource = DiscSettings.Instance.SHBlackListNames;
        }

        private void SHBlackListGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int removeBLindex = -1;
            foreach (SelectiveHealName n in DiscSettings.Instance.SHBlackListNames)
            {
                if (n.ListItem.ToString().Equals(SHBlackListGrid.Rows[e.RowIndex].Cells[0].FormattedValue.ToString()))
                {
                    removeBLindex = DiscSettings.Instance.SHBlackListNames.IndexOf(n);
                }
            }
            if (removeBLindex != -1)
            {
                DiscSettings.Instance.SHBlackListNames.RemoveAt(removeBLindex);
            }
            SHBlackListGrid.DataSource = DiscSettings.Instance.SHBlackListNames;
        }
        

    }
}
