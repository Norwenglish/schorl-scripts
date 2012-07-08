using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Bobby53;
using System.Collections;

namespace Styx.Bot.CustomBots
{
    public partial class SelectTankForm : Form
    {
        public SelectTankForm()
        {
            InitializeComponent();


        }

        private void btnSetLeader_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose a Player before clicking Set Leader");
                listView.Focus();
            }
            else
            {
                // WoWPlayer chosenPlayer = (WoWPlayer)listView.SelectedItems[0].Tag;
                ulong guid = (ulong)listView.SelectedItems[0].Tag;
                try
                {
                    Tank.Guid = guid;
                    Tank.SetAsLeader();
                    LazyRaider.IamTheTank = (StyxWoW.Me.Guid == guid);
                    LazyRaider.Log("User selected tank named {0}", LazyRaider.Safe_UnitName(Tank.Current));
                    TreeRoot.StatusText = String.Format("[lr] tank is {0}", LazyRaider.Safe_UnitName(Tank.Current));
                }
                catch
                {
                    listView.SelectedItems[0].Remove();
                }

                this.Hide();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if ( chkRunWithoutTank.Checked )
            {
                LazyRaider.Log("selected -ME-, leader cleared");
                Tank.Clear();
            }

            this.Hide();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadListView();
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void listView_Click(object sender, EventArgs e)
        {

        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (btnSetLeader.Enabled)
            {
                btnSetLeader_Click(sender, e);
            }
        }


        private void LoadListView()
        {
            listView.Items.Clear();

            AddRow(LazyRaider.Me.Guid, "-ME-", LazyRaider.Me.Class.ToString(), LazyRaider.GetGroupRoleAssigned(LazyRaider.Me).ToString(), LazyRaider.Me.MaxHealth.ToString());

            if (ObjectManager.IsInGame && ObjectManager.Me != null)
            {
                ObjectManager.Update();

                LazyRaider.Dlog("-- Group Count: {0}", LazyRaider.GroupMemberInfos.Count);

                foreach (WoWPartyMember pm in LazyRaider.GroupMemberInfos)
                {
                    if ( pm == null || pm.Guid == LazyRaider.Me.Guid)
                        continue;

                    WoWPlayer p = pm.ToPlayer();
                    string sName = p == null ? "-out of range-" : p.Name;
                    string sRole = LazyRaider.GetGroupRoleAssigned(pm).ToString();
                    string sClass= p == null ? "-n/a-" : p.Class.ToString();
                    LazyRaider.Dlog("-- Group Member: {0}.{1:X3} hp={2} is {3}", sClass, pm.Guid, pm.HealthMax, sRole);

                    AddRow(pm.Guid, sName, sClass, sRole, pm.HealthMax.ToString());
                }
            }

            this.listView.ListViewItemSorter = new ListViewItemComparer(2);
            listView.Sort();
            btnSetLeader.Enabled = true;
        }

        private void AddRow( ulong @guid, string @name, string @class, string @role, string @health  )
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Text = @name;
            lvi.Tag = @guid;

            lvi.Selected = (@guid == Tank.Guid);

            ListViewItem.ListViewSubItem lvs = new ListViewItem.ListViewSubItem();
            lvs.Text = @class;
            lvi.SubItems.Add(lvs);

            lvs = new ListViewItem.ListViewSubItem();
            lvs.Text = @role;
            lvi.SubItems.Add(lvs);

            lvs = new ListViewItem.ListViewSubItem();
            lvs.Text = @health;
            lvi.SubItems.Add(lvs);

            listView.Items.Add(lvi);
        }

        private void SelectTankForm_Shown(object sender, EventArgs e)
        {

        }

        private static bool isVisible = false;
        private void SelectTankForm_VisibleChanged(object sender, EventArgs e)
        {
            isVisible = !isVisible;

            if (isVisible)
            {
                chkAutoSelectTank.Checked = LazyRaiderSettings.Instance.AutoTankSelect;
                chkAutoFollow.Checked = LazyRaiderSettings.Instance.FollowTank;
                chkAutoTarget.Checked = LazyRaiderSettings.Instance.AutoTarget;
                numFollowDistance.Value = LazyRaiderSettings.Instance.FollowDistance;

                LoadComboBoxWithEnum<LazyRaiderSettings.Keypress>(cboKeyPause);
                SetComboBoxEnum(cboKeyPause, LazyRaiderSettings.Instance.PauseKey );

                chkDisablePlugins.Checked = LazyRaiderSettings.Instance.DisablePlugins;
                chkLockMemory.Checked = LazyRaiderSettings.Instance.LockMemory;
                numFPS.Value = LazyRaiderSettings.Instance.FPS;

                LoadListView();

                chkRunWithoutTank.Checked = LazyRaiderSettings.Instance.NoTank;
                chkDisableTank_CheckedChanged(sender, e);
                listView.Focus();
            }
            else
            {
                LazyRaiderSettings.Instance.NoTank = chkRunWithoutTank.Checked;
                LazyRaiderSettings.Instance.FollowTank = chkAutoFollow.Checked;
                LazyRaiderSettings.Instance.FollowDistance = (int)numFollowDistance.Value;
                LazyRaiderSettings.Instance.AutoTankSelect = chkAutoSelectTank.Checked;
                LazyRaiderSettings.Instance.AutoTarget = chkAutoTarget.Checked;
                LazyRaiderSettings.Instance.PauseKey = (LazyRaiderSettings.Keypress) GetComboBoxEnum(cboKeyPause);
                LazyRaiderSettings.Instance.DisablePlugins = chkDisablePlugins.Checked;
                LazyRaiderSettings.Instance.LockMemory = chkLockMemory.Checked;
                LazyRaiderSettings.Instance.FPS = (int)numFPS.Value;
                LazyRaiderSettings.Instance.Save();

                LazyRaider.RefreshSettingsCache();
            }

            return;
        }

