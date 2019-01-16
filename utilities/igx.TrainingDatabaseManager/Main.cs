using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using Dapper;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;

namespace igx.TrainingDatabaseManager
{
    public partial class Main : Form
    {
        string connectionString = ConfigurationManager.AppSettings["CommunityContext"];
        List<TrainingEnvironment> Environments;

        public Main()
        {
            InitializeComponent();
        }

        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAll.Checked)
            {
                chkAll.Text = "None?";
                for (int i = 0; i < lst.Items.Count; i++)
                {
                    lst.Items[i].Checked = true;
                }
            }
            else
            {
                chkAll.Text = "All?";
                for (int i = 0; i < lst.Items.Count; i++)
                {
                    lst.Items[i].Checked = false;
                }
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var cnn = new SqlConnection(connectionString);

            progressBar1.Visible = false;

            ColumnHeader columnHeader1 = new ColumnHeader();
            columnHeader1.Text = "Column1";
            this.lst.Columns.AddRange(new ColumnHeader[] { columnHeader1 });

            try
            {
                cnn.Open();
                Environments = cnn.Query<TrainingEnvironment>(@"
select	S.CompanyID, 
		S.UrlPrefix,
		D.Server,
		D.Username,
		D.Password 
from	CompanyDomainSetting S
		inner join Company C on C.ID = S.CompanyID
		inner join DatabaseServer D on D.ID = C.DatabaseServerID
where	S.UrlPrefix like 'train%' and S.UrlPrefix not like '%-forms' and S.UrlPrefix not like '%dev' and S.IsPrimary = 1").ToList();
                Environments.ForEach(o =>
                {
                    lst.Items.Add(new ListViewItem(o.UrlPrefix));
                    //lst.Items.Add(o.CompanyID.ToString(), o.UrlPrefix);
                });
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                cnn.Close();
                cnn.Dispose();
            }
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            lblMessages.Text = "";

            for (int i = 0; i < lst.Items.Count; i++)
            {
                if (lst.Items[i].Checked)
                {
                    Environments.Single(c => c.UrlPrefix == lst.Items[i].Text).Selected = true;
                }
            }

            btnClean.Enabled = false;
            progressBar1.Visible = true;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = Environments.Count(i => i.Selected);

            Environments.ForEach(c =>
            {
                if (c.Selected)
                {
                    SqlConnection companyConnection = null;
                    try
                    {
                        companyConnection = new SqlConnection($"Server=tcp:{c.Server};Database=D3S_{c.CompanyID};User ID={c.Username};Password={c.Password};Trusted_Connection=False;MultipleActiveResultSets=True;");
                        companyConnection.Open();

                        try {
                            companyConnection.Execute("[utility].[ClearDatabase]");
                        }
                        catch { }
                        try {
                            companyConnection.Execute("[utility].[ClearDatabase]");
                        }
                        catch { }
                        try {
                            companyConnection.Execute("[utility].[ClearDatabase]");
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        lblMessages.ForeColor = Color.Red;
                        lblMessages.Text = ex.Message;
                    }
                    finally
                    {
                        if (companyConnection != null)
                        {
                            companyConnection.Close();
                            companyConnection.Dispose();
                        }
                    }
                    c.Selected = false;
                    progressBar1.Increment(1);
                }
            });

            if (string.IsNullOrEmpty(lblMessages.Text))
            {
                lblMessages.ForeColor = Color.Green;
                lblMessages.Text = "Successfully cleaned environments.";
            }
            
            btnClean.Enabled = true;
        }
    }
    public class TrainingEnvironment
    {
        public int CompanyID { get; set; }
        public string UrlPrefix { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public bool Selected { get; set; }

    }

}
