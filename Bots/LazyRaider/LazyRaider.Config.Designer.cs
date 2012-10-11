namespace Styx.Bot.CustomBots
{
    partial class SelectTankForm
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
            this.colMaxHealth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSetLeader = new System.Windows.Forms.Button();
            this.colRole = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listView = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colClass = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRefresh = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkRaidBotLikeBehavior = new System.Windows.Forms.CheckBox();
            this.lblPauseKey = new System.Windows.Forms.Label();
            this.cboKeyPause = new System.Windows.Forms.ComboBox();
            this.chkAutoTarget = new System.Windows.Forms.CheckBox();
            this.chkAutoSelectTank = new System.Windows.Forms.CheckBox();
            this.numFollowDistance = new System.Windows.Forms.NumericUpDown();
            this.lblFollowDistance = new System.Windows.Forms.Label();
            this.chkRunWithoutTank = new System.Windows.Forms.CheckBox();
            this.chkAutoFollow = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numFPS = new System.Windows.Forms.NumericUpDown();
            this.chkDisablePlugins = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkLockMemory = new System.Windows.Forms.CheckBox();
            this.btnAutoSetup = new System.Windows.Forms.Button();
            this.btnRaidBotSettings = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnLowCpuSettings = new System.Windows.Forms.Button();
            this.chkAutoTargetOnlyIfNotValid = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFollowDistance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFPS)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // colMaxHealth
            // 
            this.colMaxHealth.Text = "Health";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(344, 333);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(89, 23);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSetLeader
            // 
            this.btnSetLeader.Location = new System.Drawing.Point(6, 19);
            this.btnSetLeader.Name = "btnSetLeader";
            this.btnSetLeader.Size = new System.Drawing.Size(75, 23);
            this.btnSetLeader.TabIndex = 1;
            this.btnSetLeader.Text = "Set Tank";
            this.toolTip1.SetToolTip(this.btnSetLeader, "Select currently highlighted Tank in list");
            this.btnSetLeader.UseVisualStyleBackColor = true;
            this.btnSetLeader.Click += new System.EventHandler(this.btnSetLeader_Click);
            // 
            // colRole
            // 
            this.colRole.Text = "Role";
            // 
            // listView
            // 
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colClass,
            this.colRole,
            this.colMaxHealth});
            this.listView.FullRowSelect = true;
            this.listView.HideSelection = false;
            this.listView.LabelWrap = false;
            this.listView.Location = new System.Drawing.Point(12, 182);
            this.listView.MultiSelect = false;
            this.listView.Name = "listView";
            this.listView.ShowGroups = false;
            this.listView.ShowItemToolTips = true;
            this.listView.Size = new System.Drawing.Size(314, 174);
            this.listView.TabIndex = 2;
            this.toolTip1.SetToolTip(this.listView, "Click Column to Sort, DblClick Row to Select");
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
            this.listView.Click += new System.EventHandler(this.listView_Click);
            this.listView.DoubleClick += new System.EventHandler(this.listView_DoubleClick);
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 120;
            // 
            // colClass
            // 
            this.colClass.Text = "Class";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(6, 48);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "Refresh";
            this.toolTip1.SetToolTip(this.btnRefresh, "Refresh list of Group Members");
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkRaidBotLikeBehavior);
            this.groupBox1.Controls.Add(this.lblPauseKey);
            this.groupBox1.Controls.Add(this.cboKeyPause);
            this.groupBox1.Controls.Add(this.chkAutoTargetOnlyIfNotValid);
            this.groupBox1.Controls.Add(this.chkAutoTarget);
            this.groupBox1.Controls.Add(this.chkAutoSelectTank);
            this.groupBox1.Controls.Add(this.numFollowDistance);
            this.groupBox1.Controls.Add(this.lblFollowDistance);
            this.groupBox1.Controls.Add(this.chkRunWithoutTank);
            this.groupBox1.Controls.Add(this.chkAutoFollow);
            this.groupBox1.Location = new System.Drawing.Point(12, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(314, 163);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Behavior";
            // 
            // chkRaidBotLikeBehavior
            // 
            this.chkRaidBotLikeBehavior.AutoSize = true;
            this.chkRaidBotLikeBehavior.Location = new System.Drawing.Point(11, 17);
            this.chkRaidBotLikeBehavior.Name = "chkRaidBotLikeBehavior";
            this.chkRaidBotLikeBehavior.Size = new System.Drawing.Size(158, 17);
            this.chkRaidBotLikeBehavior.TabIndex = 0;
            this.chkRaidBotLikeBehavior.Text = "RaidBot like (disable all frills)";
            this.toolTip1.SetToolTip(this.chkRaidBotLikeBehavior, "Disable all tank, targeting, follow, and pause behaviors");
            this.chkRaidBotLikeBehavior.UseVisualStyleBackColor = true;
            this.chkRaidBotLikeBehavior.CheckedChanged += new System.EventHandler(this.chkRaidBotLikeBehavior_CheckedChanged);
            // 
            // lblPauseKey
            // 
            this.lblPauseKey.AutoSize = true;
            this.lblPauseKey.Location = new System.Drawing.Point(11, 137);
            this.lblPauseKey.Name = "lblPauseKey";
            this.lblPauseKey.Size = new System.Drawing.Size(159, 13);
            this.lblPauseKey.TabIndex = 8;
            this.lblPauseKey.Text = "WOW Key to Pause LazyRaider";
            // 
            // cboKeyPause
            // 
            this.cboKeyPause.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboKeyPause.FormattingEnabled = true;
            this.cboKeyPause.Location = new System.Drawing.Point(177, 134);
            this.cboKeyPause.Name = "cboKeyPause";
            this.cboKeyPause.Size = new System.Drawing.Size(127, 21);
            this.cboKeyPause.TabIndex = 9;
            this.cboKeyPause.SelectedIndexChanged += new System.EventHandler(this.cboKeyPause_SelectedIndexChanged);
            // 
            // chkAutoTarget
            // 
            this.chkAutoTarget.AutoSize = true;
            this.chkAutoTarget.Location = new System.Drawing.Point(11, 107);
            this.chkAutoTarget.Name = "chkAutoTarget";
            this.chkAutoTarget.Size = new System.Drawing.Size(82, 17);
            this.chkAutoTarget.TabIndex = 6;
            this.chkAutoTarget.Text = "Auto Target";
            this.toolTip1.SetToolTip(this.chkAutoTarget, "Set best DPS target available at all times");
            this.chkAutoTarget.UseVisualStyleBackColor = true;
            this.chkAutoTarget.CheckedChanged += new System.EventHandler(this.chkAutoTarget_CheckedChanged);
            // 
            // chkAutoSelectTank
            // 
            this.chkAutoSelectTank.AutoSize = true;
            this.chkAutoSelectTank.Location = new System.Drawing.Point(11, 84);
            this.chkAutoSelectTank.Name = "chkAutoSelectTank";
            this.chkAutoSelectTank.Size = new System.Drawing.Size(109, 17);
            this.chkAutoSelectTank.TabIndex = 5;
            this.chkAutoSelectTank.Text = "Auto Select Tank";
            this.toolTip1.SetToolTip(this.chkAutoSelectTank, "Auto select new tank if tank out of range");
            this.chkAutoSelectTank.UseVisualStyleBackColor = true;
            // 
            // numFollowDistance
            // 
            this.numFollowDistance.Location = new System.Drawing.Point(259, 60);
            this.numFollowDistance.Name = "numFollowDistance";
            this.numFollowDistance.Size = new System.Drawing.Size(45, 20);
            this.numFollowDistance.TabIndex = 4;
            this.numFollowDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.numFollowDistance, "Distance to follow behind Tank");
            // 
            // lblFollowDistance
            // 
            this.lblFollowDistance.AutoSize = true;
            this.lblFollowDistance.Location = new System.Drawing.Point(174, 62);
            this.lblFollowDistance.Name = "lblFollowDistance";
            this.lblFollowDistance.Size = new System.Drawing.Size(82, 13);
            this.lblFollowDistance.TabIndex = 3;
            this.lblFollowDistance.Text = "Follow Distance";
            this.lblFollowDistance.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // chkRunWithoutTank
            // 
            this.chkRunWithoutTank.AutoSize = true;
            this.chkRunWithoutTank.Location = new System.Drawing.Point(11, 40);
            this.chkRunWithoutTank.Name = "chkRunWithoutTank";
            this.chkRunWithoutTank.Size = new System.Drawing.Size(176, 17);
            this.chkRunWithoutTank.TabIndex = 1;
            this.chkRunWithoutTank.Text = "Run Without a Tank (no leader)";
            this.toolTip1.SetToolTip(this.chkRunWithoutTank, "Disable checking/monitoring Tank");
            this.chkRunWithoutTank.UseVisualStyleBackColor = true;
            this.chkRunWithoutTank.CheckedChanged += new System.EventHandler(this.chkDisableTank_CheckedChanged);
            // 
            // chkAutoFollow
            // 
            this.chkAutoFollow.AutoSize = true;
            this.chkAutoFollow.Location = new System.Drawing.Point(11, 61);
            this.chkAutoFollow.Name = "chkAutoFollow";
            this.chkAutoFollow.Size = new System.Drawing.Size(149, 17);
            this.chkAutoFollow.TabIndex = 2;
            this.chkAutoFollow.Text = "Automatically Follow Tank";
            this.toolTip1.SetToolTip(this.chkAutoFollow, "Follow tank when not in combat");
            this.chkAutoFollow.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "FPS";
            // 
            // numFPS
            // 
            this.numFPS.Location = new System.Drawing.Point(42, 17);
            this.numFPS.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numFPS.Name = "numFPS";
            this.numFPS.Size = new System.Drawing.Size(45, 20);
            this.numFPS.TabIndex = 7;
            this.numFPS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip1.SetToolTip(this.numFPS, "Frames Per Second that Bot runs at");
            this.numFPS.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // chkDisablePlugins
            // 
            this.chkDisablePlugins.AutoSize = true;
            this.chkDisablePlugins.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.chkDisablePlugins.Location = new System.Drawing.Point(12, 40);
            this.chkDisablePlugins.Name = "chkDisablePlugins";
            this.chkDisablePlugins.Size = new System.Drawing.Size(101, 17);
            this.chkDisablePlugins.TabIndex = 5;
            this.chkDisablePlugins.Text = "Disable Plug-ins";
            this.toolTip1.SetToolTip(this.chkDisablePlugins, "Faster response (less overhead) by Disable Plug-ins");
            this.chkDisablePlugins.UseVisualStyleBackColor = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkLockMemory);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.chkDisablePlugins);
            this.groupBox2.Controls.Add(this.numFPS);
            this.groupBox2.Location = new System.Drawing.Point(343, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(195, 163);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Performance";
            // 
            // chkLockMemory
            // 
            this.chkLockMemory.AutoSize = true;
            this.chkLockMemory.Location = new System.Drawing.Point(12, 63);
            this.chkLockMemory.Name = "chkLockMemory";
            this.chkLockMemory.Size = new System.Drawing.Size(181, 17);
            this.chkLockMemory.TabIndex = 8;
            this.chkLockMemory.Text = "Frame Lock (not all CC\'s support)";
            this.toolTip1.SetToolTip(this.chkLockMemory, "Faster response by locking memory (may make unstable)");
            this.chkLockMemory.UseVisualStyleBackColor = true;
            // 
            // btnAutoSetup
            // 
            this.btnAutoSetup.Location = new System.Drawing.Point(6, 19);
            this.btnAutoSetup.Name = "btnAutoSetup";
            this.btnAutoSetup.Size = new System.Drawing.Size(75, 23);
            this.btnAutoSetup.TabIndex = 0;
            this.btnAutoSetup.Text = "Auto";
            this.toolTip1.SetToolTip(this.btnAutoSetup, "Auto detect and setup for balance of features and performance");
            this.btnAutoSetup.UseVisualStyleBackColor = true;
            this.btnAutoSetup.Click += new System.EventHandler(this.btnAutoSetup_Click);
            // 
            // btnRaidBotSettings
            // 
            this.btnRaidBotSettings.Location = new System.Drawing.Point(6, 48);
            this.btnRaidBotSettings.Name = "btnRaidBotSettings";
            this.btnRaidBotSettings.Size = new System.Drawing.Size(75, 23);
            this.btnRaidBotSettings.TabIndex = 1;
            this.btnRaidBotSettings.Text = "RaidBot";
            this.toolTip1.SetToolTip(this.btnRaidBotSettings, "For RaidBot users wantting to match behavior and performance");
            this.btnRaidBotSettings.UseVisualStyleBackColor = true;
            this.btnRaidBotSettings.Click += new System.EventHandler(this.btnRaidBotSettings_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnAutoSetup);
            this.groupBox3.Controls.Add(this.btnLowCpuSettings);
            this.groupBox3.Controls.Add(this.btnRaidBotSettings);
            this.groupBox3.Location = new System.Drawing.Point(449, 182);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(89, 127);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "1-click Setup";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btnSetLeader);
            this.groupBox4.Controls.Add(this.btnRefresh);
            this.groupBox4.Location = new System.Drawing.Point(344, 182);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(89, 84);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Tank List";
            // 
            // btnLowCpuSettings
            // 
            this.btnLowCpuSettings.Location = new System.Drawing.Point(8, 77);
            this.btnLowCpuSettings.Name = "btnLowCpuSettings";
            this.btnLowCpuSettings.Size = new System.Drawing.Size(75, 23);
            this.btnLowCpuSettings.TabIndex = 1;
            this.btnLowCpuSettings.Text = "Low CPU";
            this.toolTip1.SetToolTip(this.btnLowCpuSettings, "For users with excessive lag or without a high end computer");
            this.btnLowCpuSettings.UseVisualStyleBackColor = true;
            this.btnLowCpuSettings.Click += new System.EventHandler(this.btnLowCpuSettings_Click);
            // 
            // chkAutoTargetOnlyIfNotValid
            // 
            this.chkAutoTargetOnlyIfNotValid.AutoSize = true;
            this.chkAutoTargetOnlyIfNotValid.Location = new System.Drawing.Point(99, 107);
            this.chkAutoTargetOnlyIfNotValid.Name = "chkAutoTargetOnlyIfNotValid";
            this.chkAutoTargetOnlyIfNotValid.Size = new System.Drawing.Size(172, 17);
            this.chkAutoTargetOnlyIfNotValid.TabIndex = 7;
            this.chkAutoTargetOnlyIfNotValid.Text = "... only if not valid enemy target";
            this.toolTip1.SetToolTip(this.chkAutoTargetOnlyIfNotValid, "Stay on same enemy target until killed");
            this.chkAutoTargetOnlyIfNotValid.UseVisualStyleBackColor = true;
            // 
            // SelectTankForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 368);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.listView);
            this.Name = "SelectTankForm";
            this.Text = "LazyRaider Configuration";
            this.Activated += new System.EventHandler(this.SelectTankForm_Activated);
            this.Deactivate += new System.EventHandler(this.SelectTankForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SelectTankForm_FormClosing);
            this.Shown += new System.EventHandler(this.SelectTankForm_Shown);
            this.VisibleChanged += new System.EventHandler(this.SelectTankForm_VisibleChanged);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFollowDistance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFPS)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ColumnHeader colMaxHealth;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnSetLeader;
        private System.Windows.Forms.ColumnHeader colRole;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colClass;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown numFollowDistance;
        private System.Windows.Forms.Label lblFollowDistance;
        private System.Windows.Forms.CheckBox chkAutoFollow;
        private System.Windows.Forms.CheckBox chkRunWithoutTank;
        private System.Windows.Forms.CheckBox chkAutoSelectTank;
        private System.Windows.Forms.CheckBox chkDisablePlugins;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numFPS;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkLockMemory;
        private System.Windows.Forms.Button btnAutoSetup;
        private System.Windows.Forms.CheckBox chkAutoTarget;
        private System.Windows.Forms.Label lblPauseKey;
        private System.Windows.Forms.ComboBox cboKeyPause;
        private System.Windows.Forms.Button btnRaidBotSettings;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkRaidBotLikeBehavior;
        private System.Windows.Forms.Button btnLowCpuSettings;
        private System.Windows.Forms.CheckBox chkAutoTargetOnlyIfNotValid;

    }
}