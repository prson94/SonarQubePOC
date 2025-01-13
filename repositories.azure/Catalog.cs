using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using Dapper.Contrib.Extensions;
using repositories.resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Catalog : Repository, ICatalog
	{
		readonly Guid SYSTEM_TAG_TYPE_UID = new Guid("00000001-0000-0000-0000-b00000000011");

		readonly string TAG_API_MODEL_SQL_WITHOUT_WHERE = @"
select	t.uid, 
		t.[Value],
		cnt.UseCount,
		c.Uid as CreatedByUid,
		t.CreatedOn,
		u.Uid as UpdatedByUid,
		t.UpdatedOn,
		c.FirstName as CreatedByFirstName,
		c.LastName as CreatedByLastName,
		tt.Uid as TagTypeUID
from	Tag t
		cross apply (select count(1) as UseCount from AssetTag where TagID = t.ID) as cnt
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
		inner join TagType tt on tt.ID = t.TagTypeID";

		public Catalog(DapperConnectionProvider provider): base(provider) { }

		public async Task<RepositoryResponse<IEnumerable<TagApiModel>>> ConsolidateTagsAsync(Guid parentUid, List<Guid> uidsToMerge)
		{
			var response = new RepositoryResponse<IEnumerable<TagApiModel>>(null, 200, true, "");

			var dt = new DataTable();
			dt.Columns.Add("uid", typeof(Guid));
			uidsToMerge.ForEach(t =>
			{
				dt.Rows.Add(t);
			});

			var dbArgs = new DynamicParameters();
			dbArgs.Add("userId", CurrentUserId, DbType.Int32);
			dbArgs.Add("parentUid", parentUid);
			dbArgs.Add("children", dt.AsTableValuedParameter("dbo.UidTable"));

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response.Data = await connection.QueryAsync<TagApiModel>("exec api.ConsolidateTags @userId, @parentUid, @children", dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> CreateAssetTagAsync(long assetId, int tagId)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response = new RepositoryResponse<bool>(true, 201, true, "");
				await connection.ExecuteAsync($@"
declare @audittable table (auditID int,
Object varchar(50),
ObjectID int
);

declare @PreviousValue nvarchar(max),
@auditID int,
@ObjectID int,
@Object varchar(50);

drop table if exists #tbl;

insert into AssetTag ([uid], AssetID, TagID, CreatedOn, CreatedBy) values (@uid, @assetId, @tagId, @dt, @u);

declare @version int;
select  @version = COALESCE(max([Version]),0)+1 from reporting.Global_Audit l inner join Asset a on a.Object = l.Object and a.ObjectID = l.ObjectID and a.ID = @assetId;

insert into reporting.Global_Audit ([Object], ObjectID, ObjectName, ResourceID, [Date], [Action], [ActionObject], ActionObjectId, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version]) 
output inserted.ID, inserted.[Object], inserted.[ObjectID] into @audittable
	select	a.Object, a.ObjectID, d.DisplayValue, @u, @dt, 'Assigned', 'Tag', @tagId, 'Tags', t.[Value], 'Tag assigned', @version
	from	Asset a
			left join AssetDisplayValue d on d.AssetID = a.ID 
			join Tag t on t.ID = @tagId
	where	a.ID = @assetId;

select @auditID = auditID,
@ObjectID  = ObjectID,
@Object = Object
from @audittable;

select	'Tags' FieldName, 
string_Agg(T.Value, ', ') within group  (order by TA.id asc) NewValue
into #tbl
from	AssetTag TA 
inner join Tag T on T.ID = TA.TagID
where TA.AssetID = @assetId;

select	top 1
@PreviousValue = [Value]
from	reporting.Global_Audit a
inner join reporting.Global_FieldAudit f on f.AuditID  = a.ID and  f.FieldName = 'Tags' and f.FieldTypeID = 0
where 	a.Object = @Object
and 	a.ObjectID = @ObjectID
and a.id != @auditID
order by a.id desc;

insert into reporting.Global_FieldAudit ( AuditID, FieldTypeID, FieldName, Value, PreviousValue )
select	@auditID,
0,
FieldName,
NewValue,
@PreviousValue
from	#tbl

drop table if exists #tbl;
",
					new { assetId, tagId, uid = Guid.NewGuid(), u = CurrentUserId, dt = DateTime.UtcNow });
			}

			return response;
		}

		public async Task<RepositoryResponse<AssetCrossReference>> CreateCrossReferenceAsync(AssetCrossReference model)
		{
			var userErrorMessages = new List<string>();

			if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.ExternalID) || string.IsNullOrEmpty(model.Type))
			{
				userErrorMessages.Add(Xref.ModelNotContainFields); 
			}

			if (model?.DataSource.Length > 250)
			{
				userErrorMessages.Add(Xref.DataSourceLengthMax);
			}

			if (model?.Type.Length > 50)
			{
				userErrorMessages.Add(Xref.TypeLengthMax);
			}

			if (model?.ExternalID.Length > 250)
			{
				userErrorMessages.Add(Xref.ExternalIDLengthMax);
			}

			var response = new RepositoryResponse<AssetCrossReference>(model, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 406; // Not Acceptable.

				return response;
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var exists = await connection.QuerySingleAsync<bool>(
					"select iif(exists(select 1 from AssetCrossReference where uid = @uid and [type] = @Type and datasource = @DataSource and externalid = @ExternalID), 1, 0)",
					model
				);

				if (exists)
				{
					response.Message = Xref.AlreadyExists;
					response.StatusCode = 409; // Conflict.

					return response;
				}

				// Insert into cross reference table.
				await connection.InsertAsync(model);
				response.IsSuccess = true;
				response.StatusCode = 201;
				response.Data = model;
			}

			return response;
		}

		public async Task CreateCrossReferencesAsync(ApiExecution execution, List<AssetCrossReference> import, int timeout = 3600)
		{
			DataTable table = new DataTable();
			
			table.Columns.Add("ExecutionID", typeof(Guid));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Uid", typeof(Guid));
			table.Columns.Add("DataSource", typeof(string));
			table.Columns.Add("Type", typeof(string));
			table.Columns.Add("ExternalID", typeof(string));
			table.Columns.Add("FieldHash", typeof(string));
			table.Columns.Add("Message", typeof(string));
			table.Columns.Add("Success", typeof(bool));

			int i = 0;
			foreach (AssetCrossReference item in import)
			{
				DataRow row = table.NewRow();

				row["ExecutionID"] = execution.ExecutionID;
				row["ItemNumber"] = i++;
				row["uid"] = item.uid;
				row["DataSource"] = item.DataSource != null ? item.DataSource.Trim() : item.DataSource;
				row["Type"] = item.Type != null ? item.Type.Trim() : item.Type;
				row["ExternalID"] = item.ExternalID != null ? item.ExternalID.Trim() : item.ExternalID;
				row["FieldHash"] = item.FieldHash;

				table.Rows.Add(row);
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(
					"update api.Execution set ProcessingStartedOn = getutcdate() where ExecutionId = @ExecutionID",
					new { execution.ExecutionID }
				);
				
				using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection)
				{
					BatchSize = 5000,
					DestinationTableName = "api.ExecutionAssetCrossReference",
					BulkCopyTimeout = 0
				})
				{
					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("uid", "uid");
					bulkCopy.ColumnMappings.Add("DataSource", "DataSource");
					bulkCopy.ColumnMappings.Add("Type", "Type");
					bulkCopy.ColumnMappings.Add("ExternalID", "ExternalID");
					bulkCopy.ColumnMappings.Add("FieldHash", "FieldHash");

					await connection.OpenAsync();
					bulkCopy.WriteToServer(table);
				}

				string validationSql = @"
