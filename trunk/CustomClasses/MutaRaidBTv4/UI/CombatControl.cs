//////////////////////////////////////////////////
//              CombatControl.cs                //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace MutaRaidBT.UI
{
    public partial class CombatControl : Form
    {
        public CombatControl()
        {
            InitializeComponent();
        }

        private void buttonToggleCd_Click(object sender, EventArgs e)
        {
            Settings.Mode.mUseCooldowns = !Settings.Mode.mUseCooldowns;
            labelCdStatus.Text = "CDs: " + Settings.Mode.mUseCooldowns;
        }

        private void buttonToggleCombat_Click(object sender, EventArgs e)
        {
            Settings.Mode.mUseCombat = !Settings.Mode.mUseCombat;
            labelCombatStatus.Text = "Combat: " + Settings.Mode.mUseCombat;
        }

        private void CombatControl_Load(object sender, EventArgs e)
        {
            TopMost = true;

            labelCdStatus.Text = "CDs: " + Settings.Mode.mUseCooldowns;
            labelCombatStatus.Text = "Combat: " + Settings.Mode.mUseCombat;
            labelBehindTarStatus.Text = "Behind: " + Settings.Mode.mForceBehind;
        }

        private void buttonToggleBehindTar_Click(object sender, EventArgs e)
        {
            Settings.Mode.mForceBehind = !Settings.Mode.mForceBehind;
            labelBehindTarStatus.Text = "Behind: " + Settings.Mode.mForceBehind;
        }
    }
}
