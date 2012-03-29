//////////////////////////////////////////////////
//                Config.cs                     //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace MutaRaidBT.UI
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
        }

        private void Config_Load(object sender, EventArgs e)
        {
            comboBoxRaidPoison1.DataSource      = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxRaidPoison2.DataSource      = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxHeroicPoison1.DataSource    = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxHeroicPoison2.DataSource    = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxDungeonPoison1.DataSource   = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxDungeonPoison2.DataSource   = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxBgPoison1.DataSource        = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxBgPoison2.DataSource        = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxLevelPoison1.DataSource     = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));
            comboBoxLevelPoison2.DataSource     = Enum.GetValues(typeof(Helpers.Enumeration.PoisonSpellId));

            checkBoxRaidPoison.Checked    = Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Raid];
            checkBoxHeroicPoison.Checked  = Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.HeroicDungeon];
            checkBoxDungeonPoison.Checked = Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Dungeon];
            checkBoxBgPoison.Checked      = Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Battleground];
            checkBoxLevelPoison.Checked   = Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.World];

            comboBoxRaidPoison1.SelectedItem    = Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Raid];
            comboBoxRaidPoison2.SelectedItem    = Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Raid];
            comboBoxHeroicPoison1.SelectedItem  = Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.HeroicDungeon];
            comboBoxHeroicPoison2.SelectedItem  = Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.HeroicDungeon];
            comboBoxDungeonPoison1.SelectedItem = Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Dungeon];
            comboBoxDungeonPoison2.SelectedItem = Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Dungeon];
            comboBoxBgPoison1.SelectedItem      = Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Battleground];
            comboBoxBgPoison2.SelectedItem      = Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Battleground];
            comboBoxLevelPoison1.SelectedItem   = Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.World];
            comboBoxLevelPoison2.SelectedItem   = Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.World];

            panelRaidPoison.Enabled = checkBoxRaidPoison.Checked;
            panelHeroicPoison.Enabled = checkBoxHeroicPoison.Checked;
            panelDungeonPoison.Enabled = checkBoxDungeonPoison.Checked;
            panelBgPoison.Enabled = checkBoxBgPoison.Checked;
            panelLevelPoison.Enabled = checkBoxBgPoison.Checked;
     
            if (!Settings.Mode.mOverrideContext)
            {
                radioButtonAuto.Checked = true;
            }
            else
            {
                switch (Settings.Mode.mLocationSettings)
                {
                    case Helpers.Enumeration.LocationContext.Raid:

                        radioButtonRaid.Checked = true;
                        break;

                    case Helpers.Enumeration.LocationContext.HeroicDungeon:

                        radioButtonHeroicDungeon.Checked = true;
                        break;

                    case Helpers.Enumeration.LocationContext.Dungeon:

                        radioButtonDungeon.Checked = true;
                        break;

                    case Helpers.Enumeration.LocationContext.Battleground:

                        radioButtonBattleground.Checked = true;
                        break;

                    case Helpers.Enumeration.LocationContext.World:

                        radioButtonLevel.Checked = true;
                        break;
                }
            }

            if (Settings.Mode.mUseMovement)
            {
                radioButtonMoveOn.Checked = true;
            }
            else
            {
                radioButtonMoveOff.Checked = true;
            }

            if (Settings.Mode.mUseAoe)
            {
                radioButtonAoeOn.Checked = true;
            }
            else
            {
                radioButtonAoeOff.Checked = true;
            }

            switch (Settings.Mode.mCooldownUse)
            {
                case Helpers.Enumeration.CooldownUse.Always:

                    radioCooldownAlways.Checked = true;
                    break;

                case Helpers.Enumeration.CooldownUse.ByFocus:

                    radioCooldownByFocus.Checked = true;
                    break;

                case Helpers.Enumeration.CooldownUse.OnlyOnBosses:

                    radioCooldownByBoss.Checked = true;
                    break;

                case Helpers.Enumeration.CooldownUse.Never:

                    radioCooldownNever.Checked = true;
                    break;
            }
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            if (checkBoxRaidPoison.Checked || checkBoxHeroicPoison.Checked)
            {
                var input = MessageBox.Show("WARNING: You have enabled poisons for Raid mode or Heroic Dungeon mode. Using Apoc's Raid Bot as your botbase will result in MutaRaidBT being " +
                                             "unable to apply poisons, regardless of context, due to a design limitation with Raid Bot. ",
                                             "Warning",
                                             MessageBoxButtons.OKCancel,
                                             MessageBoxIcon.Exclamation);

                if (input == DialogResult.Cancel) return;
            }

            Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Raid]          = checkBoxRaidPoison.Checked;
            Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.HeroicDungeon] = checkBoxHeroicPoison.Checked;
            Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Dungeon]       = checkBoxDungeonPoison.Checked;
            Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.Battleground]  = checkBoxBgPoison.Checked;
            Settings.Mode.mUsePoisons[(int) Helpers.Enumeration.LocationContext.World]         = checkBoxLevelPoison.Checked;

            Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Raid]          = (Helpers.Enumeration.PoisonSpellId) comboBoxRaidPoison1.SelectedItem;
            Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Raid]           = (Helpers.Enumeration.PoisonSpellId) comboBoxRaidPoison2.SelectedItem;
            Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.HeroicDungeon] = (Helpers.Enumeration.PoisonSpellId) comboBoxHeroicPoison1.SelectedItem;
            Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.HeroicDungeon]  = (Helpers.Enumeration.PoisonSpellId) comboBoxHeroicPoison2.SelectedItem;
            Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Dungeon]       = (Helpers.Enumeration.PoisonSpellId) comboBoxDungeonPoison1.SelectedItem;
            Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Dungeon]        = (Helpers.Enumeration.PoisonSpellId) comboBoxDungeonPoison2.SelectedItem;
            Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.Battleground]  = (Helpers.Enumeration.PoisonSpellId) comboBoxBgPoison1.SelectedItem;
            Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.Battleground]   = (Helpers.Enumeration.PoisonSpellId) comboBoxBgPoison2.SelectedItem;
            Settings.Mode.mPoisonsMain[(int) Helpers.Enumeration.LocationContext.World]         = (Helpers.Enumeration.PoisonSpellId) comboBoxLevelPoison1.SelectedItem;
            Settings.Mode.mPoisonsOff[(int) Helpers.Enumeration.LocationContext.World]          = (Helpers.Enumeration.PoisonSpellId) comboBoxLevelPoison2.SelectedItem;

            Settings.Mode.mOverrideContext = !radioButtonAuto.Checked;
            Settings.Mode.mUseMovement = radioButtonMoveOn.Checked;
            Settings.Mode.mUseAoe = radioButtonAoeOn.Checked;

            if (radioButtonRaid.Checked)
            {
                Settings.Mode.mLocationSettings = Helpers.Enumeration.LocationContext.Raid;
            }
            else if (radioButtonHeroicDungeon.Checked)
            {
                Settings.Mode.mLocationSettings = Helpers.Enumeration.LocationContext.HeroicDungeon;
            }
            else if (radioButtonDungeon.Checked)
            {
                Settings.Mode.mLocationSettings = Helpers.Enumeration.LocationContext.Dungeon;
            }
            else if (radioButtonBattleground.Checked)
            {
                Settings.Mode.mLocationSettings = Helpers.Enumeration.LocationContext.Battleground;
            }
            else if (radioButtonLevel.Checked)
            {
                Settings.Mode.mLocationSettings = Helpers.Enumeration.LocationContext.World;
            }

            if (radioCooldownAlways.Checked)
            {
                Settings.Mode.mCooldownUse = Helpers.Enumeration.CooldownUse.Always;
            }
            else if (radioCooldownByFocus.Checked)
            {
                Settings.Mode.mCooldownUse = Helpers.Enumeration.CooldownUse.ByFocus;
            }
            else if (radioCooldownByBoss.Checked)
            {
                Settings.Mode.mCooldownUse = Helpers.Enumeration.CooldownUse.OnlyOnBosses;  
            }
            else
            {
                Settings.Mode.mCooldownUse = Helpers.Enumeration.CooldownUse.Never;
            }

            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonLaunchCombatControl_Click(object sender, EventArgs e)
        {
            var combatControl = new CombatControl();
            combatControl.Show();
        }

        private void checkBoxRaidPoison_CheckedChanged(object sender, EventArgs e)
        {
            panelRaidPoison.Enabled = checkBoxRaidPoison.Checked;
        }

        private void checkBoxHeroicPoison_CheckedChanged(object sender, EventArgs e)
        {
            panelHeroicPoison.Enabled = checkBoxHeroicPoison.Checked;
        }

        private void checkBoxDungeonPoison_CheckedChanged(object sender, EventArgs e)
        {
            panelDungeonPoison.Enabled = checkBoxDungeonPoison.Checked;
        }

        private void checkBoxBgPoison_CheckedChanged(object sender, EventArgs e)
        {
            panelBgPoison.Enabled = checkBoxBgPoison.Checked;
        }

        private void checkBoxLevelPoison_CheckedChanged(object sender, EventArgs e)
        {
            panelLevelPoison.Enabled = checkBoxBgPoison.Checked;
        }
    }
}
