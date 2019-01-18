namespace igx.TrainingDatabaseManager
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.lblEnvironments = new System.Windows.Forms.Label();
            this.chkAll = new System.Windows.Forms.CheckBox();
            this.lblMessage = new System.Windows.Forms.Label();
            this.btnClean = new System.Windows.Forms.Button();
            this.lblMessages = new System.Windows.Forms.Label();
            this.lst = new System.Windows.Forms.ListView();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lblEnvironments
            // 
            this.lblEnvironments.AutoSize = true;
            this.lblEnvironments.Location = new System.Drawing.Point(9, 12);
            this.lblEnvironments.Name = "lblEnvironments";
            this.lblEnvironments.Size = new System.Drawing.Size(74, 13);
            this.lblEnvironments.TabIndex = 0;
            this.lblEnvironments.Text = "Environments:";
            // 
            // chkAll
            // 
            this.chkAll.AutoSize = true;
            this.chkAll.Location = new System.Drawing.Point(104, 12);
            this.chkAll.Name = "chkAll";
            this.chkAll.Size = new System.Drawing.Size(43, 17);
            this.chkAll.TabIndex = 2;
            this.chkAll.Text = "All?";
            this.chkAll.UseVisualStyleBackColor = true;
            this.chkAll.CheckedChanged += new System.EventHandler(this.chkAll_CheckedChanged);
            // 
            // lblMessage
            // 
            this.lblMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMessage.Location = new System.Drawing.Point(153, 28);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(212, 80);
            this.lblMessage.TabIndex = 3;
            this.lblMessage.Text = "Select environments to the left, then press the Clean  button at the bottom. This" +
    " will reset selected environments to a baseline configuration.";
            // 
            // btnClean
            // 
            this.btnClean.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClean.Location = new System.Drawing.Point(153, 185);
            this.btnClean.Name = "btnClean";
            this.btnClean.Size = new System.Drawing.Size(202, 23);
            this.btnClean.TabIndex = 4;
            this.btnClean.Text = "Clean";
            this.btnClean.UseVisualStyleBackColor = true;
            this.btnClean.Click += new System.EventHandler(this.btnClean_Click);
            // 
            // lblMessages
            // 
            this.lblMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMessages.ForeColor = System.Drawing.Color.Red;
            this.lblMessages.Location = new System.Drawing.Point(153, 92);
            this.lblMessages.Name = "lblMessages";
            this.lblMessages.Size = new System.Drawing.Size(202, 61);
            this.lblMessages.TabIndex = 5;
            // 
            // lst
            // 
            this.lst.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lst.CheckBoxes = true;
            this.lst.GridLines = true;
            this.lst.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lst.Location = new System.Drawing.Point(12, 34);
            this.lst.Name = "lst";
            this.lst.ShowGroups = false;
            this.lst.Size = new System.Drawing.Size(134, 174);
            this.lst.TabIndex = 6;
            this.lst.UseCompatibleStateImageBehavior = false;
            this.lst.View = System.Windows.Forms.View.Details;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(156, 169);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(199, 10);
            this.progressBar1.TabIndex = 7;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(367, 226);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.lst);
            this.Controls.Add(this.lblMessages);
            this.Controls.Add(this.btnClean);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.chkAll);
            this.Controls.Add(this.lblEnvironments);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Main";
            this.Text = "Training Environment Manager";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblEnvironments;
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Button btnClean;
        private System.Windows.Forms.Label lblMessages;
        private System.Windows.Forms.ListView lst;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}