update	api.ExecutionAssetCrossReference
		set	Success = 0,
		Message = 'Does not contain valid Uid.' 
where	ExecutionID = @executionID 
		and Success is null 
		and (Uid is null or UID ='00000000-0000-0000-0000-000000000000'); 

update	api.ExecutionAssetCrossReference
set		Success = 0,
		Message='DataSource is required.' 
where	ExecutionID = @executionID 
		and Success is null 
		and ( DataSource is null or Trim(DataSource) ='') ;

update	api.ExecutionAssetCrossReference
set		Success = 0,
		Message='Type is required.' 
where	ExecutionID = @executionID 
		and Success is null 
		and ([Type] is null  or TRIM([Type]) = '' );

update	api.ExecutionAssetCrossReference
set		Success = 0,
		Message = 'ExternalID is required.' 
where	ExecutionID = @executionID 
		and Success is null 
		and ( ExternalID is null or TRIM(ExternalID) ='');

update	api.ExecutionAssetCrossReference
set		Success = 0,
		Message = 'Does not contain required fields.' 
where	ExecutionID = @executionID 
		and Success is null 
		and (Uid is null 
			or DataSource is null 
			or [Type] is null 
			or ExternalID is null
			or UID ='00000000-0000-0000-0000-000000000000' 
			or Trim(DataSource) ='' 
			or TRIM([Type]) = '' 
			or TRIM(ExternalID) =''
		); 

update	ECR
set		Success = 0,
		Message = 'Asset cross reference already exists'
from	api.ExecutionAssetCrossReference ECR
where	ECR.ExecutionID = @executionID 
		and Success is null 
		and exists (
			select	1 
			from	AssetCrossReference 
			where	UID = ECR.UID 
					and DataSource = ECR.DataSource 
					and [Type] = ECR.[Type] 
					and ExternalID = ECR.ExternalID
		);

update	ECR
set		Success = 0,
		Message = 'Duplicate asset cross reference;'
from	api.ExecutionAssetCrossReference ECR
		inner join (
			select	Uid, DataSource, Type, ExternalID 
			from	api.ExecutionAssetCrossReference
			where	Success is null 
					and ExecutionID = @executionID
			group by Uid, DataSource, Type, ExternalID
			having(count(*)>1)
		) T on ECR.[Uid] = T.[UID] 
			and ECR.DataSource = T.DataSource 
			and ECR.[Type] = T.[Type] 
			and ECR.ExternalID = T.ExternalID
