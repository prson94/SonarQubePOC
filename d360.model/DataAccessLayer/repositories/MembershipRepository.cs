using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;

using Dapper;

using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer
{
	public class MembershipRepository : BaseRepository, IMembershipRepository
	{
		internal ICompanyContext CompanyContext;
		internal ICommunityContext CommunityContext;
		internal IAssetRepository AssetRepository;
		internal IQueueSource QueueSource;
		internal IStorageProvider StorageProvider;

		public MembershipRepository(ICompanyContext companyContext, ICommunityContext communityContext, IAssetRepository assetRepository, IQueueSource queueSource, IStorageProvider storageProvider)
			: base(companyContext)
		{
			CompanyContext = companyContext;
			CommunityContext = communityContext;
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

		public WorkHttpStatus DeleteResources(ApiExecution execution, IEnumerable<UserApiDeleteModel> resources)
		{

			try
			{
				List<UserApiDeleteModel> models = new List<UserApiDeleteModel>();
				foreach (var model in resources)
				{
					model.Resource = CompanyContext.GlobalReportingResources.SingleOrDefault(r => r.Uid == model.Uid && r.State != CompanyResourceState.Deleted);

					if (model.Resource == null)
					{
						return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.NotFound, string.Format(MemberShipErrors.UserUidNotFound, model.Uid));
					}

					if (model.Resource.ResourceID < 1)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidUser, string.Format(MemberShipErrors.UserUidSystemUser, model.Uid));
					}

					model.CompanyResource = CommunityContext.CompanyResources.SingleOrDefault(r => r.CompanyID == CompanyContext.CurrentCompanyID && r.ResourceID == model.Resource.ResourceID && r.State != CompanyResourceState.Deleted);

					if (model.CompanyResource == null)
					{
						return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.NotFound, string.Format(MemberShipErrors.UserUidNotFound, model.Uid));
					}
				}

				CompanyContext.Add(execution);
				CompanyContext.SetApiExecutionProcessingStartTime(execution.ExecutionID);

				foreach (var model in resources)
				{
					model.Resource.State = CompanyResourceState.Deleted;
					model.CompanyResource.State = CompanyResourceState.Deleted;

					CompanyContext.Update(model.Resource);
					CommunityContext.Update(model.CompanyResource);

					CompanyContext.Query<int>($@"insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
						select	distinct
								'Resource', 
								res.ResourceId,
								SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
								@r, 
								getutcdate(), 
								'Deleted', 
								'Resource', 
								res.ResourceId,
								'Resource', 
								SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
								'This user has been removed.'
						from reporting.Global_Resource res
						where res.resourceid = @resourceId", new
					{
						r = CompanyContext.CurrentResourceID,
						resourceId = model.Resource.ResourceID
					}).ToList();
				}

				execution.Processed = resources.Count();
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);

			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);

				return new WorkHttpStatus(HttpStatusCode.InternalServerError, AssetTypeErrors.InternalServerError, MemberShipErrors.InternalServerErrorMsg);
			}

			return new WorkHttpStatus(HttpStatusCode.OK, AssetTypeErrors.Success, MemberShipErrors.UserDeletedMessage);
		}
		public async Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false)
		{
			CompanyContext.Add(execution);
			IEnumerable<UserApiUpsertResult> results;

			try
			{
				results = await ProcessUpsertUsers(execution, users, lookupFieldsPassedByValue, isInsert, IsChangePasswordReqeust).ConfigureAwait(false);

			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);

				throw;
			}
			execution.CompletedOn = DateTime.UtcNow;
			execution.Error = results.Count(r => r.Success == false);
			execution.Processed = results.Count(r => r.Success == true);
			CompanyContext.Update(execution);

			return results;
		}

		public async Task<IEnumerable<UserApiUpsertResult>> ProcessUpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false)
		{
			const int ResourceTypeID = 1;

			var executionID = execution.ExecutionID;
			var results = new List<UserApiUpsertResult>();
			var validationResults = new List<UserApiUpsertResult>();

			var fieldTypes = CompanyContext.GetAssetTypeFieldTypesCore("ResourceType", 1);

			var hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());

			#region Data Tables

			var resourceTable = new DataTable();
			var userTable = new DataTable();
			var fieldTable = new DataTable();

			resourceTable.Columns.Add("ExecutionID", typeof(Guid));
			resourceTable.Columns.Add("ItemNumber", typeof(int));
			resourceTable.Columns.Add("ResourceID", typeof(int));
			resourceTable.Columns.Add("Username", typeof(string));
			resourceTable.Columns.Add("uid", typeof(Guid));

			userTable.Columns.Add("ExecutionID", typeof(Guid));
			userTable.Columns.Add("Uid", typeof(Guid));
			userTable.Columns.Add("ResourceID", typeof(int));

			userTable.Columns.Add("ExecutionItemUid", typeof(Guid));
			userTable.Columns.Add("ItemNumber", typeof(int));
			userTable.Columns.Add("Username", typeof(string));
			userTable.Columns.Add("FirstName", typeof(string));
			userTable.Columns.Add("LastName", typeof(string));
			userTable.Columns.Add("Password", typeof(string));
			userTable.Columns.Add("State", typeof(int));
			userTable.Columns.Add("IsAdministrator", typeof(bool));
			userTable.Columns.Add("IsNew", typeof(bool));
			userTable.Columns.Add("Success", typeof(bool));
			userTable.Columns.Add("Message", typeof(string));
			userTable.Columns.Add("Object", typeof(string));
			userTable.Columns.Add("ObjectID", typeof(int));
			userTable.Columns.Add("ObjectType", typeof(string));
			userTable.Columns.Add("ObjectTypeID", typeof(int));

			fieldTable.Columns.Add("ExecutionID", typeof(Guid));
			fieldTable.Columns.Add("ItemNumber", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));
			fieldTable.Columns.Add("LookupValue", typeof(string));

			#endregion

			#region Process Community

			int itemNumber = 0;
			foreach (var user in users)
			{
				itemNumber++;
				user.ItemNumber = itemNumber;

				var row = resourceTable.NewRow();

				row["ExecutionID"] = executionID;
				row["ItemNumber"] = itemNumber;
				row["Username"] = user.Username;
				if (user.uid.HasValue)
				{
					row["uid"] = user.uid;
				}

				resourceTable.Rows.Add(row);
			}

			if (CommunityContext.Connection.State == ConnectionState.Closed)
			{
				await CommunityContext.Connection.OpenAsync();
			}

			CompanyContext.SetApiExecutionProcessingStartTime(execution.ExecutionID);

			using (SqlTransaction trans = CommunityContext.Connection.BeginTransaction())
			{
				try
				{
					await CommunityContext.Connection.ExecuteAsync(@"
						drop table if exists #UserResources;
						create table #UserResources
						(
							ExecutionID uniqueidentifier,
							ItemNumber int,
							Username nvarchar(500),
							ResourceID int,
							[uid] uniqueidentifier,
							CompanyResourceState int
						)
						", transaction: trans);

					SqlBulkCopy bulkCopy = new SqlBulkCopy(CommunityContext.Connection, SqlBulkCopyOptions.Default, trans)
					{
						DestinationTableName = "#UserResources"
					};

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Username", "Username");
					bulkCopy.ColumnMappings.Add("uid", "uid");

					await bulkCopy.WriteToServerAsync(resourceTable);

					await CommunityContext.Connection.ExecuteAsync(@"
						update  U
						set     U.ResourceID = coalesce(R2.ID, R.ID)
						from    #UserResources U
								left join [Resource] R on R.Email = U.Username
								left join [Resource] R2 on R2.[uid] = U.[uid];

						update  U
						set U.CompanyResourceState = R.[State],
							U.uid = CR.uid
						from #UserResources U
						left join [CompanyResource] R on R.ResourceID = U.ResourceID and R.CompanyID = @companyId
						left join [Resource] CR on CR.ID = R.ResourceID;
						", new { companyId = CompanyContext.CurrentCompanyID }, transaction: trans);

					var communityResults = await CommunityContext.Connection.QueryAsync<dynamic>(@"select * from #UserResources", transaction: trans);

					foreach (var result in communityResults)
					{
						var user = users.SingleOrDefault(u => u.ItemNumber == result.ItemNumber);
						if (user != null)
						{
							user.ResourceID = result.ResourceID;
							user.uid = user.IsNew ? result.uid : user.uid;
							user.CompanyResourceState = (CompanyResourceState?)result.CompanyResourceState;
						}
					}

					await CommunityContext.Connection.ExecuteAsync(@"drop table if exists #UserResources", transaction: trans);

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

			foreach (var user in users)
			{
				var row = userTable.NewRow();
				var CurrPassword = "";
				var NewPassword = "";

				var success = true;
				var messages = new List<string>();

				user.FirstName = user.FirstName.SanitizeHtml();
				user.LastName = user.LastName.SanitizeHtml();

				if (user.IsNew)
				{
					if (user.ResourceID.HasValue)
					{
						if (user.CompanyResourceState.HasValue && user.CompanyResourceState != CompanyResourceState.Deleted)
						{
							success = false;
							messages.Add(MemberShipErrors.ResourceUserNameExists);
						}
					}

					if (user.State.HasValue)
					{
						success = false;
						messages.Add(MemberShipErrors.CanNotProvideStateOfNewUser);
					}

					if (!string.IsNullOrEmpty(user.Password))
					{
						if (!validatePassword(user.Password))
						{
							success = false;
							messages.Add(MemberShipErrors.PasswordRule);
						}
					}

					if (string.IsNullOrEmpty(user.FirstName))
					{
						success = false;
						messages.Add(MemberShipErrors.FirstNameMissing);
					}

					if (string.IsNullOrEmpty(user.LastName))
					{
						success = false;
						messages.Add(MemberShipErrors.LastNameMissing);
					}
				}
				else
				{
					if (!user.uid.HasValue)
					{
						success = false;
						messages.Add(MemberShipErrors.ProvideUserUid);
					}

					if (!user.ResourceID.HasValue && user.uid.HasValue)
					{
						success = false;
						messages.Add(MemberShipErrors.ResourceUidNotFound);
					}

					//Password Change
					if (IsChangePasswordReqeust)
					{
						NewPassword = user.Fields.Where(z => z.Key == "NewPassword").Select(z => z.Value).FirstOrDefault();
						CurrPassword = user.Fields.Where(z => z.Key == "CurrentPassword").Select(z => z.Value).FirstOrDefault();

						if (NewPassword == null)
						{
							success = false;
							messages.Add(MemberShipErrors.ResourceUidNotFound);
						}
						else
						{
							user.Password = NewPassword;
						}

						if (CurrPassword == null)
						{
							success = false;
							messages.Add(MemberShipErrors.MissingCurrentPasswordParameter);
						}

						var CurrPasswordHash = PasswordHelper.HashPassword(CurrPassword);
						var existing = CommunityContext.Filter<Resource>(i => i.Password == CurrPasswordHash && i.Uid == user.uid).FirstOrDefault();

						if (existing == null)
						{
							success = false;
							messages.Add(MemberShipErrors.CurrentPasswordWrong);
						}

						if (NewPassword == CurrPassword)
						{
							success = false;
							messages.Add(MemberShipErrors.NewAndCurrentNotSame);
						}
					}

					if (!string.IsNullOrEmpty(user.Password))
					{
						if (!validatePassword(user.Password))
						{
							success = false;
							messages.Add(MemberShipErrors.PasswordRule);
						}
					}

					if (user.uid != null)
					{
						Guid currentUser = (Guid)user.uid;
						var isUser = AssetRepository.GetAssetByUID(currentUser);

						if (isUser == null || isUser.Object != "Resource")
						{
							success = false;
							messages.Add(string.Format(MemberShipErrors.UserUidNotFound, user.uid));
						}
					}

					if (string.IsNullOrEmpty(user.FirstName))
					{
						success = false;
						messages.Add(MemberShipErrors.FirstNameMissing);
					}

					if (string.IsNullOrEmpty(user.LastName))
					{
						success = false;
						messages.Add(MemberShipErrors.LastNameMissing);
					}
				}

				if (user.FirstName != null && user.FirstName.Length > 250)
				{
					success = false;
					messages.Add(MemberShipErrors.FirstNameTooLong);
				}

				if (user.LastName != null && user.LastName.Length > 250)
				{
					success = false;
					messages.Add(MemberShipErrors.LastNameTooLong);
				}

				if (string.IsNullOrEmpty(user.Username) || !Regex.IsMatch(user.Username + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
				{
					success = false;
					messages.Add(MemberShipErrors.InvalidEmail);
				}
				else if (users.Count(u => u.Username.Trim().Equals(user.Username.Trim(), StringComparison.InvariantCultureIgnoreCase)) > 1)
				{
					success = false;
					messages.Add(MemberShipErrors.UsernameDuplicate);
				}


				if (user.CompanyResourceState.HasValue)
				{
					if (user.IsNew)
					{
						user.State = CompanyResourceState.Active;
						user.IsNew = false;
					}
				}

				row["ExecutionID"] = executionID;

				if (user.uid.HasValue)
				{
					row["Uid"] = user.uid;
				}

				if (user.ResourceID.HasValue)
				{
					row["ResourceID"] = user.ResourceID;
				}

				if (user.ExecutionItemUid.HasValue)
				{
					row["ExecutionItemUId"] = user.ExecutionItemUid;
				}

				row["ItemNumber"] = user.ItemNumber;
				row["Username"] = user.Username;

				row["FirstName"] = user.FirstName;
				row["LastName"] = user.LastName;

				row["Password"] = user.Password;

				if (user.State.HasValue && !IsChangePasswordReqeust)
				{
					row["State"] = (int)user.State;
				}

				row["IsAdministrator"] = user.IsAdministrator;
				row["IsNew"] = user.IsNew;
				row["Object"] = "Resource";
				row["ObjectID"] = user.ResourceID ?? 0;
				row["ObjectType"] = "ResourceType";
				row["ObjectTypeID"] = ResourceTypeID;

				userTable.Rows.Add(row);

				if (user.Fields != null && !IsChangePasswordReqeust)
				{
					foreach (var field in user.Fields.Keys)
					{
						var fieldType = fieldTypes.FirstOrDefault(f => f.Name == field);

						if (fieldType == null)
						{
							success = false;
							messages.Add(string.Format(MemberShipErrors.FieldTypeKeyNotFound, field));
						}

						var fieldRow = fieldTable.NewRow();
						fieldRow["ExecutionID"] = executionID;
						fieldRow["ItemNumber"] = user.ItemNumber;
						fieldRow["FieldName"] = field;
						fieldRow["FieldValue"] = user.Fields[field];
						fieldRow["FieldTypeID"] = fieldType != null ? fieldType.ID : (object)DBNull.Value;

						fieldTable.Rows.Add(fieldRow);
					}
				}

				if (!success)
				{
					row["Success"] = false;
				}

				row["Message"] = messages.Any() ? string.Join(". ", messages) + ". " : "";
			}

			#region Bulk Copy Company

			if (CompanyContext.Connection.State == ConnectionState.Closed)
			{
				await CompanyContext.Connection.OpenAsync();
			}

			using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
			{
				try
				{
					await CompanyContext.Connection.ExecuteAsync(@"
						drop table if exists #UserFields;
						create table #UserFields
						(
							ExecutionID uniqueidentifier not null,
							ItemNumber int not null,
							FieldName nvarchar(250),
							FieldValue nvarchar(max),
							FieldTypeID int,
							LookupValue nvarchar(max)
						);

						", transaction: trans);

					SqlBulkCopy bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans)
					{
						DestinationTableName = "api.ExecutionUser"
					};

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Username", "Username");
					bulkCopy.ColumnMappings.Add("Uid", "Uid");
					bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");

					bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
					bulkCopy.ColumnMappings.Add("LastName", "LastName");
					bulkCopy.ColumnMappings.Add("State", "State");
					bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
					bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
					bulkCopy.ColumnMappings.Add("Success", "Success");
					bulkCopy.ColumnMappings.Add("Message", "Message");
					bulkCopy.ColumnMappings.Add("Object", "Object");
					bulkCopy.ColumnMappings.Add("ObjectID", "ObjectID");
					bulkCopy.ColumnMappings.Add("ObjectType", "ObjectType");
					bulkCopy.ColumnMappings.Add("ObjectTypeID", "ObjectTypeID");

					await bulkCopy.WriteToServerAsync(userTable);

					bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans)
					{
						DestinationTableName = "#UserFields"
					};

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
					bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
					bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					bulkCopy.ColumnMappings.Add("LookupValue", "LookupValue");

					await bulkCopy.WriteToServerAsync(fieldTable);

					#region Populate table values

					await CompanyContext.Connection.ExecuteAsync(@"
						update  U
						set     U.ResourceID = G.ResourceID
						from    api.ExecutionUser U
								inner join reporting.Global_Resource G on G.[uid] = U.[Uid] and G.[State] <> @deleted
						where   U.ExecutionID = @executionID and U.Success is null and U.IsNew = 0;						
						", new { executionID, deleted = (int)CompanyResourceState.Deleted, ResourceTypeID }, transaction: trans);

					#endregion

					#region Validation
					if (!IsChangePasswordReqeust)
					{
						await CompanyContext.Connection.ExecuteAsync(@"
						update  U
						set     U.Success = 0,
								U.Message = U.Message + 'Resource for this uid not found. '
						from    api.ExecutionUser U
						where   U.Success is null and U.IsNew = 0 and U.ResourceID is null and U.ExecutionID = @executionID;

						update  U
						set     U.Success = 0,
								U.Message = U.Message + 'One or more field values supplied is missing a field type. '
						from    api.ExecutionUser U
								cross apply (
									select  count(*) as MissingCount 
									from    #UserFields F 
									where   F.ItemNumber = U.ItemNumber 
											and F.ExecutionID = U.ExecutionID
											and F.FieldTypeID is null
								) C
						where   U.Success is null and U.ExecutionID = @executionID and C.MissingCount > 0;

						update  U
						set     U.Success = 0,
								U.Message = U.Message + 'Missing required fields. '
						from    api.ExecutionUser U
								cross apply (
									select  count(*) as MissingCount
									from    FieldType F
									where   F.Object = 'ResourceType' 
											and F.ObjectID = @ResourceTypeID and F.IsRequired = 1
											and not exists (
												select  1 
												from    #UserFields R 
												where   R.ItemNumber = U.ItemNumber 
														and R.ExecutionID = U.ExecutionID 
														and R.FieldTypeID = F.ID
											)
								) C
						where   U.Success is null and U.ExecutionID = @executionID and C.MissingCount > 0;

						", new { executionID, deleted = (int)CompanyResourceState.Deleted, ResourceTypeID }, transaction: trans);

						if (lookupFieldsPassedByValue)
						{
							CompanyContext.CopyFieldLookupValuesAsIs(execution.ExecutionID, 3600, "#UserFields", trans);
						}
						else
						{
							CompanyContext.ResolveFieldLookupValues(executionID, "#UserFields", 3600, trans);
						}

						//validate lookup fields
						await CompanyContext.Connection.ExecuteAsync(@"
						update  U
						set     U.Success = 0,
								U.Message = U.Message + 'Invalid lookup value for field ' + F.FieldName + '. '
						from    api.ExecutionUser U
						inner join #UserFields F on F.ItemNumber = U.ItemNumber and F.ExecutionID = @executionID
						inner join FieldType FT on FT.ID = F.FieldTypeID and FT.Type = 'Lookup'
						where U.ExecutionID = @executionID and F.LookupValue is null and F.FieldValue is not null
						", new { executionID }, transaction: trans);

						await CompanyContext.Connection.ExecuteAsync(@"
						insert into api.ExecutionField (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
						select  ExecutionID,
						ItemNumber,
						FieldName,
						FieldValue,
						FieldTypeID,
						LookupValue,
						null as Ignore
						from #UserFields
						", transaction: trans);
					}

					validationResults = (await CompanyContext.Connection.QueryAsync<UserApiUpsertResult>(@"
						select ItemNumber, 
						uid, 
						ExecutionItemUid, 
						Message, 
						coalesce(Success, cast(1 as bit)) as Success 
						from api.ExecutionUser 
						where ExecutionID = @executionID", new { executionID }, transaction: trans))
						.ToList();

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

			#endregion

			#region Upsert records

			foreach (var result in validationResults)
			{

				if (result.Success == true)
				{
					var user = users.SingleOrDefault(u => u.ItemNumber == result.ItemNumber);

					if (user != null)
					{
						if (!IsChangePasswordReqeust)
						{
							var requiredFieldNames = fieldTypes.Where(f => f.IsRequired && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();

							CompanyContext.ValidateFields("ResourceType",
							   ResourceTypeID,
							   isInsert,
							   fieldTypes,
							   requiredFieldNames,
							   user.Fields,
							   executionID,
							   user.ItemNumber,
							   null,
							   out bool success,
							   out string message);

							if (success == false)
							{
								result.Success = false;
								result.Message += message;

								results.Add(result);

								continue;
							}
						}

						//add resource
						if (!user.ResourceID.HasValue)
						{
							if (string.IsNullOrEmpty(user.Password))
							{
								user.Password = PasswordHelper.CreateRandomPassword();
							}

							var resource = new Resource()
							{
								FirstName = user.FirstName,
								LastName = user.LastName,
								Email = user.Username,
								Username = user.Username,
								Password = PasswordHelper.HashPassword(user.Password)
							};

							CommunityContext.Add(resource);

							user.ResourceID = resource.ID;
							user.uid = resource.Uid;
							result.uid = resource.Uid;
						}
						else
						{
							var resource = CommunityContext.Resources.FirstOrDefault(r => r.ID == (int)user.ResourceID);
							if (resource != null)
							{
								resource.FirstName = user.FirstName;
								resource.LastName = user.LastName;

								if (string.Compare(user.Username, resource.Username, true) != 0)
								{
									//disallow changing the email/username if the current user is not an admin
									if (CompanyContext.CurrentResourceIsAdmin == false)
									{
										result.Success = false;
										result.uid = user.uid;
										result.Message += "Non-administrator users cannot update the email address / username. ";
										results.Add(result);

										continue;
									}

									//check if the resource already exists in community
									var existing = CommunityContext.Filter<Resource>(i => i.Email == user.Username && i.Uid != user.uid).FirstOrDefault();

									if (existing != null)
									{
										result.Success = false;
										result.uid = user.uid;
										result.Message += "Cannot update the user because the specified email address / username is already in use. ";
										results.Add(result);

										continue;
									}

									resource.Email = user.Username;
									resource.Username = user.Username;
								}

								if (!string.IsNullOrEmpty(user.Password))
								{
									resource.Password = PasswordHelper.HashPassword(user.Password);
								}

								user.uid = resource.Uid;
								resource.UpdatedOn = DateTime.UtcNow;
								CommunityContext.Update(resource);
							}
						}

						if (!IsChangePasswordReqeust)
						{
							CompanyResource companyResource;

							if (user.CompanyResourceState.HasValue)
							{
								companyResource = CommunityContext.CompanyResources.FirstOrDefault(c => c.CompanyID == CompanyContext.CurrentCompanyID && c.ResourceID == user.ResourceID);

								if (companyResource != null)
								{
									//disallow changing the admin flag if the current user is not an admin
									if (CompanyContext.CurrentResourceIsAdmin == false && user.IsAdministrator != companyResource.IsAdministrator)
									{
										result.Success = false;
										result.uid = user.uid;
										result.Message += "Non-administrator users cannot update the administrator flag. ";
										results.Add(result);

										continue;
									}

									companyResource.IsAdministrator = user.IsAdministrator;
									companyResource.State = user.State ?? companyResource.State;

									CommunityContext.Update(companyResource);
								}
							}
							else
							{
								//disallow creating admin users if the current user is not an admin
								if (CompanyContext.CurrentResourceIsAdmin == false && user.IsAdministrator == true)
								{
									result.Success = false;
									result.uid = user.uid;
									result.Message += "Non-administrator users cannot update the administrator flag. ";
									results.Add(result);

									continue;
								}

								companyResource = new CompanyResource()
								{
									ResourceID = (int)user.ResourceID,
									CompanyID = CompanyContext.CurrentCompanyID,
									State = CompanyResourceState.Active,
									IsAdministrator = user.IsAdministrator
								};

								CommunityContext.Add(companyResource);
							}

							var globalResource = CompanyContext.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == user.ResourceID);

							if (globalResource != null)
							{
								globalResource.FirstName = user.FirstName;
								globalResource.LastName = user.LastName;
								globalResource.Email = user.Username;
								globalResource.IsAdministrator = user.IsAdministrator;
								globalResource.State = user.State ?? companyResource.State;
								globalResource.UpdatedOn = DateTime.UtcNow;

								CompanyContext.Update(globalResource);
							}
							else
							{
								globalResource = new GlobalReportingResource
								{
									IsAdministrator = user.IsAdministrator,
									ResourceID = (int)user.ResourceID,
									Email = user.Username,
									FirstName = user.FirstName,
									LastName = user.LastName,
									State = user.State ?? companyResource.State,
									UpdatedOn = DateTime.UtcNow,
									Uid = (Guid)user.uid,
									CreatedOn = DateTime.UtcNow
								};

								CompanyContext.Add(globalResource);
							}
						}
					}
				}

				if (result.Success)
				{
					result.Message = null;
				}

				results.Add(result);
			}

			#endregion

			#region Merge Fields

			if (CompanyContext.Connection.State == ConnectionState.Closed)
			{
				await CompanyContext.Connection.OpenAsync();
			}

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
						);
						", transaction: trans);

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
						update U
						set U.ObjectID = GR.ResourceID,
							U.ResourceID = GR.ResourceID,
							U.Uid = GR.Uid
						from api.ExecutionUser U
						inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.ObjectID = 0
						inner join reporting.Global_resource GR on GR.uid = R.uid

						update U
						set U.AssetId = A.Id
						from api.ExecutionUser U
						inner join Asset A on (A.Uid = U.Uid)
						inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.AssetId is null
						inner join reporting.Global_resource GR on GR.uid = R.uid

						update U
						set U.AssetId = A.Id
						from api.ExecutionUser U
						inner join Asset A on (A.Object = U.Object and A.ObjectId = U.ObjectId)
						inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.AssetId is null
						inner join reporting.Global_resource GR on GR.uid = R.uid

						update U
						set U.Success = 0,
							U.Message = R.Message
						from api.ExecutionUser U
						inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and R.Success = 0
						", transaction: trans);

					if (!IsChangePasswordReqeust)
					{
						bool isInsertForMergeField = isInsert;

						if (isInsert == true)
						{
							var UserUpdateCountResult = (await CompanyContext.Connection.QueryAsync<int>(@"
								select count(1) 
								from api.ExecutionUser U
								inner join #UserResults R 
								on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.IsNew = 0
								", new { executionID }, transaction: trans));
							var UserUpdateCount = UserUpdateCountResult.First();

							if (UserUpdateCount > 0)
							{
								isInsertForMergeField = false;
							}
						}

						CompanyContext.MergeFields(executionID, trans, "api.ExecutionUser", SystemObjects.Resource, "A.AssetID", 0, itemNumber, sendWorkflowEvents: true, isInsert: isInsertForMergeField);

						if (hasRelationshipFieldTypes)
						{
							CompanyContext.ImportRelationships(executionID, trans, "api.ExecutionUser", "A.Object", "A.ObjectID", 0, itemNumber, resolveRelationshipOnObjectId: lookupFieldsPassedByValue);
						}
					}

					trans.Commit();

					// TODO: Add event grid calls here.
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

			using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
			{
				try
				{
					string oldValuesSQL = "";
					string logMessage = "Created";
					if (!isInsert)
					{
						logMessage = "Updated";
						oldValuesSQL = @"update  ar
							set ar.OldValue = fa.Value
							from #auditRecords ar
							inner join reporting.Global_Resource gr on gr.uid = ar.uid
							outer apply (select top 1 ID from reporting.Global_Audit where Object = 'Resource' and ObjectId = gr.resourceid and Action in ('Created','Updated') order by id desc)Audit(ID)
							left join reporting.Global_FieldAudit fa on fa.auditid = audit.id and fa.fieldname = ar.fieldname";
					}

					await CompanyContext.Connection.ExecuteAsync($@"
							drop table if exists #auditRecords
							create table #auditRecords (uid uniqueidentifier, FieldName nvarchar(200), OldValue nvarchar(max), NewValue nvarchar(max))

							;with cte as (select ex.*, gr.uid as resourceUid from api.executionuser ex
							inner join reporting.Global_Resource gr on gr.resourceid = ex.ResourceID
							where ex.executionid = @executionid and (ex.success <> 0 or ex.success is null))
							insert into #auditRecords
							select cte.resourceUid, 'Email','', cte.Username from cte
							union 
							select cte.resourceUid, 'First Name','', cte.FirstName from cte
							union
							select cte.resourceUid, 'Last Name', '',cte.lastName from cte
							union
							select cte.resourceUid, 'Is Administrator', '',try_cast( cte.IsAdministrator as nvarchar(255)) from cte

							insert into #auditRecords
							select gr.uid, ef.FieldName,'', ef.fieldvalue from api.executionuser ex
							inner join reporting.Global_Resource gr on gr.resourceid = ex.ResourceID
							left join api.executionfield ef on ef.executionid = ex.executionid and ef.itemnumber = ex.ItemNumber
							where ex.executionid = @executionid and (ex.success <> 0 or ex.success is null)
							
							{oldValuesSQL}

							declare @audit table (auditId int)
							insert into reporting.Global_Audit
							OUTPUT INSERTED.ID
							INTO @audit
							select distinct 'Resource', gr.ResourceId, SUBSTRING(gr.FirstName + ' ' + gr.LastName,0,250), @currentresourceid, GETUTCDATE(), '{logMessage}', 'Resource', gr.ResourceId, 'Resource', SUBSTRING(gr.FirstName + ' ' + gr.LastName,0,250),'Resource {logMessage}' from #auditRecords ar
							inner join reporting.Global_Resource gr on gr.uid = ar.uid

							insert into reporting.global_fieldaudit
							select a.auditid,0, ar.fieldname, 1,ar.newvalue, ar.oldvalue from @audit a
							inner join reporting.Global_Audit ga on ga.id = a.auditid
							inner join reporting.Global_Resource gr on gr.ResourceId = ga.ObjectID
							inner join #auditRecords ar on gr.uid = ar.uid
							where isnull(ar.newvalue,'') <> isnull(ar.oldvalue,'')
							order by ar.uid

							insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
							select 'ObjectIndex', 'U', 'Resource', ResourceID, getdate(), AssetID
							from api.ExecutionUser 
							where ExecutionID = @executionid
							and Success is null",
							new { executionID, CompanyContext.CurrentResourceID },
							transaction: trans);

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

					execution.ErrorMessage += ";Audit Log creation failed";
					execution.CompletedOn = DateTime.UtcNow;
					CompanyContext.Update(execution);

					throw;
				}
			}

			#endregion

			await CompanyContext.Connection.ExecuteAsync(@$"
						MERGE	dbo.AssetDisplayValue as ADV
						USING	(
							SELECT
								eu.AssetId,
								DisplayValue.DisplayValue,
								CONVERT(NVARCHAR(32), HashBytes('SHA1', DisplayValue.DisplayValue), 2) as DisplayValueHash,
								SUBSTRING(DisplayValue.DisplayValue, 1, 250) as DisplayValuePrefix
							from api.ExecutionUser EU 
							cross apply GetAssetDisplayValueById(EU.AssetId) DisplayValue
							where  EU.ExecutionID = @executionID
							and EU.AssetId is not null
						) as S
						ON		(ADV.AssetID = S.AssetID)
						WHEN	matched THEN
						UPDATE	SET
							ADV.DisplayValue = s.DisplayValue,
							ADV.DisplayValueHash = s.DisplayValueHash,
							ADV.DisplayValuePrefix = s.DisplayValuePrefix
						WHEN not matched by target THEN
						INSERT	([AssetID], [DisplayValue], DisplayValueHash, DisplayValuePrefix, [UpdatedOn])
						VALUES	(S.[AssetID], S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, getutcdate());

						exec api.MergeAssetPaths @executionId, @class, @begin, @end, null, 0;",
							new
							{
								executionID = execution.ExecutionID,
								@class = (int)AssetTypeClass.User,
								begin = 0,
								end = itemNumber
							});

			return results;
		}

		public async Task<ApiExecutionInfo> UpsertBulkUsers(ApiExecution execution, UserUpsertModel model)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = CompanyContext.CurrentCompanyID,
				CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
				ExecutionID = Guid.NewGuid(),
				ResourceID = execution.ResourceID,
				Action = ApiExecutionAction.UpsertUsers
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
				results = CompanyContext.UpdateGroups(execution, groups);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}

			return results;
		}

		public List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.UpdateGroups(execution, groups);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
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

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}

			return results;
		}

		private string RoutePrefixToObjectType(string prefix)
		{
			switch (prefix)
			{
				case "artifact":
				case "domain":
				case "policy":
				case "reference":
					return char.ToUpper(prefix[0]) + prefix.ToLower().Substring(1);
				case "admin/lookups":
					return "Lookup";
				case "quality/rule":
					return "Rule";
				case "model":
					return "Taxonomy";
				case "resource":
				case "resource/list":
					return "Resource";
				case "cart":
					return "ShoppingCart";
				case "group":
				case "groups":
					return "Group";
				default:
					return "";
			}
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

		public async Task AddClaim(ClaimPostApiModel claim)
		{
			var newClaim = new ClaimMapping();
			var companyDomainSetting = CommunityContext
				.CompanyDomainSettings
				.FirstOrDefault(d =>
					d.CompanyID == CompanyContext.CurrentCompanyID
					&& d.DomainSettingID == CompanyContext.CurrentDomainSettingID);

			if (claim.Location == ClaimLocation.Environment)
			{
				newClaim.ClientId = CompanyContext.CurrentClientID;
				newClaim.CompanyId = CompanyContext.CurrentCompanyID;
				newClaim.DomainSettingId = 0;
			}
			else if (claim.Location == ClaimLocation.Idp)
			{
				newClaim.ClientId = CompanyContext.CurrentClientID;
				newClaim.CompanyId = CompanyContext.CurrentCompanyID;
				newClaim.DomainSettingId = CompanyContext.CurrentDomainSettingID;
			}
			else if (claim.Location == ClaimLocation.Client)
			{
				newClaim.ClientId = CompanyContext.CurrentClientID;
				newClaim.CompanyId = 0;
				newClaim.DomainSettingId = 0;
			}
			else
			{
				newClaim.ClientId = 0;
				newClaim.CompanyId = 0;
				newClaim.DomainSettingId = 0;
			}

			newClaim.ClaimType = claim.ClaimType;
			newClaim.AuthenticationType = companyDomainSetting.AuthenticationType;
			newClaim.Action = claim.Action;
			newClaim.Path = claim.Path;
			newClaim.IsArray = claim.IsArray;

			CommunityContext.ClaimMappings.Add(newClaim);
			CommunityContext.SaveChanges();

		}
		public async Task UpdateClaim(int id, ClaimPutApiModel claim)
		{
			var existingClaim = CommunityContext.ClaimMappings.FirstOrDefault(c => c.Id == id);
			if (existingClaim != null)
			{
				existingClaim.Action = claim.Action;
				existingClaim.Path = claim.Path;
				existingClaim.IsArray = claim.IsArray;

				CommunityContext.SaveChanges();
			}
		}

		public async Task DeleteClaim(int id)
		{
			var existingClaim = CommunityContext.ClaimMappings.FirstOrDefault(c => c.Id == id);
			if (existingClaim != null)
			{
				CommunityContext.ClaimMappings.Remove(existingClaim);
				CommunityContext.SaveChanges();
			}
		}

		public async Task<IEnumerable<ClaimApiViewModel>> GetClaims()
		{
			var sql = @"
				select 0 as Location, C.* from ClaimMapping C where ClientID = 0 and CompanyID = 0 and DomainSettingID = 0
				union all
				select 1 as Location, C.* from ClaimMapping C where ClientID = @CurrentClientID and CompanyID = 0 and DomainSettingID = 0
				union all
				select 2 as Location, C.* from ClaimMapping C where ClientID = @CurrentClientID and CompanyID = @CurrentCompanyID and DomainSettingID = 0
				union all
				select 3 as Location, C.* from ClaimMapping C where ClientID = @CurrentClientID and CompanyID = @CurrentCompanyID and DomainSettingID = @CurrentDomainSettingID
			";

			return CommunityContext.Query<ClaimApiViewModel>(sql, new { CompanyContext.CurrentClientID, CompanyContext.CurrentDomainSettingID, CompanyContext.CurrentCompanyID });
		}
	}
}
