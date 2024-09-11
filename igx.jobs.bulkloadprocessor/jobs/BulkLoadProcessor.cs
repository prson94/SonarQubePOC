using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.featureflags;
using d360.model;
using d360.model.DataAccessLayer;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace igx.jobs.bulkloadprocessor
{

	public class BulkLoadProcessor: BaseWebJob
	{
		const string FUNCTION_NAME = "BulkLoadProcessor";
		const int SqlBulkBatchSize = 5000;

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly IStorageProvider Storage;
		readonly IFeatureFlagService FeatureFlags;

		public BulkLoadProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue, IStorageProvider storage, IFeatureFlagService ff) : base(config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
			Storage = storage;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger(constants.Queue.BulkLoad, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
		{
			var loadInfo = JsonConvert.DeserializeObject<BulkLoadInfo>(myQueueItem);
			Load load = null;

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", loadInfo.CompanyID },
				{ "LoadId", loadInfo.LoadID }
			};

			using (log.BeginScope(logProperties))
			{ 
				try
				{
					#region Create EF connection

					var _c = GetCompaniesByCurrentSlot().FirstOrDefault(x => x.CompanyID == loadInfo.CompanyID);


					var context = new UriSecurityContextProvider
					{
						CompanyID = loadInfo.CompanyID,
						ResourceID = 0,
						CompanyPrefix = _c.UrlPrefix,
						IsAdministrator = true
					};
					using (var community = new CommunityContext(ConnString, Cache, Queue, context))
					{
						using (var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true))
						{
							var assetRepository = new AssetRepository(company, Queue, Storage, community, FeatureFlags);
							var tagRepository = new TagRepository(company, FeatureFlags, Queue);
							var relationshipRepository = new RelationshipRepository(community, company, Queue, Storage, FeatureFlags);

							#endregion

							try
							{
								var companyConnection = CompanyConnectionUtils.GetCompanyConnection(loadInfo.CompanyID, ConnString);

								#region Create Load Items from Load file

								load = company.Loads.Include("LoadColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID);

								companyConnection.Open();
								var loadItemRowCount = companyConnection.Query<int>("select count(1) from LoadItem where LoadID = @id", new { id = load.ID }).Single();
								companyConnection.Close();

								if (loadItemRowCount <= 0)
								{
									SLDocument xls = null;
									if (load.File == null)
									{
										using (MemoryStream stream = new MemoryStream())
										{
											await Storage.GetFileStream("bulk-loads", $"{loadInfo.CompanyID}/load_{load.ID}.{load.Extension}", stream);

											xls = new SLDocument(stream);
										}
									}
									else
									{
										var memoryStream = new MemoryStream(load.File);
										xls = new SLDocument(memoryStream);
									}

									var stats = xls.GetWorksheetStatistics();

									var rowIndex = stats.StartRowIndex + 1;
									var numberOfColumns = load.LoadColumns.Count;

									var loadItems = new List<LoadItem>();
									var loadItemColumns = new List<LoadItemColumn>();
									var loadColumns = company.GetLoadColumns(load.Action, load.Object, load.ObjectID, true);

									string qry = $@"declare @AssetTypeId int;
													Declare @LoadID int = @id;

													select @AssetTypeId = ATT.ID
													from [Load] L
													inner join AssetType ATT on ATT.uid = L.AssetTypeUid and Action = 'P';

													select L.ColumnIndex , FT.IsPartOfKey , FT.Type
													from LoadColumn L
													inner join FieldType FT on Ft.AssetTypeID = @AssetTypeId and Ft.Name = L.Name and ft.IsPartOfKey = 1
													where LoadID = @LoadID";

									var LoadColumnIsPartOfKey = companyConnection.Query<dynamic>(qry, new { id = load.ID }).ToList();

									while (rowIndex <= stats.EndRowIndex)
									{
										// Empty row validation.
										var numberOfEmptyColumns = 0;
										foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
										{
											var testValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();
											if (string.IsNullOrEmpty(testValue))
											{
												numberOfEmptyColumns++;
											}
										}

										// Empty row check.
										if (numberOfEmptyColumns < numberOfColumns)
										{
											var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex };
											loadItems.Add(loadItem);

											foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
											{
												var format = xls.GetCellStyle(rowIndex, c.ColumnIndex).FormatCode;
												var isDate = false;

												if (format.Contains("[$-404]") || format.Contains("m/d") || format.Contains("m-d") || format.Contains("d-m") ||
													format.Contains("[$-F400]") || format.Contains("[$-409]"))
												{
													isDate = true;
												}

												var loadValue = string.Empty;

												if (isDate)
												{
													loadValue = xls.GetCellValueAsDateTime(rowIndex, c.ColumnIndex).ToShortDateString();
												}
												else
												{
													if (CheckLoadColumIsPartOfKey(LoadColumnIsPartOfKey, c.ColumnIndex))
													{
														loadValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").Trim();
													}
													else
													{
														loadValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();
													}
												}

												loadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = loadValue, LookupObjectID = null });
											}
										}
										rowIndex++;
									}

									companyConnection.Open();

									#region Bulk LoadItems

									using (var trans = companyConnection.BeginTransaction())
									{
										using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, trans))
										{
											bulkCopy.BatchSize = SqlBulkBatchSize;
											bulkCopy.DestinationTableName = "dbo.LoadItem";
											bulkCopy.BulkCopyTimeout = 3600;

											var table = new System.Data.DataTable();
											var columnName = "LoadID";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											columnName = "RowIndex";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											foreach (var item in loadItems)
											{
												var row = table.NewRow();

												row["LoadID"] = item.LoadID;
												row["RowIndex"] = item.RowIndex;

												table.Rows.Add(row);
											}

											bulkCopy.WriteToServer(table);
										}
										trans.Commit();
									}

									#endregion

									#region Bulk LoadItemColumns

									using (var trans = companyConnection.BeginTransaction())
									{
										using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, trans))
										{
											bulkCopy.BatchSize = SqlBulkBatchSize;
											bulkCopy.DestinationTableName = "dbo.LoadItemColumn";
											bulkCopy.BulkCopyTimeout = 3600;

											var table = new System.Data.DataTable();
											var columnName = "LoadID";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											columnName = "RowIndex";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											columnName = "ColumnIndex";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											columnName = "Value";
											table.Columns.Add(columnName, typeof(string));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											columnName = "LookupObjectID";
											table.Columns.Add(columnName, typeof(int));
											bulkCopy.ColumnMappings.Add(columnName, columnName);

											foreach (var item in loadItemColumns)
											{
												var row = table.NewRow();

												row["LoadID"] = item.LoadID;
												row["RowIndex"] = item.RowIndex;
												row["ColumnIndex"] = item.ColumnIndex;
												if (string.IsNullOrEmpty(item.Value))
												{
													row["Value"] = DBNull.Value;
												}
												else
												{
													row["Value"] = item.Value;
												}

												if (item.LookupObjectID == null)
												{
													row["LookupObjectID"] = DBNull.Value;
												}
												else
												{
													row["LookupObjectID"] = item.LookupObjectID;
												}

												table.Rows.Add(row);
											}

											bulkCopy.WriteToServer(table);
										}
										trans.Commit();
									}

									#endregion

									#region Update Lookup Values

									var lookupColumns = loadColumns.Where(l => l.IsLookup);

									if (lookupColumns.Any())
									{
										using (var trans = companyConnection.BeginTransaction())
										{
											List<dynamic> tempLookupColumns = new List<dynamic>();

											foreach (var col in lookupColumns)
											{
												var loadCol = load.LoadColumns.FirstOrDefault(l => l.Name == col.Name);

												if (loadCol != null && col.FieldTypeId != null)
												{
													tempLookupColumns.Add(new
													{
														LoadID = load.ID,
														loadCol.ColumnIndex,
														col.Name,
														FieldTypeID = col.FieldTypeId
													});
												}
											}

											companyConnection.Execute(@"drop table if exists #tempLookupColumns;
								create table #tempLookupColumns (
									LoadID int,
									Name nvarchar(max),
									ColumnIndex int,
									FieldTypeID int
									);", transaction: trans);



											using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, trans))
											{

												bulkCopy.BatchSize = SqlBulkBatchSize;
												bulkCopy.DestinationTableName = "#tempLookupColumns";
												bulkCopy.BulkCopyTimeout = 3600;

												var table = new System.Data.DataTable();
												var columnName = "LoadID";
												table.Columns.Add(columnName, typeof(int));
												bulkCopy.ColumnMappings.Add(columnName, columnName);

												columnName = "Name";
												table.Columns.Add(columnName, typeof(string));
												bulkCopy.ColumnMappings.Add(columnName, columnName);

												columnName = "ColumnIndex";
												table.Columns.Add(columnName, typeof(int));
												bulkCopy.ColumnMappings.Add(columnName, columnName);

												columnName = "FieldTypeID";
												table.Columns.Add(columnName, typeof(int));
												bulkCopy.ColumnMappings.Add(columnName, columnName);

												foreach (var item in tempLookupColumns)
												{
													var row = table.NewRow();

													row["LoadID"] = item.LoadID;
													row["Name"] = item.Name;
													row["ColumnIndex"] = item.ColumnIndex;
													row["FieldTypeID"] = item.FieldTypeID;

													table.Rows.Add(row);
												}

												bulkCopy.WriteToServer(table);
											}


											companyConnection.Execute(@"
									declare @maxlen int = 0;

									drop table if exists #TempBulkLookupValues;

									select fieldtypeid,[Value],[Text] into  #TempBulkLookupValues 
									from FieldLookupValue flv
									where exists (select 1 from #tempLookupColumns templ
												  where templ.fieldtypeid = flv.fieldtypeid);

									select @maxlen = max(len(text)) from #TempBulkLookupValues

									if (@maxlen <= 400)
										begin
											alter table #TempBulkLookupValues alter column text nvarchar(440);
											CREATE CLUSTERED INDEX CIX_TempBulkLookupValues ON #TempBulkLookupValues ( FieldTypeID ASC,[Text])
										end
									else
										begin
											CREATE CLUSTERED INDEX CIX_TempBulkLookupValues ON #TempBulkLookupValues ( FieldTypeID ASC)
										end

									update LIC
									set LIC.LookupObjectID = FLV.Value
									from LoadItemColumn LIC
									inner join #tempLookupColumns T on T.ColumnIndex = LIC.ColumnIndex and T.LoadID = LIC.LoadID
									left join #TempBulkLookupValues FLV on FLV.FIeldTypeID = T.FieldTypeID and FLV.Text = LIC.Value

									", transaction: trans, commandTimeout: 3600);

											trans.Commit();
										}
									}

									#endregion

									companyConnection.Close();
								}

								log.LogTrace($"Load Item RowCount: {loadItemRowCount}");

								#endregion

								companyConnection.Open();

								switch (load.Action)
								{
									case "M":
										if (load.ObjectID == 0)
										{
											BulkLoadMembership(companyConnection, load.ID);
										}
										else
										{
											BulkLoadUsers(companyConnection, loadInfo.CompanyID, load.ID);
										}
										break;
									case "O":
										await BulkLoadOwnership(company, load.ID);
										break;
									case "P":   // Promotions
										await BulkLoadAssets(company, assetRepository, tagRepository, load);
										company.CreateOrUpdateTypeDisplayValuesAsync(load.ObjectID, load.Object);
										break;
									case "R":   // Relations    
										await BulkRelate(company, assetRepository, relationshipRepository, load, BulkRelationshipOperation.Relate);
										break;
									case "U":   // Unrelate
										await BulkRelate(company, assetRepository, relationshipRepository, load, BulkRelationshipOperation.Unrelate);
										break;
									default:
										break;
								}

								companyConnection.Close();

								load.DateCompleted = DateTime.UtcNow;
								company.Update(load);
							}
							catch (Exception ex)
							{
								if (load != null)
								{
									load.DateCompleted = DateTime.UtcNow;
									company.Update(load);
								}
								log.LogError(ex, "Error occured while processing load");
							}
						}
					}

				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Critical error during Bulk Load Processor Execution");
				}			
			}
		}

		private void BulkLoadMembership(SqlConnection company, int loadId)
		{
			var load = company.Query<Load>("select * from [Load] where ID = @loadId", new { loadId }).SingleOrDefault();
			if (load == null)
			{
				throw new Exception($"Bulk load membership cannot find the load job to run [{loadId}].");
			}

			// get the load columns
			var columns = company.Query<LoadColumn>("select * from LoadColumn where LoadID = @loadId", new { loadId });
			if (columns == null)
			{
				throw new Exception($"Bulk load data does not contain any columns in LoadColumn table.  Load ID [{loadId}]");
			}

			using (var trans = company.BeginTransaction())
			{
				try
				{
					company.Execute(@"
create table #GroupLoadItems (LoadID int, RowIndex int, 
    StatusMessage nvarchar(500), Status bit, 
    [Action] nvarchar(max),
    [Group] nvarchar(max),
    [GroupID] int null,
    [User] nvarchar(max),
    [UserID] int null
);
create table #GroupInsertResult (ID int);
create table #ResourceGroupInsertResult (ID int);
create table #ResourceGroupDeleteResult (ID int);", transaction: trans);

					company.Execute(@"
insert into #GroupLoadItems
    select	I.LoadID,
		    I.RowIndex,
		    I.StatusMessage,
		    I.Status,
		    C1.Value as [Action],
		    C2.Value as [Group],
		    cast(null as int) as GroupID,
		    C3.Value as [User],
		    cast(null as int) as UserID
    from	LoadItem I
		    inner join LoadItemColumn C1 on C1.LoadID = I.LoadID and C1.RowIndex = I.RowIndex and C1.ColumnIndex = 1
		    inner join LoadItemColumn C2 on C2.LoadID = I.LoadID and C2.RowIndex = I.RowIndex and C2.ColumnIndex = 2
		    inner join LoadItemColumn C3 on C3.LoadID = I.LoadID and C3.RowIndex = I.RowIndex and C3.ColumnIndex = 3
    where	I.LoadID = @id", new { id = loadId }, transaction: trans);

					company.Execute(@"
merge into	[Group] as T
using		(
			select	distinct
					ltrim(rtrim([Group])) as Name
			from	#GroupLoadItems
			) S
on			(T.Name  = S.Name)
when not matched by target then
	insert (Name, UpdatedOn, UpdatedBy)
	values (S.Name, getutcdate(), 0)
output inserted.ID into #GroupInsertResult;

merge	into 
		Asset as A
using	(
		select	G.ID,
				GT.ID as AssetTypeID,
				G.UpdatedOn,
				G.UpdatedBy
		from	[Group] G
				inner join #GroupInsertResult U on U.ID = G.ID
				inner join AssetType GT on GT.Object = 'GroupType'
		) G
on		(A.Object = 'Group' and A.ObjectID = G.ID)
when	matched then
update	set 
		A.UpdatedOn = G.UpdatedOn,
		A.UpdatedBy = G.UpdatedBy
when not matched then
insert	(AssetTypeID, Object, ObjectID, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
values	(G.AssetTypeID, 'Group', G.ID, G.UpdatedOn, G.UpdatedBy, G.UpdatedOn, G.UpdatedBy);", transaction: trans);

					company.Execute(@"
update	T
set		T.GroupID = S.ID,
		T.StatusMessage = case
							when I.ID is not null then 'Group created. '
							else T.StatusMessage
						  end
from	#GroupLoadItems T
		inner join [Group] S on S.Name= T.[Group]
		left join #GroupInsertResult I on I.ID = S.ID;

update	T
set		T.UserID = S.ResourceID
from	#GroupLoadItems T
		inner join reporting.Global_Resource S on S.Email = T.[User];

update	#GroupLoadItems
set		Status = 0,
		StatusMessage = 'No user found with this email address. '
where	UserID is null;", transaction: trans);

					company.Execute($@"
merge       AssetDisplayValue as T
using       (
                select  A.ID,
                        ADV.DisplayValue,
                        CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
                        SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
                from    Asset A
                        inner join [Group] G on G.ID = A.ObjectID and A.Object = 'Group'
                        inner join #GroupInsertResult L on L.ID = G.ID
						inner join AssetDisplayValue ADV on ADV.AssetID = A.ID
                where   ADV.DisplayValue is not null
            ) as S 
on          ( T.AssetID = S.ID )
when		not matched by target then
insert		(AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, getutcdate());", transaction: trans);

					company.Execute(@"
merge into	[ResourceGroup] as T
using		(
			select	distinct
					[UserID],
					[GroupID]
			from	#GroupLoadItems
			where	UserID is not null and GroupID is not null and [Action] = 'Add'
			) S
on			(T.ResourceID = S.UserID and T.GroupID = S.GroupID)
when not matched by target then
	insert (ResourceID, GroupID)
	values (S.UserID, S.GroupID)
output inserted.ResourceID into #ResourceGroupInsertResult;", transaction: trans);

					company.Execute(@"
update	T
set		T.Status = 1,
		T.StatusMessage = coalesce(T.StatusMessage, '') + case
							when I.ID is not null then 'Membership created. '
							else 'Membership already exists.'
						  end
from	#GroupLoadItems T
		left join #ResourceGroupInsertResult I on I.ID = T.UserID
where	T.UserID is not null and T.GroupID is not null and T.[Action] = 'Add';", transaction: trans);

					company.Execute(@"
merge into	[ResourceGroup] as T
using		(
			select	distinct
					[UserID],
					[GroupID]
			from	#GroupLoadItems
			where	UserID is not null and GroupID is not null and [Action] = 'Remove'
			) S
on			(T.ResourceID = S.UserID and T.GroupID = S.GroupID)
when matched and T.ResourceID = S.UserID and T.GroupID = S.GroupID then
	delete
output deleted.ResourceID into #ResourceGroupDeleteResult;", new { id = loadId }, transaction: trans);

					company.Execute(@"
update	T
set		T.Status =	case
						when I.ID is not null then 1
						else 0
					end,
		T.StatusMessage = coalesce(T.StatusMessage, '') + case
							when I.ID is not null then 'Membership removed. '
							else 'Membership does not exist.'
						  end
from	#GroupLoadItems T
		left join #ResourceGroupDeleteResult I on I.ID = T.UserID
where	T.UserID is not null and T.GroupID is not null and T.[Action] = 'Remove';

update	T
set		T.Status = S.Status,
		T.StatusMessage = S.StatusMessage
from	LoadItem T
		inner join #GroupLoadItems S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;", new { id = loadId }, transaction: trans);

					trans.Commit();
				}
				catch
				{
					trans.Rollback();
					throw;
				}
			}
		}

		private void BulkLoadUsers(SqlConnection company, int companyID, int loadId)
		{
			var load = company.Query<Load>("select * from [Load] where ID = @loadId", new { loadId }).SingleOrDefault();
			if (load == null)
			{
				throw new Exception($"Bulk load users cannot find the load job to run [{loadId}].");
			}

			// get the load columns
			var columns = company.Query<LoadColumn>("select * from LoadColumn where LoadID = @loadId", new { loadId });
			if (columns == null)
			{
				throw new Exception($"Bulk load data does not contain any columns in LoadColumn table.  Load ID [{loadId}]");
			}
			if (columns.Count() < 4)
			{
				throw new Exception($"Bulk load data does not contain the correct number of columns in LoadColumn table.  Load ID [{loadId}]");
			}

			var usersToLoad = company.Query<CommunityUserAddModel>(@"
select	I.LoadID,
		I.RowIndex,
		rtrim(ltrim(C1.Value)) as [UserStatus],
		rtrim(ltrim(C2.Value)) as [Email],
		rtrim(ltrim(C3.Value)) as [FirstName],
		rtrim(ltrim(C4.Value)) as [LastName]
from	LoadItem I
		inner join LoadItemColumn C1 on C1.LoadID = I.LoadID and C1.RowIndex = I.RowIndex and C1.ColumnIndex = 1
		inner join LoadItemColumn C2 on C2.LoadID = I.LoadID and C2.RowIndex = I.RowIndex and C2.ColumnIndex = 2
		inner join LoadItemColumn C3 on C3.LoadID = I.LoadID and C3.RowIndex = I.RowIndex and C3.ColumnIndex = 3
		inner join LoadItemColumn C4 on C4.LoadID = I.LoadID and C4.RowIndex = I.RowIndex and C4.ColumnIndex = 4
where	I.LoadID = @loadId", new { loadId }, commandTimeout: 1200).ToList();

			#region Generate data sets

			var tbl = new System.Data.DataTable();

			tbl.Columns.Add("LoadID", typeof(int));
			tbl.Columns.Add("RowIndex", typeof(int));
			tbl.Columns.Add("UserStatus", typeof(string));
			tbl.Columns.Add("Email", typeof(string));
			tbl.Columns.Add("FirstName", typeof(string));
			tbl.Columns.Add("LastName", typeof(string));
			tbl.Columns.Add("EnvironmentID", typeof(int));
			tbl.Columns.Add("Success", typeof(bool));
			tbl.Columns.Add("Message", typeof(string));

			foreach (var userToLoad in usersToLoad)
			{
				var row = tbl.NewRow();

				row["LoadID"] = userToLoad.LoadID;
				row["RowIndex"] = userToLoad.RowIndex;
				row["UserStatus"] = userToLoad.UserStatus;
				row["Message"] = "";
				if (string.IsNullOrEmpty(userToLoad.Email) || !Regex.IsMatch(userToLoad.Email + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
				{
					row["Success"] = false;
					row["Message"] = "Email is not in a valid format; ";
				}
				row["Email"] = userToLoad.Email + "";
				row["FirstName"] = userToLoad.FirstName;
				row["LastName"] = userToLoad.LastName;
				row["EnvironmentID"] = companyID;

				tbl.Rows.Add(row);
			}

			#endregion

			List<CommunityUserAddResultModel> userResults = null;

			#region Process in Community database.

			using (var community = new SqlConnection(ConnString))
			{
				community.Open();
				using (var trans = community.BeginTransaction())
				{
					try
					{
						community.Execute(@"
DROP TABLE IF EXISTS #Users;
DROP TABLE IF EXISTS #UsersResult;
DROP TABLE IF EXISTS #UserMembershipsResult;", transaction: trans);

						community.Execute(@"
create table #Users (
    LoadID int not null,
    RowIndex int not null,
    UserStatus nvarchar(50) null,
    Email nvarchar(500) null,
    FirstName nvarchar(250) null,
    LastName nvarchar(250) null,
	EnvironmentID int not null, 
	ClientID int null,
	ResourceID int null,
    [uid] uniqueidentifier null,
    Success bit null,
    Message nvarchar(2500) null
);
create table #UsersResult (LoadID int, RowIndex int, ResourceID int, [uid] uniqueidentifier, [Action] varchar(25) not null);
create table #UserMembershipsResult (ResourceID int, [Action] varchar(25) not null);
CREATE NONCLUSTERED INDEX IX_TempUsers ON #Users ( Email ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_LoadID_Email ON #Users ( LoadID ASC, Email ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_LoadID_RowIndex_Email ON #Users ( LoadID ASC, RowIndex ASC, Email ASC );
", transaction: trans);

						var usersBulkCopy = new SqlBulkCopy(community, SqlBulkCopyOptions.Default, trans)
						{
							BatchSize = SqlBulkBatchSize,
							DestinationTableName = "#Users",
							BulkCopyTimeout = 3600
						};

						usersBulkCopy.ColumnMappings.Add("LoadID", "LoadID");
						usersBulkCopy.ColumnMappings.Add("RowIndex", "RowIndex");
						usersBulkCopy.ColumnMappings.Add("UserStatus", "UserStatus");
						usersBulkCopy.ColumnMappings.Add("Email", "Email");
						usersBulkCopy.ColumnMappings.Add("FirstName", "FirstName");
						usersBulkCopy.ColumnMappings.Add("LastName", "LastName");
						usersBulkCopy.ColumnMappings.Add("EnvironmentID", "EnvironmentID");
						usersBulkCopy.ColumnMappings.Add("Success", "Success");
						usersBulkCopy.ColumnMappings.Add("Message", "Message");

						usersBulkCopy.WriteToServer(tbl);

						community.Execute(@"update	T
set		T.ClientID = S.ClientID
from	#Users T
		inner join Company S on S.ID = T.EnvironmentID;", transaction: trans);

						// Check for duplicate email addresses and invalidate the ones with higher row indices.
						community.Execute(@"update	T
set		T.Success = 0,
		T.Message = 'User email address already used in bulk load file'
from	#Users T
		inner join	(
					select LoadID, min(RowIndex) as MinRowIndex, Email from #Users group by LoadID, Email
					) S on S.LoadID = T.LoadID and S.Email = T.Email and S.MinRowIndex <> T.RowIndex;", transaction: trans);

						community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid email address; '
where   [Email] is null or [Email] = '';", transaction: trans);

						community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid first name; '
where   [FirstName] is null or [FirstName] = '';", transaction: trans);

						community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid last name; '
where   [LastName] is null or [LastName] = '';", transaction: trans);

						string inclause = string.Join(",", CompanyResourceState.Active.GetList().Select(s => "'" + s.Name + "'"));
						
						community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid status; '
where   [UserStatus] IS NULL OR [UserStatus] NOT IN (" + inclause + ");", transaction: trans);

						community.Execute(@"update	T
set		T.ResourceID = S.ID
from	#Users T
		inner join [Resource] S on S.Email = T.Email;", transaction: trans);

						community.Execute(@"update	T
set		T.Success = case
						when S.[Count] > 0 then cast(0 as bit)
						else null
					end,
		T.Message = case
						when S.[Count] > 0 then 'User is a member of another account and may not be modified; '
						else null
					end
from	#Users T
		cross apply (
			select	count(1) as [Count]
			from	CompanyResource CR
					inner join Company C on C.ID = CR.CompanyID and C.ClientID <> T.ClientID and CR.ResourceID = T.ResourceID
		) S
where   T.Success is null;", transaction: trans);

						community.Execute(@"
merge into  [Resource] T
using       (
            select  *
            from    #Users
			where	Success is null
            ) S
on          (
                T.ID = S.ResourceID
            )
when matched then
	update
	set	T.FirstName = S.FirstName,
		T.LastName = S.LastName
when not matched by target then
    insert  (Username, [Password], LastName, FirstName, Email)
    values  (S.Email, 'not set', S.LastName, S.FirstName, S.Email)
output S.LoadID, S.RowIndex, inserted.ID, inserted.[uid], $action into #UsersResult;", transaction: trans);

						community.Execute(@"
update	T
set		T.Success = 1,
		T.ResourceID = S.ResourceID,
        T.[uid] = S.[uid],
		Message = case S.[Action]
					when 'INSERT' then 'User created. '
					else 'User updated. '
				  end
from	#Users T
		inner join #UsersResult S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;", transaction: trans);

						community.Execute(@"
merge into  [CompanyResource] T
using       (
            select  distinct
					EnvironmentID as CompanyID,
					ResourceID,
                    case UserStatus 
                        when 'Active' then 1 
                        when 'Inactive' then 2
                        when 'Deleted' then 3
                    end as [State]
            from    #Users
			where	Success = 1
            ) S
on          (
                T.CompanyID = S.CompanyID and T.ResourceID = S.ResourceID
            )
when matched then
	update 
		set T.[State] = S.[State]
when not matched by target then
    insert  (CompanyID, ResourceID, IsAdministrator, [State])
    values  (S.CompanyID, S.ResourceID, 0, S.[State])
output inserted.ResourceID, $action into #UserMembershipsResult;", transaction: trans);

						community.Execute(@"
update	T
set		T.Message = T.Message + 
					case S.[Action]
						when 'INSERT' then 'User added to environment. '
						else 'User already assigned to environment. '
					end
from	#Users T
		left join #UserMembershipsResult S on S.ResourceID = T.ResourceID
where	T.Success = 1", transaction: trans);

						userResults = community.Query<CommunityUserAddResultModel>("select * from #Users", transaction: trans).ToList();

						trans.Commit();
					}
					catch
					{
						trans.Rollback();
						throw;
					}
				}
			}

			#endregion

			#region Process in Environment database.

			tbl = new System.Data.DataTable();

			tbl.Columns.Add("LoadID", typeof(int));
			tbl.Columns.Add("RowIndex", typeof(int));
			tbl.Columns.Add("UserStatus", typeof(string));
			tbl.Columns.Add("Email", typeof(string));
			tbl.Columns.Add("FirstName", typeof(string));
			tbl.Columns.Add("LastName", typeof(string));
			tbl.Columns.Add("ResourceID", typeof(int));
			tbl.Columns.Add("uid", typeof(Guid));
			tbl.Columns.Add("Success", typeof(bool));
			tbl.Columns.Add("Message", typeof(string));

			foreach (var userResult in userResults)
			{
				var row = tbl.NewRow();

				row["LoadID"] = userResult.LoadID;
				row["RowIndex"] = userResult.RowIndex;
				row["UserStatus"] = userResult.UserStatus;
				row["Email"] = userResult.Email + "";
				row["FirstName"] = userResult.FirstName;
				row["LastName"] = userResult.LastName;

				if (userResult.ResourceID.HasValue)
				{
					row["ResourceID"] = userResult.ResourceID.Value;
				}

				row["uid"] = userResult.Uid;
				row["Success"] = userResult.Success;
				row["Message"] = userResult.Message;

				tbl.Rows.Add(row);
			}

			using (var trans = company.BeginTransaction())
			{
				try
				{
					company.Execute(@"DROP TABLE IF EXISTS #Users;", transaction: trans);

					company.Execute(@"
create table #Users (
    LoadID int not null,
    RowIndex int not null,
    UserStatus nvarchar(50) null,
    Email nvarchar(500) null,
    FirstName nvarchar(250) null,
    LastName nvarchar(250) null,
	ResourceID int null,
    [uid] uniqueidentifier null,
    Success bit null,
    Message nvarchar(2500) null
)
CREATE NONCLUSTERED INDEX IX_TempUsers_Load ON #Users ( LoadID ASC, RowIndex ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_ResourceID ON #Users ( ResourceID ASC );
", transaction: trans);

					var usersBulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
					{
						BatchSize = SqlBulkBatchSize,
						DestinationTableName = "#Users",
						BulkCopyTimeout = 3600
					};

					usersBulkCopy.ColumnMappings.Add("LoadID", "LoadID");
					usersBulkCopy.ColumnMappings.Add("RowIndex", "RowIndex");
					usersBulkCopy.ColumnMappings.Add("UserStatus", "UserStatus");
					usersBulkCopy.ColumnMappings.Add("Email", "Email");
					usersBulkCopy.ColumnMappings.Add("FirstName", "FirstName");
					usersBulkCopy.ColumnMappings.Add("LastName", "LastName");
					usersBulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
					usersBulkCopy.ColumnMappings.Add("uid", "uid");
					usersBulkCopy.ColumnMappings.Add("Success", "Success");
					usersBulkCopy.ColumnMappings.Add("Message", "Message");

					usersBulkCopy.WriteToServer(tbl);

					company.Execute(@"
merge into  reporting.Global_Resource T
using       (
            select  ResourceID, 
                    [uid],
                    LastName, 
                    FirstName, 
                    Email, 
                    case UserStatus 
                        when 'Active' then 1 
                        when 'Inactive' then 2
                        when 'Deleted' then 3
                    end as [State]
            from    #Users
			where	Success = 1
            ) S
on          (
                T.ResourceID = S.ResourceID
            )
when matched then
	update
	set	T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.[State] = S.[State]
when not matched by target then
    insert  ([uid], ResourceID, LastName, FirstName, Email, [State], IsAdministrator)
    values  (S.[uid], S.ResourceID, S.LastName, S.FirstName, S.Email, S.[State], 0);", transaction: trans);

					company.Execute(@"exec [bulkload].[UpdateDynamicLookupFieldColumns] @loadId", new { loadId }, transaction: trans);

					company.Execute(@"
merge into  Field T
using       (
			select	A.ID as AssetID,
					A.Object,
					A.ObjectID,
					FT.ID as FieldTypeID,
					case 
						when FT.[Type] = 'Boolean' and LOWER(CI.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
						when FT.[Type] = 'Boolean' and LOWER(CI.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
						when FT.[Type] = 'Lookup' then cast(CI.LookupObjectID as nvarchar(250))
						else CI.Value
					end as Value,
					0 as UpdatedBy
			from	LoadItem I
					inner join #Users U on U.LoadID = I.LoadID and U.RowIndex = I.RowIndex and U.Success = 1
					inner join Asset A on A.Object = 'Resource' and A.ObjectID = U.ResourceID
					inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex > 4
					inner join LoadItemColumn CI on CI.LoadID = I.LoadID and CI.RowIndex = I.RowIndex and CI.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.Name = C.Name
            ) S
on          (
                T.AssetID = S.AssetID and T.FieldTypeID = S.FieldTypeID
            )
when matched and S.Value is null then
	delete
when matched and S.Value is not null then
	update
	set	T.Value = S.Value,
		T.UpdatedBy = S.UpdatedBy
when not matched by target and S.Value is not null then
    insert  (AssetID, FieldTypeID, Value, UpdatedBy)
    values  (S.AssetID, S.FieldTypeID, S.Value, S.UpdatedBy);", transaction: trans);

					company.Execute(@"
update	T
set		T.Status = S.Success,
		T.StatusMessage = S.Message
from	LoadItem T
		inner join #Users S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;
update	[Load]
set		DateCompleted = getutcdate()
where	ID = @loadId", new { loadId }, transaction: trans);

					trans.Commit();
				}
				catch
				{
					trans.Rollback();
					throw;
				}
			}

			#endregion
		}

		private async Task BulkLoadOwnership(CompanyContext company, int loadId)
		{
			try
			{
				var load = company.Loads.Where(x => x.ID == loadId).FirstOrDefault();

				if (load == null)
				{
					throw new Exception($"Bulk load relate cannot find the load job to run [{loadId}].");
				}

				// get the load columns
				var columns = company.LoadColumns.Where(x => x.LoadID == loadId).ToList();

				if (columns == null)
				{
					throw new Exception($"Bulk load data doesnt contain any columns in LoadColumn table.  Load ID [{loadId}]");
				}

				int assetUidIndex = -1;
				int responsibilityIndex = -1;
				int resourceIndex = -1;

				foreach (var column in columns)
				{
					if (string.Compare(column.Name, "Asset UID") == 0)
					{
						assetUidIndex = column.ColumnIndex;
					}
					else if (string.Compare(column.Name, "Resource") == 0)
					{
						resourceIndex = column.ColumnIndex;
					}
					else if (string.Compare(column.Name, "Responsibility") == 0)
					{
						responsibilityIndex = column.ColumnIndex;
					}
				}

				var connection = company.Connection;
				if (connection.State != System.Data.ConnectionState.Open)
				{
					await connection.OpenAsync();
				}

				using (var trans = connection.BeginTransaction())
				{
					try
					{
						//create and load temp table
						await connection.ExecuteAsync(@"
drop table if exists #ResponsibilityTypeOverride;
create table #ResponsibilityTypeOverride
(
    RowIndex int,
    ResponsibilityName nvarchar(max),
    ResourceName nvarchar(max),
    AssetUid nvarchar(max),
    [uid] uniqueidentifier,
    [AssetID] int,
    ResponsibilityTypeID int,
    SecurityAsset nvarchar(10),
    SecurityAssetID int,
    Message nvarchar(max),
    Success bit not null
);"
, transaction: trans);

						await connection.ExecuteAsync(@"
insert into #ResponsibilityTypeOverride
select LI.RowIndex, 
LIC1.Value as ResponsibilityName,
LIC2.Value as ResourceName,
LIC3.Value as AssetUid,
null as [uid],
null as [AssetID],
null as ResponsibilityTypeID,
null as SecurityAsset,
null as SecurityAssetID,
null as Message,
cast(1 as bit) as Success
from  LoadItem LI
left join LoadItemColumn LIC1 on LIC1.ColumnIndex = @responsibilityIndex and LIC1.RowIndex = LI.RowIndex and LIC1.LoadID = LI.LoadID
left join LoadItemColumn LIC2 on LIC2.ColumnIndex = @resourceIndex and LIC2.RowIndex = LI.RowIndex and LIC2.LoadID = LI.LoadID
left join LoadItemColumn LIC3 on LIC3.ColumnIndex = @assetUidIndex and LIC3.RowIndex = LI.RowIndex and LIC3.LoadID = LI.LoadID
where LI.LoadID = @loadId
", new { loadId = load.ID, responsibilityIndex, resourceIndex, assetUidIndex }
													  , transaction: trans);

						//validate records and populate IDs
						await connection.ExecuteAsync(@"
--check for null values
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities cannot find the responsibility column in row ' + cast(RowIndex as varchar(50)) where coalesce(ResponsibilityName,'') = '';
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities cannot find the resource column in row ' + cast(RowIndex as varchar(50)) where coalesce(ResourceName,'') = '';
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities cannot find the asset column in row ' + cast(RowIndex as varchar(50)) where coalesce(AssetUid,'') = '';



--asset validation
update #ResponsibilityTypeOverride set [uid] = try_cast(AssetUid as uniqueidentifier) where Success = 1;
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities asset UID value ' + AssetUid + ' is not a valid asset Uid.  Asset UID values must be an unique identifier.' where Success = 1 and [uid] is null;

update R set R.AssetID = A.ID
from #ResponsibilityTypeOverride R 
inner join Asset A on A.[uid] = R.[uid]
where Success = 1;
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities asset UID value ' + AssetUid + ' is not a valid asset Uid.  Asset cannot be found.' where Success = 1 and AssetID is null;



--resource validation
update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities resource value ' + ResourceName + ' is not a valid resource it must be formatted [type]:[id].' where Success = 1 and ResourceName not like '%:%';
update #ResponsibilityTypeOverride set SecurityAsset = case when ResourceName like 'user:%' then 'R' else 'G' end where Success = 1;

update R
set SecurityAssetID = G.ResourceID
from #ResponsibilityTypeOverride R
inner join reporting.Global_Resource G on G.Email = replace(R.ResourceName,'user:','')
where R.SecurityAsset = 'R' and R.Success = 1;

update R
set SecurityAssetID = G.ID
from #ResponsibilityTypeOverride R
inner join [Group] G on G.Name = replace(R.ResourceName,'group:','')
where R.SecurityAsset = 'G' and R.Success = 1;

update #ResponsibilityTypeOverride set Success = 0, 
Message = case when SecurityAsset = 'R' then 
    'Bulk load responsibilities user value ' + replace(ResourceName,'user:','') + ' is not a valid resource and the email cannot be found in the resources table.' 
else 
    'Bulk load responsibilities group name value ' + replace(ResourceName,'group:','') + ' is not a valid group name it cannot be found in the groups table.' 
end
where Success = 1 and SecurityAssetID is null;



--responsibility validation
update R
set ResponsibilityTypeID = G.ID
from #ResponsibilityTypeOverride R
inner join [ResponsibilityType] G on G.Name = R.ResponsibilityName
where R.Success = 1;

update #ResponsibilityTypeOverride set Success = 0, Message = 'Bulk load responsibilities responsibility value '+ ResponsibilityName + ' is not a valid responsibility type it cannot be found in the responsibility type table.' where Success = 1 and ResponsibilityTypeID is null;

--Validate responsibility for asset

drop table if exists #ValidateRespAsset;

select distinct att.id AssetTypeid,rtr.responsibilitytypeid
into #ValidateRespAsset
from #ResponsibilityTypeOverride rto
inner join asset a on a.id = rto.AssetID
inner join assettype att on a.assettypeid = att.id
inner join responsibilitytyperelation rtr 
on rtr.responsibilitytypeid = rto.ResponsibilityTypeID 
and rtr.ObjectType = att.object and rtr.ObjectID = att.ObjectID;

create nonclustered index idx_ValidateRespAsset on #ValidateRespAsset(AssetTypeid,responsibilitytypeid);

update rto 
set Success = 0, Message = 'Responsibility value '+ ResponsibilityName + ' is not a valid for asset ' + AssetUid + '.' 
from #ResponsibilityTypeOverride rto
where Success = 1 
and not exists (select 1 
                from asset a 
			    inner join assettype att on a.assettypeid = att.id
				inner join #ValidateRespAsset rtr 
                on rtr.AssetTypeid = att.ID 
                and rtr.responsibilitytypeid = rto.ResponsibilityTypeID
				where a.id = rto.AssetID );



--mark duplicate records among the batch except the first row of each group
update R
set R.Message = 'Responsibility already exists.'
from #ResponsibilityTypeOverride R
inner join (
		select min(RowIndex) as RowIndex, R.ResponsibilityTypeID, R.AssetID, R.SecurityAsset, R.SecurityAssetID
		from #ResponsibilityTypeOverride R
		where R.Success = 1 
		group by R.ResponsibilityTypeID, R.AssetID, R.SecurityAsset, R.SecurityAssetID
		having count(*) > 1
) D on D.ResponsibilityTypeID = R.ResponsibilityTypeID and D.AssetID = R.AssetID and D.SecurityAsset = R.SecurityAsset and D.SecurityAssetID = R.SecurityAssetID and D.RowIndex <> R.RowIndex
where R.Success = 1;
", transaction: trans);

						//merge valid records and update load table
						await connection.ExecuteAsync(@"
drop table if exists #MergeResult;
create table #MergeResult (RowIndex int, [Action] nvarchar(max))

merge into  ResponsibilityTypeRelationOverrideItem T
using		(
			select      
						RowIndex,
						ResponsibilityTypeID,
						AssetID,
						SecurityAsset,
						SecurityAssetID,
						null as Context,
						@updatedBy as UpdatedBy,
						getutcdate() as UpdatedOn
			from        #ResponsibilityTypeOverride
			where		Success = 1 and Message is null
        ) S
on      ( T.ResponsibilityTypeID = S.ResponsibilityTypeID and T.AssetID = S.AssetID and T.SecurityAsset = S.SecurityAsset and T.SecurityAssetID = S.SecurityAssetID )
when matched then
	update set
			T.UpdatedBy = @updatedBy,
			T.UpdatedOn = getutcdate()
when not matched by target then
	insert  (ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID, UpdatedBy, UpdatedOn)
	values  (S.ResponsibilityTypeID, S.AssetID, S.SecurityAsset, S.SecurityAssetID, @updatedBy, getutcdate())
output  S.RowIndex, $action into #MergeResult;


update R
set R.Message = case when M.[Action] = 'INSERT' then 'Responsibility added sucessfully.' else 'Responsibility already exists.' end
from #ResponsibilityTypeOverride R
inner join #MergeResult M on M.RowIndex = R.RowIndex;

update LI
set LI.Status = R.Success, LI.StatusMessage = R.Message
from LoadItem LI
inner join #ResponsibilityTypeOverride R on R.RowIndex = LI.RowIndex
where LI.LoadID = @loadId"
, new { loadId = load.ID, updatedBy = load.UpdatedBy.GetValueOrDefault() }
, transaction: trans);

						// Get score events
						var today = DateTime.UtcNow.Date;
						var results = await connection.QueryAsync<Guid>(@"
    select  distinct
			A.Uid
    from    #ResponsibilityTypeOverride O 
			inner join ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
            inner join Asset A on A.ID = O.AssetID
            inner join AssetType T on T.ID = A.AssetTypeID
            inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
            inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
            inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
                and ( 
                    (@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
                    (@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
                    ) 
                and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
                and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = RT.uid
		        and V.Definition <> '{}'
	where	O.Success = 1", new { today }, transaction: trans);
						var impactedAssets = results.ToList();

						trans.Commit();

						company.CreateRescoreRequests(impactedAssets, ScoreType.Governance);
					}
					catch (Exception ex)
					{
						try
						{
							if (trans != null)
							{
								trans.Rollback();
							}
						}
						catch
						{
							// suppress exceptions raised by rollback we crashed any way so lets move on.
						}

						//mark incomplete records as failed
						(await company.QueryAsync(@"update LoadItem set Status = 0, StatusMessage = 'A fatal error occurred while attempting to load responsibilities.' where LoadID = @loadId and coalesce(Status,0) <> 1", new { loadId = load.ID })).FirstOrDefault();
						throw;
					}
				}

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		private async Task BulkLoadAssets(CompanyContext company, IAssetRepository repository, ITagRepository tagRepository, Load load)
		{
			await company.BulkLoadAssets(load, repository, tagRepository);
		}

		private async Task BulkRelate(CompanyContext company, IAssetRepository assetRepository, IRelationshipRepository relationshipRepository, Load load, BulkRelationshipOperation operation)
		{
			await company.BulkRelation(load, relationshipRepository, assetRepository, operation);
		}

		private bool CheckLoadColumIsPartOfKey(List<dynamic> loadcolumnIsPartOkKey, int ColumnIdx)
		{
			if (loadcolumnIsPartOkKey != null && loadcolumnIsPartOkKey.Count > 0)
			{
				var ColData = loadcolumnIsPartOkKey.Where(x => x.ColumnIndex == ColumnIdx).FirstOrDefault();
				if (ColData != null)
				{
					return true;
				}
			}
			return false;
		}

	}
}
