namespace igx.CertificateManager
{
    partial class AddSsoCertificate
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddSsoCertificate));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnFindCertificate = new System.Windows.Forms.Button();
            this.lblFileName = new System.Windows.Forms.Label();
            this.lblSsoEndpoint = new System.Windows.Forms.Label();
            this.lblSloEndpoint = new System.Windows.Forms.Label();
            this.txtFriendlyName = new System.Windows.Forms.TextBox();
            this.lblCertificateName = new System.Windows.Forms.Label();
            this.txtSsoEndpoint = new System.Windows.Forms.TextBox();
            this.txtSloEndpoint = new System.Windows.Forms.TextBox();
            this.chkSignRequest = new System.Windows.Forms.CheckBox();
            this.lblHashType = new System.Windows.Forms.Label();
            this.ddlHashType = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.ddlClient = new System.Windows.Forms.ComboBox();
            this.dsClient = new System.Data.DataSet();
            this.dataTable1 = new System.Data.DataTable();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataColumn2 = new System.Data.DataColumn();
            this.dataTable2 = new System.Data.DataTable();
            this.dataColumn3 = new System.Data.DataColumn();
            this.dataColumn4 = new System.Data.DataColumn();
            this.lblClient = new System.Windows.Forms.Label();
            this.lblEnvironment = new System.Windows.Forms.Label();
            this.lstEnvironment = new System.Windows.Forms.ListView();
            this.ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Level = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UrlPrefix = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkAutoProvision = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.btnDownloadCertificate = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.dsClient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.grpStatus.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnFindCertificate
            // 
            this.btnFindCertificate.Location = new System.Drawing.Point(12, 58);
            this.btnFindCertificate.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnFindCertificate.Name = "btnFindCertificate";
            this.btnFindCertificate.Size = new System.Drawing.Size(256, 46);
            this.btnFindCertificate.TabIndex = 0;
            this.btnFindCertificate.Text = "Find Certificate File";
            this.btnFindCertificate.UseVisualStyleBackColor = true;
            this.btnFindCertificate.Click += new System.EventHandler(this.btnFindCertificate_Click);
            // 
            // lblFileName
            // 
            this.lblFileName.Location = new System.Drawing.Point(18, 110);
            this.lblFileName.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(502, 52);
            this.lblFileName.TabIndex = 1;
            // 
            // lblSsoEndpoint
            // 
            this.lblSsoEndpoint.AutoSize = true;
            this.lblSsoEndpoint.Location = new System.Drawing.Point(8, 387);
            this.lblSsoEndpoint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSsoEndpoint.Name = "lblSsoEndpoint";
            this.lblSsoEndpoint.Size = new System.Drawing.Size(180, 25);
            this.lblSsoEndpoint.TabIndex = 6;
            this.lblSsoEndpoint.Text = "SSO Endpoint Url";
            // 
            // lblSloEndpoint
            // 
            this.lblSloEndpoint.AutoSize = true;
            this.lblSloEndpoint.Location = new System.Drawing.Point(6, 496);
            this.lblSloEndpoint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSloEndpoint.Name = "lblSloEndpoint";
            this.lblSloEndpoint.Size = new System.Drawing.Size(178, 25);
            this.lblSloEndpoint.TabIndex = 7;
            this.lblSloEndpoint.Text = "SLO Endpoint Url";
            // 
            // txtFriendlyName
            // 
            this.txtFriendlyName.Location = new System.Drawing.Point(12, 312);
            this.txtFriendlyName.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.txtFriendlyName.MaxLength = 250;
            this.txtFriendlyName.Name = "txtFriendlyName";
            this.txtFriendlyName.Size = new System.Drawing.Size(504, 31);
            this.txtFriendlyName.TabIndex = 4;
            // 
            // lblCertificateName
            // 
            this.lblCertificateName.AutoSize = true;
            this.lblCertificateName.Location = new System.Drawing.Point(6, 281);
            this.lblCertificateName.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblCertificateName.Name = "lblCertificateName";
            this.lblCertificateName.Size = new System.Drawing.Size(254, 25);
            this.lblCertificateName.TabIndex = 5;
            this.lblCertificateName.Text = "Certificate Friendly Name";
            // 
            // txtSsoEndpoint
            // 
            this.txtSsoEndpoint.Location = new System.Drawing.Point(12, 417);
            this.txtSsoEndpoint.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.txtSsoEndpoint.Name = "txtSsoEndpoint";
            this.txtSsoEndpoint.Size = new System.Drawing.Size(504, 31);
            this.txtSsoEndpoint.TabIndex = 8;
            // 
            // txtSloEndpoint
            // 
            this.txtSloEndpoint.Location = new System.Drawing.Point(14, 525);
            this.txtSloEndpoint.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.txtSloEndpoint.Name = "txtSloEndpoint";
            this.txtSloEndpoint.Size = new System.Drawing.Size(502, 31);
            this.txtSloEndpoint.TabIndex = 9;
            // 
            // chkSignRequest
            // 
            this.chkSignRequest.AutoSize = true;
            this.chkSignRequest.Location = new System.Drawing.Point(278, 217);
            this.chkSignRequest.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.chkSignRequest.Name = "chkSignRequest";
            this.chkSignRequest.Size = new System.Drawing.Size(235, 29);
            this.chkSignRequest.TabIndex = 10;
            this.chkSignRequest.Text = "Sign SSO Request?";
            this.chkSignRequest.UseVisualStyleBackColor = true;
            // 
            // lblHashType
            // 
            this.lblHashType.AutoSize = true;
            this.lblHashType.Location = new System.Drawing.Point(18, 183);
            this.lblHashType.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblHashType.Name = "lblHashType";
            this.lblHashType.Size = new System.Drawing.Size(212, 25);
            this.lblHashType.TabIndex = 11;
            this.lblHashType.Text = "Hash Algorithm Type";
            // 
            // ddlHashType
            // 
            this.ddlHashType.FormattingEnabled = true;
            this.ddlHashType.Items.AddRange(new object[] {
            "SHA1",
            "SHA256",
            "SHA224",
            "SHA384",
            "SHA512"});
            this.ddlHashType.Location = new System.Drawing.Point(12, 213);
            this.ddlHashType.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.ddlHashType.Name = "ddlHashType";
            this.ddlHashType.Size = new System.Drawing.Size(250, 33);
            this.ddlHashType.TabIndex = 12;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSave.Location = new System.Drawing.Point(16, 604);
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 44);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Location = new System.Drawing.Point(14, 31);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1100, 69);
            this.lblStatus.TabIndex = 14;
            // 
            // ddlClient
            // 
            this.ddlClient.DataSource = this.dsClient;
            this.ddlClient.DisplayMember = "Table1.Name";
            this.ddlClient.FormattingEnabled = true;
            this.ddlClient.Location = new System.Drawing.Point(12, 81);
            this.ddlClient.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.ddlClient.Name = "ddlClient";
            this.ddlClient.Size = new System.Drawing.Size(560, 33);
            this.ddlClient.TabIndex = 15;
            this.ddlClient.ValueMember = "Table1.ID";
            this.ddlClient.SelectedIndexChanged += new System.EventHandler(this.ddlClient_SelectedIndexChanged);
            // 
            // dsClient
            // 
            this.dsClient.DataSetName = "dsClient";
            this.dsClient.Tables.AddRange(new System.Data.DataTable[] {
            this.dataTable1,
            this.dataTable2});
            // 
            // dataTable1
            // 
            this.dataTable1.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn1,
            this.dataColumn2});
            this.dataTable1.TableName = "Table1";
            // 
            // dataColumn1
            // 
            this.dataColumn1.ColumnName = "ID";
            this.dataColumn1.DataType = typeof(int);
            // 
            // dataColumn2
            // 
            this.dataColumn2.ColumnName = "Name";
            // 
            // dataTable2
            // 
            this.dataTable2.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn3,
            this.dataColumn4});
            this.dataTable2.TableName = "Table2";
            // 
            // dataColumn3
            // 
            this.dataColumn3.ColumnName = "ID";
            this.dataColumn3.DataType = typeof(int);
            // 
            // dataColumn4
            // 
            this.dataColumn4.ColumnName = "Name";
            // 
            // lblClient
            // 
            this.lblClient.AutoSize = true;
            this.lblClient.Location = new System.Drawing.Point(12, 44);
            this.lblClient.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblClient.Name = "lblClient";
            this.lblClient.Size = new System.Drawing.Size(67, 25);
            this.lblClient.TabIndex = 16;
            this.lblClient.Text = "Client";
            // 
            // lblEnvironment
            // 
            this.lblEnvironment.AutoSize = true;
            this.lblEnvironment.Location = new System.Drawing.Point(12, 162);
            this.lblEnvironment.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblEnvironment.Name = "lblEnvironment";
            this.lblEnvironment.Size = new System.Drawing.Size(132, 25);
            this.lblEnvironment.TabIndex = 17;
            this.lblEnvironment.Text = "Environment";
            // 
            // lstEnvironment
            // 
            this.lstEnvironment.CheckBoxes = true;
            this.lstEnvironment.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ID,
            this.Level,
            this.UrlPrefix});
            this.lstEnvironment.HideSelection = false;
            this.lstEnvironment.Location = new System.Drawing.Point(18, 192);
            this.lstEnvironment.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.lstEnvironment.Name = "lstEnvironment";
            this.lstEnvironment.Size = new System.Drawing.Size(554, 314);
            this.lstEnvironment.TabIndex = 19;
            this.lstEnvironment.UseCompatibleStateImageBehavior = false;
            this.lstEnvironment.View = System.Windows.Forms.View.Details;
            // 
            // Level
            // 
            this.Level.Width = 76;
            // 
            // UrlPrefix
            // 
            this.UrlPrefix.Width = 138;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox1.Controls.Add(this.chkAutoProvision);
            this.groupBox1.Controls.Add(this.ddlClient);
            this.groupBox1.Controls.Add(this.lstEnvironment);
            this.groupBox1.Controls.Add(this.lblClient);
            this.groupBox1.Controls.Add(this.lblEnvironment);
            this.groupBox1.Location = new System.Drawing.Point(16, 58);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupBox1.Size = new System.Drawing.Size(588, 521);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "1. Client and Environments";
            // 
            // chkAutoProvision
            // 
            this.chkAutoProvision.AutoSize = true;
            this.chkAutoProvision.Checked = true;
            this.chkAutoProvision.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoProvision.Location = new System.Drawing.Point(378, 154);
            this.chkAutoProvision.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.chkAutoProvision.Name = "chkAutoProvision";
            this.chkAutoProvision.Size = new System.Drawing.Size(194, 29);
            this.chkAutoProvision.TabIndex = 20;
            this.chkAutoProvision.Text = "Auto-provision?";
            this.chkAutoProvision.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnFindCertificate);
            this.groupBox2.Controls.Add(this.lblFileName);
            this.groupBox2.Controls.Add(this.lblCertificateName);
            this.groupBox2.Controls.Add(this.txtFriendlyName);
            this.groupBox2.Controls.Add(this.ddlHashType);
            this.groupBox2.Controls.Add(this.lblHashType);
            this.groupBox2.Controls.Add(this.txtSsoEndpoint);
            this.groupBox2.Controls.Add(this.lblSsoEndpoint);
            this.groupBox2.Controls.Add(this.chkSignRequest);
            this.groupBox2.Controls.Add(this.lblSloEndpoint);
            this.groupBox2.Controls.Add(this.txtSloEndpoint);
            this.groupBox2.Location = new System.Drawing.Point(616, 58);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupBox2.Size = new System.Drawing.Size(532, 590);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "2. Certificate Information";
            // 
            // grpStatus
            // 
            this.grpStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpStatus.Controls.Add(this.lblStatus);
            this.grpStatus.Location = new System.Drawing.Point(16, 660);
            this.grpStatus.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.grpStatus.Size = new System.Drawing.Size(1419, 129);
            this.grpStatus.TabIndex = 22;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "Status: ";
            // 
            // btnDownloadCertificate
            // 
            this.btnDownloadCertificate.Location = new System.Drawing.Point(24, 54);
            this.btnDownloadCertificate.Margin = new System.Windows.Forms.Padding(6);
            this.btnDownloadCertificate.Name = "btnDownloadCertificate";
            this.btnDownloadCertificate.Size = new System.Drawing.Size(205, 46);
            this.btnDownloadCertificate.TabIndex = 13;
            this.btnDownloadCertificate.Text = "Download Certs";
            this.btnDownloadCertificate.UseVisualStyleBackColor = true;
            this.btnDownloadCertificate.Click += new System.EventHandler(this.btnDownloadCertificate_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnDownloadCertificate);
            this.groupBox3.Location = new System.Drawing.Point(1167, 62);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(268, 586);
            this.groupBox3.TabIndex = 23;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Global Options";
            // 
            // AddSsoCertificate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1459, 815);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSave);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "AddSsoCertificate";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Add SSO Settings";
            this.Load += new System.EventHandler(this.AddSsoCertificate_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dsClient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.grpStatus.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnFindCertificate;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.Label lblSsoEndpoint;
        private System.Windows.Forms.Label lblSloEndpoint;
        private System.Windows.Forms.TextBox txtFriendlyName;
        private System.Windows.Forms.Label lblCertificateName;
        private System.Windows.Forms.TextBox txtSsoEndpoint;
        private System.Windows.Forms.TextBox txtSloEndpoint;
        private System.Windows.Forms.CheckBox chkSignRequest;
        private System.Windows.Forms.Label lblHashType;
        private System.Windows.Forms.ComboBox ddlHashType;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox ddlClient;
        private System.Windows.Forms.Label lblClient;
        private System.Data.DataSet dsClient;
        private System.Data.DataTable dataTable1;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Windows.Forms.Label lblEnvironment;
        private System.Data.DataTable dataTable2;
        private System.Data.DataColumn dataColumn3;
        private System.Data.DataColumn dataColumn4;
        private System.Windows.Forms.ListView lstEnvironment;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.ColumnHeader Level;
        private System.Windows.Forms.ColumnHeader UrlPrefix;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.CheckBox chkAutoProvision;
		private System.Windows.Forms.Button btnDownloadCertificate;
		private System.Windows.Forms.GroupBox groupBox3;
	}
}