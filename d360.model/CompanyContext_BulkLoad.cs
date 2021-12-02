using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.model.DataAccessLayer;
using Dapper;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    partial class CompanyContext : BaseContext
    {
        public string BulkLoadStatusMsg { get; set; }

        #region DbSets

        public DbSet<Load> Loads { get; set; }

        public DbSet<LoadItem> LoadItems { get; set; }

        public DbSet<LoadItemColumn> LoadItemColumns { get; set; }

        public DbSet<LoadColumn> LoadColumns { get; set; }

        #endregion

        #region Engine Methods

        #region Get Methods

        string LoadDetailBaseSql = @"select	L.ID,
		L.[Object],
		L.ObjectID,
		case 
			when L.[Action] = 'M' and L.ObjectID = 0 then 'Group Membership'
			when L.[Action] = 'M' and L.ObjectID = 1 then 'Users'
            when L.[Action] in ('P','R','U') then coalesce(C_D.[Name], '[Deleted]')  
			else coalesce(C_D.[Name], 'Default') 
		end as ObjectName,
		L.Notes, 
        coalesce(EA.ErrorMessage, '' ) + iif(EA.ErrorMessage is null, '', '; ') + coalesce(EE.ErrorMessage, '' ) as ErrorMessage,
		'MyFile.' + L.Extension as FilePath,
		L.DateStarted,
		case when L.Action in ('P','R','U') and L.[File] is null then
            case when (select count(*) from LoadItem where LoadID = L.ID) = (select count(*) from LoadItem where LoadID = L.ID and Status = 0) then
                L.DateCompleted
            when (L.PutExecutionId is not null and EE.CompletedOn is null) or (L.PostExecutionId is not null and EA.CompletedOn is null) then
                null
            when coalesce(EE.CompletedOn, '1/1/1900') > coalesce(EA.CompletedOn, '1/1/1900') then
                EE.CompletedOn
            else
                EA.CompletedOn      
            end
        else 
            L.DateCompleted 
        end as DateCompleted,
		case L.[Action]
			when 'M' then 'Users/Groups'
            when 'P' then 'Promotion'
			when 'R' then 'Relation'
			when 'U' then 'Unrelation'
            when 'L' then 'Lineage'
            when 'O' then 'Responsibilities'
            when 'T' then 'Lineage : Technical'
            when 'S' then 'Synonyms'
			when 'W' then 'Promotion (via Propose Workflow)'
		end as [Action],
        S.C as Success,
        E.C as Error,
        I.C as Incomplete,
		T.C as Total,
        R.FirstName + ' ' + R.LastName as Requestor
from	[Load] L
        left join api.Execution EE on EE.ExecutionId = L.PutExecutionID
        left join api.Execution EA on EA.ExecutionId = L.PostExecutionID
		left join (
			select [Name], [Object] ,ObjectID from AssetType
			union all
			select ITN.[Name] as [Name], 'IntersectType' as [Object], ID as ObjectID from IntersectType IT
			cross apply dbo.GetIntersectTypeNames(IT.ID) ITN

		) C_D on C_D.[Object] = L.[Object] and C_D.ObjectID = L.ObjectID 
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy       
        {0}";

        public IEnumerable<LoadDetail> GetLoadDetails()
        {
            var countSql = @"
                cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
                cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
                cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
                cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T";

            return Query<LoadDetail>(string.Format(LoadDetailBaseSql, countSql) + " order by L.ID desc");
        }

        public LoadDetail GetLoadDetail(int id)
        {
            var load = GetById<Load>(id);
            var useExecutionTable = false;

            if (v2ApiActions.Contains(load.Action) && (load.PostExecutionID != null || load.PutExecutionID != null))
                useExecutionTable = true;

            string countSql = "";

            if (useExecutionTable)
            {
                switch(load.Action)
                {
                    case "P":
                        countSql = @"
		                    cross apply (
				                    select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 0
				                    union all
				                    select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    ) R
			                    ) E
		                    cross apply (
				                    select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success is null
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    union all
				                    select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    ) R
			                    ) T";
                        break;
                    case "R":
                        countSql = @"		
                            cross apply (
				                    select count(*) as C from api.ExecutionRelationship where ExecutionID = L.PostExecutionID and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select Error as I from api.Execution where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) E
		                    cross apply (
				                select case when CompletedOn is null then (Total - Processed) else 0 end as C from api.Execution where ExecutionID = L.PostExecutionID
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select Total as I from api.Execution where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) T";
                        break;
                    case "U":
                        countSql = @"
                            cross apply (
				                    select count(*) as C from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success = 0
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) E
		                    cross apply (
				                    select count(*) as C from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success is null
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) T";
                        break;
                }
            }
            else
            {
                countSql = @"
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T";
            }

            return Query<LoadDetail>(string.Format(LoadDetailBaseSql, countSql) + " where L.ID = " + id).SingleOrDefault();
        }

        public IEnumerable<dynamic> GetLoadColumnDetails(int id)
        {
            return Query<dynamic>(@"
	            select		'Column' + cast(LC.ColumnIndex as varchar) as datafield,
	    		            LC.Name as text,
			                Lower(FT.[Type]) as type
                from		LoadColumn LC
                    inner join Load L on (LC.LoadID = L.ID)
                    left join FieldType FT on (FT.[Object] = L.[Object] and FT.[ObjectID] = L.[ObjectID] and LC.Name = FT.Name)
                where		LoadID = @id
                order by	ColumnIndex", new { id });
        }

        public IEnumerable<dynamic> GetLoadItemDetails(int id)
        {
            var load = GetById<Load>(id);
            var useExecutionTable = false;

            if (v2ApiActions.Contains(load.Action) && (load.PutExecutionID.HasValue || load.PostExecutionID.HasValue))
                useExecutionTable = true;

            var columns = Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var sql = "";
            var sqlColumns = "";
            var sqlTables = "";

            if (useExecutionTable)
            {
                
                switch (load.Action)
                {
                    case "P":
                        AssetType assetType = Filter<AssetType>(a => a.uid == load.AssetTypeUid).FirstOrDefault();
                        AssetType parentAssetType = assetType == null ? null : GetParentTypeById(assetType.ID);

                        sqlColumns = $"select @id as LoadID, I.RowIndex as RowIndex\n";
                        sqlTables = @"
                            from (
		                        select ExecutionId, ItemNumber, ExecutionItemUid, ParentAssetID, Message, Success from api.ExecutionAsset where ExecutionId = {0}
		                        union all
		                        select ExecutionID, ItemNumber, ExecutionItemUid, null as ParentAssetID, Message, cast(0 as bit) as Success from api.ExecutionAssetError where ExecutionId = {0}
	                         ) EA
                             left join LoadItem I on I.LoadID = @id and I.ExecutionItemUid = EA.ExecutionItemUid";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            if (parentAssetType != null && c.Name == parentAssetType.Name)
                            {
                                sqlColumns += $",EF{i}.DisplayValue + ' [' + cast(EF{i}.[uid] as varchar(50)) + ']' as Column{i}\n";
                                sqlTables += $" left join AssetDetail EF{i} on EF{i}.ID = EA.ParentAssetID\n";
                            }
                            else
                            {
                                sqlColumns += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                                sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";
                                sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";
                            }

                        });
                        sqlColumns += $", case EA.Success when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                        sqlColumns += ", case when EA.Message is null and EA.Success = 1 then '{0}' else  EA.Message end as StatusMessage\n";

                        sql = $"select * from ({string.Format(sqlColumns, "Item successfully updated.")} {string.Format(sqlTables, "@putExecutionID")} where EA.ExecutionID = @putExecutionID\n";
                        sql += $"union all\n";
                        sql += $"{string.Format(sqlColumns, "Item successfully added.")} {string.Format(sqlTables, "@postExecutionID")} where EA.ExecutionID = @postExecutionID) R order by R.RowIndex";

                        break;
                    case "R":
                        sqlColumns = $"select @id as LoadID, I.RowIndex as RowIndex\n";
                        sqlTables = @"from LoadItem I
                                      left join api.ExecutionRelationship EA on I.ExecutionItemUid = EA.ExecutionItemUid and EA.ExecutionID = @postExecutionID
                                      left join api.ExecutionRelationshipError ER on ER.ExecutionItemUid = I.ExecutionItemUid and ER.ExecutionID = @postExecutionID
                                      left join api.Execution E on E.ExecutionID = @postExecutionID ";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            sqlColumns += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                            sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";
                            sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";

                        });
                        sqlColumns += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else case when E.CompletedOn is null then 'Queued' else 'Failed' end end as [Status]\n";
                        sqlColumns += ", case when coalesce(EA.Message, ER.Message, I.StatusMessage) is null and EA.Success = 1 then case when EA.IsNew = 1 then 'Item successfully added.' else 'Item successfully updated.' end else coalesce(EA.Message, ER.Message, I.StatusMessage) end as StatusMessage\n";

                        sql = $"{sqlColumns} {sqlTables} where I.LoadID = @id order by RowIndex\n";

                        break;
                    case "U":
                        sqlColumns = $"select @id as LoadID, I.RowIndex as RowIndex\n";
                        sqlTables = @"from LoadItem I
                                        left join api.ExecutionDeletedRelationship EA on I.ExecutionItemUid = EA.ExecutionItemUid and EA.ExecutionID = @postExecutionID";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            sqlColumns += $",C{i}.[Value] as Column{i}\n";
                            sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";

                        });
                        sqlColumns += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                        sqlColumns += ", case when coalesce(EA.Message, I.StatusMessage) is null and EA.Success = 1 then 'Relationship successfully removed.' else  coalesce(EA.Message, I.StatusMessage) end as StatusMessage\n";

                        sql = $"{sqlColumns} {sqlTables} where I.LoadID = @id order by RowIndex\n";
                        break;
                }


                return Query<dynamic>(sql, new { id, putExecutionID = load.PutExecutionID, postExecutionID = load.PostExecutionID });
            }
            else
            {
                sqlColumns = "select I.LoadID, I.RowIndex";
                sqlTables = "from LoadItem I";
                columns.ForEach(c =>
                {
                    sqlColumns += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                    sqlTables += string.Format(" left join LoadItemColumn C{0} on C{0}.LoadID = I.LoadID and C{0}.RowIndex = I.RowIndex and C{0}.ColumnIndex = {0}", c.ColumnIndex);
                });
                sqlColumns += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage";

                sql += sqlColumns + " " + sqlTables + " where I.LoadID = @id order by I.RowIndex";

                return Query<dynamic>(sql, new { id });
            }
        }

        public BulkLoadGetLoadColumnsModel GetLoadColumns(string action, SystemObjects type, int id, bool includeLookupValues)
        {
            return GetLoadColumns(action, type.ToString(), id, includeLookupValues);
        }

        public BulkLoadGetLoadColumnsModel GetLoadColumns(string action, string type, int id, bool includeLookupValues)
        {
            var jsonItems = Query<string>($"bulkload.GetLoadColumns @action, @type, @id, @getLookups", new { action, type = type, id, getLookups = includeLookupValues });
            var json = string.Concat(jsonItems);
            var model = JsonConvert.DeserializeObject<BulkLoadGetLoadColumnsModel>(json);

            return model;
        }

        #endregion
        

        #region Process Data Methods

        private int getAssetIDFieldIndex(string objectType, string objectName, int objectId, List<LoadColumn> columns)
        {
            if (objectType == "IntersectType")
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName}", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET ID COLUMN : [{objectName}]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
            else if (objectType == "ReferenceItemType" && objectId == 0)
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName} Asset Type Uid", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET UID COLUMN : [{objectName} Asset Uid]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
            else
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName} Asset Uid", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET UID COLUMN : [{objectName} Asset Uid]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
        }

        #endregion

        #region v2 API Methods

        private const int timeout = 3600;
        private const int defaultBulkLoadLoopSize = 500;
        private readonly List<string> v2ApiActions = new List<string>() { "P", "R", "U" };

        internal class BulkLoadExecutionFields_Assets
        {
            public Guid AssetTypeUid { get; set; }
            public int LoadID { get; set; }
        }

        internal class BulkLoadExecutionFields_Relationships
        {
            public Guid IntersectTypeUid { get; set; }
            public int LoadID { get; set; }
        }


        internal ApiExecution getPromoteApiExecution(Load load, int total)
        {

            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = null,
                Method = "BULK",
                ResourceID = load.UpdatedBy ?? 0,
                Total = total,
                Fields = load.AssetTypeUid.HasValue ? JsonConvert.SerializeObject(
                    new BulkLoadExecutionFields_Assets
                    {
                        AssetTypeUid = (Guid)load.AssetTypeUid,
                        LoadID = load.ID
                    }) : null,
                Error = 0,
                Processed = 0,
                ApplicationId = "Internal/BulkLoad/Promote"
            };

            return execution;
        }

        internal ApiExecution getRelateApiExecution(Load load, int total)
        {

            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = null,
                Method = null,
                ResourceID = load.UpdatedBy ?? 0,
                Total = total,
                Fields = load.IntersectTypeUid.HasValue ? JsonConvert.SerializeObject(
                    new BulkLoadExecutionFields_Relationships
                    {
                        IntersectTypeUid = (Guid)load.IntersectTypeUid,
                        LoadID = load.ID
                    }) : null,
                Error = 0,
                Processed = 0,
                ApplicationId = "Internal/BulkLoad/Relate"
            };

            return execution;
        }

        private async Task GenerateExecutionItemUids(Load load, int timeout = 90)
        {
            await QueryAsync<int>(@"update LoadItem set ExecutionItemUid = newid() where LoadID = @id and ExecutionItemUid is null", new { id = load.ID }, timeout: timeout);
        }

        #region Bulk Promote

        public async Task BulkLoadAssets(Load load, IAssetRepository repository)
        {

            if (load == null)
                throw new ArgumentNullException("load cannot be null");

            if (!load.AssetTypeUid.HasValue)
                throw new ArgumentNullException("asset type uid cannot be null");

            try
            {
                var assetTypeUid = (Guid)load.AssetTypeUid;
                AssetType assetType = repository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    throw new Exception($"Asset type with uid {assetTypeUid} not found");


                var hasLookups = FieldTypes.Any(f => f.AssetTypeID == assetType.ID && f.LookupObjectID != null);

                await GenerateExecutionItemUids(load, timeout);

                //get parent type info if applicable
                var parentAssetType = GetParentType(assetType.ObjectID, SystemObjectHelper.GetSystemObjects(assetType.Class));
                int? intersectTypeId = null;
                PredicateType? predicateType = null;
                bool calculateParentHashByUid = false;
                int maxLevel = 1;

                switch (assetType.Class)
                {
                    case AssetTypeClass.BusinessAsset:
                    case AssetTypeClass.TechnicalAsset:
                    case AssetTypeClass.ReferenceItemType:
                        predicateType = PredicateType.InterTypeHierarchy;
                        calculateParentHashByUid = true;
                        break;
                    case AssetTypeClass.Policy:
                    case AssetTypeClass.Model:
                        predicateType = PredicateType.IntraTypeHierarchy;
                        calculateParentHashByUid = false;
                        break;
                }

                if (predicateType.HasValue)
                {
                    var intersectType = Filter<IntersectType>(o => o.Object == assetType.Object && o.ObjectID == assetType.ObjectID && o.Predicate.Type == predicateType).FirstOrDefault();
                    intersectTypeId = intersectType?.ID;
                }



                await Connection.OpenAsync();
                //calculate key hashes and resolve lookup values
                using (var trans = Connection.BeginTransaction())
                {
                    try
                    {
                        var executionID = Guid.NewGuid();

                        if (assetType.Class == AssetTypeClass.Model)
                        {
                            maxLevel = await Connection.QuerySingleAsync<int>(@"
                            select coalesce(max(L.[Level]), 1) from LoadColumn LC
                            inner join LoadItemColumn LIC on LIC.LoadID = @ID and LIC.ColumnIndex = LC.ColumnIndex and LIC.[Value] is not null
                            inner join (
								select	AssetTypeID, [Level], [Name] 
								from	AssetTypeLevel L
								where	L.AssetTypeID = @atID
								union all
								select	T.ID, N.Level, 'Level ' + cast(N.Level as nvarchar(30)) 
								from	AssetType T
										outer apply (select top 100 row_number() over (order by (select null)) [Level] FROM sys.objects) N
								where	T.ID = @atID and N.[Level] <= T.HierarchyMaximumDepth
										and not exists (select 1 from AssetTypeLevel where AssetTypeID = T.ID and [Level] = N.[Level])
                            ) L on L.AssetTypeID = @atID and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
                            where LC.Loadid = @ID;", new { load.ID, atID = assetType.ID }, transaction: trans);
                        }

                        await Connection.ExecuteAsync(@"
                        drop table if exists #BulkExecutionAsset;
                        create table #BulkExecutionAsset (ExecutionID uniqueidentifier, ItemNumber int, ParentUid uniqueidentifier, ProposedKey varchar(32), AssetUid uniqueidentifier, AssetID bigint, Success bit, Message nvarchar(max))

                        create nonclustered index IX_TempBulkExecutionField on #BulkExecutionAsset (ItemNumber asc );

                        drop table if exists #BulkExecutionField;
                        create table #BulkExecutionField (ExecutionID uniqueidentifier, ItemNumber int, FieldName nvarchar(250), FieldValue nvarchar(max), FieldTypeID int, LookupValue nvarchar(max), Ignore bit, ColumnIndex int);
                        create nonclustered index IX_TempBulkExecutionAsset on #BulkExecutionField (ColumnIndex asc,ItemNumber asc);                        
                        ", transaction: trans, commandTimeout: timeout);

                        //load temp tables and calculate key hashes
                        await Connection.ExecuteAsync($@"
                        insert into #BulkExecutionAsset
                        select	@executionID as ExecutionID,
		                        RowIndex as ItemNumber,
		                        ParentAssetUid as ParentUid,
		                        null as ProposedKey,
                                null as AssetUid,
                                null as AssetID,
                                null as [Success],
                                null as [Message]
                        from	[LoadItem] L
                        where	L.LoadID = @ID

                        insert into #BulkExecutionField
                        select	BA.ExecutionID,
		                        I.RowIndex as ItemNumber,
		                        coalesce(FT.[Name], LC.[Name]) as FieldName,
		                        I.[Value] as FieldValue,
		                        FT.ID as FieldTypeID,
		                        I.LookupObjectID as LookupValue,
		                        null as Ignore,
                                I.ColumnIndex
                        from    [Load] L
                                inner join AssetType T on T.[Object] = L.[Object] and T.ObjectID = L.ObjectID
                                inner join LoadColumn LC on LC.LoadID = L.ID
                                inner join LoadItemColumn I on I.LoadID = L.ID and I.ColumnIndex = LC.ColumnIndex
		                        inner join #BulkExecutionAsset BA on BA.ItemNumber = I.RowIndex
                                left join FieldType FT on FT.[Name] = LC.[Name] and FT.[Object] = T.[Object] and FT.ObjectID = T.ObjectID
                        where   L.ID = @ID;

                        --handle ref lists
                        if @class = 9
                        begin
                            delete from #BulkExecutionField where (FieldTypeID is null and FieldName <> 'Code');
                        end

                        --handle model levels
                        if @class = 2
                        begin

                            update  F
                            set     F.FieldName = FT.Name,
                                    F.FieldTypeID = FT.ID
                            from    #BulkExecutionField F
                                    inner join AssetType T on T.ID = @atID
                                    inner join (
								        select	AssetTypeID, [Level], [Name] 
								        from	AssetTypeLevel L
								        where	L.AssetTypeID = @atID
								        union all
								        select	T.ID, N.Level, 'Level ' + cast(N.Level as nvarchar(30))
								        from	AssetType T
										        outer apply (select top 100 row_number() over (order by (select null)) [Level] FROM sys.objects) N
								        where	T.ID = @atID and N.[Level] <= T.HierarchyMaximumDepth
										        and not exists (select 1 from AssetTypeLevel where AssetTypeID = T.ID and [Level] = N.[Level])
                            ) L on L.AssetTypeID = T.ID and L.[Level] <= @maxLevel
                                    inner join FieldType FT on FT.Name = replace(F.FieldName,L.[Name] + ' ', '')  and FT.[Object] = T.[Object] and FT.ObjectID = T.ObjectID
                            where   F.FieldName = (coalesce(L.Name,'') + ' ' + coalesce(FT.Name,'')) and F.FieldTypeID is null;

                            delete from #BulkExecutionField where FieldTypeID is null;


                            --build model text path from sheet to find existing assets and parent assets

                            drop table if exists #PathFields;
                            create table #PathFields 
                            (
                            ItemNumber int,
                            ColumnIndex int,
                            DisplayValue nvarchar(max)
                            );

                            drop table if exists #PathValues;
                            create table #PathValues
                            (
	                            ItemNumber int,
	                            FullPath nvarchar(max),
	                            ParentPath nvarchar(max),
	                            [Uid] uniqueidentifier,
	                            ParentUid uniqueidentifier
                            );

                            insert into #PathFields
                            select		A.ItemNumber,
			                            D.ColumnIndex,
			                            string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
                            from		#BulkExecutionAsset A
			                            inner join AssetType T on T.ID = @atID
			                            outer apply (
						                            select	TL.value,
								                            F.FieldValue as FormattedValue,
								                            F.ColumnIndex,
								                            F.ItemNumber
						                            from	string_split(replace(T.DisplayFormat, '{{', ' | '), '|') TF
								                            cross apply string_split(replace(TF.[value], '}}', '|'), '|') TL
								                            left join FieldType FT on FT.AssetTypeID = T.ID and FT.Name like TRIM(TL.Value)
								                            left join #BulkExecutionField F on F.FieldTypeID = FT.ID
						                            where	RTRIM(TF.value) <> ''
								                            and RTRIM(TL.value) <> ''
						                            ) D
                            where		A.ItemNumber = D.ItemNumber
                            group by	A.ItemNumber,
			                            D.ColumnIndex

                            insert into #PathValues
                            select		F.ItemNumber, 
			                            string_agg(F.DisplayValue,'/')  within group (order by F.ColumnIndex asc) as FullPath,
			                            null as ParentPath,
			                            null as [Uid],
			                            null as [ParentUid]
                            from		#PathFields F
                            group by	F.ItemNumber;


                            update V
                            set V.ParentPath = P.ParentPath
                            from #PathValues V
                            cross apply (
	                            select	F.ItemNumber, 
			                            string_agg(F.DisplayValue,'/') within group (order by F.ColumnIndex asc) as ParentPath
	                            from	#PathFields F
	                            where	F.ColumnIndex < (select max(ColumnIndex) from #PathFields)
	                            group by F.ItemNumber
                            ) P
                            where P.ItemNumber = V.ItemNumber;

                            update V
                            set V.Uid = A.UId
                            from #PathValues V 
                            inner join Asset A on A.AssetTypeID = @atID
                            cross apply dbo.GetAssetTextPathById(A.ID, '/') T
                            where V.FullPath = T.TextPath;

                            update V
                            set V.ParentUid = A.Uid
                            from #PathValues V 
                            inner join Asset A on A.AssetTypeID = @atID
                            cross apply dbo.GetAssetTextPathById(A.ID, '/') T
                            where V.ParentPath = T.TextPath;


                            update A
                            set A.AssetUid = P.Uid,
                            A.ParentUid = P.ParentUid
                            from #BulkExecutionAsset A
                            inner join #PathValues P on P.ItemNumber = A.ItemNumber;
                            
                            update B
                            set B.AssetID = A.ID
                            from #BulkExecutionAsset B
                            inner join Asset A on A.[uid] = B.AssetUid;

                            --update LoadItem with correct parent uid for API
						    update L
							set L.ParentAssetUid = A.ParentUid
							from LoadItem L
							inner join #BulkExecutionAsset A on A.ItemNumber = L.RowIndex
							where L.LoadID = @ID;


                        end
                        "
                            , new { executionID, load.ID, atID = assetType.ID, @class = assetType.Class, maxLevel }, transaction: trans, commandTimeout: timeout);

                        if (intersectTypeId.HasValue && calculateParentHashByUid)
                        {
                            //need to parse parent column here to be used in proposed key
                            await Connection.ExecuteAsync(@"
                                --flag records with missing parent uids, they will error out later in the API
                                update A
                                set A.Success = 0
                                from #BulkExecutionAsset A
                                inner join [LoadColumn] LC on LC.Name = @parentAssetTypeName and LC.LoadID = @ID
                                inner join #BulkExecutionField F on F.ColumnIndex = LC.ColumnIndex and F.ItemNumber = A.ItemNumber
                                where A.ExecutionID = @executionID 
                                and (charindex('[', F.FieldValue) = 0 or charindex(']', F.FieldValue) = 0)

                                update A
                                set A.ParentUid =
                                reverse(
	                                substring(	reverse(F.FieldValue), 
				                                charindex(']',reverse(F.FieldValue)) + 1, 
				                                charindex('[',reverse(F.FieldValue)) - charindex(']',reverse(F.FieldValue)) - 1))
                                from #BulkExecutionAsset A
                                inner join [LoadColumn] LC on LC.Name = @parentAssetTypeName and LC.LoadID = @ID
                                inner join #BulkExecutionField F on F.ColumnIndex = LC.ColumnIndex and F.ItemNumber = A.ItemNumber
                                where A.ExecutionID = @executionID and (A.Success is null or A.Success = 1)
                            ", new { load.ID, executionID, parentAssetTypeName = parentAssetType.Name}, transaction: trans, commandTimeout: timeout);
                        }

                        CalculateProposedKeyHashes(assetType, executionID, timeout, intersectTypeId, trans, "#BulkExecutionAsset", "#BulkExecutionField");

                        if (assetType.Class == AssetTypeClass.Reference)
                        {
                            await Connection.ExecuteAsync(@"
                                drop table if exists #AssetActiveKey;

                               select  A.Uid,                                                
                                    utility.GetHash(cast(@atID as nvarchar) + '|' + A.Code)  as ActiveKey
                                    into #AssetActiveKey
                                        from  Asset A    
                                        where A.AssetTypeID = @atID
                                    group by A.Uid, Code

                                Create index idx_AssetActiveKey on #AssetActiveKey(ActiveKey);

                                

                                update T set T.AssetUid = K.Uid                                  
                                from #BulkExecutionAsset T                                   
                                inner join  #AssetActiveKey K on K.ActiveKey = T.ProposedKey; 

                                update L
                                set L.AssetUid = T.AssetUid
                                from LoadItem L
                                inner join #BulkExecutionAsset T on T.ItemNumber = L.RowIndex
                                where L.LoadID = @ID
                            ", new { atID = assetType.ID, load.ID }, transaction: trans, commandTimeout: timeout);
                        }
                        else
                        {

                            if ((calculateParentHashByUid || assetType.Class == AssetTypeClass.Model) && intersectTypeId.HasValue)
                            {
                                await Connection.ExecuteAsync(@"
                                    declare @assetttypeID int = (select @atID);
                                    declare @fieldtypeid int = 0;

                                    drop table if exists #tempfielddata;
                                    drop table if exists tempcalasset;
                                    drop table if exists #AssetActiveKey;

                                    ----Below statement to get first fieldtypeid primary key column order by columnorder
                                    select top 1    @fieldtypeid =  ft.ID
                                    from            #BulkExecutionField f
                                                    inner join FieldType ft on f.FieldTypeID = ft.id and FT.IsPartOfKey = 1
                                    order by        ft.ColumnOrder,FT.Name;

                                    -- getting field value of above query getting fieldtypeid. No need to calculate hash. 
                                    -- Calculate only for requested data only.
                                    -- Later it help to get qualified asset

                                    select distinct     TRIM(FieldValue) FieldValue
                                    into                #tempfielddata
                                    from                #BulkExecutionField
                                    where               FieldTypeID = @fieldtypeid;

                                    -- getting asset information of qualified fieldvalue with fieldtypeid.
                                    select  a.uid [AssetUid],
                                            a.Object,
                                            a.ObjectID,
                                            a.ID
                                    into    #tempcalasset
                                    from    Field f
                                            inner join asset a on a.ID = f.AssetID
                                    where   FieldTypeID = @fieldtypeid
                                            and exists (select 1 from #tempfielddata t where t.FieldValue = trim(f.FormattedValue));

                                    -- create clustered index on AssetUid because no further insert,delete,update on temporary table
                                    create clustered index idx_tempasset on #tempcalasset([AssetUid]);

                                    -- hash value only required asset only with all primary key 
                                    select		t.AssetUid [Uid],
			                                    utility.GetHash(cast(@assetttypeID as nvarchar) + '|' + COALESCE(cast(P.Uid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.Value, F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey
                                    into		#AssetActiveKey
                                    from		#tempcalasset t
			                                    left join [Intersect] I on I.IntersectTypeID = @intersectTypeId and I.Object = t.Object and I.ObjectID = t.ObjectID
			                                    left join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
			                                    inner join FieldType FT on FT.AssetTypeID = @assetttypeID and FT.IsPartOfKey = 1
			                                    left join Field F on FT.ID = F.FieldTypeID and F.AssetID = T.ID
                                    group by    t.AssetUid, P.Uid

                                    Create index idx_AssetActiveKey on #AssetActiveKey(ActiveKey);

                                    update  T                                  
                                    set     T.AssetUid = K.Uid                                  
                                    from    #BulkExecutionAsset T                                   
                                            inner join  #AssetActiveKey K on K.ActiveKey = T.ProposedKey;

                                    update  L
                                    set     L.AssetUid = T.AssetUid
                                    from    LoadItem L
                                            inner join #BulkExecutionAsset T on T.ItemNumber = L.RowIndex
                                    where   L.LoadID = @ID
                                    ", new { atID = assetType.ID, load.ID, intersectTypeId }, transaction: trans, commandTimeout: timeout);

                            }
                            else
                            {
                                await Connection.ExecuteAsync(@"

                                drop table if exists #AssetActiveKey;

                                select		A.Uid,
			                                utility.GetHash(cast(@atID as nvarchar) + '|' + STRING_AGG(coalesce(F.Value, F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey
                                Into		#AssetActiveKey
                                from		Asset A
                                            outer apply dbo.getassetlevelbyid(A.ID) L
			                                inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			                                left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
                                where	    A.AssetTypeID = @atID and coalesce(L.[Level], 1) = @maxLevel
                                group by    A.Uid

                                Create index idx_AssetActiveKey on #AssetActiveKey(ActiveKey);

                                update T                                  
                                set T.AssetUid = K.Uid                                  
                                from #BulkExecutionAsset T                                   
                                inner join  #AssetActiveKey K
                                on K.ActiveKey = T.ProposedKey;

                                update L
                                set L.AssetUid = T.AssetUid
                                from LoadItem L
                                inner join #BulkExecutionAsset T on T.ItemNumber = L.RowIndex
                                where L.LoadID = @ID
                            ", new { atID = assetType.ID, load.ID, maxLevel }, transaction: trans, commandTimeout: timeout);
                            }

                        }

                        trans.Commit();
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
                        }
                        throw ex;
                    }
                }

                var putAssets = new List<AssetUpdate>();
                var postAssets = new List<AssetInsert>();

                var loadItems = new List<LoadItem>();
                var loadColumns = Query<LoadColumn>("select * from LoadColumn LC where LoadID = @id", new { id = load.ID }).ToList();
                

                var assetTypeLevels = new Dictionary<int, string>();

                //build level info for models
                if (assetType.Class == AssetTypeClass.Model)
                {
                    for (var i = 1; i <= assetType.HierarchyMaximumDepth; i++)
                    {
                        var level = AssetTypeLevels.Where(l => l.AssetTypeID == assetType.ID).FirstOrDefault(l => l.Level == i);
                        if (level != null)
                            assetTypeLevels.Add(i, level.Name);
                        else
                            assetTypeLevels.Add(i, $"Level {i}");
                    }

                    loadItems = (await QueryAsync<LoadItem>(@"
                        select I.*, L.[Level] from LoadItem I
                        outer apply (
                            select      coalesce(max(L.[Level]), 1) as [Level]
                                from		AssetType ATT
			                                inner join (
								                select	AssetTypeID, [Level], [Name] 
								                from	AssetTypeLevel L
								                where	L.AssetTypeID = @atID
								                union all
								                select	T.ID, N.Level, 'Level ' + cast(N.Level as nvarchar(30)) 
								                from	AssetType T
										                outer apply (select top 100 row_number() over (order by (select null)) [Level] FROM sys.objects) N
								                where	T.ID = @atID and N.[Level] <= T.HierarchyMaximumDepth
										                and not exists (select 1 from AssetTypeLevel where AssetTypeID = T.ID and [Level] = N.[Level])
                                            ) L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')
			                                inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
			                                inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = I.RowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
                                where		ATT.[ObjectID] = @ObjectID
                        ) L
                        where I.LoadID = @id ", new { id = load.ID, assetType.ObjectID, atID = assetType.ID }, timeout: timeout)).ToList();
                }
                else
                {
                    loadItems = Query<LoadItem>("select * from LoadItem where LoadID = @id", new { id = load.ID }).ToList();
                }

                //do this in blocks of n items at a time to avoid loading everything in one shot.
                int loopSize = defaultBulkLoadLoopSize;

                //check for an override to the default.
                if (int.TryParse(ConfigurationManager.AppSettings["BulkLoadLoopSize"], out int tempLoopSize))
                {
                    loopSize = tempLoopSize >= 0 ? tempLoopSize : defaultBulkLoadLoopSize;
                }                

                int currentLocation = 0; 
                int numberOfLoops = (int)Math.Ceiling((decimal)(loadItems.Count - currentLocation) / loopSize);
                int beginItemNumber = currentLocation;
                int endItemNumber = (currentLocation + loopSize) > loadItems.Count ? loadItems.Count : currentLocation + loopSize;
                int rowIndexStartNumber = 2;

                for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                {
                    //bulk load rowindex starts with 2!
                    var loadItemColumns = Query<LoadItemColumn>("select * from LoadItemColumn where LoadID = @id and RowIndex between @beginItemNumber and @endItemNumber", new { id = load.ID, beginItemNumber = beginItemNumber+ rowIndexStartNumber, endItemNumber = endItemNumber + rowIndexStartNumber }).ToList();

                    //create API models                    
                    for(int currentIndex = beginItemNumber; currentIndex < endItemNumber; currentIndex++)
                    {                        
                        var item = loadItems[currentIndex];
                        var fieldsToSkip = new List<string>();
                        string assetTypeLevel = null;

                        var rowColumns = loadItemColumns.Where(l => l.RowIndex == item.RowIndex).ToList();
                        
                        if (assetType.Class == AssetTypeClass.Model)
                        {
                            assetTypeLevel = assetTypeLevels[item.Level];

                            //ignore parent key fields, not needed for API
                            var keyFields = FieldTypes.Where(f => f.Object == assetType.Object && f.ObjectID == assetType.ObjectID && f.IsPartOfKey);
                            foreach (var k in keyFields)
                                fieldsToSkip.AddRange(assetTypeLevels.ToList().Where(l => l.Key != item.Level).Select(l => $"{l.Value} {k.Name}"));
                        }

                        if (!item.AssetUid.HasValue)
                        {
                            var insert = new AssetInsert();
                            insert.ExecutionItemUid = item.ExecutionItemUid;
                            insert.Fields = new Dictionary<string, string>();

                            //resolve model parent
                            if (assetType.Class == AssetTypeClass.Model && item.Level > 1)
                            {
                                var parentKeyHash = await GetModelKeyHashForLevel(item, assetType, item.Level - 1);
                                var itemPath = await GetModelPathForLevel(item, assetType, item.Level);

                                Guid? parentUid = (await QueryAsync<Guid?>(@"select [uid] from asset a
                                cross apply GetAssetKeyHashById(A.ID) S
								cross apply dbo.GetAssetTextPathById(A.ID, '>') TP
                                where a.AssetTypeID = @assetTypeId 
                                and TP.TextPath like @textPath
                                and S.KeyHash = @parentKeyHash", new { parentKeyHash, assetTypeId = assetType.ID, textPath = itemPath })).FirstOrDefault();

                                if (parentUid.HasValue)
                                    insert.ParentUid = parentUid;
                            }

                            foreach (var field in rowColumns)
                            {
                                var col = loadColumns.FirstOrDefault(c => c.ColumnIndex == field.ColumnIndex);

                                //resolve parent
                                if (parentAssetType != null && col.Name == parentAssetType.Name)
                                {
                                    if (!string.IsNullOrWhiteSpace(field.Value))
                                    {
                                        string parentUid = "";
                                        int endIndex = field.Value.LastIndexOf(']');
                                        int startIndex = field.Value.LastIndexOf('[') + 1;
                                        if (startIndex > -1 && endIndex > -1 && startIndex < endIndex)
                                        {
                                            parentUid = field.Value.Substring(startIndex, (endIndex - startIndex));
                                            insert.ParentUid = new Guid(parentUid);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(field.Value) && !fieldsToSkip.Contains(col.Name))
                                    {
                                        if (!string.IsNullOrEmpty(assetTypeLevel) && col.Name.StartsWith($"{assetTypeLevel} "))
                                            insert.Fields.Add(col.Name.Replace($"{assetTypeLevel} ", ""), field.Value);
                                        else
                                            insert.Fields.Add(col.Name, field.Value);
                                    }
                                }
                            }
                            postAssets.Add(insert);
                        }
                        else
                        {
                            var update = new AssetUpdate();
                            update.ExecutionItemUid = item.ExecutionItemUid;

                            if ((parentAssetType != null || assetType.Class == AssetTypeClass.Model) && item.ParentAssetUid.HasValue)
                                update.ParentUid = item.ParentAssetUid;

                            update.Uid = ((Guid)item.AssetUid);
                            update.Fields = new Dictionary<string, string>();

                            foreach (var field in rowColumns)
                            {
                                var col = loadColumns.FirstOrDefault(c => c.ColumnIndex == field.ColumnIndex);

                                if (parentAssetType != null && col.Name == parentAssetType.Name)
                                {
                                    continue;
                                }
                                else if (!fieldsToSkip.Contains(col.Name))
                                {
                                    if (assetTypeLevel != null && col.Name.StartsWith($"{assetTypeLevel} "))
                                        update.Fields.Add(col.Name.Replace($"{assetTypeLevel} ", ""), field.Value);
                                    else
                                        update.Fields.Add(col.Name, field.Value);
                                }
                            }
                            putAssets.Add(update);
                        }
                    }

                    beginItemNumber += loopSize;
                    endItemNumber += loopSize;
                    if (endItemNumber > loadItems.Count)
                    {
                        endItemNumber = loadItems.Count;
                    }
                }

                if (putAssets.Any())
                {
                    var execution = getPromoteApiExecution(load, putAssets.Count);
                    ApiExecutionInfo executionInfo = await repository.PutBulkAssets(assetTypeUid, putAssets, execution, false);
                    load.PutExecutionID = executionInfo.ExecutionID;
                }

                if (postAssets.Any())
                {
                    var execution = getPromoteApiExecution(load, postAssets.Count);
                    ApiExecutionInfo executionInfo = await repository.PostBulkAssets(postAssets, execution, false);
                    load.PostExecutionID = executionInfo.ExecutionID;
                }

                await SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> GetModelKeyHashForLevel(LoadItem item, AssetType assetType, int level)
        {
            return (await QueryAsync<string>(@"select
       K.KeyHash
from    LoadItem T
        left join	(
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					        2) as KeyHash
	        from		(
					        select top 100 percent
						        IC.RowIndex, 
						        FT.ID as FieldTypeID, 
						        coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],'') as [Value] 
					        from LoadColumn LC
					        inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @rowIndex and IC.ColumnIndex = LC.ColumnIndex
					        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
					        where LC.LoadID = @id and LC.ColumnIndex in (
			 			        select		LC.ColumnIndex 
						        from		AssetType ATT
									        inner join (
	                                            select	AssetTypeID, [Level], [Name] 
								                from	AssetTypeLevel L
								                where	L.AssetTypeID = @atID
								                union all
								                select	T.ID, N.Level, 'Level ' + cast(N.Level as nvarchar(30)) 
								                from	AssetType T
										                outer apply (select top 100 row_number() over (order by (select null)) [Level] FROM sys.objects) N
								                where	T.ID = @atID and N.[Level] <= T.HierarchyMaximumDepth
										                and not exists (select 1 from AssetTypeLevel where AssetTypeID = T.ID and [Level] = N.[Level])
                                            ) L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')																	
									        inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
									        inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @rowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
						        where		ATT.ObjectID = @ObjectID and L.[Level] = @currLevel
						        )
				        ) A
	        group by	A.RowIndex
        ) K on K.RowIndex = T.RowIndex
where	T.LoadID = @id and T.RowIndex = @rowIndex;", new { id = item.LoadID, rowIndex = item.RowIndex, currLevel = level, atID = assetType.ID, @object = new DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = assetType.Object }, objectID = assetType.ObjectID }, timeout: timeout)).FirstOrDefault();
        }

        private async Task<string> GetModelPathForLevel(LoadItem item, AssetType assetType, int level) 
        {

            return (await QueryAsync<string>(@"
            select STRING_AGG( ISNULL(coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],''), ' '), '>') as [Value]
                from LoadColumn LC
                inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @rowIndex and IC.ColumnIndex = LC.ColumnIndex
                inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
                    where LC.LoadID = @id and LC.ColumnIndex in (
                        select		LC.ColumnIndex 
                        from		AssetType ATT
                            inner join (
	                            select	AssetTypeID, [Level], [Name] 
								from	AssetTypeLevel L
								where	L.AssetTypeID = @atID
								union all
								select	T.ID, N.Level, 'Level ' + cast(N.Level as nvarchar(30)) 
								from	AssetType T
										outer apply (select top 100 row_number() over (order by (select null)) [Level] FROM sys.objects) N
								where	T.ID = @atID and N.[Level] <= T.HierarchyMaximumDepth
										and not exists (select 1 from AssetTypeLevel where AssetTypeID = T.ID and [Level] = N.[Level]) 
                            ) L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')																	
                            inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
                            inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @rowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
                        where		ATT.ObjectID = @ObjectID and L.[Level] not like @currLevel)"
                    , new { 
                        id = item.LoadID, 
                        rowIndex = item.RowIndex, 
                        currLevel = level, 
                        atID = assetType.ID,
                        @object = new DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = assetType.Object }, 
                        objectID = assetType.ObjectID }, timeout: timeout)).FirstOrDefault();
        }

        #endregion

        #region Bulk Relate/Unrelate

        public async Task BulkRelation(Load load, IRelationshipRepository relationshipRepository, IAssetRepository assetRepository, BulkRelationshipOperation operation)
        {
            if (load == null)
            {
                throw new ArgumentNullException("load cannot be null");
            }

            if (!load.IntersectTypeUid.HasValue)
            {
                throw new ArgumentNullException("intersect type uid cannot be null");
            }

            await GenerateExecutionItemUids(load, timeout);

            try
            {
                var intersectType = relationshipRepository.GetIntersectTypeByUid((Guid)load.IntersectTypeUid);

                if (intersectType == null)
                {
                    throw new Exception($"intersect type for uid {load.IntersectTypeUid} not found");
                }

                var subjectUid = (await QueryAsync<Guid?>("select [uid] from AssetType where Object = @subject and ObjectID = @subjectID", new { intersectType.Subject, intersectType.SubjectID })).FirstOrDefault();
                var objectUid = (await QueryAsync<Guid?>("select [uid] from AssetType where Object = @object and ObjectID = @objectID", new { intersectType.Object, intersectType.ObjectID })).FirstOrDefault();

                if (subjectUid == null || objectUid == null)
                {
                    throw new Exception($"Intersect subject or asset not found");
                }

                var subjectAssetType = assetRepository.GetAssetTypeByUID((Guid)subjectUid);
                var objectAssetType = assetRepository.GetAssetTypeByUID((Guid)objectUid);

                if (subjectAssetType == null)
                {
                    throw new Exception($"Could not find subject asset type for uid [{subjectUid}]");
                }
                if (objectAssetType == null)
                {
                    throw new Exception($"Could not find object asset type for uid [{objectUid}]");
                }

                var subjectIsReferenceItemType = subjectAssetType.Object == "ReferenceItemType" && subjectAssetType.ObjectID == 0;
                var objectIsReferenceItemType = objectAssetType.Object == "ReferenceItemType" && objectAssetType.ObjectID == 0;

                // get the load columns
                var columns = LoadColumns.Where(x => x.LoadID == load.ID).ToList();
                if (columns == null)
                {
                    throw new Exception($"Bulk load data doesnt contain any columns in LoadColumn table.  Load ID [{load.ID}]");
                }

                var fieldColumns = columns.ToList();

                var subjectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Subject, subjectAssetType.Name, intersectType.SubjectID, fieldColumns);
                var objectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Object, objectAssetType.Name, intersectType.ObjectID, fieldColumns);

                var loadItems = Query<LoadItem>("select * from LoadItem where LoadID = @id", new { id = load.ID }).ToList();
                var loadColumns = Query<LoadColumn>("select * from LoadColumn LC where LoadID = @id", new { id = load.ID }).ToList();
                var loadItemColumns = Query<LoadItemColumn>("select * from LoadItemColumn where LoadID = @id", new { id = load.ID }).ToList();


                if (operation == BulkRelationshipOperation.Relate)
                {
                    RelationshipInserts upserts = new RelationshipInserts();
                    foreach (var item in loadItems)
                    {
                        RelationshipInsert upsert = new RelationshipInsert();
                        upsert.ExecutionItemUid = item.ExecutionItemUid;
                        item.StatusMessage = "";
                        item.Status = null;

                        var rowColumns = loadItemColumns.Where(l => l.RowIndex == item.RowIndex).ToList();

                        foreach (var field in rowColumns)
                        {
                            if (field.ColumnIndex == subjectAssetIDFieldIndex)
                            {
                                Guid uid = Guid.Empty;
                                
                                if (!Guid.TryParse(field.Value, out uid))
                                {
                                    item.Status = false;
                                    item.StatusMessage += "Subject asset uid is not in a valid format.";
                                }

                                upsert.SubjectAssetUid = uid;
                            }
                            else if (field.ColumnIndex == objectAssetIDFieldIndex)
                            {
                                Guid uid = Guid.Empty;
                                
                                if (!Guid.TryParse(field.Value, out uid))
                                {
                                    item.Status = false;
                                    item.StatusMessage += "Subject asset uid is not in a valid format.";
                                }

                                upsert.ObjectAssetUid = uid;
                            }
                            else
                            {
                                var col = loadColumns.FirstOrDefault(c => c.ColumnIndex == field.ColumnIndex);
                                upsert.Fields.Add(col.Name, field.Value);
                            }
                        }

                        if (item.Status == false)
                        {
                            await Connection.ExecuteAsync(@"
                                update  LoadItem 
                                set     Status = 0, 
                                        StatusMessage = @msg 
                                where   LoadID = @id 
                                        and RowIndex = @rowIndex", 
                                        new { load.ID, msg = item.StatusMessage, rowIndex = item.RowIndex }, commandTimeout: timeout);
                        }
                        else
                        {
                            upserts.Add(upsert);
                        }
                    }


                    if (upserts.Any())
                    {
                        var execution = getRelateApiExecution(load, upserts.Count);
                        ApiExecutionInfo executionInfo = await relationshipRepository.BulkPostRelationships(intersectType.uid, upserts, execution, false);
                        load.PostExecutionID = executionInfo.ExecutionID;
                    }
                }

                if (operation == BulkRelationshipOperation.Unrelate)
                {
                    RelationshipDeletes deletes = new RelationshipDeletes();

                    if (Connection.State == ConnectionState.Closed)
                    {
                        await Connection.OpenAsync();
                    }

                    //populate intersect IDs
                    await Connection.ExecuteAsync($@"update L
                        set     L.IntersectUid = coalesce(I.[uid], 0x0)
                        from    LoadItem L
                                inner join LoadItemColumn CS on CS.RowIndex = L.RowIndex and CS.ColumnIndex = @subjectAssetIDFieldIndex and CS.LoadID = @id
                                inner join LoadItemColumn CO on CO.RowIndex = L.RowIndex and CO.ColumnIndex = @objectAssetIDFieldIndex and CO.LoadID = @id
                                left join {(subjectIsReferenceItemType ? "AssetType" : "Asset")} SA on SA.Uid = try_cast(CS.[Value] as uniqueidentifier)
                                left join {(objectIsReferenceItemType ? "AssetType" : "Asset")} OA on OA.Uid = try_cast(CO.[Value] as uniqueidentifier)
                                inner join IntersectType T on T.[uid] = @intersectTypeUid
                                left join [Intersect] I on I.IntersectTypeID = T.ID and I.[Subject] = SA.[Object] and I.SubjectID = SA.ObjectID 
                                    and I.[Object] = OA.[Object] and I.ObjectID = OA.ObjectID
                        where   L.LoadID = @id

                        update  L
                        set     L.Status = 0,
                                L.StatusMessage = 'Relationship could not be found.'
                        from    LoadItem L
                        where   L.LoadID = @id 
                                and (L.IntersectUid = 0x0 or L.IntersectUid is null);

                        update	L
                        set		L.[Status] = 0, 
		                        L.[StatusMessage] = 'This relationship is specified more than once.'
                        from    LoadItem L
                        cross apply (
	                        select IntersectUid from LoadItem where LoadId = @id group by IntersectUid
	                        having count(*) > 1
                        ) D
                        where	D.IntersectUid = L.IntersectUid 
		                        and L.LoadId = @id
		                        and L.Status is null;

                        ", new { id = load.ID, subjectAssetIDFieldIndex, objectAssetIDFieldIndex, intersectTypeUid = intersectType.uid}, commandTimeout: timeout);

                    var results = (await QueryAsync<RelationshipDelete>(@"
                        select      L.ExecutionItemUid, 
                                    cast(0 as bit) as [Cascade], 
                                    L.IntersectUid as [Uid]
                        from    LoadItem L 
                        where   L.LoadID = @id and L.Status is null
                        ", new { id = load.ID, intersectTypeUid = intersectType.uid }, timeout: timeout)).ToList();

                    deletes.AddRange(results);

                    if (deletes.Any())
                    {
                        var fields = new BulkLoadExecutionFields_Relationships
                        {
                            IntersectTypeUid = intersectType.uid,
                            LoadID = load.ID
                        };

                        var execution = getRelateApiExecution(load, deletes.Count);
                        ApiExecutionInfo executionInfo = await relationshipRepository.BulkDeleteRelationships(intersectType.uid, deletes, execution, false);
                        load.PostExecutionID = executionInfo.ExecutionID;
                    }
                }

                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
