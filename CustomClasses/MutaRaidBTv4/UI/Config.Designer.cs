//////////////////////////////////////////////////
//             Config.Designer.cs               //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

namespace MutaRaidBT.UI
{
    partial class Config
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonApply = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonLaunchCombatControl = new System.Windows.Forms.Button();
            this.labelUseCooldowns = new System.Windows.Forms.Label();
            this.panelCooldowns = new System.Windows.Forms.Panel();
            this.radioCooldownNever = new System.Windows.Forms.RadioButton();
            this.radioCooldownByBoss = new System.Windows.Forms.RadioButton();
            this.radioCooldownByFocus = new System.Windows.Forms.RadioButton();
            this.radioCooldownAlways = new System.Windows.Forms.RadioButton();
            this.labelModePrompt = new System.Windows.Forms.Label();
            this.radioButtonDungeon = new System.Windows.Forms.RadioButton();
            this.radioButtonHeroicDungeon = new System.Windows.Forms.RadioButton();
            this.radioButtonRaid = new System.Windows.Forms.RadioButton();
            this.radioButtonAuto = new System.Windows.Forms.RadioButton();
            this.radioButtonBattleground = new System.Windows.Forms.RadioButton();
            this.radioButtonHolderPanel = new System.Windows.Forms.Panel();
            this.radioButtonLevel = new System.Windows.Forms.RadioButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.labelUseMovement = new System.Windows.Forms.Label();
            this.panelMovement = new System.Windows.Forms.Panel();
            this.radioButtonMoveOff = new System.Windows.Forms.RadioButton();
            this.radioButtonMoveOn = new System.Windows.Forms.RadioButton();
            this.labelApplyPoisons = new System.Windows.Forms.Label();
            this.panelRaidPoison = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxRaidPoison2 = new System.Windows.Forms.ComboBox();
            this.labelRaidPoisonMain = new System.Windows.Forms.Label();
            this.comboBoxRaidPoison1 = new System.Windows.Forms.ComboBox();
            this.checkBoxRaidPoison = new System.Windows.Forms.CheckBox();
            this.panelHeroicPoison = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxHeroicPoison2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxHeroicPoison1 = new System.Windows.Forms.ComboBox();
            this.checkBoxHeroicPoison = new System.Windows.Forms.CheckBox();
            this.panelBgPoison = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxBgPoison2 = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxBgPoison1 = new System.Windows.Forms.ComboBox();
            this.checkBoxBgPoison = new System.Windows.Forms.CheckBox();
            this.panelDungeonPoison = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxDungeonPoison2 = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBoxDungeonPoison1 = new System.Windows.Forms.ComboBox();
            this.checkBoxDungeonPoison = new System.Windows.Forms.CheckBox();
            this.panelLevelPoison = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.comboBoxLevelPoison2 = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBoxLevelPoison1 = new System.Windows.Forms.ComboBox();
            this.checkBoxLevelPoison = new System.Windows.Forms.CheckBox();
            this.panelUseAoe = new System.Windows.Forms.Panel();
            this.radioButtonAoeOff = new System.Windows.Forms.RadioButton();
            this.radioButtonAoeOn = new System.Windows.Forms.RadioButton();
            this.label10 = new System.Windows.Forms.Label();
            this.panelCooldowns.SuspendLayout();
            this.radioButtonHolderPanel.SuspendLayout();
            this.panelMovement.SuspendLayout();
            this.panelRaidPoison.SuspendLayout();
            this.panelHeroicPoison.SuspendLayout();
            this.panelBgPoison.SuspendLayout();
            this.panelDungeonPoison.SuspendLayout();
            this.panelLevelPoison.SuspendLayout();
            this.panelUseAoe.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonApply
            // 
            this.buttonApply.Location = new System.Drawing.Point(12, 361);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(95, 23);
            this.buttonApply.TabIndex = 2;
            this.buttonApply.Text = "Apply and Close";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(309, 361);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonLaunchCombatControl
            // 
            this.buttonLaunchCombatControl.Location = new System.Drawing.Point(143, 361);
            this.buttonLaunchCombatControl.Name = "buttonLaunchCombatControl";
            this.buttonLaunchCombatControl.Size = new System.Drawing.Size(136, 23);
            this.buttonLaunchCombatControl.TabIndex = 4;
            this.buttonLaunchCombatControl.Text = "Launch Combat Control";
            this.buttonLaunchCombatControl.UseVisualStyleBackColor = true;
            this.buttonLaunchCombatControl.Click += new System.EventHandler(this.buttonLaunchCombatControl_Click);
            // 
            // labelUseCooldowns
            // 
            this.labelUseCooldowns.AutoSize = true;
            this.labelUseCooldowns.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUseCooldowns.Location = new System.Drawing.Point(160, 9);
            this.labelUseCooldowns.Name = "labelUseCooldowns";
            this.labelUseCooldowns.Size = new System.Drawing.Size(105, 24);
            this.labelUseCooldowns.TabIndex = 5;
            this.labelUseCooldowns.Text = "Cooldowns";
            // 
            // panelCooldowns
            // 
            this.panelCooldowns.Controls.Add(this.radioCooldownNever);
            this.panelCooldowns.Controls.Add(this.radioCooldownByBoss);
            this.panelCooldowns.Controls.Add(this.radioCooldownByFocus);
            this.panelCooldowns.Controls.Add(this.radioCooldownAlways);
            this.panelCooldowns.Location = new System.Drawing.Point(164, 36);
            this.panelCooldowns.Name = "panelCooldowns";
            this.panelCooldowns.Size = new System.Drawing.Size(103, 96);
            this.panelCooldowns.TabIndex = 6;
            // 
            // radioCooldownNever
            // 
            this.radioCooldownNever.AutoSize = true;
            this.radioCooldownNever.Location = new System.Drawing.Point(4, 72);
            this.radioCooldownNever.Name = "radioCooldownNever";
            this.radioCooldownNever.Size = new System.Drawing.Size(54, 17);
            this.radioCooldownNever.TabIndex = 3;
            this.radioCooldownNever.TabStop = true;
            this.radioCooldownNever.Text = "Never";
            this.radioCooldownNever.UseVisualStyleBackColor = true;
            // 
            // radioCooldownByBoss
            // 
            this.radioCooldownByBoss.AutoSize = true;
            this.radioCooldownByBoss.Location = new System.Drawing.Point(4, 49);
            this.radioCooldownByBoss.Name = "radioCooldownByBoss";
            this.radioCooldownByBoss.Size = new System.Drawing.Size(97, 17);
            this.radioCooldownByBoss.TabIndex = 2;
            this.radioCooldownByBoss.TabStop = true;
            this.radioCooldownByBoss.Text = "Only on bosses";
            this.radioCooldownByBoss.UseVisualStyleBackColor = true;
            // 
            // radioCooldownByFocus
            // 
            this.radioCooldownByFocus.AutoSize = true;
            this.radioCooldownByFocus.Location = new System.Drawing.Point(4, 26);
            this.radioCooldownByFocus.Name = "radioCooldownByFocus";
            this.radioCooldownByFocus.Size = new System.Drawing.Size(66, 17);
            this.radioCooldownByFocus.TabIndex = 1;
            this.radioCooldownByFocus.TabStop = true;
            this.radioCooldownByFocus.Text = "By focus";
            this.radioCooldownByFocus.UseVisualStyleBackColor = true;
            // 
            // radioCooldownAlways
            // 
            this.radioCooldownAlways.AutoSize = true;
            this.radioCooldownAlways.Location = new System.Drawing.Point(4, 4);
            this.radioCooldownAlways.Name = "radioCooldownAlways";
            this.radioCooldownAlways.Size = new System.Drawing.Size(58, 17);
            this.radioCooldownAlways.TabIndex = 0;
            this.radioCooldownAlways.TabStop = true;
            this.radioCooldownAlways.Text = "Always";
            this.radioCooldownAlways.UseVisualStyleBackColor = true;
            // 
            // labelModePrompt
            // 
            this.labelModePrompt.AutoSize = true;
            this.labelModePrompt.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelModePrompt.Location = new System.Drawing.Point(12, 9);
            this.labelModePrompt.Name = "labelModePrompt";
            this.labelModePrompt.Size = new System.Drawing.Size(59, 24);
            this.labelModePrompt.TabIndex = 0;
            this.labelModePrompt.Text = "Mode";
            // 
            // radioButtonDungeon
            // 
            this.radioButtonDungeon.AutoSize = true;
            this.radioButtonDungeon.Location = new System.Drawing.Point(3, 72);
            this.radioButtonDungeon.Name = "radioButtonDungeon";
            this.radioButtonDungeon.Size = new System.Drawing.Size(98, 17);
            this.radioButtonDungeon.TabIndex = 4;
            this.radioButtonDungeon.TabStop = true;
            this.radioButtonDungeon.Text = "Dungeon mode";
            this.radioButtonDungeon.UseVisualStyleBackColor = true;
            // 
            // radioButtonHeroicDungeon
            // 
            this.radioButtonHeroicDungeon.AutoSize = true;
            this.radioButtonHeroicDungeon.Location = new System.Drawing.Point(3, 49);
            this.radioButtonHeroicDungeon.Name = "radioButtonHeroicDungeon";
            this.radioButtonHeroicDungeon.Size = new System.Drawing.Size(132, 17);
            this.radioButtonHeroicDungeon.TabIndex = 1;
            this.radioButtonHeroicDungeon.TabStop = true;
            this.radioButtonHeroicDungeon.Text = "Heroic Dungeon mode";
            this.radioButtonHeroicDungeon.UseVisualStyleBackColor = true;
            // 
            // radioButtonRaid
            // 
            this.radioButtonRaid.AutoSize = true;
            this.radioButtonRaid.Location = new System.Drawing.Point(3, 26);
            this.radioButtonRaid.Name = "radioButtonRaid";
            this.radioButtonRaid.Size = new System.Drawing.Size(76, 17);
            this.radioButtonRaid.TabIndex = 0;
            this.radioButtonRaid.TabStop = true;
            this.radioButtonRaid.Text = "Raid mode";
            this.radioButtonRaid.UseVisualStyleBackColor = true;
            // 
            // radioButtonAuto
            // 
            this.radioButtonAuto.AutoSize = true;
            this.radioButtonAuto.Location = new System.Drawing.Point(3, 4);
            this.radioButtonAuto.Name = "radioButtonAuto";
            this.radioButtonAuto.Size = new System.Drawing.Size(72, 17);
            this.radioButtonAuto.TabIndex = 2;
            this.radioButtonAuto.TabStop = true;
            this.radioButtonAuto.Text = "Automatic";
            this.radioButtonAuto.UseVisualStyleBackColor = true;
            // 
            // radioButtonBattleground
            // 
            this.radioButtonBattleground.AutoSize = true;
            this.radioButtonBattleground.Location = new System.Drawing.Point(3, 95);
            this.radioButtonBattleground.Name = "radioButtonBattleground";
            this.radioButtonBattleground.Size = new System.Drawing.Size(114, 17);
            this.radioButtonBattleground.TabIndex = 3;
            this.radioButtonBattleground.TabStop = true;
            this.radioButtonBattleground.Text = "Battleground mode";
            this.radioButtonBattleground.UseVisualStyleBackColor = true;
            // 
            // radioButtonHolderPanel
            // 
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonLevel);
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonBattleground);
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonAuto);
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonRaid);
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonHeroicDungeon);
            this.radioButtonHolderPanel.Controls.Add(this.radioButtonDungeon);
            this.radioButtonHolderPanel.Location = new System.Drawing.Point(16, 36);
            this.radioButtonHolderPanel.Name = "radioButtonHolderPanel";
            this.radioButtonHolderPanel.Size = new System.Drawing.Size(136, 138);
            this.radioButtonHolderPanel.TabIndex = 1;
            // 
            // radioButtonLevel
            // 
            this.radioButtonLevel.AutoSize = true;
            this.radioButtonLevel.Location = new System.Drawing.Point(3, 118);
            this.radioButtonLevel.Name = "radioButtonLevel";
            this.radioButtonLevel.Size = new System.Drawing.Size(80, 17);
            this.radioButtonLevel.TabIndex = 5;
            this.radioButtonLevel.TabStop = true;
            this.radioButtonLevel.Text = "Level mode";
            this.radioButtonLevel.UseVisualStyleBackColor = true;
            // 
            // labelUseMovement
            // 
            this.labelUseMovement.AutoSize = true;
            this.labelUseMovement.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUseMovement.Location = new System.Drawing.Point(287, 9);
            this.labelUseMovement.Name = "labelUseMovement";
            this.labelUseMovement.Size = new System.Drawing.Size(99, 24);
            this.labelUseMovement.TabIndex = 7;
            this.labelUseMovement.Text = "Movement";
            // 
            // panelMovement
            // 
            this.panelMovement.Controls.Add(this.radioButtonMoveOff);
            this.panelMovement.Controls.Add(this.radioButtonMoveOn);
            this.panelMovement.Location = new System.Drawing.Point(291, 36);
            this.panelMovement.Name = "panelMovement";
            this.panelMovement.Size = new System.Drawing.Size(51, 55);
            this.panelMovement.TabIndex = 8;
            // 
            // radioButtonMoveOff
            // 
            this.radioButtonMoveOff.AutoSize = true;
            this.radioButtonMoveOff.Location = new System.Drawing.Point(3, 26);
            this.radioButtonMoveOff.Name = "radioButtonMoveOff";
            this.radioButtonMoveOff.Size = new System.Drawing.Size(39, 17);
            this.radioButtonMoveOff.TabIndex = 1;
            this.radioButtonMoveOff.TabStop = true;
            this.radioButtonMoveOff.Text = "Off";
            this.radioButtonMoveOff.UseVisualStyleBackColor = true;
            // 
            // radioButtonMoveOn
            // 
            this.radioButtonMoveOn.AutoSize = true;
            this.radioButtonMoveOn.Location = new System.Drawing.Point(3, 3);
            this.radioButtonMoveOn.Name = "radioButtonMoveOn";
            this.radioButtonMoveOn.Size = new System.Drawing.Size(39, 17);
            this.radioButtonMoveOn.TabIndex = 0;
            this.radioButtonMoveOn.TabStop = true;
            this.radioButtonMoveOn.Text = "On";
            this.radioButtonMoveOn.UseVisualStyleBackColor = true;
            // 
            // labelApplyPoisons
            // 
            this.labelApplyPoisons.AutoSize = true;
            this.labelApplyPoisons.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelApplyPoisons.Location = new System.Drawing.Point(12, 188);
            this.labelApplyPoisons.Name = "labelApplyPoisons";
            this.labelApplyPoisons.Size = new System.Drawing.Size(77, 24);
            this.labelApplyPoisons.TabIndex = 9;
            this.labelApplyPoisons.Text = "Poisons";
            // 
            // panelRaidPoison
            // 
            this.panelRaidPoison.Controls.Add(this.label1);
            this.panelRaidPoison.Controls.Add(this.comboBoxRaidPoison2);
            this.panelRaidPoison.Controls.Add(this.labelRaidPoisonMain);
            this.panelRaidPoison.Controls.Add(this.comboBoxRaidPoison1);
            this.panelRaidPoison.Location = new System.Drawing.Point(108, 215);
            this.panelRaidPoison.Name = "panelRaidPoison";
            this.panelRaidPoison.Size = new System.Drawing.Size(278, 22);
            this.panelRaidPoison.TabIndex = 10;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(148, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Offhand";
            // 
            // comboBoxRaidPoison2
            // 
            this.comboBoxRaidPoison2.FormattingEnabled = true;
            this.comboBoxRaidPoison2.Location = new System.Drawing.Point(193, 0);
            this.comboBoxRaidPoison2.Name = "comboBoxRaidPoison2";
            this.comboBoxRaidPoison2.Size = new System.Drawing.Size(76, 21);
            this.comboBoxRaidPoison2.TabIndex = 3;
            // 
            // labelRaidPoisonMain
            // 
            this.labelRaidPoisonMain.AutoSize = true;
            this.labelRaidPoisonMain.Location = new System.Drawing.Point(10, 5);
            this.labelRaidPoisonMain.Name = "labelRaidPoisonMain";
            this.labelRaidPoisonMain.Size = new System.Drawing.Size(54, 13);
            this.labelRaidPoisonMain.TabIndex = 2;
            this.labelRaidPoisonMain.Text = "Mainhand";
            // 
            // comboBoxRaidPoison1
            // 
            this.comboBoxRaidPoison1.AllowDrop = true;
            this.comboBoxRaidPoison1.FormattingEnabled = true;
            this.comboBoxRaidPoison1.Location = new System.Drawing.Point(70, 0);
            this.comboBoxRaidPoison1.Name = "comboBoxRaidPoison1";
            this.comboBoxRaidPoison1.Size = new System.Drawing.Size(76, 21);
            this.comboBoxRaidPoison1.TabIndex = 1;
            // 
            // checkBoxRaidPoison
            // 
            this.checkBoxRaidPoison.AutoSize = true;
            this.checkBoxRaidPoison.Location = new System.Drawing.Point(19, 219);
            this.checkBoxRaidPoison.Name = "checkBoxRaidPoison";
            this.checkBoxRaidPoison.Size = new System.Drawing.Size(48, 17);
            this.checkBoxRaidPoison.TabIndex = 0;
            this.checkBoxRaidPoison.Text = "Raid";
            this.checkBoxRaidPoison.UseVisualStyleBackColor = true;
            this.checkBoxRaidPoison.CheckedChanged += new System.EventHandler(this.checkBoxRaidPoison_CheckedChanged);
            // 
            // panelHeroicPoison
            // 
            this.panelHeroicPoison.Controls.Add(this.label2);
            this.panelHeroicPoison.Controls.Add(this.comboBoxHeroicPoison2);
            this.panelHeroicPoison.Controls.Add(this.label3);
            this.panelHeroicPoison.Controls.Add(this.comboBoxHeroicPoison1);
            this.panelHeroicPoison.Location = new System.Drawing.Point(108, 242);
            this.panelHeroicPoison.Name = "panelHeroicPoison";
            this.panelHeroicPoison.Size = new System.Drawing.Size(278, 22);
            this.panelHeroicPoison.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(148, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Offhand";
            // 
            // comboBoxHeroicPoison2
            // 
            this.comboBoxHeroicPoison2.FormattingEnabled = true;
            this.comboBoxHeroicPoison2.Location = new System.Drawing.Point(193, 0);
            this.comboBoxHeroicPoison2.Name = "comboBoxHeroicPoison2";
            this.comboBoxHeroicPoison2.Size = new System.Drawing.Size(76, 21);
            this.comboBoxHeroicPoison2.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 5);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Mainhand";
            // 
            // comboBoxHeroicPoison1
            // 
            this.comboBoxHeroicPoison1.FormattingEnabled = true;
            this.comboBoxHeroicPoison1.Location = new System.Drawing.Point(70, 0);
            this.comboBoxHeroicPoison1.Name = "comboBoxHeroicPoison1";
            this.comboBoxHeroicPoison1.Size = new System.Drawing.Size(76, 21);
            this.comboBoxHeroicPoison1.TabIndex = 1;
            // 
            // checkBoxHeroicPoison
            // 
            this.checkBoxHeroicPoison.AutoSize = true;
            this.checkBoxHeroicPoison.Location = new System.Drawing.Point(19, 246);
            this.checkBoxHeroicPoison.Name = "checkBoxHeroicPoison";
            this.checkBoxHeroicPoison.Size = new System.Drawing.Size(62, 17);
            this.checkBoxHeroicPoison.TabIndex = 0;
            this.checkBoxHeroicPoison.Text = "Heroics";
            this.checkBoxHeroicPoison.UseVisualStyleBackColor = true;
            this.checkBoxHeroicPoison.CheckedChanged += new System.EventHandler(this.checkBoxHeroicPoison_CheckedChanged);
            // 
            // panelBgPoison
            // 
            this.panelBgPoison.Controls.Add(this.label4);
            this.panelBgPoison.Controls.Add(this.comboBoxBgPoison2);
            this.panelBgPoison.Controls.Add(this.label5);
            this.panelBgPoison.Controls.Add(this.comboBoxBgPoison1);
            this.panelBgPoison.Location = new System.Drawing.Point(108, 296);
            this.panelBgPoison.Name = "panelBgPoison";
            this.panelBgPoison.Size = new System.Drawing.Size(278, 22);
            this.panelBgPoison.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(148, 5);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Offhand";
            // 
            // comboBoxBgPoison2
            // 
            this.comboBoxBgPoison2.FormattingEnabled = true;
            this.comboBoxBgPoison2.Location = new System.Drawing.Point(193, 0);
            this.comboBoxBgPoison2.Name = "comboBoxBgPoison2";
            this.comboBoxBgPoison2.Size = new System.Drawing.Size(76, 21);
            this.comboBoxBgPoison2.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 5);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Mainhand";
            // 
            // comboBoxBgPoison1
            // 
            this.comboBoxBgPoison1.FormattingEnabled = true;
            this.comboBoxBgPoison1.Location = new System.Drawing.Point(70, 0);
            this.comboBoxBgPoison1.Name = "comboBoxBgPoison1";
            this.comboBoxBgPoison1.Size = new System.Drawing.Size(76, 21);
            this.comboBoxBgPoison1.TabIndex = 1;
            // 
            // checkBoxBgPoison
            // 
            this.checkBoxBgPoison.AutoSize = true;
            this.checkBoxBgPoison.Location = new System.Drawing.Point(19, 300);
            this.checkBoxBgPoison.Name = "checkBoxBgPoison";
            this.checkBoxBgPoison.Size = new System.Drawing.Size(86, 17);
            this.checkBoxBgPoison.TabIndex = 0;
            this.checkBoxBgPoison.Text = "Battleground";
            this.checkBoxBgPoison.UseVisualStyleBackColor = true;
            this.checkBoxBgPoison.CheckedChanged += new System.EventHandler(this.checkBoxBgPoison_CheckedChanged);
            // 
            // panelDungeonPoison
            // 
            this.panelDungeonPoison.Controls.Add(this.label6);
            this.panelDungeonPoison.Controls.Add(this.comboBoxDungeonPoison2);
            this.panelDungeonPoison.Controls.Add(this.label7);
            this.panelDungeonPoison.Controls.Add(this.comboBoxDungeonPoison1);
            this.panelDungeonPoison.Location = new System.Drawing.Point(108, 269);
            this.panelDungeonPoison.Name = "panelDungeonPoison";
            this.panelDungeonPoison.Size = new System.Drawing.Size(278, 22);
            this.panelDungeonPoison.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(148, 5);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Offhand";
            // 
            // comboBoxDungeonPoison2
            // 
            this.comboBoxDungeonPoison2.FormattingEnabled = true;
            this.comboBoxDungeonPoison2.Location = new System.Drawing.Point(193, 0);
            this.comboBoxDungeonPoison2.Name = "comboBoxDungeonPoison2";
            this.comboBoxDungeonPoison2.Size = new System.Drawing.Size(76, 21);
            this.comboBoxDungeonPoison2.TabIndex = 3;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 5);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 13);
            this.label7.TabIndex = 2;
            this.label7.Text = "Mainhand";
            // 
            // comboBoxDungeonPoison1
            // 
            this.comboBoxDungeonPoison1.FormattingEnabled = true;
            this.comboBoxDungeonPoison1.Location = new System.Drawing.Point(70, 0);
            this.comboBoxDungeonPoison1.Name = "comboBoxDungeonPoison1";
            this.comboBoxDungeonPoison1.Size = new System.Drawing.Size(76, 21);
            this.comboBoxDungeonPoison1.TabIndex = 1;
            // 
            // checkBoxDungeonPoison
            // 
            this.checkBoxDungeonPoison.AutoSize = true;
            this.checkBoxDungeonPoison.Location = new System.Drawing.Point(19, 273);
            this.checkBoxDungeonPoison.Name = "checkBoxDungeonPoison";
            this.checkBoxDungeonPoison.Size = new System.Drawing.Size(70, 17);
            this.checkBoxDungeonPoison.TabIndex = 0;
            this.checkBoxDungeonPoison.Text = "Dungeon";
            this.checkBoxDungeonPoison.UseVisualStyleBackColor = true;
            this.checkBoxDungeonPoison.CheckedChanged += new System.EventHandler(this.checkBoxDungeonPoison_CheckedChanged);
            // 
            // panelLevelPoison
            // 
            this.panelLevelPoison.Controls.Add(this.label8);
            this.panelLevelPoison.Controls.Add(this.comboBoxLevelPoison2);
            this.panelLevelPoison.Controls.Add(this.label9);
            this.panelLevelPoison.Controls.Add(this.comboBoxLevelPoison1);
            this.panelLevelPoison.Location = new System.Drawing.Point(108, 323);
            this.panelLevelPoison.Name = "panelLevelPoison";
            this.panelLevelPoison.Size = new System.Drawing.Size(278, 22);
            this.panelLevelPoison.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(148, 5);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(45, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Offhand";
            // 
            // comboBoxLevelPoison2
            // 
            this.comboBoxLevelPoison2.FormattingEnabled = true;
            this.comboBoxLevelPoison2.Location = new System.Drawing.Point(193, 0);
            this.comboBoxLevelPoison2.Name = "comboBoxLevelPoison2";
            this.comboBoxLevelPoison2.Size = new System.Drawing.Size(76, 21);
            this.comboBoxLevelPoison2.TabIndex = 3;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(10, 5);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(54, 13);
            this.label9.TabIndex = 2;
            this.label9.Text = "Mainhand";
            // 
            // comboBoxLevelPoison1
            // 
            this.comboBoxLevelPoison1.FormattingEnabled = true;
            this.comboBoxLevelPoison1.Location = new System.Drawing.Point(70, 0);
            this.comboBoxLevelPoison1.Name = "comboBoxLevelPoison1";
            this.comboBoxLevelPoison1.Size = new System.Drawing.Size(76, 21);
            this.comboBoxLevelPoison1.TabIndex = 1;
            // 
            // checkBoxLevelPoison
            // 
            this.checkBoxLevelPoison.AutoSize = true;
            this.checkBoxLevelPoison.Location = new System.Drawing.Point(19, 327);
            this.checkBoxLevelPoison.Name = "checkBoxLevelPoison";
            this.checkBoxLevelPoison.Size = new System.Drawing.Size(52, 17);
            this.checkBoxLevelPoison.TabIndex = 0;
            this.checkBoxLevelPoison.Text = "Level";
            this.checkBoxLevelPoison.UseVisualStyleBackColor = true;
            this.checkBoxLevelPoison.CheckedChanged += new System.EventHandler(this.checkBoxLevelPoison_CheckedChanged);
            // 
            // panelUseAoe
            // 
            this.panelUseAoe.Controls.Add(this.radioButtonAoeOff);
            this.panelUseAoe.Controls.Add(this.radioButtonAoeOn);
            this.panelUseAoe.Location = new System.Drawing.Point(291, 131);
            this.panelUseAoe.Name = "panelUseAoe";
            this.panelUseAoe.Size = new System.Drawing.Size(51, 55);
            this.panelUseAoe.TabIndex = 10;
            // 
            // radioButtonAoeOff
            // 
            this.radioButtonAoeOff.AutoSize = true;
            this.radioButtonAoeOff.Location = new System.Drawing.Point(3, 26);
            this.radioButtonAoeOff.Name = "radioButtonAoeOff";
            this.radioButtonAoeOff.Size = new System.Drawing.Size(39, 17);
            this.radioButtonAoeOff.TabIndex = 1;
            this.radioButtonAoeOff.TabStop = true;
            this.radioButtonAoeOff.Text = "Off";
            this.radioButtonAoeOff.UseVisualStyleBackColor = true;
            // 
            // radioButtonAoeOn
            // 
            this.radioButtonAoeOn.AutoSize = true;
            this.radioButtonAoeOn.Location = new System.Drawing.Point(3, 3);
            this.radioButtonAoeOn.Name = "radioButtonAoeOn";
            this.radioButtonAoeOn.Size = new System.Drawing.Size(39, 17);
            this.radioButtonAoeOn.TabIndex = 0;
            this.radioButtonAoeOn.TabStop = true;
            this.radioButtonAoeOn.Text = "On";
            this.radioButtonAoeOn.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(287, 104);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(47, 24);
            this.label10.TabIndex = 9;
            this.label10.Text = "AoE";
            // 
            // Config
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 389);
            this.Controls.Add(this.panelUseAoe);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.panelLevelPoison);
            this.Controls.Add(this.panelDungeonPoison);
            this.Controls.Add(this.panelBgPoison);
            this.Controls.Add(this.panelHeroicPoison);
            this.Controls.Add(this.checkBoxLevelPoison);
            this.Controls.Add(this.checkBoxBgPoison);
            this.Controls.Add(this.checkBoxDungeonPoison);
            this.Controls.Add(this.checkBoxHeroicPoison);
            this.Controls.Add(this.checkBoxRaidPoison);
            this.Controls.Add(this.panelRaidPoison);
            this.Controls.Add(this.labelApplyPoisons);
            this.Controls.Add(this.panelMovement);
            this.Controls.Add(this.labelUseMovement);
            this.Controls.Add(this.panelCooldowns);
            this.Controls.Add(this.labelUseCooldowns);
            this.Controls.Add(this.buttonLaunchCombatControl);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.radioButtonHolderPanel);
            this.Controls.Add(this.labelModePrompt);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Config";
            this.Text = "MutaRaidBT Settings";
            this.Load += new System.EventHandler(this.Config_Load);
            this.panelCooldowns.ResumeLayout(false);
            this.panelCooldowns.PerformLayout();
            this.radioButtonHolderPanel.ResumeLayout(false);
            this.radioButtonHolderPanel.PerformLayout();
            this.panelMovement.ResumeLayout(false);
            this.panelMovement.PerformLayout();
            this.panelRaidPoison.ResumeLayout(false);
            this.panelRaidPoison.PerformLayout();
            this.panelHeroicPoison.ResumeLayout(false);
            this.panelHeroicPoison.PerformLayout();
            this.panelBgPoison.ResumeLayout(false);
            this.panelBgPoison.PerformLayout();
            this.panelDungeonPoison.ResumeLayout(false);
            this.panelDungeonPoison.PerformLayout();
            this.panelLevelPoison.ResumeLayout(false);
            this.panelLevelPoison.PerformLayout();
            this.panelUseAoe.ResumeLayout(false);
            this.panelUseAoe.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonLaunchCombatControl;
        private System.Windows.Forms.Label labelUseCooldowns;
        private System.Windows.Forms.Panel panelCooldowns;
        private System.Windows.Forms.RadioButton radioCooldownNever;
        private System.Windows.Forms.RadioButton radioCooldownByBoss;
        private System.Windows.Forms.RadioButton radioCooldownByFocus;
        private System.Windows.Forms.RadioButton radioCooldownAlways;
        private System.Windows.Forms.Label labelModePrompt;
        private System.Windows.Forms.RadioButton radioButtonDungeon;
        private System.Windows.Forms.RadioButton radioButtonHeroicDungeon;
        private System.Windows.Forms.RadioButton radioButtonRaid;
        private System.Windows.Forms.RadioButton radioButtonAuto;
        private System.Windows.Forms.RadioButton radioButtonBattleground;
        private System.Windows.Forms.Panel radioButtonHolderPanel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.RadioButton radioButtonLevel;
        private System.Windows.Forms.Label labelUseMovement;
        private System.Windows.Forms.Panel panelMovement;
        private System.Windows.Forms.RadioButton radioButtonMoveOff;
        private System.Windows.Forms.RadioButton radioButtonMoveOn;
        private System.Windows.Forms.Label labelApplyPoisons;
        private System.Windows.Forms.Panel panelRaidPoison;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxRaidPoison2;
        private System.Windows.Forms.Label labelRaidPoisonMain;
        private System.Windows.Forms.ComboBox comboBoxRaidPoison1;
        private System.Windows.Forms.CheckBox checkBoxRaidPoison;
        private System.Windows.Forms.Panel panelHeroicPoison;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxHeroicPoison2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxHeroicPoison1;
        private System.Windows.Forms.CheckBox checkBoxHeroicPoison;
        private System.Windows.Forms.Panel panelBgPoison;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxBgPoison2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBoxBgPoison1;
        private System.Windows.Forms.CheckBox checkBoxBgPoison;
        private System.Windows.Forms.Panel panelDungeonPoison;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxDungeonPoison2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBoxDungeonPoison1;
        private System.Windows.Forms.CheckBox checkBoxDungeonPoison;
        private System.Windows.Forms.Panel panelLevelPoison;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox comboBoxLevelPoison2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBoxLevelPoison1;
        private System.Windows.Forms.CheckBox checkBoxLevelPoison;
        private System.Windows.Forms.Panel panelUseAoe;
        private System.Windows.Forms.RadioButton radioButtonAoeOff;
        private System.Windows.Forms.RadioButton radioButtonAoeOn;
        private System.Windows.Forms.Label label10;
    }
}