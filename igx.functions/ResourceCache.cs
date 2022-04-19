using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.utils.company;

using Dapper;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace igx.functions.consumption
{
	public class ResourceCache
	{
		private const string functionName = "ResourceCache_Generate";
		private CoreFunction CoreFunction;

#if DEBUG
		private const string timerSettings = "*/2 * * * * *";
#else
		const string timerSettings = "0 */2 * * * *";
#endif

		[FunctionName(functionName)]
		public async Task Run([TimerTrigger(timerSettings)] TimerInfo myTimer, ExecutionContext context, TextWriter log)
		{
			var config = new ConfigurationBuilder()
				   .SetBasePath(context.FunctionAppDirectory)
				   .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
				   .AddEnvironmentVariables()
				   .Build();

			CoreFunction = new CoreFunction(config);

			try
			{
#if DEBUG
				var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 2).ToList();
#else
				var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

				using (var cnn = new SqlConnection(CoreFunction.GetConnectionString("CommunityContext")))
				{
					cnn.Open();

					foreach (var c in companies)
					{
						try
						{
							using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
							{
								companyConnection.Open();

								#region Get updated resources

								var resources = await cnn.ExecuteReaderAsync(
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
								param: new { c.CompanyID });
								var updatedResourceIDs = new HashSet<int>(resources.RecordsAffected > 0 ? resources.RecordsAffected : 0);

								#endregion

								#region Insert/Update Logic

								using (var transaction = companyConnection.BeginTransaction())
								{

									await companyConnection.ExecuteAsync(
										sql: @"IF OBJECT_ID('tempdb..#users') IS NOT NULL
											DROP TABLE #users;

										create table #users (                                            			                                
											ResourceID int not null primary key ,
											FirstName nvarchar(250) not null,
											LastName nvarchar(250) not null,
											LastLoggedInOn datetime null,
											Email nvarchar(500) not null,
											[State] int not null,
											IsAdministrator bit not null,
											[uid] uniqueidentifier not null,
											UpdatedOn datetime null
										);", 
										transaction: transaction);

									using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, transaction))
									{
										bulkCopy.BatchSize = 5000; //We may put this value to the configs, but I'm not sure it's valuable at this point.
										bulkCopy.DestinationTableName = "#users";
										bulkCopy.BulkCopyTimeout = 300;

										using (DataTable table = PrepareSourceTable(bulkCopy))
										{
											while (resources.Read())
											{
												var resourceId = resources.GetInt32("ResourceID");
												updatedResourceIDs.Add(resourceId);

												var row = table.NewRow();

												row["ResourceID"] = resourceId;
												row["FirstName"] = resources["FirstName"];
												row["LastName"] = resources["LastName"];
												row["LastLoggedInOn"] = resources["LastLoggedInOn"];
												row["Email"] = resources["Email"];
												row["State"] = resources["State"];
												row["IsAdministrator"] = resources["IsAdministrator"];
												row["uid"] = resources["uid"];
												row["UpdatedOn"] = resources["UpdatedOn"];

												table.Rows.Add(row);

												//We read rows from DataReader and then send them to the server by 5000 items chanks.
												if (table.Rows.Count % 5000 == 0)
												{
													await bulkCopy.WriteToServerAsync(table);
													table.Rows.Clear();
												}
											}

											if (table.Rows.Count > 0)
											{
												await bulkCopy.WriteToServerAsync(table);
											}
										}
									}

									int rowsAffected = await companyConnection.ExecuteAsync(
										sql: @"declare @mergeResults table ([action] varchar(50));
											merge	reporting.Global_Resource as T
											using	(
													select	ResourceID,
															FirstName,
															LastName,
															LastLoggedInOn,
															Email,
															[State],
															IsAdministrator,
															[uid],
															UpdatedOn
													from	#users
													) as S
											on		(T.ResourceID = S.ResourceID)
											when	matched and ((coalesce(T.UpdatedOn, '1/1/1900') < S.UpdatedOn) or (coalesce(T.LastLoggedInOn, '1/1/1900') < S.LastLoggedInOn)) then
													update	
													set		T.FirstName = S.FirstName,
															T.LastName = S.LastName,
															T.LastLoggedInOn = S.LastLoggedInOn,
															T.Email = S.Email,
															T.[State] = S.[State],
															T.IsAdministrator = S.IsAdministrator,
															T.[uid] = S.[uid],
															T.CreatedOn = case when T.CreatedOn is null then getutcdate() else T.CreatedOn end,
															T.UpdatedOn = S.UpdatedOn
											when	not matched by target then
													insert (ResourceID, FirstName, LastName, LastLoggedInOn, Email, [State], IsAdministrator, [uid], CreatedOn, UpdatedOn)
													values (S.ResourceID, S.FirstName, S.LastName, S.LastLoggedInOn, S.Email, S.[State], S.IsAdministrator, S.[uid], getutcdate(), getutcdate())
											output
													$action into @mergeResults;

											select count(1) from @mergeResults;",
									transaction: transaction,
									commandTimeout: 300
									);

									log.WriteLine($"Found {resources.RecordsAffected} users for company {c.CompanyID}. Upsert affected {rowsAffected} rows.");

									transaction.Commit();
								}

								#endregion

								#region Delete Logic

								try
								{
									var currentResourceIDs = companyConnection.Query<int>("select ResourceID from reporting.Global_Resource").ToList();
									Stack<int> toDeleteIds = new Stack<int>(currentResourceIDs.Intersect(updatedResourceIDs));
									LinkedList<int> idsToSend = new LinkedList<int>();

									//We need the following code because SQL Server allows us to send only 2100 parameters per query.
									while (toDeleteIds.TryPop(out int id))
									{
										idsToSend.AddLast(id);
										
										if (idsToSend.Count >= 2099)
										{
											companyConnection.Execute("delete reporting.Global_Resource where ResourceID in @toDeleteIds", new { idsToSend });
										}
									}

									if (idsToSend.Count > 0)
									{
										companyConnection.Execute("delete reporting.Global_Resource where ResourceID in @toDeleteIds", new { idsToSend });
									}

									if (toDeleteIds.Any())
									{
										log.WriteLine("Removed {0} users for company {1}.", toDeleteIds.Count(), c.CompanyID);
									}
								}
								catch (Exception ex)
								{
									CoreFunction.AITrackException(functionName, ex, c.CompanyID);
								}

								try
								{
									companyConnection.Execute("delete ResponsibilityTypeRelationOverrideItem where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
									companyConnection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
								}
								catch (Exception ex)
								{
									CoreFunction.AITrackException(functionName, ex, c.CompanyID);
								}

								#endregion

							}
						}
						catch (Exception ex)
						{
							CoreFunction.AITrackException(functionName, ex, c.CompanyID);
							log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
						}
					}
				}
			}
			catch (Exception ex)
			{
				CoreFunction.AITrackException(functionName, ex);
				log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
			}

			CoreFunction.AIFlush();
		}

		private static DataTable PrepareSourceTable(SqlBulkCopy bulkCopy)
		{
			var table = new DataTable();

			var columnName = "ResourceID";
			table.Columns.Add(columnName, typeof(int));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "FirstName";
			table.Columns.Add(columnName, typeof(string));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "LastName";
			table.Columns.Add(columnName, typeof(string));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "LastLoggedInOn";
			table.Columns.Add(columnName, typeof(DateTime));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "Email";
			table.Columns.Add(columnName, typeof(string));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "State";
			table.Columns.Add(columnName, typeof(int));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "IsAdministrator";
			table.Columns.Add(columnName, typeof(bool));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "uid";
			table.Columns.Add(columnName, typeof(Guid));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			columnName = "UpdatedOn";
			table.Columns.Add(columnName, typeof(DateTime));
			bulkCopy.ColumnMappings.Add(columnName, columnName);

			return table;
		}
	}
}