where	ECR.Success is null  
		and ExecutionID=@executionID";

				await connection.ExecuteAsync(
					validationSql, 
					new { executionID = execution.ExecutionID }, 
					commandTimeout: timeout
				);

				// Insert into cross reference table.
				await connection.ExecuteAsync(@"
insert into AssetCrossReference (Uid, DataSource, Type, ExternalID, FieldHash)
	select	Uid, DataSource, Type, ExternalID, FieldHash 
	from	api.ExecutionAssetCrossReference
	where	ExecutionID=@executionID 
			and Success is null;

update	api.ExecutionAssetCrossReference
set		Success = 1,
		Message = 'Added Successfully'
where	ExecutionID = @executionID 
		and Success is null;",
					new { executionID = execution.ExecutionID }, commandTimeout: timeout
				);
			}
		}

		public Task CreateSemanticType()
		{
			throw new NotImplementedException();
		}

		public async Task<RepositoryResponse<TagApiModel>> CreateTagAsync(string value, Guid? tagTypeUid)
		{
			RepositoryResponse<TagApiModel> response;

			if (!tagTypeUid.HasValue)
			{
				tagTypeUid = SYSTEM_TAG_TYPE_UID;
			}

			value = (value ?? "").Trim();

			if (string.IsNullOrEmpty(value))
			{
				return new RepositoryResponse<TagApiModel>(400, "Tag is empty.");
			}

			if (value.Length < 1)
			{
				return new RepositoryResponse<TagApiModel>(400, Error.InvalidTagTypeShort);
			}

			if (value.Length > 100)
			{
				return new RepositoryResponse<TagApiModel>(400, Error.InvalidTagTypeLong);
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var tagTypeId = await connection.QuerySingleOrDefaultAsync<int>("select id from TagType where Uid = @uid and State = @state", new { uid = tagTypeUid.Value, state = State.Active });
				if (tagTypeId > 0)
				{
					var tagExists = await connection.QuerySingleOrDefaultAsync<bool>(
						"select cast(iif(count(1) > 0, 1, 0) as bit) from Tag where TagTypeID = @tagTypeId and [Value] = @value and state = @state",
						new { tagTypeId, value , state = State.Active}
					);

					if (tagExists)
					{
						response = new RepositoryResponse<TagApiModel>(409, Error.TagExists);
					}
					else
					{
						response = new RepositoryResponse<TagApiModel>(null, 201, true, "");
						response.Data = await connection.QuerySingleAsync<TagApiModel>(
							$@"
declare @tagId int,
@auditID int;

insert into Tag ([uid], [Value], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [State], [TagTypeId])
values (@uid, @value, @dt, @u, @dt, @u, @State, @tagTypeId);
select @tagId = SCOPE_IDENTITY();

insert into reporting.Global_Audit ([Object], ObjectID, ObjectName, ResourceID, [Date], [Action], [ActionObject], ActionObjectId, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version]) 
values ('Tag', @tagId, @Value, @u, @dt, 'Created', 'Tag', @tagId, 'Tags', @Value, 'Tag created', 1);
select @auditID = SCOPE_IDENTITY();

insert into reporting.Global_FieldAudit ( AuditID, FieldTypeID, FieldName, Value, PreviousValue )
values (@auditID,0,'Name',@Value,Null)

{TAG_API_MODEL_SQL_WITHOUT_WHERE} where t.ID = @tagId;", 
							new { tagTypeId, value, uid = Guid.NewGuid(), state = (int)State.Active, u = CurrentUserId, dt = DateTime.UtcNow });
					}
				}
				else
				{
					response = new RepositoryResponse<TagApiModel>(404, Error.TagTypeNotFound);
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<TagTypeApiModel>> CreateTagTypeAsync(string value)
		{
			RepositoryResponse<TagTypeApiModel> response;
			
			value = (value ?? "").Trim();
			if (string.IsNullOrEmpty(value))
			{
				return new RepositoryResponse<TagTypeApiModel>(null, 400, false, Error.InvalidTagTypeSpecifiedNoValue);
			}
			if (value.Length < 1)
			{
				return new RepositoryResponse<TagTypeApiModel>(null, 400, false, Error.InvalidTagTypeShort);
			}
			if (value.Length > 100)
			{
				return new RepositoryResponse<TagTypeApiModel>(null, 400, false, Error.InvalidTagTypeLong);
			}
			if (!value.IsValidForTag())
			{
				return new RepositoryResponse<TagTypeApiModel>(null, 400, false, Error.InvalidTagTypeCharacters);
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var exists = await connection.QuerySingleOrDefaultAsync<bool>(
					"select cast(iif(count(1) > 0, 1, 0) as bit) from TagType where [Value] = @value and State = @state",
					new { value, state = State.Active }
				);

				if (exists)
				{
					response = new RepositoryResponse<TagTypeApiModel>(null, 409, false, Error.TagExists);
				}
				else
				{
					response = new RepositoryResponse<TagTypeApiModel>(null, 201, true, "");
					response.Data = await connection.QueryFirstAsync<TagTypeApiModel>(
						@"
declare @id int;
insert into TagType ([uid], [Value], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy], [State])
values (@uid, @value, @dt, @CurrentUserId, @dt, @CurrentUserId, @state);
select @id = SCOPE_IDENTITY();

select	t.uid, 
		t.[Value],
		c.Uid as CreatedByUid,
		t.CreatedOn,
		u.Uid as UpdatedByUid,
		t.UpdatedOn
from	TagType t
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
where	t.ID = @id;", new { uid = Guid.NewGuid(), value, CurrentUserId, dt = DateTime.UtcNow, state = State.Active });
				}
			}

			return response;
		}

		public async Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			const string sql = @"
WITH cte AS (  
	select	*, 
			0 as lvl
	from	AssetType
	where	[uid] = @assetUid
	union all
	select	a.*,
			cte.lvl - 1 
	from	IntersectType it
            inner join [Predicate] p on it.PredicateID = p.ID and p.Type IN (3) 
			inner join cte on cte.ID = it.ObjectAssetTypeID 
            inner join AssetType a on a.ID = it.SubjectAssetTypeID
)
select		*
from		cte
order by	lvl";

			IEnumerable<AssetType> results;
			using (var connection = ConnectionProvider.Connect())
			{
				results = await connection.QueryAsync<AssetType>(sql, new { assetUid });
			}
			return results.ToList();
		}


		public async Task<RepositoryResponse<List<dynamic>>> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			string value = "";
			var response = new RepositoryResponse<List<dynamic>>(null, 200, true, "");

			Guid exceptUid = Guid.Empty;
			int maxNumberOfResults = 200;
			bool ignoreCounts = false;
			foreach (var queryitem in queryParams)
			{
				switch (queryitem.Key.ToLowerInvariant())
				{
					case "exceptuid":
						try
						{
							exceptUid = Guid.Parse(queryitem.Value);
						}
						catch
						{
							exceptUid = Guid.Empty;
						}
						break;
					case "ignorecounts":
						if (queryitem.Value.ToLowerInvariant() == "true")
						{
							ignoreCounts = true;
						}
						break;
					case "value":
						value = $"%{queryitem.Value.ToLowerInvariant()}%";
						break;
					case "maxnumberofresults":
						int size;
						if (int.TryParse(queryitem.Value, out size))
						{
							maxNumberOfResults = size;
						}
						else
						{
							throw new ArgumentNullException(Error.InvalidPageSize);
						}
						break;
				}
			}

			string sql;


			if (!ignoreCounts)
			{
				sql = $@"
				drop table if exists #temptagdata

				select top {maxNumberOfResults} T.ID, T.Value as name, T.uid as code , cast(0 as bigint) [count]
				into #temptagdata
				from Tag T 
				where State = 1 and T.Value like @value and T.uid != @exceptUid;

				update t
				set [count] = (select count(1) from AssetTag atg where atg.TagID = t.ID)
				from #temptagdata t

				select name,code,[count]
				from #temptagdata t
				order by name
				";
			}
			else
			{
				sql = $@"select top {maxNumberOfResults} T.Value as name, T.uid as code from Tag T 
						where State = 1 and T.Value like @value and T.uid != @exceptUid
						order by name";
			}
			IEnumerable<dynamic> results;
			using (var connection = ConnectionProvider.Connect())
			{
				results = await connection.QueryAsync<dynamic>(sql, new { value, exceptUid });
			}
			response.Data = results.ToList();
			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<AssetTagList>>> ReadAssetBreadcrumbsByTagAsync(Guid tagUid)
		{
			var response = new RepositoryResponse<IEnumerable<AssetTagList>>(null, 200, true, "");

			string sql = @"
select	D.DisplayValue,
		A.uid,
		AST.[Class],
		AST.Name
from	Tag T
		inner join AssetTag AT on AT.TagId = T.Id
		inner join Asset A on A.ID = AT.AssetID
		inner join AssetType AST ON AST.ID = A.AssetTypeId
		inner join AssetDisplayValue D on D.AssetID = A.ID
where	t.uid = @uid";

			using (var connection = ConnectionProvider.Connect())
			{
				var result = await connection.QueryAsync<dynamic>(sql, new { uid = tagUid });

				var ret = new List<AssetTagList>();
				var chevron = " <i class=\"fa fa-chevron-right\"></i> ";
				foreach (var item in result)
				{
					var itemClass = (AssetTypeClass)item.Class;
					string breadcrumb = "";
					switch (itemClass)
					{
						case AssetTypeClass.TechnicalAsset:
							breadcrumb = Label.AssetTypeClass_Technical;
							break;
						case AssetTypeClass.Policy:
							breadcrumb = Label.AssetTypeClass_Policy;
							break;
						case AssetTypeClass.Model:
							breadcrumb = Label.AssetTypeClass_Model;
							break;
						case AssetTypeClass.Rule:
							breadcrumb = Label.AssetTypeClass_Rule;
							break;
						case AssetTypeClass.Diagram:
							breadcrumb = Label.AssetTypeClass_Task;
							break;
						default:
							breadcrumb = Label.AssetTypeClass_Business;
							break;
					}
					breadcrumb += $"{chevron}{item.Name}";

					var atl = new AssetTagList
					{
						Breadcrumbs = breadcrumb,
						DisplayName = item.DisplayValue,
						Url = $"/asset/{item.uid}"
					};
					ret.Add(atl);
				}
				response.Data = ret;
			}

			return response;
		}

		public async Task<AssetDetail> ReadAssetDetail(long id)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@id", id);

			var sql = @"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName,
		Type,
		TypeID,
		uid
from	AssetDetail
where   ID = @id";

			AssetDetail model = null;
			using (var connection = ConnectionProvider.Connect())
			{
				model = (
					await connection.QueryAsync<AssetDetail>(sql, dbArgs)
					).SingleOrDefault();
			}
			return model;
		}

		public async Task<AssetDetail> ReadAssetDetail(string @object, int objectId)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@o", @object);
			dbArgs.Add("@oid", objectId);

			var sql = @"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName as AssetTypeName,
		Type,
		TypeID,
		uid,
		assetTypeUid
