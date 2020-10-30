namespace ShivExcelLogging
{
    partial class wfDodao
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
            this.lblCloseDodao = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblProcess = new System.Windows.Forms.Label();
            this.lblDoDao1 = new System.Windows.Forms.Label();
            this.lblDoDao2 = new System.Windows.Forms.Label();
            this.lblDoDao3 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblCloseDodao
            // 
            this.lblCloseDodao.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCloseDodao.AutoSize = true;
            this.lblCloseDodao.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCloseDodao.ForeColor = System.Drawing.Color.Gray;
            this.lblCloseDodao.Location = new System.Drawing.Point(320, 3);
            this.lblCloseDodao.Name = "lblCloseDodao";
            this.lblCloseDodao.Size = new System.Drawing.Size(38, 13);
            this.lblCloseDodao.TabIndex = 0;
            this.lblCloseDodao.Text = "Close";
            this.lblCloseDodao.Click += new System.EventHandler(this.lblCloseDodao_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(22, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 22);
            this.label1.TabIndex = 1;
            this.label1.Text = "Đang lấy dữ liệu độ đảo";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(193)))), ((int)(((byte)(208)))), ((int)(((byte)(240)))));
            this.panel1.Controls.Add(this.lblDoDao3);
            this.panel1.Controls.Add(this.lblDoDao2);
            this.panel1.Controls.Add(this.lblDoDao1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(4, 25);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(349, 116);
            this.panel1.TabIndex = 2;
            // 
            // lblProcess
            // 
            this.lblProcess.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.lblProcess.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.lblProcess.Location = new System.Drawing.Point(1, 144);
            this.lblProcess.Name = "lblProcess";
            this.lblProcess.Size = new System.Drawing.Size(142, 27);
            this.lblProcess.TabIndex = 3;
            this.lblProcess.Text = "________";
            // 
            // lblDoDao1
            // 
            this.lblDoDao1.BackColor = System.Drawing.Color.White;
            this.lblDoDao1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.lblDoDao1.Location = new System.Drawing.Point(8, 47);
            this.lblDoDao1.Name = "lblDoDao1";
            this.lblDoDao1.Size = new System.Drawing.Size(107, 48);
            this.lblDoDao1.TabIndex = 2;
            this.lblDoDao1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDoDao2
            // 
            this.lblDoDao2.BackColor = System.Drawing.Color.White;
            this.lblDoDao2.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.lblDoDao2.Location = new System.Drawing.Point(121, 47);
            this.lblDoDao2.Name = "lblDoDao2";
            this.lblDoDao2.Size = new System.Drawing.Size(107, 48);
            this.lblDoDao2.TabIndex = 2;
            this.lblDoDao2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDoDao3
            // 
            this.lblDoDao3.BackColor = System.Drawing.Color.White;
            this.lblDoDao3.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(163)));
            this.lblDoDao3.Location = new System.Drawing.Point(234, 47);
            this.lblDoDao3.Name = "lblDoDao3";
            this.lblDoDao3.Size = new System.Drawing.Size(107, 48);
            this.lblDoDao3.TabIndex = 2;
            this.lblDoDao3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // wfDodao
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(254)))), ((int)(((byte)(254)))));
            this.ClientSize = new System.Drawing.Size(360, 189);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lblCloseDodao);
            this.Controls.Add(this.lblProcess);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "wfDodao";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "ButtonKheho";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.wfDodao_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCloseDodao;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblProcess;
        private System.Windows.Forms.Label lblDoDao1;
        private System.Windows.Forms.Label lblDoDao3;
        private System.Windows.Forms.Label lblDoDao2;
    }
}