        private void SelectTankForm_Activated(object sender, EventArgs e)
        {
            return;
        }

        private void SelectTankForm_Deactivate(object sender, EventArgs e)
        {
        }

        private void SelectTankForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // hide instead of close when user clicks X
            this.Hide();
            e.Cancel = true;
        }

        private void chkDisableTank_CheckedChanged(object sender, EventArgs e)
        {
            chkAutoFollow.Enabled = !chkRunWithoutTank.Checked;
            lblFollowDistance.Enabled = !chkRunWithoutTank.Checked;
            numFollowDistance.Enabled = !chkRunWithoutTank.Checked;
            btnSetLeader.Enabled = !chkRunWithoutTank.Checked;
            listView.Enabled = !chkRunWithoutTank.Checked;
            btnRefresh.Enabled = !chkRunWithoutTank.Checked;
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.listView.ListViewItemSorter = new ListViewItemComparer(e.Column);
            listView.Sort();
        }

        class ListViewItemComparer : IComparer
        {
            public int Column { get; set; }

            public ListViewItemComparer()
            {
                Column = 2;
            }
            public ListViewItemComparer(int column)
            {
                Column = column;
            }

            public int Compare(object x, object y)
            {
                int returnVal = -1;

                if (Column == 2)
                    returnVal = CompareRoleRank(((ListViewItem)x).SubItems[Column].Text) - CompareRoleRank(((ListViewItem)y).SubItems[Column].Text);
                else if (Column == 3)
                    returnVal = - (int)(HealthRank(((ListViewItem)x).SubItems[Column].Text) - HealthRank(((ListViewItem)y).SubItems[Column].Text));
                else
                    returnVal = String.Compare(((ListViewItem)x).SubItems[Column].Text, ((ListViewItem)y).SubItems[Column].Text);

                return returnVal;
            }

            private int CompareRoleRank(string role)
            {
                if (String.IsNullOrEmpty(role))
                    return 5;

                role = role.ToUpper();
                switch (role[0])
                {
                    case 'D':
                        return 3;

                    case 'H':
                        return 2;

                    case 'T':
                        return 1;
                }

                return 4;
            }

            private long HealthRank(string health)
            {
                if (String.IsNullOrEmpty(health))
                    return 0;

                return Convert.ToInt64(health);
            }
        }

        private void btnMaxPerformance_Click(object sender, EventArgs e)
        {
            int fps = (int) Math.Round(LazyRaider.GetFramerate());
            if (fps < 15)
                fps = 15;
            else if (fps > 60)
                fps = 60;

            numFPS.Value = fps;
            chkDisablePlugins.Checked = true;

            // if (RoutineManager.Current != null && RoutineManager.Current.Name.Substring(0, 7).ToUpper() != "SHAMWOW")
            chkLockMemory.Checked = true;
        }

        public static void LoadComboBoxWithEnum<T>(ComboBox cbo)
        {
            cbo.Items.Clear();
            var array = (T[])(Enum.GetValues(typeof(T)).Cast<T>());
            var array2 = Enum.GetNames(typeof(T)).ToArray<string>();
            for (int i = 0; i < array.Length; i++)
            {
                string name = array2[i];
                int value = Convert.ToInt32(array[i]);
                cbo.Items.Add(new CboItem(value, name));
            }
            return;
        }

        private static void SetComboBoxEnum(ComboBox cbo, Enum e)
        {
            CboItem item;
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                item = (CboItem)cbo.Items[i];
                if (item.s == e.ToString() )
                {
                    cbo.SelectedIndex = i;
                    return;
                }
            }

            item = (CboItem)cbo.Items[0];
            Styx.Helpers.Logging.WriteDebug("Dialog Error: combobox {0} does not have enum({1}) in list, defaulting to enum({2})", cbo.Name, e, item.e);
            cbo.SelectedIndex = 0;
        }

        private static int GetComboBoxEnum(System.Windows.Forms.ComboBox cbo)
        {
            CboItem item = (CboItem)cbo.Items[cbo.SelectedIndex];
            return item.e;
        }

        public class CboItem
        {
            public int e;
            public string s;

            public override string ToString()
            {
                return s;
            }

            public CboItem(int pe, string ps)
            {
                e = pe;
                s = ps;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void cboKeyPause_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
