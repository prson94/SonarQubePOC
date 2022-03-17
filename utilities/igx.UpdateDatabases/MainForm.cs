using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;

using Dapper;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SpreadsheetLight;

namespace igx.UpdateDatabases
{
    public partial class MainForm : Form
    {
        private List<CompanyWithDatabaseServerSettings> Companies;

        public bool SelectOnly => bool.Parse(ConfigurationManager.AppSettings["SelectOnly"]);

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
                .ToList()
                .ForEach(c =>
                {
                    lbDatabases.Items.Add($"{c.CompanyID} - {c.UrlPrefix}", CheckState.Unchecked);
                });

            if (SelectOnly)
            {
                chkSaveResultsInJson.Checked = true;
            }
        }

        private void BackgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            btnRun.Invoke((MethodInvoker)delegate
            {
                btnRun.Enabled = false;
            });

            txtMessages.Invoke((MethodInvoker)delegate
            {
                txtMessages.Text = "";
            });

            var sql = "";

            txtSql.Invoke((MethodInvoker)delegate
            {
                sql = txtSql.Text;
            });

            if (string.IsNullOrEmpty(txtSql.Text))
            {
                lblSqlValidation.Invoke((MethodInvoker)delegate
                {
                    lblSqlValidation.Text = "Sql Statement cannot be empty.";
                });
            }
            else
            {
                lblSqlValidation.Invoke((MethodInvoker)delegate
                {
                    lblSqlValidation.Text = "";
                });

                var sqlStatements = sql.Split(new string[] { "GO;" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                var count = lbDatabases.CheckedItems.Count;

                var results = new List<Result>();

                #region

                var document = new SLDocument();

                var rowNumbers = new Dictionary<string, int>();
                int ix = 1;
                foreach (var s in sqlStatements)
                {
                    rowNumbers.Add($"Query {ix}", 2);
                    document.AddWorksheet($"Query {ix}");
                    ix++;
                }

                document.DeleteWorksheet(SLDocument.DefaultFirstSheetName);

                #endregion

                for (var i = 0; i < count; i++)
                {
                    var prefix = (string)lbDatabases.CheckedItems[i];
                    var c = Companies.FirstOrDefault(o => prefix == $"{o.CompanyID} - {o.UrlPrefix}");
                    
                    if (c != null)
                    {
                        var result = new Result { Server = c.Server, DatabaseName = $"D3S_{c.CompanyID}", UrlPrefix = c.UrlPrefix, StartedOn = DateTime.Now, Queries = new List<DatabaseResult>() };
                        var cnn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID);
                        try
                        {
                            ix = 1;
                            var writeColumnHeaders = true;
                            sqlStatements.ForEach(s =>
                            {
                                var queryName = $"Query {ix}";
                                document.SelectWorksheet(queryName);

                                document.SetCellValue(1, 1, "Server");
                                document.SetCellValue(1, 2, "Url Prefix");
                                document.SetCellValue(1, 3, "Database");

                                var databaseResult = new DatabaseResult { QueryText = s };
                                cnn.Open();

                                if (SelectOnly)
                                {
                                    var items = cnn.Query<dynamic>(s, commandTimeout: 12000).ToList();
                                    databaseResult.Results = JsonConvert.DeserializeObject<JArray>(JsonConvert.SerializeObject(items));

                                    int columnNumber = 4;
                                    foreach (JObject dbResult in databaseResult.Results)
                                    {
                                        if (writeColumnHeaders)
                                        {
                                            foreach (JProperty prop in dbResult.Properties())
                                            {
                                                document.SetCellValue(1, columnNumber, prop.Name);
                                                columnNumber++;
                                            }
                                            writeColumnHeaders = false;
                                        }

                                        document.SetCellValue(rowNumbers[queryName], 1, result.Server);
                                        document.SetCellValue(rowNumbers[queryName], 2, result.UrlPrefix);
                                        document.SetCellValue(rowNumbers[queryName], 3, result.DatabaseName);
                                        columnNumber = 4;
                                        foreach (JProperty prop in dbResult.Properties())
                                        {
                                            document.SetCellValue(rowNumbers[queryName], columnNumber, prop.Value.ToString());
                                            columnNumber++;
                                        }

                                        rowNumbers[queryName]++;
                                    }
                                }
                                else
                                {
                                    var cmd = new System.Data.SqlClient.SqlCommand
                                    {
                                        CommandText = s,
                                        Connection = cnn,
                                        CommandTimeout = 12000,
                                        CommandType = CommandType.Text
                                    };
                                    cmd.ExecuteNonQuery();
                                }
                                cnn.Close();
                                result.Queries.Add(databaseResult);

                                writeColumnHeaders = true;
                                ix++;
                            });

                            txtMessages.Invoke((MethodInvoker)delegate
                            {
                                txtMessages.Text += $"SUCCESS: {c.UrlPrefix} ({c.CompanyID}){System.Environment.NewLine}";
                            });
                        }
                        catch (Exception ex)
                        {
                            result.Message = ex.GetFullExceptionData();
                            txtMessages.Invoke((MethodInvoker)delegate
                            {
                                txtMessages.Text += $"ERROR: {c.UrlPrefix} ({c.CompanyID}){System.Environment.NewLine}{ex.GetFullExceptionData()}{System.Environment.NewLine}";
                            });
                        }
                        finally
                        {
                            result.CompletedOn = DateTime.Now;
                            results.Add(result);
                        }
                    }

                    var progress = (double)(i + 1) / count;
                    progress = progress * 100;
                    backgroundWorker1.ReportProgress((int)Math.Round(progress, 0));
                }

                if (chkSaveResultsInJson.Checked)
                {
                    File.WriteAllText($"Results_{DateTime.Now.ToString("yyyyMMdd.HHmmss")}.json", JsonConvert.SerializeObject(results, Formatting.Indented));
                    var stream = new FileStream($"Results_{DateTime.Now.ToString("yyyyMMdd.HHmmss")}.xlsx", FileMode.CreateNew);
                    document.SaveAs(stream);
                    stream.Close();
                }
            }

            btnRun.Invoke((MethodInvoker)delegate
            {
                btnRun.Enabled = true;
            });

            e.Result = true;
        }

        private void chkDevelopment_CheckedChanged(object sender, EventArgs e)
        {
            var chk = chkDevelopment.CheckState == CheckState.Checked;
            checkRelevantItems(chk, EnvironmentLevel.Development);
        }

        private void chkUat_CheckedChanged(object sender, EventArgs e)
        {
            var chk = chkUat.CheckState == CheckState.Checked;
            checkRelevantItems(chk, EnvironmentLevel.UAT);
        }

        private void chkProduction_CheckedChanged(object sender, EventArgs e)
        {
            var chk = chkProduction.CheckState == CheckState.Checked;
            checkRelevantItems(chk, EnvironmentLevel.Production);
        }

        private void checkRelevantItems(bool chk, EnvironmentLevel env)
        {
            Companies.ForEach(c =>
            {
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
            var chk = chkPreview.CheckState == CheckState.Checked;
            checkRelevantItems(chk, EnvironmentLevel.Nightly);
        }
    }
}
