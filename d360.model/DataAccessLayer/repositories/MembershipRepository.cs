using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;
using d360.core.validators;
using d360.extensions;
using d360.featureflags;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;
using Dapper;
using MoreLinq;
using repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public class MembershipRepository : BaseRepository, IMembershipRepository
	{
		internal IAssetRepository AssetRepository;
		internal IQueueSource QueueSource;
		internal IStorageProvider StorageProvider;

		public MembershipRepository(
			ICompanyContext companyContext, 
			ISecurityContextProvider securityContext,
			IAssetRepository assetRepository, 
			IQueueSource queueSource, 
			IStorageProvider storageProvider, IFeatureFlagService ff)
			: base(companyContext, securityContext, ff)
		{
			AssetRepository = assetRepository;
			QueueSource = queueSource;
			StorageProvider = storageProvider;
		}

		public async Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			bool listColorsAsJSON = false;
			List<string> condition = new List<string>();
			string resourceString = "";
			string paginationStatement = "";

			var fieldColumns = new DynamicQuerySelects();
			var fieldJoins = new DynamicQueryJoins();

			if (queryParams != null)
			{
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
				{
					var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;

					if (Guid.TryParse(uidString, out Guid uid))
					{
						if (uid != Guid.Empty)
						{
							condition.Add("A.Uid = @Uid");
							dbArgs.Add("uid", uid);
						}

					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "name"))
				{

					var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.Trim();

					if (!string.IsNullOrEmpty(name))
					{

						condition.Add("G.Name like  @name");
						dbArgs.Add("name", name + '%');
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "resourceuid"))
				{

					var user = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value.Trim();

					if (!string.IsNullOrEmpty(user))
					{
						resourceString = @"left join Asset U on U.[uid] = @user
										left join[dbo].[ResourceGroup] RG on RG.[ResourceID] = U.ObjectID ";
						condition.Add("RG.[GroupID] = G.ID");
						dbArgs.Add("user", user);
					}
				}

				var pageSize = queryParams.FirstOrDefault(q => q.Key == "_pageSize");
				var pageNum = queryParams.FirstOrDefault(q => q.Key == "_pageNum");

				if (int.TryParse(pageSize.Value, out int _pageSize) && int.TryParse(pageNum.Value, out int _pageNum))
				{
					paginationStatement = $"offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
				}
			}

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_listcolorsasjson"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_listcolorsasjson").Value, out listColorsAsJSON);
			}

			var groupIdList = CompanyContext.AssetTypes.Where(a => a.Class == AssetTypeClass.Group).Select(s => s.ID);

			var fieldTypes = CompanyContext.FieldTypes.Where(f => groupIdList.Contains(f.AssetTypeID.Value)).ToList();
			getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, listColorsAsJSON: listColorsAsJSON);

			if (queryParams != null)
			{
				if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
				{
					var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();

					if (!string.IsNullOrEmpty(simpleFilter))
					{
						simpleFilter = CompanyContext.GetEscapedFilterString(simpleFilter);

						dbArgs.Add("@simpleFilter", simpleFilter);

						List<string> simpleFilters = new List<string>();

						//There may be multiple OwnershipLookup fields, but they all look to the same table for filtering, so that will be dealt with below
						var fields = fieldTypes.Zip(fieldColumns.Selects(), (type, column) => (type, column))
							.Where(x => x.type.IsListable == true && x.type.Type != DataType.OwnershipLookup.ToString());

						foreach (var (ft, column) in fields)
						{
							simpleFilters.Add($"{column.FilterStatement} like @simpleFilter");
						}

						simpleFilters.Add($"G.Name like @simpleFilter");

						condition.Add($"({string.Join(" or ", simpleFilters)})");
					}
				}
			}

			var sqlOrderBy = CompanyContext.ParseOrderColumn(queryParams, Enumerable
				.Zip(
					fieldTypes,
					fieldColumns.Selects(),
					(type, column) => new DefaultFilter(type.Name, column.FilterStatement, SqlFieldType.Text))
				.Concat(new[] { new DefaultFilter("Name", "G.Name", SqlFieldType.Text) })
				.ToList(),
				"Name");

			var sqlOrderDirection = this.CompanyContext.ParseOrderDirection(queryParams, "asc");

			var whereStatements = condition.Count != 0 ? $" where  {string.Join(" and ", condition)}" : "";
			var sql = $@"
				   Select 
					   A.Uid,
					   {(fieldColumns.GetStatements().Count > 0 ? string.Join(",\n", fieldColumns.GetStatements()) + "," : "")}
					   G.Name,
					   G.Description,
					   gr1.uid as PrimaryOwnerUid,
					   gr2.uid as SecondaryOwnerUid,
					   G.IsActiveDirectoryGroup
					   from [Group] G
						   inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
						   left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
						   left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
						   {(fieldJoins.Count > 0 ? string.Join("\n", fieldJoins.SQLJoinStatement) : "")}
						   {resourceString} 
						   {whereStatements}  
						   order by {sqlOrderBy} {sqlOrderDirection}
						   {paginationStatement}";

			var countSql = $@"Select count(*) from [Group] G
			inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
			left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
			left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
			   {(fieldJoins.Count > 0 ? string.Join("\n", fieldJoins.GetStatements()) : "")}
				{resourceString} 
				{whereStatements}  ";

			var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
			var count = countResults.First();

			var results = await CompanyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout);

			return new GroupApiModels() { items = results, Total = count };
		}

		public async Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(ApiExecution execution, IEnumerable<UserApiModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false)
		{
			CompanyContext.Add(execution);
			IEnumerable<UserApiUpsertResult> results;

			try
			{
				results = await ProcessUpsertUsers(execution, users, lookupFieldsPassedByValue, isInsert).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
				throw ex;
			}

			return results;
		}

		public async Task<IEnumerable<UserApiUpsertResult>> ProcessUpsertUsers(ApiExecution execution, IEnumerable<UserApiModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false)
		{
			const int ResourceTypeID = 1;

			var executionID = execution.ExecutionID;
			var results = new List<UserApiModel>();
			var validationResults = new List<UserApiUpsertResult>();

			var fieldTypes = CompanyContext.GetAssetTypeFieldTypesCore("ResourceType", 1);

			var hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());

			#region Data Tables

			var userTable = new DataTable();
			var fieldTable = new DataTable();

			userTable.Columns.Add("ItemNumber", typeof(int));
			userTable.Columns.Add("ResourceID", typeof(int));
			userTable.Columns.Add("Uid", typeof(Guid));
			userTable.Columns.Add("Username", typeof(string));
			userTable.Columns.Add("Email", typeof(string));
			userTable.Columns.Add("FirstName", typeof(string));
			userTable.Columns.Add("LastName", typeof(string));
			userTable.Columns.Add("State", typeof(int));
			userTable.Columns.Add("IsAdministrator", typeof(bool));

			fieldTable.Columns.Add("ItemNumber", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));
			//fieldTable.Columns.Add("LookupValue", typeof(string));

			#endregion

			users.ForEach(u => {
				var userRow = userTable.NewRow();
				userRow["ItemNumber"] = u.ItemNumber;
				userRow["ResourceID"] = u.ResourceID;
				userRow["Uid"] = u.uid;
				userRow["Username"] = u.Username;
				userRow["Email"] = u.Email;
				userRow["FirstName"] = u.FirstName;
				userRow["LastName"] = u.LastName;
				userRow["State"] = u.State;
				userRow["IsAdministrator"] = u.IsAdministrator;
				userTable.Rows.Add(userRow);

				u.Fields.ForEach(f =>
				{
					var ft = fieldTypes.FirstOrDefault(o => o.Name == f.Key.Trim());
					if (ft != null)
					{
						var fieldRow = fieldTable.NewRow();

						fieldRow["ItemNumber"] = u.ItemNumber;
						fieldRow["ResourceID"] = u.ResourceID;
						fieldRow["FieldName"] = f.Key.Trim();
						fieldRow["FieldValue"] = f.Value;
						fieldRow["FieldTypeID"] = ft.ID;

						fieldTable.Rows.Add(fieldRow);
					}
				});
			});

			SqlBulkCopy bulkCopy = null;

			using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
			{
				await CompanyContext.Connection.ExecuteAsync(@"
create table #Users (
	ItemNumber int, ResourceID int, [Uid] uniqueidentifier, Username nvarchar(500), Email nvarchar(500),
	FirstName nvarchar(250), LastName nvarchar(250), [State] int, IsAdministrator bit
);

create table #Fields (
	ItemNumber int, ResourceID int, 
	FieldName nvarchar(250), FieldTypeID int, FieldValue nvarchar(max), LookupValue nvarchar(max)
);", transaction: trans);

				bulkCopy = CompanyContext.Connection.CreateBulkCopy("#Users", 1000, 1200, trans);
				bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
				bulkCopy.ColumnMappings.Add("Uid", "Uid");
				bulkCopy.ColumnMappings.Add("Username", "Username");
				bulkCopy.ColumnMappings.Add("Email", "Email");
				bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
				bulkCopy.ColumnMappings.Add("LastName", "LastName");
				bulkCopy.ColumnMappings.Add("State", "State");
				bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
				await bulkCopy.WriteToServerAsync(userTable);

				bulkCopy = CompanyContext.Connection.CreateBulkCopy("#Fields", 1000, 1200, trans);
				bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
				bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
				bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
				bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
				await bulkCopy.WriteToServerAsync(fieldTable);

				await CompanyContext.Connection.ExecuteAsync(@"
merge	reporting.Global_Resource as T
using	(select * from #Users) as S
on		(T.ResourceID = S.ResourceID)
when	matched then
update  set
		T.IsAdministrator = S.IsAdministrator,
		T.State = S.State,
		T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.Email = S.Email
when	not matched by target then
insert	(ResourceID, FirstName, LastName, Email, IsAdministrator, CreatedOn, State, Uid, UpdatedOn)
values	(S.ResourceID, S.FirstName, S.LastName, S.IsAdministrator, getutcdate(), S.State, Uid, getutcdate());
", transaction: trans);
			}

			using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
			{
				try
				{


					#region Populate table values

					await CompanyContext.Connection.ExecuteAsync(@"
update  U
set     U.ResourceID = G.ResourceID
from    api.ExecutionUser U
		inner join reporting.Global_Resource G on G.[uid] = U.[Uid] and G.[State] <> @deleted
where   U.ExecutionID = @executionID and U.Success is null and U.IsNew = 0;",
						new { executionID, deleted = (int)CompanyResourceState.Deleted, ResourceTypeID }, transaction: trans
					);

					#endregion

					#region Validation



					if (lookupFieldsPassedByValue)
					{
						CompanyContext.Database.Connection.Execute(@"
update	T
set		T.LookupValue = T.[FieldValue]
from	#Fields T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup'", 
							transaction: trans);
					}
					else
					{
						CompanyContext.Database.Connection.Execute(@"
declare @listFieldTypes table (FieldTypeID int, AllowMultipleValues bit);
declare @uniqueListValues table (FieldTypeID int, AllowMultipleValues bit, FieldValue nvarchar(max), LookupValue nvarchar(max))

insert into @listFieldTypes
	select	t.FieldTypeID, s.AllowMultipleValues
	from	#Fields t
			inner join FieldType s on s.ID = t.FieldTypeID and s.[Type] = 'Lookup'
	group by t.FieldTypeID, s.AllowMultipleValues;

insert into @uniqueListValues
	select	t.FieldTypeID, s.AllowMultipleValues, t.FieldValue
	from	#Fields t
			inner join @listFieldTypes s on s.FieldTypeID = t.FieldTypeID
			cross apply string_split(t.FieldValue, ',') tmv
	group by t.FieldTypeID, s.AllowMultipleValues, t.FieldValue;

update	t
set		t.LookupValue = t.[Value]
from	@uniqueListValues t
		inner join FieldLookupValue s on s.FieldTypeID = t.FieldTypeID and s.[Text] = t.FieldValue;

update	T
set		T.LookupValue = T.[FieldValue]
from	#Fields T
		inner join @listFieldTypes ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup'


update	T
set		T.LookupValue = T.[FieldValue]
from	#Fields T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup'",
							transaction: trans);
					}

					//validate lookup fields
					await CompanyContext.Connection.ExecuteAsync(@"
update  U
set     U.Success = 0,
		U.Message = U.Message + 'Invalid lookup value for field ' + F.FieldName + '. '
from    api.ExecutionUser U
		inner join #UserFields F on F.ItemNumber = U.ItemNumber and F.ExecutionID = @executionID
		inner join FieldType FT on FT.ID = F.FieldTypeID and FT.Type = 'Lookup'
		where U.ExecutionID = @executionID and F.LookupValue is null and F.FieldValue is not null",
						new { executionID }, transaction: trans
					);

					await CompanyContext.Connection.ExecuteAsync(@"
insert into api.ExecutionField (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select  ExecutionID,
			ItemNumber,
			FieldName,
			FieldValue,
			FieldTypeID,
			LookupValue,
			null as Ignore
	from	#UserFields",
						transaction: trans
					);

					validationResults = (await CompanyContext.Connection.QueryAsync<UserApiUpsertResult>(@"
select	ItemNumber, 
		uid, 
		ExecutionItemUid, 
		Message, 
		coalesce(Success, cast(1 as bit)) as Success 
from	api.ExecutionUser 
where	ExecutionID = @executionID",
						new { executionID }, transaction: trans)
					).ToList();

					#endregion

					trans.Commit();
				}
				catch (Exception)
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
					}

					throw;
				}
			}

			#region Upsert records

			if (validationResults.Count > 0)
			{
				var userAssetType = CompanyContext.AssetTypes.SingleOrDefault(o => o.Class == AssetTypeClass.User);
				
				for (int i = 0; i < validationResults.Count; i++)
				{
					var validationResult = validationResults[i];

					if (validationResult.Success)
					{
						var upsertUser = users.SingleOrDefault(u => u.ItemNumber == (int)validationResult.ItemNumber);

						if (upsertUser != null)
						{
							var requiredFieldNames = fieldTypes.Where(f => f.IsRequired && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();

							CompanyContext.ValidateFields("ResourceType",
								ResourceTypeID,
								isInsert,
								fieldTypes,
								requiredFieldNames,
								upsertUser.Fields,
								executionID,
								upsertUser.ItemNumber,
								null,
								out bool success,
								out string message);

							if (success == false)
							{
								validationResult.Success = false;
								validationResult.Message += message;

								results.Add(validationResult);

								continue;
							}

							// Add resource.
							if (!upsertUser.ResourceID.HasValue)
							{
								if (string.IsNullOrEmpty(upsertUser.Password))
								{
									upsertUser.Password = PasswordHelper.CreateRandomPassword();
								}

								var resource = new Resource()
								{
									FirstName = upsertUser.FirstName,
									LastName = upsertUser.LastName,
									Email = upsertUser.Email,
									Username = upsertUser.Username,
									Password = PasswordHelper.HashPassword(upsertUser.Password)
								};

								CommunityContext.Add(resource);

								upsertUser.ResourceID = resource.ID;
								upsertUser.uid = resource.Uid;
								validationResult.uid = resource.Uid;
							}
							else
							{
								var resource = CommunityContext.Resources.FirstOrDefault(r => r.ID == (int)upsertUser.ResourceID);
								if (resource != null)
								{
									resource.FirstName = upsertUser.FirstName;
									resource.LastName = upsertUser.LastName;

									if ((string.Compare(upsertUser.Username, resource.Username, true) != 0) || string.Compare(upsertUser.Email, resource.Email, true) != 0)
									{
										//disallow changing the email/username if the current user is not an admin
										if (SecurityContext.IsAdministrator == false)
										{
											validationResult.Success = false;
											validationResult.uid = upsertUser.uid;
											validationResult.Message += "Non-administrator users cannot update the email address / username. ";
											results.Add(validationResult);

											continue;
										}

										//check if the resource already exists in community
										var existing = CommunityContext.Filter<Resource>(i => i.Email == upsertUser.Email  && i.Uid != upsertUser.uid).FirstOrDefault();
										if (existing == null)
										{
											existing = CommunityContext.Filter<Resource>(i => i.Username == upsertUser.Username	 && i.Uid != upsertUser.uid).FirstOrDefault();
										}

										if (existing != null)
										{
											validationResult.Success = false;
											validationResult.uid = upsertUser.uid;
											validationResult.Message += "Cannot update the user because the specified email address / username is already in use. ";
											results.Add(validationResult);

											continue;
										}

										resource.Email = upsertUser.Email;
										resource.Username = upsertUser.Username;
									}

									if (!string.IsNullOrEmpty(upsertUser.Password))
									{
										resource.Password = PasswordHelper.HashPassword(upsertUser.Password);
									}

									upsertUser.uid = resource.Uid;
									validationResult.uid = upsertUser.uid;
									resource.UpdatedOn = DateTime.UtcNow;
									CommunityContext.Update(resource);
								}
							}

							// Handle CompanyResource record in Community.
							CompanyResource companyResource;
							if (upsertUser.CompanyResourceState.HasValue)
							{
								companyResource = CommunityContext.CompanyResources.FirstOrDefault(c => c.CompanyID == SecurityContext.CompanyID && c.ResourceID == upsertUser.ResourceID);

								if (companyResource != null)
								{
									//disallow changing the admin flag if the current user is not an admin
									if (!SecurityContext.IsAdministrator && upsertUser.IsAdministrator != companyResource.IsAdministrator)
									{
										validationResult.Success = false;
										validationResult.uid = upsertUser.uid;
										validationResult.Message += "Non-administrator users cannot update the administrator flag. ";
										results.Add(validationResult);

										continue;
									}

									companyResource.IsAdministrator = upsertUser.IsAdministrator;
									companyResource.State = upsertUser.State ?? companyResource.State;

									CommunityContext.Update(companyResource);
								}
							}
							else
							{
								//disallow creating admin users if the current user is not an admin
								if (!SecurityContext.IsAdministrator && upsertUser.IsAdministrator)
								{
									validationResult.Success = false;
									validationResult.uid = upsertUser.uid;
									validationResult.Message += "Non-administrator users cannot update the administrator flag. ";
									results.Add(validationResult);

									continue;
								}

								companyResource = new CompanyResource()
								{
									ResourceID = (int)upsertUser.ResourceID,
									CompanyID = SecurityContext.CompanyID,
									State = CompanyResourceState.Active,
									IsAdministrator = upsertUser.IsAdministrator
								};

								CommunityContext.Add(companyResource);
							}


							// Handle GlobalResource record in Environment.
							var globalResource = CompanyContext.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == upsertUser.ResourceID);
							if (globalResource != null)
							{
								globalResource.FirstName = upsertUser.FirstName;
								globalResource.LastName = upsertUser.LastName;
								globalResource.Email = upsertUser.Email;
								globalResource.IsAdministrator = upsertUser.IsAdministrator;
								globalResource.State = upsertUser.State ?? companyResource.State;
								globalResource.UpdatedOn = DateTime.UtcNow;

								CompanyContext.Update(globalResource);
							}
							else
							{
								globalResource = new GlobalReportingResource
								{
									IsAdministrator = upsertUser.IsAdministrator,
									ResourceID = (int)upsertUser.ResourceID,
									Email = upsertUser.Email,
									FirstName = upsertUser.FirstName,
									LastName = upsertUser.LastName,
									State = upsertUser.State ?? companyResource.State,
									UpdatedOn = DateTime.UtcNow,
									Uid = (Guid)upsertUser.uid,
									CreatedOn = DateTime.UtcNow
								};

								CompanyContext.Add(globalResource);
							}


							// Handle Asset record in Environment.
							var userAsset = CompanyContext.Assets.SingleOrDefault(o => o.Object == "Resource" && o.ObjectID == upsertUser.ResourceID);
							if (userAsset != null)
							{
								userAsset.UpdatedBy = SecurityContext.ResourceID;
								userAsset.UpdatedOn = DateTime.UtcNow;
								CompanyContext.Update(userAsset);
							}
							else
							{
								if (userAssetType != null)
								{
									CompanyContext.Connection.Execute(
										"insert into Asset (AssetTypeID, State, Object, ObjectID, SourceID, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, Uid) values (@ID, 1, 'Resource', @ResourceID, @ResourceID, @Dt, @r, @Dt, @r, @uid)",
										new { userAssetType.ID, upsertUser.ResourceID, Dt = DateTime.UtcNow, r = SecurityContext.ResourceID, uid = (Guid)upsertUser.uid }
									);
								}
							}

						}
					
						validationResult.Message = null;
					}

					results.Add(validationResult);
				}
			}
			#endregion

			#region Merge Fields

			await CompanyContext.Connection.OpenIfClosed();

			using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
			{
				try
				{
					await CompanyContext.Connection.ExecuteAsync(@"
						drop table if exists #UserResults;
						create table #UserResults
						(
							ExecutionID uniqueidentifier not null,
							ItemNumber int not null,
							[uid] uniqueidentifier null,
							Success bit null,
							Message nvarchar(max)
						);", transaction: trans);

					var resultsTable = new DataTable();

					resultsTable.Columns.Add("ExecutionID", typeof(Guid));
					resultsTable.Columns.Add("ItemNumber", typeof(int));
					resultsTable.Columns.Add("uid", typeof(Guid));
					resultsTable.Columns.Add("Success", typeof(bool));
					resultsTable.Columns.Add("Message", typeof(string));

					results.ForEach(r =>
					{
						var row = resultsTable.NewRow();
						row["ExecutionID"] = executionID;
						row["ItemNumber"] = r.ItemNumber;

						if (r.uid.HasValue)
						{
							row["uid"] = r.uid;
						}

						if (r.Success == false)
						{
							row["Success"] = false;
						}

						row["Message"] = r.Message ?? "";

						resultsTable.Rows.Add(row);
					});

					var bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans)
					{
						DestinationTableName = "#UserResults"
					};

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("uid", "uid");
					bulkCopy.ColumnMappings.Add("Success", "Success");
					bulkCopy.ColumnMappings.Add("Message", "Message");

					await bulkCopy.WriteToServerAsync(resultsTable);

					await CompanyContext.Connection.ExecuteAsync(@"
update	U
set		U.ObjectID = GR.ResourceID,
		U.ResourceID = GR.ResourceID,
		U.Uid = GR.Uid
from	api.ExecutionUser U
		inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.ObjectID = 0
		inner join reporting.Global_resource GR on GR.uid = R.uid

update	U
set		U.AssetId = A.Id
from	api.ExecutionUser U
		inner join Asset A on (A.Uid = U.Uid)
		inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.AssetId is null
		inner join reporting.Global_resource GR on GR.uid = R.uid

update	U
		set U.AssetId = A.Id
from	api.ExecutionUser U
		inner join Asset A on (A.Object = U.Object and A.ObjectId = U.ObjectId)
		inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.AssetId is null
		inner join reporting.Global_resource GR on GR.uid = R.uid

update	U
set		U.Success = 0,
		U.Message = R.Message
from	api.ExecutionUser U
		inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and R.Success = 0", 
						transaction: trans
					);

					bool isInsertForMergeField = isInsert;

					if (isInsert == true)
					{
						var UserUpdateCountResult = (await CompanyContext.Connection.QueryAsync<int>(@"
select	count(1) 
from	api.ExecutionUser U
	inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.IsNew = 0", 
							new { executionID }, transaction: trans)
						);
						var UserUpdateCount = UserUpdateCountResult.First();

						if (UserUpdateCount > 0)
						{
							isInsertForMergeField = false;
						}
					}

					CompanyContext.MergeFields(executionID, trans, "api.ExecutionUser", SystemObjects.Resource, "A.AssetID", 0, itemNumber, sendWorkflowEvents: true, isInsert: isInsertForMergeField);

					if (hasRelationshipFieldTypes)
					{
						CompanyContext.ImportRelationships(execution, trans, "api.ExecutionUser", "A.Object", "A.ObjectID", 0, itemNumber, resolveRelationshipOnObjectId: lookupFieldsPassedByValue);
					}

					await CompanyContext.Connection.ExecuteAsync(@"
insert into api.ExecutionLog (ExecutionId, [Payload])
select	@Id,
		(select U.ResourceID as ObjectId,
				U.AssetId,
				U.ItemNumber,
				U.FirstName,
				U.LastName, 
				U.Username,
				U.IsAdministrator,
				coalesce(U.FirstName + ' ' + U.LastName, U.Username) as ObjectName,
				@isInsert as IsNew
		for json path
		) as Payload
from	api.ExecutionUser U
		inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.Success is null;

update	U
set		U.Success = 1
from	api.ExecutionUser U
	inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.Success is null", 
						new { executionID, execution.Id, isInsert }, transaction: trans
					);

					trans.Commit();
				}
				catch (Exception)
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
					}
					throw;
				}
			}

			#endregion

			//Asset Display Value logic
			await CompanyContext.Connection.ExecuteAsync(@$"
MERGE	AssetDisplayValue as ADV
USING	(
		SELECT	eu.AssetId,
				DisplayValue.DisplayValue,
				CONVERT(NVARCHAR(32), HashBytes('SHA1', DisplayValue.DisplayValue), 2) as DisplayValueHash,
				SUBSTRING(DisplayValue.DisplayValue, 1, 250) as DisplayValuePrefix
		from	api.ExecutionUser EU 
				cross apply GetAssetDisplayValueById(EU.AssetId) DisplayValue
		where	EU.ExecutionID = @executionID
				and EU.AssetId is not null
		) as S
ON		(ADV.AssetID = S.AssetID)
WHEN	matched THEN
		UPDATE	SET
				ADV.DisplayValue = s.DisplayValue,
				ADV.DisplayValueHash = s.DisplayValueHash,
				ADV.DisplayValuePrefix = s.DisplayValuePrefix
WHEN	not matched by target THEN
		INSERT	([AssetID], [DisplayValue], DisplayValueHash, DisplayValuePrefix, [UpdatedOn])
		VALUES	(S.[AssetID], S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, getutcdate());

exec api.MergeAssetPaths @executionId, @class, @begin, @end, null, 0;",
				new { executionID = execution.ExecutionID, @class = (int)AssetTypeClass.User, begin = 0, end = itemNumber }
			);

			CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.UpsertUsers);

			QueueSource.CreateMessage(constants.Queue.PostExecution, new PostExecutionQueueMessage { Action = PostExecutionQueueMessageAction.History, CompanyID = SecurityContext.CompanyID, ExecutionId = execution.Id });
			QueueSource.CreateMessage(constants.Queue.PostExecutionIndex, new PostExecutionQueueMessage { CompanyID = SecurityContext.CompanyID, ExecutionId = execution.Id });

			return results;
		}

		public async Task<ApiExecutionInfo> UpsertBulkUsers(ApiExecution execution, UserUpsertModel model)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = SecurityContext.CompanyID,
				CompanyDomainPrefix = SecurityContext.CompanyPrefix,
				ExecutionID = execution.ExecutionID,
				ResourceID = execution.ResourceID
			};

			return await CreateApiBatchJob(executionInfo, execution, model, StorageProvider, QueueSource).ConfigureAwait(false);
		}

		private bool validatePassword(string password)
		{
			if (string.IsNullOrEmpty(password)
				|| password.Length < 7 || password.Length > 25
				|| !password.Any(char.IsUpper) || !password.Any(char.IsLower)
				|| !password.Any(char.IsDigit))
			{
				return false;
			}

			return true;
		}

		public List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.UpsertGroups(execution, groups);
				CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.PutGroups);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		public List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.UpsertGroups(execution, groups);
				CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.PostGroups);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		public List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups)
		{
			CompanyContext.Add(execution);

			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.DeleteGroups(execution, groups);
				CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.DeleteGroups);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		[Obsolete]
		public async Task ClearFavorites(int resourceID)
		{
			await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && !i.IsHomePage);
		}

		public async Task DeleteFavorites(int resourceID, List<int> favoriteIds)
		{
			await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && favoriteIds.Contains(i.ID));
		}
	}
}
