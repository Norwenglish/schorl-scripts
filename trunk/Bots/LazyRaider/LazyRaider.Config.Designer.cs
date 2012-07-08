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
            this.colMaxHealth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSetLeader = new System.Windows.Forms.Button();
            this.colRole = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listView = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colClass = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRefresh = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
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
            this.btnMaxPerformance = new System.Windows.Forms.Button();
            this.cboKeyPause = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFollowDistance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFPS)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // colMaxHealth
            // 
            this.colMaxHealth.Text = "Health";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(343, 164);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSetLeader
            // 
            this.btnSetLeader.Location = new System.Drawing.Point(343, 193);
            this.btnSetLeader.Name = "btnSetLeader";
            this.btnSetLeader.Size = new System.Drawing.Size(75, 23);
            this.btnSetLeader.TabIndex = 1;
            this.btnSetLeader.Text = "Set Tank";
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
            this.listView.Location = new System.Drawing.Point(12, 164);
            this.listView.MultiSelect = false;
            this.listView.Name = "listView";
            this.listView.ShowGroups = false;
            this.listView.Size = new System.Drawing.Size(314, 174);
            this.listView.TabIndex = 0;
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
            this.btnRefresh.Location = new System.Drawing.Point(343, 222);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cboKeyPause);
            this.groupBox1.Controls.Add(this.chkAutoTarget);
            this.groupBox1.Controls.Add(this.chkAutoSelectTank);
            this.groupBox1.Controls.Add(this.numFollowDistance);
            this.groupBox1.Controls.Add(this.lblFollowDistance);
            this.groupBox1.Controls.Add(this.chkRunWithoutTank);
            this.groupBox1.Controls.Add(this.chkAutoFollow);
            this.groupBox1.Location = new System.Drawing.Point(12, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(314, 145);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Behavior";
            // 
            // chkAutoTarget
            // 
            this.chkAutoTarget.AutoSize = true;
            this.chkAutoTarget.Location = new System.Drawing.Point(6, 85);
            this.chkAutoTarget.Name = "chkAutoTarget";
            this.chkAutoTarget.Size = new System.Drawing.Size(82, 17);
            this.chkAutoTarget.TabIndex = 5;
            this.chkAutoTarget.Text = "Auto Target";
            this.chkAutoTarget.UseVisualStyleBackColor = true;
            // 
            // chkAutoSelectTank
            // 
            this.chkAutoSelectTank.AutoSize = true;
            this.chkAutoSelectTank.Location = new System.Drawing.Point(6, 62);
            this.chkAutoSelectTank.Name = "chkAutoSelectTank";
            this.chkAutoSelectTank.Size = new System.Drawing.Size(109, 17);
            this.chkAutoSelectTank.TabIndex = 4;
            this.chkAutoSelectTank.Text = "Auto Select Tank";
            this.chkAutoSelectTank.UseVisualStyleBackColor = true;
            // 
            // numFollowDistance
            // 
            this.numFollowDistance.Location = new System.Drawing.Point(254, 38);
            this.numFollowDistance.Name = "numFollowDistance";
            this.numFollowDistance.Size = new System.Drawing.Size(45, 20);
            this.numFollowDistance.TabIndex = 3;
            this.numFollowDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblFollowDistance
            // 
            this.lblFollowDistance.AutoSize = true;
            this.lblFollowDistance.Location = new System.Drawing.Point(169, 40);
            this.lblFollowDistance.Name = "lblFollowDistance";
            this.lblFollowDistance.Size = new System.Drawing.Size(82, 13);
            this.lblFollowDistance.TabIndex = 2;
            this.lblFollowDistance.Text = "Follow Distance";
            this.lblFollowDistance.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // chkRunWithoutTank
            // 
            this.chkRunWithoutTank.AutoSize = true;
            this.chkRunWithoutTank.Location = new System.Drawing.Point(6, 18);
            this.chkRunWithoutTank.Name = "chkRunWithoutTank";
            this.chkRunWithoutTank.Size = new System.Drawing.Size(176, 17);
            this.chkRunWithoutTank.TabIndex = 0;
            this.chkRunWithoutTank.Text = "Run Without a Tank (no leader)";
            this.chkRunWithoutTank.UseVisualStyleBackColor = true;
            this.chkRunWithoutTank.CheckedChanged += new System.EventHandler(this.chkDisableTank_CheckedChanged);
            // 
            // chkAutoFollow
            // 
            this.chkAutoFollow.AutoSize = true;
            this.chkAutoFollow.Location = new System.Drawing.Point(6, 39);
            this.chkAutoFollow.Name = "chkAutoFollow";
            this.chkAutoFollow.Size = new System.Drawing.Size(149, 17);
            this.chkAutoFollow.TabIndex = 1;
            this.chkAutoFollow.Text = "Automatically Follow Tank";
            this.chkAutoFollow.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "FPS";
            // 
            // numFPS
            // 
            this.numFPS.Location = new System.Drawing.Point(39, 17);
            this.numFPS.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numFPS.Name = "numFPS";
            this.numFPS.Size = new System.Drawing.Size(45, 20);
            this.numFPS.TabIndex = 7;
            this.numFPS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.chkDisablePlugins.Location = new System.Drawing.Point(9, 45);
            this.chkDisablePlugins.Name = "chkDisablePlugins";
            this.chkDisablePlugins.Size = new System.Drawing.Size(101, 17);
            this.chkDisablePlugins.TabIndex = 5;
            this.chkDisablePlugins.Text = "Disable Plug-ins";
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
            this.groupBox2.Size = new System.Drawing.Size(192, 91);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Performance";
            // 
            // chkLockMemory
            // 
            this.chkLockMemory.AutoSize = true;
            this.chkLockMemory.Location = new System.Drawing.Point(9, 66);
            this.chkLockMemory.Name = "chkLockMemory";
            this.chkLockMemory.Size = new System.Drawing.Size(181, 17);
            this.chkLockMemory.TabIndex = 8;
            this.chkLockMemory.Text = "Frame Lock (not all CC\'s support)";
            this.chkLockMemory.UseVisualStyleBackColor = true;
            // 
            // btnMaxPerformance
            // 
            this.btnMaxPerformance.Location = new System.Drawing.Point(424, 164);
            this.btnMaxPerformance.Name = "btnMaxPerformance";
            this.btnMaxPerformance.Size = new System.Drawing.Size(75, 23);
            this.btnMaxPerformance.TabIndex = 2;
            this.btnMaxPerformance.Text = "Max DPS";
            this.btnMaxPerformance.UseVisualStyleBackColor = true;
            this.btnMaxPerformance.Click += new System.EventHandler(this.btnMaxPerformance_Click);
            // 
            // cboKeyPause
            // 
            this.cboKeyPause.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboKeyPause.FormattingEnabled = true;
            this.cboKeyPause.Location = new System.Drawing.Point(172, 112);
            this.cboKeyPause.Name = "cboKeyPause";
            this.cboKeyPause.Size = new System.Drawing.Size(127, 21);
            this.cboKeyPause.TabIndex = 7;
            this.cboKeyPause.SelectedIndexChanged += new System.EventHandler(this.cboKeyPause_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 115);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(159, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "WOW Key to Pause LazyRaider";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // SelectTankForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(541, 350);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnMaxPerformance);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSetLeader);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.btnRefresh);
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
        private System.Windows.Forms.Button btnMaxPerformance;
        private System.Windows.Forms.CheckBox chkAutoTarget;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboKeyPause;

    }
}