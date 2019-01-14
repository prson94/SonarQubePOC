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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.chkAutoProvision = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dsClient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.grpStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnFindCertificate
            // 
            this.btnFindCertificate.Location = new System.Drawing.Point(6, 30);
            this.btnFindCertificate.Name = "btnFindCertificate";
            this.btnFindCertificate.Size = new System.Drawing.Size(128, 24);
            this.btnFindCertificate.TabIndex = 0;
            this.btnFindCertificate.Text = "Find Certificate File";
            this.btnFindCertificate.UseVisualStyleBackColor = true;
            this.btnFindCertificate.Click += new System.EventHandler(this.btnFindCertificate_Click);
            // 
            // lblFileName
            // 
            this.lblFileName.Location = new System.Drawing.Point(9, 57);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(251, 27);
            this.lblFileName.TabIndex = 1;
            // 
            // lblSsoEndpoint
            // 
            this.lblSsoEndpoint.AutoSize = true;
            this.lblSsoEndpoint.Location = new System.Drawing.Point(4, 201);
            this.lblSsoEndpoint.Name = "lblSsoEndpoint";
            this.lblSsoEndpoint.Size = new System.Drawing.Size(90, 13);
            this.lblSsoEndpoint.TabIndex = 6;
            this.lblSsoEndpoint.Text = "SSO Endpoint Url";
            // 
            // lblSloEndpoint
            // 
            this.lblSloEndpoint.AutoSize = true;
            this.lblSloEndpoint.Location = new System.Drawing.Point(3, 258);
            this.lblSloEndpoint.Name = "lblSloEndpoint";
            this.lblSloEndpoint.Size = new System.Drawing.Size(89, 13);
            this.lblSloEndpoint.TabIndex = 7;
            this.lblSloEndpoint.Text = "SLO Endpoint Url";
            // 
            // txtFriendlyName
            // 
            this.txtFriendlyName.Location = new System.Drawing.Point(6, 162);
            this.txtFriendlyName.MaxLength = 250;
            this.txtFriendlyName.Name = "txtFriendlyName";
            this.txtFriendlyName.Size = new System.Drawing.Size(254, 20);
            this.txtFriendlyName.TabIndex = 4;
            // 
            // lblCertificateName
            // 
            this.lblCertificateName.AutoSize = true;
            this.lblCertificateName.Location = new System.Drawing.Point(3, 146);
            this.lblCertificateName.Name = "lblCertificateName";
            this.lblCertificateName.Size = new System.Drawing.Size(124, 13);
            this.lblCertificateName.TabIndex = 5;
            this.lblCertificateName.Text = "Certificate Friendly Name";
            // 
            // txtSsoEndpoint
            // 
            this.txtSsoEndpoint.Location = new System.Drawing.Point(6, 217);
            this.txtSsoEndpoint.Name = "txtSsoEndpoint";
            this.txtSsoEndpoint.Size = new System.Drawing.Size(254, 20);
            this.txtSsoEndpoint.TabIndex = 8;
            // 
            // txtSloEndpoint
            // 
            this.txtSloEndpoint.Location = new System.Drawing.Point(7, 273);
            this.txtSloEndpoint.Name = "txtSloEndpoint";
            this.txtSloEndpoint.Size = new System.Drawing.Size(253, 20);
            this.txtSloEndpoint.TabIndex = 9;
            // 
            // chkSignRequest
            // 
            this.chkSignRequest.AutoSize = true;
            this.chkSignRequest.Location = new System.Drawing.Point(139, 113);
            this.chkSignRequest.Name = "chkSignRequest";
            this.chkSignRequest.Size = new System.Drawing.Size(121, 17);
            this.chkSignRequest.TabIndex = 10;
            this.chkSignRequest.Text = "Sign SSO Request?";
            this.chkSignRequest.UseVisualStyleBackColor = true;
            // 
            // lblHashType
            // 
            this.lblHashType.AutoSize = true;
            this.lblHashType.Location = new System.Drawing.Point(9, 95);
            this.lblHashType.Name = "lblHashType";
            this.lblHashType.Size = new System.Drawing.Size(105, 13);
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
            this.ddlHashType.Location = new System.Drawing.Point(6, 111);
            this.ddlHashType.Name = "ddlHashType";
            this.ddlHashType.Size = new System.Drawing.Size(127, 21);
            this.ddlHashType.TabIndex = 12;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSave.Location = new System.Drawing.Point(8, 314);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Location = new System.Drawing.Point(7, 16);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(550, 36);
            this.lblStatus.TabIndex = 14;
            // 
            // ddlClient
            // 
            this.ddlClient.DataSource = this.dsClient;
            this.ddlClient.DisplayMember = "Table1.Name";
            this.ddlClient.FormattingEnabled = true;
            this.ddlClient.Location = new System.Drawing.Point(6, 42);
            this.ddlClient.Name = "ddlClient";
            this.ddlClient.Size = new System.Drawing.Size(282, 21);
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
            this.lblClient.Location = new System.Drawing.Point(6, 23);
            this.lblClient.Name = "lblClient";
            this.lblClient.Size = new System.Drawing.Size(33, 13);
            this.lblClient.TabIndex = 16;
            this.lblClient.Text = "Client";
            // 
            // lblEnvironment
            // 
            this.lblEnvironment.AutoSize = true;
            this.lblEnvironment.Location = new System.Drawing.Point(6, 84);
            this.lblEnvironment.Name = "lblEnvironment";
            this.lblEnvironment.Size = new System.Drawing.Size(66, 13);
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
            this.lstEnvironment.Location = new System.Drawing.Point(9, 100);
            this.lstEnvironment.Name = "lstEnvironment";
            this.lstEnvironment.Size = new System.Drawing.Size(279, 165);
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
            this.groupBox1.Location = new System.Drawing.Point(8, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(294, 271);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "1. Client and Environments";
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
            this.groupBox2.Location = new System.Drawing.Point(308, 30);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(266, 307);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "2. Certificate Information";
            // 
            // grpStatus
            // 
            this.grpStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpStatus.Controls.Add(this.lblStatus);
            this.grpStatus.Location = new System.Drawing.Point(8, 343);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(566, 67);
            this.grpStatus.TabIndex = 22;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "Status: ";
            // 
            // chkAutoProvision
            // 
            this.chkAutoProvision.AutoSize = true;
            this.chkAutoProvision.Checked = true;
            this.chkAutoProvision.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoProvision.Location = new System.Drawing.Point(189, 80);
            this.chkAutoProvision.Name = "chkAutoProvision";
            this.chkAutoProvision.Size = new System.Drawing.Size(99, 17);
            this.chkAutoProvision.TabIndex = 20;
            this.chkAutoProvision.Text = "Auto-provision?";
            this.chkAutoProvision.UseVisualStyleBackColor = true;
            // 
            // AddSsoCertificate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 424);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSave);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
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
    }
}