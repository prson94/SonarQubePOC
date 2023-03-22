using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace igx.jobs.reportlayer
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage()
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public static class ReportLayerGenerator
    {
        #region Utility

        static string cleanObjectName(string name)
        {
            name = name.Replace("'", "").Replace(" ", "").Replace("-", "").Replace("&", "And").Replace(":", "").Replace(";", "").Trim();
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            name = rgx.Replace(name, "");
            return name;
        }


        static void executeSqlWithTry(SqlConnection companyConnection, string viewSql)
        {
            try
            {
                companyConnection.Execute(viewSql.ToString());
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, null, new Dictionary<string, string>() { { "Attempted SQL: ", viewSql } });
            }
        }

        #endregion

        const string functionName = "ReportingLayer_Generate";

#if DEBUG
        const string timerSettings = "*/1 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 3).ToList();
#endif

                companies.ForEach(c =>
                {
                    var synonymNames = new List<string>();
                    var viewNames = new List<string>();
                    string SCHEMA = "reporting";

                    try
                    {
                        using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            companyConnection.Open();

                            var selectSql = "";
                            var objectName = "";

                            #region General Views

                            #region REPORING USERS (dynamic)

                            var fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'ResourceType'").ToList();
                            var fjoins = string.Empty;
                            var ffields = string.Empty;
                            fieldTypes.ForEach(f =>
                            {  
                                fjoins += $@" left join field as [Type_{f.ID}] on 
                                        [Type_{f.ID}].fieldtypeId={f.ID} and [Type_{f.ID}].AssetID=A.ID ";
                                ffields += $@",[Type_{f.ID}].FormattedValue as [{f.FriendlyName}]";
                            });

                            objectName = $"{SCHEMA}.[Users]";
                            viewNames.Add(objectName);

                            selectSql = $@"select 
                                    r.FirstName ,
                                    r.LastName ,
                                    r.Email, 
                                    r.ResourceID,
                                    '/Resource/' + cast(r.ResourceID as varchar(250)) as ResourceURI,
                                    r.LastLoggedInOn as DateLastLoggedIn, 
                                    case when r.[State] = 1 then 'Active' else 'Inactive' end as [Status], 
                                    r.LastLoggedInOn, 
                                    r.[State], 
                                    r.IsAdministrator
                                    {ffields}
                                    from reporting.Global_Resource as r
									inner join Asset A on r.uid = A.Uid
                                    {fjoins}";

                            executeSqlWithTry(companyConnection, $@"CREATE OR ALTER VIEW {objectName} AS {selectSql}");

                            #endregion

                            #endregion

                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.BusinessAsset, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.TechnicalAsset, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.Model, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.Policy, viewNames, log);

                            RemoveSynonyms(companyConnection, synonymNames, log, SCHEMA);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        private static void RemoveSynonyms(SqlConnection companyConnection, List<string> synonymNames, TextWriter log, string schemaName)
        {
            var currentSynonyms = companyConnection.Query<string>(@"select name from sys.synonyms where base_object_name like '%reporting%' and base_object_name not in (select '[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']' from [INFORMATION_SCHEMA].[VIEWS] where TABLE_SCHEMA = 'reporting')").ToList();

            currentSynonyms.ForEach(cv =>
            {
                cv = $"{schemaName}.{cv}";

                if (!synonymNames.Contains(cv))
                {
                    try
                    {
                        companyConnection.Execute(string.Format(@"drop synonym {0}", cv));
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                        log.WriteLine(msg);
                    }
                }
            });

        }

        private static void RemoveOldDynamicViews(SqlConnection connection, AssetTypeClass className, List<string> viewNames, TextWriter log)
        {
            var currentViewNames = connection.Query<string>($@"select TABLE_SCHEMA + '.[' + TABLE_NAME + ']' from [INFORMATION_SCHEMA].[VIEWS] where TABLE_SCHEMA = 'reporting' and TABLE_NAME like '{className}_%' and TABLE_NAME not in('model_all','model_fields', 'ModelInterRelationships','policy_all')").ToList();
                        
            currentViewNames.ForEach(cv =>
            {
                if (!viewNames.Contains(cv))
                {
                    try
                    {
                        connection.Execute(string.Format(@"drop view {0}", cv));
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                        log.WriteLine(msg);
                    }
                }
            });
        }
    }
}
