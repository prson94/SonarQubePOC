using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json.Linq;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	internal abstract record BaseReadAssetAsyncModel { public long AssetId { get; set; } }
	internal abstract record BaseReadAssetFieldAsyncModel : BaseReadAssetAsyncModel { public string Name { get; set; } }

	internal record ReadAssetAsyncModel: BaseReadAssetAsyncModel
	{
		public Guid AssetUid { get; set; }
		public string XrefId { get; set; }
		public int AssetTypeId { get; set; }
		public Guid AssetTypeUid { get; set; }
		public DateTime UpdatedOn { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Color { get; set; }
		public string Fields { get; set; }
	}
	internal record ReadAssetPathValueAsyncModel : BaseReadAssetFieldAsyncModel
	{
		public string DisplayPath { get; set; }
		public string PathValue { get; set; }
	}
	internal record ReadAssetRelationAsyncModel : BaseReadAssetFieldAsyncModel
	{
		public Guid Uid { get; set; }
		public string DisplayValue { get; set; }
	}
	internal record ReadAssetCounterAsyncModel : BaseReadAssetFieldAsyncModel
	{
		public string CounterPrefix { get; set; }
		public int Value { get; set; }
	}
	internal record ReadAssetLookupAsyncModel : BaseReadAssetFieldAsyncModel
	{
		public Guid Uid { get; set; }
		public string DisplayValue { get; set; }
	}

	internal record ReadAssetTagAsyncModel : BaseReadAssetFieldAsyncModel
	{
		public int TagTypeId { get; set; }
		public Guid Uid { get; set; }
		public string Value { get; set; }
	}

	public partial class Catalog
	{
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
			using (var connection = ConnectionProvider.Connect(true))
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
			using (var connection = ConnectionProvider.Connect((true)))
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

			using (var connection = ConnectionProvider.Connect((true)))
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

		public async Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadAssetsAsync(Guid assetTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<PagedApiBaseViewModel<dynamic>> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@assetTypeUid", assetTypeUid);
			dbArgs.Add("@state", (int)State.Active);
			dbArgs.Add("@userId", CurrentUserId);

			string definitionSql =
				"select * from AssetType where Uid = @assetTypeUid; " +
				"select f.* from FieldType f inner join AssetType a on a.ID = f.AssetTypeID and a.Uid = @assetTypeUid";

			AssetType assetType = null;
			List<FieldType> fieldTypes = null;
			using (var connection = ConnectionProvider.Connect(true))
			{
				var definitionQuery = await connection.QueryMultipleAsync(definitionSql, dbArgs);
				assetType = await definitionQuery.ReadFirstOrDefaultAsync<AssetType>();
				fieldTypes = (await definitionQuery.ReadAsync<FieldType>()).AsList();
			}

			dbArgs.Add("@assetTypeId", assetType.ID);
			int pageNumber = queryParams.CheckForPageNumber();
			int pageSize = queryParams.CheckForPageSize();
			dbArgs.Add("@offset", (pageNumber - 1) * pageSize);
			dbArgs.Add("@size", pageSize);
			dbArgs.Add("@sortFieldTypeId", fieldTypes.Single(o => o.Name == "Name").ID);

			// Figure out what filters and extra SQL logic we need to account for.
			//dbArgs.Add("@search", "%NUMC%");
			//string searchQuery = "";//"and exists (select 1 from Field where AssetID = a.ID and FieldTypeID in @fieldTypeIds and FormattedValue like @search)";

			List<string> filters = new();
			List<string> wheres = new();

			if (queryParams.IsQueryParameterPresent("_assetuid"))
			{
				var assetUids = queryParams.ReadQueryParameterValue("_assetuid").Split(',').Select(x => {
					var guid = Guid.Empty;
					Guid.TryParse(x, out guid);
					return guid;
				}).ToList();

				if (assetUids.Any(x => x == Guid.Empty))
				{
					response.IsSuccess = false;
					response.Message = Error.InvalidAssetUid;
					response.StatusCode = 400;
				}

				if (assetUids.Count > 0)
				{
					dbArgs.Add("@assetUids", assetUids);
					wheres.Add($"and a.uid in @assetUids");
				}
			}

			//dbArgs.Add("@filter1", "4");
			//filters.Add("and exists (select 1 from AssetField where AssetID = a.ID and FieldTypeID = 6550 and [Value] = @filter1)");

			// Get total and base set of asset Ids.
			var sql = $@"
create table #ids (RowOrder int identity, AssetId bigint);
create clustered index cix_temp_ids on #ids (RowOrder);
create nonclustered index ix_temp_ids_assetId on #ids (AssetId);

declare @total int = 0;

select	@total = count(1)
from	Asset a 
		{string.Join(Environment.NewLine, filters)} 
where	a.AssetTypeID = @assetTypeId {string.Join(" ", wheres)};

insert into #ids
	select		a.ID
	from		Asset a 
				inner join AssetField sf with(index(IX_AssetField_Secondary)) on sf.AssetID = a.ID and sf.FieldTypeID = @sortFieldTypeId 
				{string.Join(Environment.NewLine, filters)} 
	where		a.AssetTypeID = @assetTypeId {string.Join(" ", wheres)}
	order by	sf.[Value] asc
	offset @offset rows fetch next @size rows only;";

			// Return the total we got above
			sql += "select @total as [Total];";

			// Get core asset and simple fields.
			List<string> columns = ["a.Id as AssetId", "a.Uid as AssetUid", "a.SourceID as XrefId", "a.AssetTypeId", "@assetTypeUid as AssetTypeUid", "a.UpdatedOn", "a.CreatedOn", "a.Color", "f.Fields"];
			sql += $@"
select  {string.Join(", ", columns)}
from	Asset a     
		inner join #ids id on id.AssetID = a.ID
		cross apply (
			select	json_objectagg(
						ft.[Name]:
						f.[Value]
					) as Fields
			from	AssetField f
					inner join FieldType ft on ft.AssetTypeID = @assetTypeId
						and f.FieldTypeID = ft.ID 
						and f.AssetId = a.Id 
						and ft.[Type] in ('Boolean', 'Date', 'DateTime', 'Html', 'Number', 'Decimal', 'Text', 'Link')		
		) f
order by id.RowOrder;";

			// Deal with and resolve path fields.
			bool hasPathField = fieldTypes.Any(ft => ft.Type == "Path");
			if (hasPathField)
			{ 
				sql += $@"
select  id.AssetId,
		[dbo].GetDisplayPath(a.[Path], ' > ', ' / ') as DisplayPath,
		--a.[Path],
		l1.Name,
		l1.PathValue
from	Asset a
		inner join #ids id on id.AssetID = a.ID
		outer apply (
			select	l1t.Name,
					cast([Path].query('for $s in /path/segment[@assetTypeId=sql:column(""a1t.ID"")] return data($s)') as nvarchar(850)) as PathValue
			from	FieldType l1t
					inner join AssetType a1t on a1t.Uid = json_value(l1t.Definition, '$.AssetTypeUid') and l1t.[Type] = 'Path'
		) l1
order by id.RowOrder;";			
			}

			// Get the relationships fields [Where asset is the SUBJECT]
			bool hasRelationField = fieldTypes.Any(ft => ft.Type == "Relationship");
			if (hasRelationField)
			{
				sql += $@"
select	a.AssetId,
		ft.Name,
		d.[Uid],
		d.DisplayValue
from	#ids a
		inner join FieldType ft on ft.[Type] = 'Relationship' and ft.AssetTypeID = @assetTypeId
		inner join [Intersect] i on i.IntersectTypeID = ft.LookupObjectID and i.SubjectAssetID = a.AssetId and ft.IsSubject = 0
		inner join Asset d on d.ID = i.ObjectAssetID;";

				// Get the relationships fields [Where asset is the OBJECT]
				sql += $@"
select	a.AssetID,
		ft.Name,
		d.[uid],
		d.DisplayValue
from	#ids a
		inner join FieldType ft on ft.[Type] = 'Relationship' and ft.AssetTypeID = @assetTypeId
		inner join [Intersect] i on i.IntersectTypeID = ft.LookupObjectID and i.ObjectAssetID = a.AssetId and ft.IsSubject = 1
		inner join Asset d on d.ID = i.SubjectAssetID;";
			}

			// Get the counter values for target assets.
			bool hasCounterField = fieldTypes.Any(ft => ft.Type == "Counter");
			if (hasCounterField)
			{
				sql += $@"
select	a.AssetId,
		ft.Name,
		ft.CounterPrefix,
		f.[Value]
from	#ids a
		inner join FieldType ft on ft.[Type] = 'Counter' and ft.AssetTypeID = @assetTypeId
		inner join FieldCounterValue f on f.FieldTypeID = ft.ID and f.AssetId = a.AssetId;";
			}

			// Get the lookup values for target assets.
			bool hasLookupField = fieldTypes.Any(ft => ft.Type == "Lookup");
			if (hasLookupField)
			{
				sql += $@"
select	a.AssetId,
		ft.Name,
		d.[Uid],
		d.DisplayValue
from	#ids a
		inner join FieldType ft on ft.[Type] = 'Lookup' and ft.AssetTypeID = @assetTypeId
		inner join AssetField f on f.FieldTypeID = ft.ID and f.AssetId = a.AssetId 
		inner join Asset d on d.ID = f.[Value] and f.FieldTypeID = ft.ID;";
			}

			// Get the tags for target assets.
			bool hasTagField = fieldTypes.Any(ft => ft.Type == "Tag");
			if (hasTagField)
			{
				sql += $@"
select	a.AssetId,
		ft.Name,
		t.TagTypeId,
		t.[Uid],
		t.[Value]
from	#ids a
		inner join FieldType ft on ft.[Type] = 'Tag' and ft.AssetTypeID = @assetTypeId
		inner join AssetTag f on f.AssetId = a.AssetId 
		inner join Tag t on t.ID = f.TagID;";
			}

			using (var connection = ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryMultipleAsync(sql, dbArgs);
				int total = await query.ReadSingleAsync<int>();
				var items = (await query.ReadAsync<ReadAssetAsyncModel>()).AsList();
				List<ReadAssetPathValueAsyncModel> paths = [];
				if (hasPathField)
				{
					paths = (await query.ReadAsync<ReadAssetPathValueAsyncModel>()).AsList();
				}
				List<ReadAssetRelationAsyncModel> relations = [];
				if (hasRelationField)
				{
					relations = (await query.ReadAsync<ReadAssetRelationAsyncModel>()).AsList();	// Subject
					relations.AddRange(await query.ReadAsync<ReadAssetRelationAsyncModel>());		// Object
				}
				List<ReadAssetCounterAsyncModel> counters = [];
				if (hasCounterField)
				{
					counters = (await query.ReadAsync<ReadAssetCounterAsyncModel>()).AsList();
				}
				List<ReadAssetLookupAsyncModel> lookups = [];
				if (hasLookupField)
				{
					lookups = (await query.ReadAsync<ReadAssetLookupAsyncModel>()).AsList();
				}
				List<ReadAssetTagAsyncModel> tags = [];
				if (hasTagField)
				{
					tags = (await query.ReadAsync<ReadAssetTagAsyncModel>()).AsList();
				}


				List<Dictionary<String, object>> alteredItems = new List<Dictionary<String, object>>();

				foreach (var item in items)
				{
					Dictionary<String, object> alteredItem = new()
					{
						{ "AssetId", item.AssetId },
						{ "AssetUid", item.AssetUid },
						{ "XrefId", item.XrefId },
						{ "AssetTypeId", item.AssetTypeId },
						{ "AssetTypeUid", item.AssetTypeUid },
						{ "CreatedOn", item.CreatedOn },
						{ "UpdatedOn", item.UpdatedOn },
						{ "Color", item.Color }
					};

					var fields = JObject.Parse(item.Fields);
					foreach (var token in fields)
					{
						alteredItem.Add(token.Key, token.Value);
					}

					paths
						.Where(o => o.AssetId == item.AssetId)
						.ToList()
						.ForEach(o => { 
							alteredItem.Add(o.Name, string.IsNullOrEmpty(o.PathValue) ? o.PathValue : o.DisplayPath); 
						});

					relations
						.Where(o => o.AssetId == item.AssetId)
						.GroupBy(o => o.Name)
						.Select(o => new { o.Key, Items = o.Select(i => new { i.Uid, i.DisplayValue }) })
						.ToList()
						.ForEach(o => {
							alteredItem.Add(o.Key, o.Items);
						});

					counters
						.Where(o => o.AssetId == item.AssetId)
						.ToList()
						.ForEach(o => {
							alteredItem.Add(o.Name, $"{o.CounterPrefix}{o.Value}");
						});

					lookups
						.Where(o => o.AssetId == item.AssetId)
						.GroupBy(o => o.Name)
						.Select(o => new { o.Key, Items = o.Select(i => new { i.Uid, i.DisplayValue }) })
						.ToList()
						.ForEach(o => {
							alteredItem.Add(o.Key, o.Items);
						});

					tags
						.Where(o => o.AssetId == item.AssetId)
						.GroupBy(o => o.Name)
						.Select(o => new { o.Key, Items = o.Select(i => new { i.Uid, i.Value }) })
						.ToList()
						.ForEach(o => {
							alteredItem.Add(o.Key, o.Items);
						});

					alteredItems.Add(alteredItem);
				}

				response.Data = new PagedApiBaseViewModel<dynamic>
				{
					items = alteredItems,
					pageNum = pageNumber,
					pageSize = pageSize,
					total = total
				};
			}

			return response;
		}

		public async Task<RepositoryResponse<List<AssetApiResultModel>>> UpsertAssetsAsync(
	int executionId, List<AssetApiModel> models, bool lookupFieldsPassedByValue = false, bool enableJsonAttributes = false)
		{
			RepositoryResponse<List<AssetApiResultModel>> response = new([], 200, true);

			List<FieldTypeValidation> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<FieldTypeValidation>(
					$"set nocount on; " +
					$"declare @assetTypeId int; " +
					$"select @assetTypeId = a.ID from AssetType a inner join api.Execution e on e.ID = @executionId and a.Uid = JSON_VALUE(e.Fields, '$.AssetTypeUid'); " +
					$"select {FIELD_VALIDATION_COLUMNS} from FieldType f where f.AssetTypeID = @assetTypeId; ",
					new { executionId }
					)).ToList();
			}

			#region Data Tables

			var table = new DataTable();

			table.Columns.Add("ExecutionId", typeof(int));
			table.Columns.Add("ExecutionItemUid", typeof(Guid));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Properties", typeof(string));
			table.Columns.Add("CustomProperties", typeof(string));
			table.Columns.Add("Success", typeof(bool));
			table.Columns.Add("Message", typeof(string));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			models.ForEach(model => {
				var row = table.NewRow();
				var jsonObject = JObject.Parse("{}");
				Guid? executionItemUid = null;

				itemNumber++;
				row["ExecutionId"] = executionId;
				row["ItemNumber"] = itemNumber;

				if (model.ExecutionItemUid.HasValue)
				{
					row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
					executionItemUid = model.ExecutionItemUid.Value;
				}

				if (model.Uid != Guid.Empty)
				{
					jsonObject.Add("Uid", model.Uid);
				}
				if (model.ParentUid.HasValue && model.ParentUid != Guid.Empty)
				{
					jsonObject.Add("ParentUid", model.ParentUid);
				}
				if (!string.IsNullOrEmpty(model.SourceID))
				{
					jsonObject.Add("SourceID", model.SourceID);
				}

				row["Properties"] = jsonObject.ToString();
				var fieldProcessingResult = parseFieldAndAddToRow(row, fieldTypes, model.Fields);

				if (fieldProcessingResult.Item1)
				{
					// Passed basic validation.
					table.Rows.Add(row);
				}
				else
				{
					// Failed basic validation.
					string message = string.Join("; ", fieldProcessingResult.Item2);
					// Add error to outgoing.
					response.Data.Add(new AssetApiResultModel { ItemNumber = itemNumber, Message = message, Success = false, ExecutionItemUid = executionItemUid });
				}
			});

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();

				if (table.Rows.Count > 0)
				{
					await connection.ExecuteAsync(@"delete from api.ExecutionItem where executionid = @executionId", new { executionId });

					SqlBulkCopy bulkCopy = connection.CreateBulkCopy("api.ExecutionItem", 1000, 1200);
					bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
					bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Properties", "Properties");
					bulkCopy.ColumnMappings.Add("CustomProperties", "CustomProperties");
					await bulkCopy.WriteToServerAsync(table);

					await connection.ExecuteAsync(
						@"exec api.UpsertAssets @executionId, @lookupFieldsPassedByValue, @enableJsonAttributes",
						new { executionId, lookupFieldsPassedByValue, enableJsonAttributes }
					);

					response.Data.AddRange(await connection.QueryAsync<AssetApiResultModel>(
						"select ItemNumber, cast(JSON_VALUE(Properties, '$.Uid') as uniqueidentifier) as uid, ExecutionItemUid, Message, Success " +
						"from api.ExecutionItem " +
						"where ExecutionID = @executionId;"
						, new { executionId }));
				}
				else
				{
					int total = models.Count;
					int success = 0;
					int error = models.Count;
					await connection.ExecuteAsync(
						"update api.Execution " +
						"set    CompletedOn = getutcdate(), [Total] = @total, Processed = @success, [Error] = @error " +
						"where	Id = @executionId", new { executionId, total, success, error });
				}
			}

			return response;
		}


		public async Task<(bool isFieldCounterType, int? CounterInitialIndex)> IsFieldCounterType(int? fieldTypeId)
		{
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var sql = @"
							SELECT  * from dbo.FieldType 
							WHERE Type = 'Counter' AND ID = @fieldTypeId
							  ";

				var result = await connection.QueryFirstOrDefaultAsync<FieldType>(sql, new { fieldTypeId });
				if (result != null)
				{
					return (true, result.CounterInitialIndex);
				}
				return (false, null);
			}
		}

		public async Task<IEnumerable<int>> GetAssetsByFieldType(int? assetTypeId)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var sql = @"
							  SELECT a.ID FROM
							  dbo.Asset AS a LEFT JOIN dbo.FieldCounterValue AS fcv
							  ON a.AssetId = fcv.AssetId
							 WHERE a.AssetTypeID = @assetTypeId AND fcv.AssetId IS NULL;
							  ";

					return await connection.QueryAsync<int>(sql, new { assetTypeId });
				}
			}
			catch (Exception)
			{
				return null;
			}
		}


		public async Task<bool> InsertAssetWithCounter(int counterStartValue, int assetTypeId, int fieldTypeId, IEnumerable<int> assetIds)
		{
			if (assetIds == null || !assetIds.Any())
			{
				Console.WriteLine("No asset IDs provided for insertion.");
				return false;
			}

			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					connection.Open();
					using (var transaction = connection.BeginTransaction())
					{
						// Get the current maximum counter value (within the transaction)
						var maxCounterSql = "SELECT ISNULL(MAX(Value), 0) FROM dbo.FieldCounterValue WITH (UPDLOCK, HOLDLOCK)";
						int existingMaxCounterValue = await connection.QuerySingleOrDefaultAsync<int>(maxCounterSql, transaction: transaction);

						int currentCounterSeed = Math.Max(counterStartValue, existingMaxCounterValue) + 1;

						List<FieldCounterValue> allValuesToInsert = new List<FieldCounterValue>();
						int currentOffset = 0;
						foreach (var assetId in assetIds)
						{
							allValuesToInsert.Add(new FieldCounterValue
							{
								AssetId = assetId,
								AssetTypeId = assetTypeId,
								FieldTypeId = fieldTypeId,
								Value = currentCounterSeed + currentOffset
							});
							currentOffset++;
						}
						const int batchSize = 25000;

						// Create batches and insert
						var sql = @"
                        INSERT INTO dbo.FieldCounterValue
                        (AssetID, AssetTypeId, FieldTypeId, Value)
                        VALUES
                        (@AssetId, @AssetTypeId, @FieldTypeId, @Value);
                    ";

						for (int i = 0; i < allValuesToInsert.Count; i += batchSize)
						{
							var batch = allValuesToInsert.Skip(i).Take(batchSize);
							await connection.ExecuteAsync(sql, batch, transaction: transaction);
						}

						transaction.Commit();
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				// The transaction will be implicitly rolled back if using (transaction) is exited by an exception
				return false;
			}
		}
	}
}