from	AssetDetail
where   [ObjectID] = @oid and [Object] = @o";

			AssetDetail model = null;
			using (var connection = ConnectionProvider.Connect())
			{
				model = (
					await connection.QueryAsync<AssetDetail>(sql, dbArgs)
					).SingleOrDefault();
			}
			return model;
		}

		public async Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000)
		{
			var dbArgs = new DynamicParameters();

			dbArgs.Add("@assetTypeId", assetTypeId);
			dbArgs.Add("@pageNum", pageNum);
			dbArgs.Add("@pageSize", pageSize);
			dbArgs.Add("@offset", pageSize * (pageNum - 1));

			var sql = $@"

				DROP TABLE IF EXISTS #tempassetPath;
				create table #tempassetPath (id int identity(1,1), AssetId bigint);
				create index ix_tempassetPath on #tempassetPath	(AssetId);

				insert into #tempassetPath
				select a.ID
				from Asset A
				where A.assetTypeId = @assetTypeId
				order by A.ID
				OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
				option (recompile);

				select	A.[uid],
						AP.[keypath] as [path]
				from #tempassetPath TempPA
				inner join Asset A on A.ID = TempPA.AssetId
				inner join AssetPath AP on a.ID=ap.ID
				order by TempPA.ID
				option (recompile);";

			var model = new AssetPathResults();

			using (var connection = ConnectionProvider.Connect())
			{
				var countSql = "select count(1) from Asset where AssetTypeId = @assetTypeId";
				if (includeTotal) 
				{
					model.total = await connection.QueryFirstAsync<int>(countSql, dbArgs);
				}
				model.items = await connection.QueryAsync<AssetPathResult>(sql, dbArgs);
			}

			return model;
		}

		public async Task<IEnumerable<AssetTypeApiViewModel>> ReadAssetTypes(int pageNum = 0, int pageSize = 5000)
		{
			var dbArgs = new DynamicParameters();

			var sql = $@"
SELECT     A.[Name]
			,ISNULL(A.[Description],'') as Description
			,A.[Class] as ClassID
			,ISNULL(A.[Notes],'') as Notes
			,A.SourceID
			,A.[uid]
			,A.HierarchyMaximumDepth
			,A.DisplayFormat
			,A.UseAsTransformation
			,0 as 'CanOwnFusion'
			,A.AutoDisplayParent
			,A.FlowObjectType
			,A.CanEditParent
			,A.IsDescriptionEnabled
			,A.IsDescriptionVisibleByDefault
			,A.DescriptionButtonName
			,cast(iif(A.DefaultPermissions = 1, 1, 0) as bit) as IsDefaultReadAccessEnabled
			,P.[Path]
			,AT.IconBackColor as BackColor
			,AT.Icon as Icon
			,AT.IconForeColor as ForeColor
FROM        AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
			left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
where       A.[State] = 1
order by    P.[Path];";

			IEnumerable<AssetTypeApiViewModel> model;

			using (var connection = ConnectionProvider.Connect())
			{
				model = await connection.QueryAsync<AssetTypeApiViewModel>(sql, dbArgs);
			}

			return model;
		}
		
		public Task ReadAssetTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<AssetCrossReferenceResult>> ReadCrossReferenceResultsAsync(Guid executionId)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("executionId", executionId);

			string sql = "select ItemNumber, Uid, Message, Success from [api].[ExecutionAssetCrossReference] where ExecutionID = @executionId";

			IEnumerable<AssetCrossReferenceResult> models = null;

			using (var connection = ConnectionProvider.Connect())
			{
				models = await connection.QueryAsync<AssetCrossReferenceResult>(sql, dbArgs);
			}

			return models;
		}
		
		public async Task<IEnumerable<AssetCrossReference>> ReadCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			var sql = "select uid, DataSource, Type, ExternalID, FieldHash from AssetCrossReference";
			var queryFilters = new List<string>();

			queryParams.CheckForQueryParameter<Guid>("_assetuid", "[UID]", "@assetUid", ref dbArgs, ref queryFilters);
			queryParams.CheckForQueryParameter<string>("_externalid", "[ExternalID]", "@externalid", ref dbArgs, ref queryFilters);
			queryParams.CheckForQueryParameter<string>("_datasource", "[DataSource]", "@datasource", ref dbArgs, ref queryFilters);
			queryParams.CheckForQueryParameter<string>("_type", "[type]", "@type", ref dbArgs, ref queryFilters);

			if (queryFilters.Count > 0)
			{
				sql += " where " + string.Join(" and ", queryFilters);
			}

			IEnumerable<AssetCrossReference> models = null;

			using (var connection = ConnectionProvider.Connect())
			{
				models = await connection.QueryAsync<AssetCrossReference>(sql, dbArgs);
			}

			return models;
		}

		public Task ReadProfiles()
		{
			throw new NotImplementedException();
		}

		public Task ReadRelationTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public Task ReadSemanticTypes()
		{
			throw new NotImplementedException();
		}

		public async Task<RepositoryResponse<TagApiModel>> ReadTagAsync(Guid uid)
		{
			var response = new RepositoryResponse<TagApiModel>(null, 404, false, "");

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response.Data = await connection.QuerySingleOrDefaultAsync<TagApiModel>($@"{TAG_API_MODEL_SQL_WITHOUT_WHERE} where t.Uid = @uid", new { uid });
				if (response.Data != null)
				{
					response.Message = "";
					response.IsSuccess = true;
					response.StatusCode = 200;
				}
				else
				{
					response = new RepositoryResponse<TagApiModel>(null, 404, false, "Tag not found.");
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>> ReadTagsAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>(
				new PagedApiBaseViewModel<TagApiModel>(), 200, true, "");
			string parameterValue = "";
			var dbArgs = new DynamicParameters();
			var queryFilters = new List<string>();
			var validOrderFields = new List<SortColumnOption> { 
				new SortColumnOption("uid", "t.[uid]"),
				new SortColumnOption("value", "t.[Value]"),
				new SortColumnOption("usecount", "cnt.UseCount"),
				new SortColumnOption("createdon", "t.CreatedOn"),
				new SortColumnOption("createdbyuid", "c.[Uid]"),
				new SortColumnOption("updatedon", "t.UpdatedOn"),
				new SortColumnOption("updatedbyuid", "u.[Uid]"),
				new SortColumnOption("tagtypeuid", "tt.[Uid]")
			};

			#region Validate Parameter
			
			if (!queryParams.ValidateForQueryParameter<Guid>("uid",ref parameterValue))
			{
				return new RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>(new PagedApiBaseViewModel<TagApiModel>(), (int)HttpStatusCode.BadRequest, false, string.Format(Error.InvalidTagUid, parameterValue));
			}
				
			var validOrderFieldsList = validOrderFields.Select(x => x.QueryStringPropertyName).ToList();

			if (!queryParams.ValidateForQueryParameterFromList("_order", validOrderFieldsList, ref parameterValue))
			{
				return new RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>(new PagedApiBaseViewModel<TagApiModel>(), (int)HttpStatusCode.BadRequest, false, string.Format(Error.InvalidOrderBy, parameterValue));
			}

			if (!queryParams.ValidateForQueryParameter<string>("_direction", ref parameterValue))
			{
				return new RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>(new PagedApiBaseViewModel<TagApiModel>(), (int)HttpStatusCode.BadRequest, false, string.Format(Error.InvalidDirection, parameterValue));
			}

			#endregion

			var countSql = $@"select count(1) 
from	Tag t
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
		inner join TagType tt on tt.ID = t.TagTypeID";

			var sql = $@"
select	t.uid, 
		t.[Value], 
		cnt.UseCount,
		c.Uid as CreatedByUid,
		t.CreatedOn,
		u.Uid as UpdatedByUid,
		t.UpdatedOn,
		c.FirstName as CreatedByFirstName,
		c.LastName as CreatedByLastName,
		tt.Uid as TagTypeUID,
		tt.Value as TagTypeValue
from	Tag t
		cross apply (select count(1) as UseCount from AssetTag where TagID = t.ID) as cnt
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
		inner join TagType tt on tt.ID = t.TagTypeID";

			dbArgs.Add("@state", State.Active);
			queryFilters.Add($"t.state = @state");
			queryParams.CheckForQueryParameter<Guid>("uid", "t.[UID]", "@uid", ref dbArgs, ref queryFilters);
			queryParams.CheckForQueryParameter("tagtypeuid", "tt.[uid]", "@tagtypeid", ref dbArgs, ref queryFilters, SYSTEM_TAG_TYPE_UID);
			queryParams.CheckForQueryParameter<int>("id", "t.[ID]", "@id", ref dbArgs, ref queryFilters);
			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_tag"))
			{
				var searchPhrase = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_tag").Value.Trim();
				if (!string.IsNullOrEmpty(searchPhrase))
				{
					dbArgs.Add("@searchPhrase", $"%{searchPhrase}%");
					queryFilters.Add($"t.[Value] like @searchPhrase");
				}
			}

			response.Data.pageNum = queryParams.CheckForPageNumber();
			response.Data.pageSize = queryParams.CheckForPageSize();

			if (response.Data.pageSize > 250)
			{
				response.Data.pageSize = 250; // max page size is 250 people.
			}

			bool includeTotal = queryParams.CheckForIncludeTotal();

			if (queryFilters.Count > 0)
			{
				var whereSql = " where " + string.Join(" and ", queryFilters);
				sql += whereSql;
				countSql += whereSql;
			}

			var orderColumn = queryParams.CheckForSortColumn(validOrderFields, "t.[Value]");
			var direction = queryParams.CheckForSortDirection();
			sql += $" order by {orderColumn} {direction}";

			if (includeTotal)
			{
				sql += $" offset {response.Data.pageSize * (response.Data.pageNum - 1)} rows fetch next {response.Data.pageSize} rows only";
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				if (includeTotal)
				{
					var qry = await connection.QueryMultipleAsync($"{countSql}; {sql}; ", dbArgs);
					response.Data.total = await qry.ReadSingleAsync<int>();
					response.Data.items = (await qry.ReadAsync<TagApiModel>()).ToList();
				}
				else
				{
					response.Data.items = (await connection.QueryAsync<TagApiModel>(sql, dbArgs)).ToList();
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<TagTypeApiModel>> ReadTagTypeAsync(Guid uid)
		{
			var response = new RepositoryResponse<TagTypeApiModel>(null, 404, false, "");

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response.Data = await connection.QuerySingleOrDefaultAsync<TagTypeApiModel>(
					@"
select	t.uid, 
		t.[Value],
		c.Uid as CreatedByUid,
		t.CreatedOn,
		u.Uid as UpdatedByUid,
		t.UpdatedOn
from	TagType t
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
where	t.Uid = @uid
		and t.State = @state", new { uid, state = State.Active });

				if (response.Data != null)
				{
					response.Message = "";
					response.IsSuccess = true;
					response.StatusCode = 200;
				}
				else
				{
					response = new RepositoryResponse<TagTypeApiModel>(null, 404, false, "Tag not found.");
				}
			}

			return response;
		}

		public async Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync()
		{
			IEnumerable<TagTypeApiModel> models = null;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				models = await connection.QueryAsync<TagTypeApiModel>(
					
					$@"
select	t.uid, 
		t.ID,
		t.[Value],
		c.Uid as CreatedByUid,
		t.CreatedOn,
		u.Uid as UpdatedByUid,
		t.UpdatedOn
from	TagType t
		inner join reporting.Global_Resource c on c.ResourceID = t.CreatedBy
		inner join reporting.Global_Resource u on u.ResourceID = t.UpdatedBy
where	t.State = {(int)State.Active}
order by	t.[value]");
			}

			return models;
		}

		public async Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync(Guid assetTypeUid, string name)
		{
			IEnumerable<TagTypeApiModel> models = null;
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				string sql = $@"
DECLARE @assetTypeId INT;

SET @assetTypeId = (SELECT ID FROM AssetType WHERE uid = @assetTypeUid);

SELECT 
    t.uid, 
    t.ID,
    t.[Value],
    c.Uid AS CreatedByUid,
    t.CreatedOn,
    u.Uid AS UpdatedByUid,
    t.UpdatedOn
FROM 
    TagType t
INNER JOIN 
    reporting.Global_Resource c ON c.ResourceID = t.CreatedBy
INNER JOIN 
    reporting.Global_Resource u ON u.ResourceID = t.UpdatedBy
LEFT JOIN 
    FieldType f ON f.TagTypeID = t.ID AND f.AssetTypeID = @assetTypeId AND f.Name != ISNULL(@name, '')
WHERE 
    t.State = {(int)State.Active} 
    AND f.TagTypeID IS NULL 
ORDER BY 
    t.[Value]";

				models = await connection.QueryAsync<TagTypeApiModel>(sql, new { assetTypeUid,name });
			}

			return models;
		}

		public async Task<RepositoryResponse<bool>> RemoveAssetTagAsync(long assetId, int tagId)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response = new RepositoryResponse<bool>(true, 201, true, "");
				await connection.ExecuteAsync($@"
declare @audittable table (auditID int,
Object varchar(50),
ObjectID int
);

declare @PreviousValue nvarchar(max),
@auditID int,
@ObjectID int,
@Object varchar(50);

drop table if exists #tbl;

delete AssetTag where AssetID = @assetId and TagID = @tagId;

declare @version int;
select  @version = COALESCE(max([Version]),0)+1 from reporting.Global_Audit l inner join Asset a on a.Object = l.Object and a.ObjectID = l.ObjectID and a.ID = @assetId;

insert into reporting.Global_Audit ([Object], ObjectID, ObjectName, ResourceID, [Date], [Action], [ActionObject], ActionObjectId, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version]) 
output inserted.ID, inserted.[Object], inserted.[ObjectID] into @audittable
	select	a.Object, a.ObjectID, d.DisplayValue, @u, @dt, 'Unassigned', 'Tag', @tagId, 'Tags', t.[Value], 'Tag unassigned', @version
	from	Asset a
			left join AssetDisplayValue d on d.AssetID = a.ID 
			join Tag t on t.ID = @tagId
	where	a.ID = @assetId;

select @auditID = auditID,
@ObjectID  = ObjectID,
@Object = Object
from @audittable;

select	'Tags' FieldName, 
string_Agg(T.Value, ', ') within group  (order by TA.id asc) NewValue
into #tbl
from	AssetTag TA 
inner join Tag T on T.ID = TA.TagID
where TA.AssetID = @assetId;

select	top 1
@PreviousValue = [Value]
from	reporting.Global_Audit a
inner join reporting.Global_FieldAudit f on f.AuditID  = a.ID and  f.FieldName = 'Tags' and f.FieldTypeID = 0
where 	a.Object = @Object
and 	a.ObjectID = @ObjectID
and a.id != @auditID
order by a.id desc;

insert into reporting.Global_FieldAudit ( AuditID, FieldTypeID, FieldName, Value, PreviousValue )
select	@auditID,
0,
FieldName,
NewValue,
@PreviousValue
from	#tbl

drop table if exists #tbl;

",
					new { assetId, tagId, uid = Guid.NewGuid(), u = CurrentUserId, dt = DateTime.UtcNow });
			}

			return response;
		}

		public async Task<RepositoryResponse<AssetCrossReference>> RemoveCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			var sql = "delete AssetCrossReference";
			List<string> queryFilters = new List<string>();

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_assetuid"))
			{
				Guid assetUid = new Guid();

				var assetUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_assetuid").Value;
				if (Guid.TryParse(assetUidString, out assetUid))
				{
					dbArgs.Add("@assetuid", assetUid);
					queryFilters.Add($"[UID] = @assetuid");
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_externalid"))
			{
				var externalId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_externalid").Value;
				dbArgs.Add("@externalid", externalId);
				queryFilters.Add($"[ExternalID] = @externalid");
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_datasource"))
			{
				var ds = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_datasource").Value;
				dbArgs.Add("@datasource", ds);
				queryFilters.Add($"[DataSource] = @datasource");
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_type"))
			{
				var ty = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_type").Value;
				dbArgs.Add("@type", ty);
				queryFilters.Add($"[type] = @type");
			}

			var response = new RepositoryResponse<AssetCrossReference>(null, 0, false, "");

			if (queryFilters.Count > 0)
			{
				sql += " where " + string.Join(" and ", queryFilters);
			}
			else
			{
				response.StatusCode = 400;
				response.IsSuccess = false;
				response.Message = Xref.SpecifyDeleteCriteria;
			}

			using (var connection = ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = null;
			}

			return response;
		}

		public async Task<RepositoryResponse<string>> RemoveSemanticType()
		{
			throw new NotImplementedException();
		}

		public async Task<RepositoryResponse<bool>> RemoveTagsAsync(List<Guid> tags)
		{
			var response = new RepositoryResponse<bool>(false, 200, false);

			var dt = new DataTable();
			dt.Columns.Add("uid", typeof(Guid));
			tags.ForEach(t =>
			{
				dt.Rows.Add(t);
			});

			var dbArgs = new DynamicParameters();
			dbArgs.Add("userId", CurrentUserId, DbType.Int32);
			dbArgs.Add("uids", dt.AsTableValuedParameter("dbo.UidTable"));

			using (var connection = ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync("exec api.DeleteTags @userId, @uids", dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveTagTypesAsync(List<Guid> tagTypes)
		{
			var response = new RepositoryResponse<bool>(false, 200, false);

			if (tagTypes.Any(t => t == SYSTEM_TAG_TYPE_UID))
			{
				return new RepositoryResponse<bool>( 
					false, 403, false, string.Format(Error.TagTypeNotDeletable, SYSTEM_TAG_TYPE_UID)
				);
			}

			var dt = new DataTable();
			dt.Columns.Add("uid", typeof(Guid));
			tagTypes.ForEach(t =>
			{
				dt.Rows.Add(t);
			});

			var dbArgs = new DynamicParameters();
			dbArgs.Add("userId", CurrentUserId, DbType.Int32);
			dbArgs.Add("uids", dt.AsTableValuedParameter("dbo.UidTable"));

			using (var connection = ConnectionProvider.Connect())
			{
				var success = await connection.QuerySingleAsync<bool>("DECLARE @return_status INT; exec @return_status = api.DeleteTagTypes @userId, @uids; select @return_status", dbArgs);
				response.IsSuccess = success;
				response.StatusCode = success ? 200 : 409;
				response.Message = success ? "" : "Unable to remove tag types.";
				response.Data = success;
			}

			return response;
		}

		public async Task<RepositoryResponse<AssetCrossReference>> UpdateCrossReferenceAsync(AssetCrossReference model)
		{
			var userErrorMessages = new List<string>();

			if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.ExternalID) || string.IsNullOrEmpty(model.Type))
			{
				userErrorMessages.Add(Xref.ModelNotContainFields);
			}

			if (model?.DataSource.Length > 250)
			{
				userErrorMessages.Add(Xref.DataSourceLengthMax);
			}

			if (model?.Type.Length > 50)
			{
				userErrorMessages.Add(Xref.TypeLengthMax);
			}

			if (model?.ExternalID.Length > 250)
			{
				userErrorMessages.Add(Xref.ExternalIDLengthMax);
			}

			var response = new RepositoryResponse<AssetCrossReference>(model, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 406; // Not Acceptable.

				return response;
			}

			var sql = "update AssetCrossReference set ExternalID = @ExternalID, Fieldhash = @FieldHash where [uid] = @uid and [DataSource] = @DataSource and [Type] = @Type";

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, model);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = model;
			}

			return response;
		}

		public async Task<RepositoryResponse<Semantic>> UpdateSemanticType()
		{
			throw new NotImplementedException();
		}

		public async Task<RepositoryResponse<bool>> UpdateTagAsync(Guid uid, string value)
		{
			var response = new RepositoryResponse<bool>(false, 200, false, "");

			value = (value ?? "").Trim();

			if (string.IsNullOrEmpty(value))
			{
				return new RepositoryResponse<bool>(false, 400, false, "Tag is empty.");
			}
			if (value.Length < 1 || value.Length > 100)
			{
				return new RepositoryResponse<bool>(false, 400, false, "Tag must be a text value with a length from 1 to 100..");
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{

				var (tagId, tagTypeId) = await connection.QuerySingleOrDefaultAsync<(int, int)>(
					"select id, tagTypeId from Tag where Uid = @uid", new { uid });
				
				if (tagId > 0)
				{
					var tagExists = await connection.QuerySingleOrDefaultAsync<bool>(
						"select cast(iif(count(1) > 0, 1, 0) as bit) " +
						"from Tag " +
						"where TagTypeID = @tagTypeId and ID <> @tagId and [Value] = @value",
						new { tagId, tagTypeId, value }
					);

					if (tagExists)
					{
						response = new RepositoryResponse<bool>(false, 409, false, Error.TagExists);
					}
					else
					{
						await connection.ExecuteAsync(
							@"
declare @PreviousValue nvarchar(max),
@auditID int;

declare @dt datetime = getutcdate();
update	Tag
set		[Value] = @value,
		UpdatedOn = @dt,
		UpdatedBy = @userId
where	ID = @tagId;

declare @version int;
select @version = COALESCE(max([Version]),0)+1 from reporting.Global_Audit where Object = 'Tag' and ObjectID = @tagId;

insert into reporting.Global_Audit ([Object], ObjectID, ObjectName, ResourceID, [Date], [Action], [ActionObject], ActionObjectId, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version]) 
values ('Tag', @tagId, @value, @userId, @dt, 'Updated', 'Tag', @tagId, 'Tags', @value, 'Tag updated', @version);

select @auditID = SCOPE_IDENTITY();

select	top 1
@PreviousValue = [Value]
from	reporting.Global_Audit a
inner join reporting.Global_FieldAudit f on f.AuditID  = a.ID and  f.FieldName in ('Name','Tag Name') and f.FieldTypeID = 0
where 	a.Object = 'Tag'
and 	a.ObjectID = @tagId
and		a.id != @auditID
order by a.id desc;

insert into reporting.Global_FieldAudit ( AuditID, FieldTypeID, FieldName, Value, PreviousValue )
values(@auditID,0,'Name', @value,@PreviousValue);

---Asset log generate

drop table if exists #TempTagValues;
create table #TempTagValues(AssetId bigint,Object varchar(50), ObjectID int);
create clustered index cx_TempTagValues on #TempTagValues (Object,ObjectID);



insert into #TempTagValues(AssetId, Object, ObjectID)
select distinct atg.AssetID,A.Object,A.ObjectID
from AssetTag atg
inner join asset a on atg.AssetId = A.ID
where tagid = @tagId

create table #tbl (ID bigint, Object varchar(50), ObjectID int);
create clustered index cx_tbl on #tbl (Object,ObjectID);

insert into reporting.Global_Audit ([Object], ObjectID, ObjectName, ResourceID, [Date], [Action], [ActionObject], ActionObjectId, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version]) 
output inserted.ID, inserted.Object, inserted.ObjectID into #tbl
select tta.Object,tta.ObjectID,substring(adv.DisplayValue,1,250),@userId,@dt,'Updated','Tag',@tagId,'Tags',@value,'Tag Updated',mv.[Version]
from #TempTagValues tta
inner join AssetDisplayValue adv on adv.AssetId = tta.Assetid
cross apply (select coalesce(max(ga.[Version]),0)+1 as [Version] 
			from reporting.Global_Audit ga
			where ga.Object =  tta.Object and ga.ObjectID =  tta.ObjectID) mv;

insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Value], PreviousValue)
select t.Id,0,'Tags', @value,@PreviousValue
from #tbl t;

drop table if exists #tbl;
drop table if exists #TempTagValues;
", 
							new { tagId, value, userId = CurrentUserId });

						response = new RepositoryResponse<bool>(true, 200, true, "Tag updated.");
					}
				}
				else
				{
					response = new RepositoryResponse<bool>(false, 404, false, Error.TagUidNotExists);
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpdateTagTypeAsync(Guid uid, string value)
		{
			RepositoryResponse<bool> response;

			value = (value ?? "").Trim();

			if (string.IsNullOrEmpty(value))
			{
				return new RepositoryResponse<bool>(400, Error.InvalidTagTypeSpecifiedNoValue);
			}
			if (value.Length < 1)
			{
				return new RepositoryResponse<bool>(400, Error.InvalidTagTypeShort);
			}
			if (value.Length > 100)
			{
				return new RepositoryResponse<bool>(400, Error.InvalidTagTypeLong);
			}
			if (!value.IsValidForTag())
			{
				return new RepositoryResponse<bool>(400, Error.InvalidTagTypeCharacters);
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var id = await connection.QuerySingleOrDefaultAsync<int>(
					"select id from TagType where Uid = @uid and State = @state", new { uid, state = State.Active });

				if (id > 0)
				{
					var tagExists = await connection.QuerySingleOrDefaultAsync<bool>(
						"select cast(iif(count(1) > 0, 1, 0) as bit) " +
						"from TagType " +
						"where ID <> @id and [Value] = @value and State = @state",
						new { id, value, state = State.Active }
					);

					if (tagExists)
					{
						response = new RepositoryResponse<bool>(false, 409, false, Error.TagExists);
					}
					else
					{
						await connection.ExecuteAsync(
							@"
declare @dt datetime = getutcdate();
update	TagType
set		[Value] = @value,
		UpdatedOn = @dt,
		UpdatedBy = @userId
where	ID = @id;",
							new { id, value, userId = CurrentUserId });

						response = new RepositoryResponse<bool>(true, 200, true, "Tag updated.");
					}
				}
				else
				{
					response = new RepositoryResponse<bool>(400, Error.TagUidNotExists);
				}
			}

			return response;
		}
	}
}
