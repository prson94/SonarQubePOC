using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.jobs.reportlayer
{
	public class ReportLayerGenerator : BaseWebJob
	{
        const string FUNCTION_NAME = "ReportLayerGenerator";
        const string TIMER_SETTINGS = "0 */60 * * * *";

		public ReportLayerGenerator(IConfiguration config) : base(config)
		{

		}

		[FunctionName(FUNCTION_NAME)]
		public void Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            try
            {
                var companies = GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", c.CompanyID },
						{ "UrlPrefix", c.UrlPrefix }
					};

					using (log.BeginScope(logProperties))
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

								try
								{
									companyConnection.Execute($@"CREATE OR ALTER VIEW {objectName} AS {selectSql}");
								}
								catch (Exception ex)
								{
									log.LogError(ex, $"Error create or altering view: {objectName}");
								}

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
							log.LogError(ex, "Error when running report layer for environment.");
						}
					}
                });
            }
            catch (Exception ex)
            {
				var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME }
					};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error at the root of this web job.");
				}
            }
        }

        private void RemoveSynonyms(SqlConnection companyConnection, List<string> synonymNames, ILogger log, string schemaName)
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
						log.LogError(ex, "Error removing synonyms");
                    }
                }
            });
        }

        private void RemoveOldDynamicViews(SqlConnection connection, AssetTypeClass className, List<string> viewNames, ILogger log)
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
						log.LogError(ex, "Error removing dynamic views");
                    }
                }
            });
        }
    }
}
