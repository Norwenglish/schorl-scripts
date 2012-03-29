//////////////////////////////////////////////////
//          CombatControl.Designer.cs           //
//      Part of MutaRaidBT by fiftypence        //
//////////////////////////////////////////////////

namespace MutaRaidBT.UI
{
    partial class CombatControl
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
            this.buttonToggleCd = new System.Windows.Forms.Button();
            this.buttonToggleCombat = new System.Windows.Forms.Button();
            this.labelCdStatus = new System.Windows.Forms.Label();
            this.labelCombatStatus = new System.Windows.Forms.Label();
            this.labelBehindTarStatus = new System.Windows.Forms.Label();
            this.buttonToggleBehindTar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonToggleCd
            // 
            this.buttonToggleCd.Location = new System.Drawing.Point(12, 36);
            this.buttonToggleCd.Name = "buttonToggleCd";
            this.buttonToggleCd.Size = new System.Drawing.Size(114, 20);
            this.buttonToggleCd.TabIndex = 0;
            this.buttonToggleCd.Text = "Toggle Cooldowns";
            this.buttonToggleCd.UseVisualStyleBackColor = true;
            this.buttonToggleCd.Click += new System.EventHandler(this.buttonToggleCd_Click);
            // 
            // buttonToggleCombat
            // 
            this.buttonToggleCombat.Location = new System.Drawing.Point(135, 36);
            this.buttonToggleCombat.Name = "buttonToggleCombat";
            this.buttonToggleCombat.Size = new System.Drawing.Size(113, 20);
            this.buttonToggleCombat.TabIndex = 2;
            this.buttonToggleCombat.Text = "Toggle Combat";
            this.buttonToggleCombat.UseVisualStyleBackColor = true;
            this.buttonToggleCombat.Click += new System.EventHandler(this.buttonToggleCombat_Click);
            // 
            // labelCdStatus
            // 
            this.labelCdStatus.AutoSize = true;
            this.labelCdStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCdStatus.Location = new System.Drawing.Point(12, 9);
            this.labelCdStatus.Name = "labelCdStatus";
            this.labelCdStatus.Size = new System.Drawing.Size(45, 24);
            this.labelCdStatus.TabIndex = 3;
            this.labelCdStatus.Text = "CDs";
            // 
            // labelCombatStatus
            // 
            this.labelCombatStatus.AutoSize = true;
            this.labelCombatStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCombatStatus.Location = new System.Drawing.Point(131, 9);
            this.labelCombatStatus.Name = "labelCombatStatus";
            this.labelCombatStatus.Size = new System.Drawing.Size(75, 24);
            this.labelCombatStatus.TabIndex = 5;
            this.labelCombatStatus.Text = "Combat";
            // 
            // labelBehindTarStatus
            // 
            this.labelBehindTarStatus.AutoSize = true;
            this.labelBehindTarStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBehindTarStatus.Location = new System.Drawing.Point(300, 9);
            this.labelBehindTarStatus.Name = "labelBehindTarStatus";
            this.labelBehindTarStatus.Size = new System.Drawing.Size(70, 24);
            this.labelBehindTarStatus.TabIndex = 7;
            this.labelBehindTarStatus.Text = "Behind";
            // 
            // buttonToggleBehindTar
            // 
            this.buttonToggleBehindTar.Location = new System.Drawing.Point(304, 36);
            this.buttonToggleBehindTar.Name = "buttonToggleBehindTar";
            this.buttonToggleBehindTar.Size = new System.Drawing.Size(113, 20);
            this.buttonToggleBehindTar.TabIndex = 6;
            this.buttonToggleBehindTar.Text = "Toggle Behind Target";
            this.buttonToggleBehindTar.UseVisualStyleBackColor = true;
            this.buttonToggleBehindTar.Click += new System.EventHandler(this.buttonToggleBehindTar_Click);
            // 
            // CombatControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 61);
            this.Controls.Add(this.labelBehindTarStatus);
            this.Controls.Add(this.buttonToggleBehindTar);
            this.Controls.Add(this.labelCombatStatus);
            this.Controls.Add(this.labelCdStatus);
            this.Controls.Add(this.buttonToggleCombat);
            this.Controls.Add(this.buttonToggleCd);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CombatControl";
            this.Text = "MutaRaidBT Combat Control";
            this.Load += new System.EventHandler(this.CombatControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonToggleCd;
        private System.Windows.Forms.Button buttonToggleCombat;
        private System.Windows.Forms.Label labelCdStatus;
        private System.Windows.Forms.Label labelCombatStatus;
        private System.Windows.Forms.Label labelBehindTarStatus;
        private System.Windows.Forms.Button buttonToggleBehindTar;
    }
}