namespace igx.UpdateDatabases
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lbDatabases = new System.Windows.Forms.CheckedListBox();
            this.chkDevelopment = new System.Windows.Forms.CheckBox();
            this.chkProduction = new System.Windows.Forms.CheckBox();
            this.txtSql = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtMessages = new System.Windows.Forms.TextBox();
            this.lblMesages = new System.Windows.Forms.Label();
            this.btnRun = new System.Windows.Forms.Button();
            this.lblSqlValidation = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnClear = new System.Windows.Forms.Button();
            this.chkUat = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkLegacy = new System.Windows.Forms.CheckBox();
            this.chkAlternate = new System.Windows.Forms.CheckBox();
            this.chkPreview = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbDatabases
            // 
            this.lbDatabases.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbDatabases.CheckOnClick = true;
            this.lbDatabases.FormattingEnabled = true;
            this.lbDatabases.Location = new System.Drawing.Point(13, 172);
            this.lbDatabases.Name = "lbDatabases";
            this.lbDatabases.Size = new System.Drawing.Size(198, 304);
            this.lbDatabases.TabIndex = 0;
            // 
            // chkDevelopment
            // 
            this.chkDevelopment.AutoSize = true;
            this.chkDevelopment.Location = new System.Drawing.Point(6, 38);
            this.chkDevelopment.Name = "chkDevelopment";
            this.chkDevelopment.Size = new System.Drawing.Size(89, 17);
            this.chkDevelopment.TabIndex = 1;
            this.chkDevelopment.Text = "Development";
            this.chkDevelopment.UseVisualStyleBackColor = true;
            this.chkDevelopment.CheckedChanged += new System.EventHandler(this.chkDevelopment_CheckedChanged);
            // 
            // chkProduction
            // 
            this.chkProduction.AutoSize = true;
            this.chkProduction.Location = new System.Drawing.Point(6, 81);
            this.chkProduction.Name = "chkProduction";
            this.chkProduction.Size = new System.Drawing.Size(77, 17);
            this.chkProduction.TabIndex = 2;
            this.chkProduction.Text = "Production";
            this.chkProduction.UseVisualStyleBackColor = true;
            this.chkProduction.CheckedChanged += new System.EventHandler(this.chkProduction_CheckedChanged);
            // 
            // txtSql
            // 
            this.txtSql.AcceptsReturn = true;
            this.txtSql.AllowDrop = true;
            this.txtSql.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSql.Location = new System.Drawing.Point(220, 68);
            this.txtSql.MaxLength = 100000000;
            this.txtSql.Multiline = true;
            this.txtSql.Name = "txtSql";
            this.txtSql.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtSql.Size = new System.Drawing.Size(739, 194);
            this.txtSql.TabIndex = 3;
            this.txtSql.WordWrap = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(217, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Sql Statement to Run";
            // 
            // txtMessages
            // 
            this.txtMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessages.Location = new System.Drawing.Point(220, 290);
            this.txtMessages.Multiline = true;
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtMessages.Size = new System.Drawing.Size(739, 194);
            this.txtMessages.TabIndex = 5;
            // 
            // lblMesages
            // 
            this.lblMesages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMesages.AutoSize = true;
            this.lblMesages.Location = new System.Drawing.Point(217, 274);
            this.lblMesages.Name = "lblMesages";
            this.lblMesages.Size = new System.Drawing.Size(119, 13);
            this.lblMesages.TabIndex = 6;
            this.lblMesages.Text = "Messages Encountered";
            // 
            // btnRun
            // 
            this.btnRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRun.Location = new System.Drawing.Point(883, 28);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 7;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // lblSqlValidation
            // 
            this.lblSqlValidation.AutoSize = true;
            this.lblSqlValidation.ForeColor = System.Drawing.Color.Red;
            this.lblSqlValidation.Location = new System.Drawing.Point(332, 52);
            this.lblSqlValidation.Name = "lblSqlValidation";
            this.lblSqlValidation.Size = new System.Drawing.Size(0, 13);
            this.lblSqlValidation.TabIndex = 8;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(342, 268);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(617, 19);
            this.progressBar1.TabIndex = 9;
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Location = new System.Drawing.Point(802, 28);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 10;
            this.btnClear.Text = "Clear Sql";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // chkUat
            // 
            this.chkUat.AutoSize = true;
            this.chkUat.Location = new System.Drawing.Point(6, 58);
            this.chkUat.Name = "chkUat";
            this.chkUat.Size = new System.Drawing.Size(48, 17);
            this.chkUat.TabIndex = 11;
            this.chkUat.Text = "UAT";
            this.chkUat.UseVisualStyleBackColor = true;
            this.chkUat.CheckedChanged += new System.EventHandler(this.chkUat_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkLegacy);
            this.groupBox1.Controls.Add(this.chkAlternate);
            this.groupBox1.Controls.Add(this.chkPreview);
            this.groupBox1.Controls.Add(this.chkDevelopment);
            this.groupBox1.Controls.Add(this.chkUat);
            this.groupBox1.Controls.Add(this.chkProduction);
            this.groupBox1.Location = new System.Drawing.Point(11, 52);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 106);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Environments";
            // 
            // chkLegacy
            // 
            this.chkLegacy.AutoSize = true;
            this.chkLegacy.Location = new System.Drawing.Point(115, 38);
            this.chkLegacy.Name = "chkLegacy";
            this.chkLegacy.Size = new System.Drawing.Size(84, 17);
            this.chkLegacy.TabIndex = 15;
            this.chkLegacy.Text = "Legacy Dev";
            this.chkLegacy.UseVisualStyleBackColor = true;
            this.chkLegacy.CheckedChanged += new System.EventHandler(this.chkLegacy_CheckedChanged);
            // 
            // chkAlternate
            // 
            this.chkAlternate.AutoSize = true;
            this.chkAlternate.Location = new System.Drawing.Point(115, 18);
            this.chkAlternate.Name = "chkAlternate";
            this.chkAlternate.Size = new System.Drawing.Size(68, 17);
            this.chkAlternate.TabIndex = 14;
            this.chkAlternate.Text = "Alternate";
            this.chkAlternate.UseVisualStyleBackColor = true;
            this.chkAlternate.CheckedChanged += new System.EventHandler(this.chkAlternate_CheckedChanged);
            // 
            // chkPreview
            // 
            this.chkPreview.AutoSize = true;
            this.chkPreview.Location = new System.Drawing.Point(6, 18);
            this.chkPreview.Name = "chkPreview";
            this.chkPreview.Size = new System.Drawing.Size(64, 17);
            this.chkPreview.TabIndex = 14;
            this.chkPreview.Text = "Preview";
            this.chkPreview.UseVisualStyleBackColor = true;
            this.chkPreview.CheckedChanged += new System.EventHandler(this.chkPreview_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(331, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(291, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Individual commands MUST be separated by the value: GO;";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(971, 503);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.lblSqlValidation);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.lblMesages);
            this.Controls.Add(this.txtMessages);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSql);
            this.Controls.Add(this.lbDatabases);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Govern Database Updater";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox lbDatabases;
        private System.Windows.Forms.CheckBox chkDevelopment;
        private System.Windows.Forms.CheckBox chkProduction;
        private System.Windows.Forms.TextBox txtSql;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtMessages;
        private System.Windows.Forms.Label lblMesages;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label lblSqlValidation;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.CheckBox chkUat;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkPreview;
        private System.Windows.Forms.CheckBox chkAlternate;
        private System.Windows.Forms.CheckBox chkLegacy;
        private System.Windows.Forms.Label label2;
    }
}

