using d360.core.enums;
using Dapper;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace igx.CertificateManager
{
    public partial class AddSsoCertificate : Form
    {
        string connectionString = ConfigurationManager.AppSettings["CommunityContext"];

        public AddSsoCertificate()
        {
            InitializeComponent();
        }

        private void btnFindCertificate_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                lblFileName.Text = openFileDialog1.FileName;
            }
            else
            {
                lblFileName.Text = string.Empty;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var errorMessages = "";
            var okToContinue = false;

            lblStatus.Text = "";
            btnSave.Enabled = false;

            if (string.IsNullOrEmpty(lblFileName.Text))
            {
                errorMessages += "File not selected.";
                okToContinue = false;
            }
            else
            {
                okToContinue = true;
            }

            if (lstEnvironment.CheckedItems.Count > 0)
            {
                okToContinue = true;
            }
            else
            {
                errorMessages += "No environments selected.";
                okToContinue = false;
            }

            HashAlgorithmType hash = HashAlgorithmType.SHA256;
            switch (ddlHashType.SelectedItem)
            {
                case "SHA1":
                    hash = HashAlgorithmType.SHA1;
                    okToContinue = true;
                    break;
                case "SHA224":
                    hash = HashAlgorithmType.SHA224;
                    okToContinue = true;
                    break;
                case "SHA256":
                    hash = HashAlgorithmType.SHA256;
                    okToContinue = true;
                    break;
                case "SHA384":
                    hash = HashAlgorithmType.SHA384;
                    okToContinue = true;
                    break;
                case "SHA512":
                    hash = HashAlgorithmType.SHA512;
                    okToContinue = true;
                    break;
                default:
                    errorMessages += "Invalid hash algorithm selected.";
                    okToContinue = false;
                    break;
            }

            if (okToContinue)
            {
                var bytes = File.ReadAllBytes(lblFileName.Text);

                var cnn = new SqlConnection(connectionString);
                cnn.Open();

                var dcID = cnn.ExecuteScalar<int>("insert into DomainCertificate (Name, [File]) values (@n, @f); select SCOPE_IDENTITY()", new { n = txtFriendlyName.Text, f = bytes });

                if (dcID > 0)
                {
                    var dsID = cnn.ExecuteScalar<int>(@"insert into DomainSetting (
                            IdpSsoEndpoint, IdpSloEndpoint, IdpDomainCertificateID, HashAlgorithmType, SignInitialSSORequest
                        ) values (
                            @sso, @slo, @dc, @hat, @sign
                        ); select SCOPE_IDENTITY()", new
                    {
                        sso = txtSsoEndpoint.Text,
                        slo = txtSloEndpoint.Text,
                        dc = dcID,
                        hat = (int)hash,
                        sign = chkSignRequest.Checked
                    });

                    if (dsID > 0)
                    {
                        var numberOfUpdatedConfigs = 0;
                        for (int i = 0; i < lstEnvironment.CheckedItems.Count; i++)
                        {
                            var checkedItem = lstEnvironment.CheckedItems[i];
                            var environmentID = int.Parse(checkedItem.SubItems[0].Text);
                            var url = checkedItem.SubItems[2].Text;

                            cnn.Execute(
                                @"update CompanyDomainSetting set DomainSettingID = @dsID, AuthenticationType = 2, AllowNewUserLogin = @ap where CompanyID = @environmentID and UrlPrefix = @url", 
                                new { dsID, ap = chkAutoProvision.Checked, environmentID, url }
                            );

                            numberOfUpdatedConfigs++;
                        }

                        lblStatus.Text = $"Successfully saved certificate and domain settings, and updated {numberOfUpdatedConfigs} environment URL(s).";
                    }
                }
            }

            if (!string.IsNullOrEmpty(errorMessages))
            {
                lblStatus.Text = errorMessages;
            }

            btnSave.Enabled = true;
        }

        private void AddSsoCertificate_Load(object sender, EventArgs e)
        {
            var da = new SqlDataAdapter("select ID, Name from Client order by Name", connectionString);
            da.Fill(dsClient.Tables[0]);
            var emptyRow = dsClient.Tables[0].NewRow();
            emptyRow.ItemArray[0] = 0;
            dsClient.Tables[0].Rows.InsertAt(emptyRow, 0);
        }

        private void ddlClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            dsClient.Tables[1].Clear();
            lstEnvironment.Items.Clear();

            var clientID = ddlClient.SelectedValue;

            if (clientID is int)
            {
                var da = new SqlDataAdapter($"select EnvironmentID as ID, EnvironmentLevel as Name, S.UrlPrefix from ClientEnvironment E inner join CompanyDomainSetting S on S.CompanyID = E.EnvironmentID where ClientID = {clientID} order by EnvironmentLevel", connectionString);
                da.Fill(dsClient.Tables[1]);

                foreach (DataRow dr in dsClient.Tables[1].Rows)
                {
                    ListViewItem lvi = new ListViewItem(new string[] { dr["ID"].ToString(), dr["Name"].ToString(), dr["UrlPrefix"].ToString() });
                    lstEnvironment.Items.Add(lvi);
                }
            }
        }
    }
}
