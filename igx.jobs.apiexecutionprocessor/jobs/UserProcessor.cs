using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class UserProcessor : BaseWebJob
	{
		private const string FUNCTION_NAME = "UserProcessor";
		private const string TIMER_SETTINGS = "0 */2 * * * *";

		public UserProcessor(IConfiguration config, ICommunity community) : base(community, config) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			try
			{
				var slot = GetEnvironmentLevelCurrentSlot();
				var tenants = await Community.ReadTenantConnectionSettingsByCurrentSlotAsync(slot);

				string roConnectionString = Configuration["ReadOnlyConnectionString"];
				using (var cnn = new SqlConnection(roConnectionString))
				{
					await cnn.OpenIfClosed();

					foreach (var c in tenants)
					{
						Guid executionUid = Guid.NewGuid();

						bool IsError = false;

						var logProperties = new Dictionary<string, object> {
							{ "Function", FUNCTION_NAME},
							{ "CompanyID", c.CompanyID },
							{ "UrlPrefix", c.UrlPrefix }
						};

						using (log.BeginScope(logProperties))
						{
							try
							{
								using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
								{
									await companyConnection.OpenIfClosed();

									var updatedResourceIDs = new HashSet<int>();

									#region Insert/Update Logic

									using (var transaction = companyConnection.BeginTransaction())
									{

									 int executionid = await companyConnection.ExecuteScalarAsync<int>(
											sql: @"
													declare @d datetime = getutcdate();
													insert into api.Execution (ExecutionID, ResourceID, Total, Processed, [Error], StartedOn, ProcessingStartedOn, CompletedOn, [Action],ApplicationID)
													values (@executionUid, 0, 0, 0, 0, @d, @d, null,16,'User_Synchronization_Process');

													select Id from api.Execution where ExecutionID = @executionUid;",
											transaction: transaction,param: new { executionUid});

										using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, transaction))
										{
											bulkCopy.BatchSize = 5000; //We may put this value to the configs, but I'm not sure it's valuable at this point.
											bulkCopy.DestinationTableName = "api.ExecutionItem";
											bulkCopy.BulkCopyTimeout = 300;

											int itemNumber = 0;

											using (DataTable table = PrepareSourceTable(bulkCopy))
											{
												using (var resources = await cnn.ExecuteReaderAsync(
												sql: @"select R.ID as ResourceID, 
														R.FirstName, 
														R.LastName, 
														C.LastLoggedInOn, 
														R.Email, 
														C.[State], 
														C.IsAdministrator,
														R.[uid],
														R.UpdatedOn
													from [Resource] R 
													inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @CompanyID",
													param: new { c.CompanyID }))
												{
													try
													{
														while (resources.Read())
														{
															var resourceId = (int)resources["ResourceID"];
															updatedResourceIDs.Add(resourceId);

															var row = table.NewRow();
															var jsonObject = JObject.Parse("{}");
															var state = (int)(resources["State"] ?? resources["State"]);

															itemNumber++;
															row["ExecutionId"] = executionid;
															row["ItemNumber"] = itemNumber;

															jsonObject.Add("Uid", (Guid)resources["uid"]);
															jsonObject.Add("ObjectID", resourceId);
															jsonObject.Add("Email",(string)resources["Email"]);
															jsonObject.Add("FirstName", (string)resources["FirstName"]);
															jsonObject.Add("LastName", (string)resources["LastName"]);
															jsonObject.Add("State", state);
															jsonObject.Add("IsAdministrator", (bool)resources["IsAdministrator"]);
															if (resources["LastLoggedInOn"] != DBNull.Value)
															{
																jsonObject.Add("LastLoggedInOn", (DateTime)resources["LastLoggedInOn"]);
															}

															if (resources["UpdatedOn"] != DBNull.Value)
															{
																jsonObject.Add("UpdatedOn", (DateTime)resources["UpdatedOn"]);
															}

															row["Properties"] = jsonObject.ToString();

															table.Rows.Add(row);

															//We read rows from DataReader and then send them to the server by 5000 items chanks.
															if (table.Rows.Count % 5000 == 0)
															{
																await bulkCopy.WriteToServerAsync(table);
																table.Rows.Clear();
															}
														}

														resources.Close();

														if (table.Rows.Count > 0)
														{
															await bulkCopy.WriteToServerAsync(table);
														}
													}
													catch (Exception ex)
													{
														if (!resources.IsClosed)
														{
															resources.Close();
														}
														IsError = true;
														log.LogError(ex, "When user data into temp table");
													}
												}
											}
										}

										int rowsAffected = await companyConnection.ExecuteScalarAsync<int>(
											sql: @"exec api.UpsertUsers @executionId, 1, 1
												   select Processed from api.Execution E where E.Id = @executionid;",
										param: new {executionid},
										transaction: transaction,
										commandTimeout: 300
										);

										log.LogInformation($"Found {updatedResourceIDs.Count} users for company {c.CompanyID}. Upsert affected {rowsAffected} rows.");

										transaction.Commit();
									}

									#endregion

									#region Delete Logic

									if (!IsError)
									{
										try
										{
											var currentResourceIDs = companyConnection.Query<int>("select ResourceID from reporting.Global_Resource").ToList();
											var toDeleteIds = new List<int>(currentResourceIDs.Except(updatedResourceIDs));

											//We need the following code because SQL Server allows us to send only 2100 parameters per query.
											int total = toDeleteIds.Count;
											while (toDeleteIds.Count > 0)
											{
												int take = toDeleteIds.Count > 1000 ? 1000 : toDeleteIds.Count;
												var idsToSend = toDeleteIds.Take(take).ToList();
												toDeleteIds.RemoveRange(0, take);
												companyConnection.Execute("delete reporting.Global_Resource where ResourceID in @idsToSend", new { idsToSend });
											}
											if (total > 0)
											{
												log.LogInformation("Removed {0} users for company {1}.", total, c.CompanyID);
											}
										}
										catch (Exception ex)
										{
											IsError = true;
											log.LogError(ex, "Delete Logic for user");
										}
									}

									if (!IsError)
									{
										try
										{
											companyConnection.Execute("delete ResponsibilityTypeRelationOverrideItem where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
											companyConnection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
										}
										catch (Exception ex)
										{
											log.LogError(ex, "Delete Logic for Manual/Automatic Responsibility");
										}
									}
									#endregion

								}
							}
							catch (Exception ex)
							{
								log.LogError(ex, "When processing users for company.");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME }
				};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error when recaching users.");
				}
			}
		}

		private static DataTable PrepareSourceTable(SqlBulkCopy bulkCopy)
		{
			var table = new DataTable();

			var columnName = "ExecutionId";
			table.Columns.Add(columnName, typeof(int));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "ItemNumber";
			table.Columns.Add(columnName, typeof(int));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "Properties";
			table.Columns.Add(columnName, typeof(string));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			return table;
		}
	}
}
