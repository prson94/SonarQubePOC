using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace igx.UpdateDatabases
{
    public partial class MainForm : Form
    {
        List<CompanyWithDatabaseServerSettings> Companies;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            Companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();
            Companies
                .OrderBy(i => i.CompanyID)  
                //.ThenBy(i => i.CompanyID)
                //.ThenBy(i => i.UrlPrefix)
                .ToList()
                .ForEach(c => {
                    lbDatabases.Items.Add($"{c.CompanyID} - {c.UrlPrefix}", CheckState.Unchecked);
                });
        }

        private void BackgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            btnRun.Invoke((MethodInvoker)delegate {
                btnRun.Enabled = false;
            });

            txtMessages.Invoke((MethodInvoker)delegate {
                txtMessages.Text = "";
            });

            var sql = "";

            txtSql.Invoke((MethodInvoker)delegate {
                sql = txtSql.Text;
            });

            if (string.IsNullOrEmpty(txtSql.Text))
            {
                lblSqlValidation.Invoke((MethodInvoker)delegate {
                    lblSqlValidation.Text = "Sql Statement cannot be empty.";
                });
            }
            else
            {
                lblSqlValidation.Invoke((MethodInvoker)delegate {
                    lblSqlValidation.Text = "";
                });

                var sqlStatements = sql.Split(new string[] { "GO;" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                var count = lbDatabases.CheckedItems.Count;

                for (var i = 0; i < count; i++)
                {
                    var prefix = (string)lbDatabases.CheckedItems[i];
                    var c = Companies.FirstOrDefault(o => prefix == $"{o.CompanyID} - {o.UrlPrefix}");
                    if (c != null)
                    {
                        var cnn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID);
                        //var cmd = new System.Data.SqlClient.SqlCommand();
                        try
                        {
                            //
                            sqlStatements.ForEach(s => {
                                cnn.Open();
                                var cmd = new System.Data.SqlClient.SqlCommand();
                                cmd.CommandText = s;
                                cmd.Connection = cnn;
                                cmd.CommandTimeout = 1200;
                                cmd.CommandType = CommandType.Text;
                                cmd.ExecuteNonQuery();
                                cnn.Close();

                                //cnn.Execute("sp_executesql @s", new { s }, commandTimeout: 1200);
                            });
                            //
                            
                            txtMessages.Invoke((MethodInvoker)delegate {
                                txtMessages.Text += $"SUCCESS: {c.UrlPrefix} ({c.CompanyID}){System.Environment.NewLine}";
                            });
                        }
                        catch (Exception ex)
                        {
                            txtMessages.Invoke((MethodInvoker)delegate {
                                txtMessages.Text += $"ERROR: {c.UrlPrefix} ({c.CompanyID}){System.Environment.NewLine}{ex.GetFullExceptionData()}{System.Environment.NewLine}";
                            });
                        }
                    }
                    var progress = (double)(i + 1) / count;
                    progress = progress * 100;
                    backgroundWorker1.ReportProgress((int)Math.Round(progress, 0));
                }
            }

            btnRun.Invoke((MethodInvoker)delegate {
                btnRun.Enabled = true;
            });

            e.Result = true;
        }

        private void chkDevelopment_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkDevelopment.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.Development);
        }

        private void chkUat_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkUat.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.UAT);
        }

        private void chkProduction_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkProduction.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.Production);
        }

        private void checkRelevantItems(bool chk, EnvironmentLevel env)
        {
            Companies.ForEach(c => {
                if (c.EnvironmentLevel == env)
                {
                    for (var i = 0; i < lbDatabases.Items.Count; i++)
                    {
                        var item = (string)lbDatabases.Items[i];
                        if (item == $"{c.CompanyID} - {c.UrlPrefix}")
                        {
                            lbDatabases.SetItemChecked(i, chk);
                        }
                    }
                }
            });
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSql.Text = "";
        }

        private void chkPreview_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkPreview.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.Nightly);
        }

        private void chkAlternate_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkAlternate.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.Alternate);
        }

        private void chkLegacy_CheckedChanged(object sender, EventArgs e)
        {
            var chk = (chkLegacy.CheckState == CheckState.Checked);
            checkRelevantItems(chk, EnvironmentLevel.LegacyDevelopment);
        }
    }
}
