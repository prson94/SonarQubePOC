using d360.core.entities;
using Dapper;
using Dapper.Contrib.Extensions;
using DocumentFormat.OpenXml.EMMA;
using repositories.resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Catalog : Repository, ICatalog
	{
		public Catalog(DapperConnectionProvider provider): base(provider) { }

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

			var sql = "update AssetCrossReference set ExternalID = @ExternalID, @Fieldhash = @FieldHash where [uid] = @uid and [DataSource] = @DataSource and [Type] = @Type";

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
	}
}
