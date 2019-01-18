namespace igx.GovernRestClient
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.ApiKey = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.UriText = new System.Windows.Forms.TextBox();
            this.ApiSecret = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.HttpChunked = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Method = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.JsonRequestFilePath = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SelectJsonButton = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.ApiResponseJson = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.ErrorText = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveUserDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ApiKey
            // 
            this.ApiKey.Location = new System.Drawing.Point(9, 43);
            this.ApiKey.Name = "ApiKey";
            this.ApiKey.Size = new System.Drawing.Size(305, 20);
            this.ApiKey.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.UriText);
            this.groupBox1.Controls.Add(this.ApiSecret);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.HttpChunked);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.Method);
            this.groupBox1.Controls.Add(this.ApiKey);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(12, 36);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(776, 131);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "API Credentials";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(355, 75);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Uri";
            // 
            // UriText
            // 
            this.UriText.Location = new System.Drawing.Point(358, 91);
            this.UriText.Name = "UriText";
            this.UriText.Size = new System.Drawing.Size(412, 20);
            this.UriText.TabIndex = 8;
            // 
            // ApiSecret
            // 
            this.ApiSecret.Location = new System.Drawing.Point(6, 91);
            this.ApiSecret.Name = "ApiSecret";
            this.ApiSecret.PasswordChar = '*';
            this.ApiSecret.Size = new System.Drawing.Size(308, 20);
            this.ApiSecret.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "API Secret";
            // 
            // HttpChunked
            // 
            this.HttpChunked.AutoSize = true;
            this.HttpChunked.Location = new System.Drawing.Point(607, 47);
            this.HttpChunked.Name = "HttpChunked";
            this.HttpChunked.Size = new System.Drawing.Size(101, 17);
            this.HttpChunked.TabIndex = 6;
            this.HttpChunked.Text = "HTTP Chunked";
            this.HttpChunked.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "API Key";
            // 
            // Method
            // 
            this.Method.FormattingEnabled = true;
            this.Method.Items.AddRange(new object[] {
            "GET",
            "DELETE",
            "POST",
            "PUT"});
            this.Method.Location = new System.Drawing.Point(358, 43);
            this.Method.Name = "Method";
            this.Method.Size = new System.Drawing.Size(243, 21);
            this.Method.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(355, 27);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Method";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.JsonRequestFilePath);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.SelectJsonButton);
            this.groupBox3.Location = new System.Drawing.Point(13, 173);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(374, 319);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Json Request";
            // 
            // JsonRequestFilePath
            // 
            this.JsonRequestFilePath.Location = new System.Drawing.Point(119, 46);
            this.JsonRequestFilePath.Name = "JsonRequestFilePath";
            this.JsonRequestFilePath.ReadOnly = true;
            this.JsonRequestFilePath.Size = new System.Drawing.Size(249, 20);
            this.JsonRequestFilePath.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(116, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "File Path";
            // 
            // SelectJsonButton
            // 
            this.SelectJsonButton.Location = new System.Drawing.Point(8, 43);
            this.SelectJsonButton.Name = "SelectJsonButton";
            this.SelectJsonButton.Size = new System.Drawing.Size(105, 23);
            this.SelectJsonButton.TabIndex = 0;
            this.SelectJsonButton.Text = "Select Json File";
            this.SelectJsonButton.UseVisualStyleBackColor = true;
            this.SelectJsonButton.Click += new System.EventHandler(this.SelectJsonButton_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.ApiResponseJson);
            this.groupBox4.Location = new System.Drawing.Point(414, 173);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(374, 319);
            this.groupBox4.TabIndex = 4;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Json Response";
            // 
            // ApiResponseJson
            // 
            this.ApiResponseJson.Location = new System.Drawing.Point(7, 20);
            this.ApiResponseJson.Multiline = true;
            this.ApiResponseJson.Name = "ApiResponseJson";
            this.ApiResponseJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ApiResponseJson.Size = new System.Drawing.Size(361, 293);
            this.ApiResponseJson.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(13, 499);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(99, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "Send Request";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ErrorText
            // 
            this.ErrorText.ForeColor = System.Drawing.Color.Red;
            this.ErrorText.Location = new System.Drawing.Point(119, 504);
            this.ErrorText.Name = "ErrorText";
            this.ErrorText.Size = new System.Drawing.Size(669, 23);
            this.ErrorText.TabIndex = 6;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveUserDataToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveUserDataToolStripMenuItem
            // 
            this.saveUserDataToolStripMenuItem.Name = "saveUserDataToolStripMenuItem";
            this.saveUserDataToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveUserDataToolStripMenuItem.Text = "Save User Data";
            this.saveUserDataToolStripMenuItem.Click += new System.EventHandler(this.saveUserDataToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 533);
            this.Controls.Add(this.ErrorText);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Govern REST Client";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ApiKey;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox UriText;
        private System.Windows.Forms.TextBox ApiSecret;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox HttpChunked;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Method;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button SelectJsonButton;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox ApiResponseJson;
        private System.Windows.Forms.TextBox JsonRequestFilePath;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label ErrorText;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveUserDataToolStripMenuItem;
    }
}

