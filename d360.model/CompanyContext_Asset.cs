using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.exceptions;
using d360.core.queue;
using d360.core.resources;
using d360.model.DataAccessLayer;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext 
	{
		#region DbSets

		DbSet<AssetDetail> AssetDetails { get; set; }

		DbSet<Asset> Assets { get; set; }

		DbSet<AssetProcessDiagram> AssetProcessDiagrams { get; set; }

		DbSet<AssetTypeExportTemplate> AssetTypeExportTemplates { get; set; }

		DbSet<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }

		DbSet<AssetTypeLevel> AssetTypeLevels { get; set; }

		DbSet<AssetTypeStyle> AssetTypeStyles { get; set; }

		DbSet<AssetType> AssetTypes { get; set; }

		#endregion

		#region Methods

		Task BulkLoadAssets(Load load, IAssetRepository repository, ITagRepository tagRepository);

		Task<AssetsQueryResults> ExecuteGetAssetsQuery(string getAllQuery, CancellationToken cancellationToken, DynamicParameters dbArgs, bool includeTotal, bool includeOwnershipData);

		AssetDetail GetAssetDetail(long id);

		AssetDetail GetAssetDetail(string objectType, long objectId);

		AssetTypeStyle GetAssetTypeStyle(int assetTypeId);

		AssetTypeStyle GetAssetTypeStyle(Guid assetTypeUid);

		AssetTypeStyle GetAssetTypeStyle(string type, int id);

		Guid GetAssetUid(int objectId, SystemObjects assetType);

		List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500, bool sendGraphEvents = true, bool useTempTablesForField = false);

		List<DatabaseBulkAssetResult> RemoveAssets(ApiExecution execution, AssetType at, AssetDeletes import, int timeout = 3600, bool sendWorkflowEvents = true);

		List<DatabaseBulkAssetTypeResult> RemoveAssetTypes(ApiExecution execution, AssetTypeDeletes import, int timeout = 7200, bool stateChangeOnly = true);

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetDetail> AssetDetails { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        public DbSet<FieldApiModel> FieldApiModels { get; set; }

		#endregion

		#region Utility

        private string escapeForSQLLike(string value, bool isContains = true)
        {
            char[] escapeChars = new char[] { '%', '_', '^', '[' };
            string escapedValue = "";

            foreach (char c in value)
            {
                if (escapeChars.Contains(c))
                {
                    escapedValue += $"[{c}]";
                }
                else
                {
                    escapedValue += c;
                }
            }

            return escapedValue;
        }

		private int DeleteAssetsByChunk(ApiExecution execution, int timeout, Dictionary<string, double> metrics, int step, DateTime dt, bool canHaveProcess, Stopwatch sw, PredicateType? predicateType, int beginItemNumber, int endItemNumber, int currentLoop, int retryCount, string querySuffix, SqlTransaction trans)
		{
			#region Delete workflow items

			Connection.Execute($@"
								declare @count bigint = 0;

								create table #w (ItemID int);
								create nonclustered index cix_tempw on #w(ItemID);

								drop table if exists #tempExecutionDeletedAsset;
	
								select S.[Object], S.[ObjectID],s.[AssetID]
								into #tempExecutionDeletedAsset
								from api.ExecutionDeletedAsset S
								where {querySuffix};

								create nonclustered index cix_tempExecutionDeletedAsset on #tempExecutionDeletedAsset([Object], [ObjectID]);

								insert into #w
									select	distinct 
											wi.ID 
									from	workflow.[Type] wt
											inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
											inner join workflow.[Version] wv on wt.id = wv.typeId
											inner join workflow.Item wi on 	wv.id = wi.VersionID
											inner join #tempExecutionDeletedAsset S on S.Object = wi.Object and S.ObjectID = wi.ObjectID;

								insert into #w
									select	wi.id 
									from	workflow.Item wi
											inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
											inner join #tempExecutionDeletedAsset S on S.AssetID = i.AssetID;

								drop table if exists #tempExecutionDeletedAsset;

								select @count = count(1) from #w;

								if(@count > 0)
								begin
									delete	T
									from	[workflow].[ItemAssignment] T
											where exists(select 1 from #w S where S.ItemID = T.ItemID);

									delete  T
									from	[workflow].[ItemStepTransition] T
											inner join workflow.itemstep wis on (wis.ID = T.ToItemStepID or wis.ID = T.FromItemStepID)
											where exists (select 1 from #w S where S.ItemID = wis.ItemID);

									delete  wis
									from workflow.itemstep wis
									where	exists (Select 1 from #w S where S.ItemID = wis.ItemID);
 
									delete  wi
									from [workflow].[Item] wi
									where	exists (Select 1 from #w S where S.ItemID = wi.ID);
								end;", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			addMeasurement(metrics, $"Delete workflow items>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region De-index queue / Audit

			Connection.Execute($@"
								INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
									select	distinct 
											'ObjectIndex', 'D',	S.Object, S.ObjectID, S.AssetID 
									from    api.ExecutionDeletedAsset S
									where   {querySuffix} and S.Object is not null and S.ObjectID is not null;

								insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
									select	distinct
											O.Object, 
											O.ObjectID,
											SUBSTRING(O.DisplayValue,1,250), 
											@r, 
											@dt, 
											'Deleted', 
											O.Object, 
											O.ObjectID, 
											O.TypeName, 
											SUBSTRING(O.DisplayValue,1,250), 
											'This asset has been removed.' 
									from	AssetDetail O
											inner join api.ExecutionDeletedAsset S on S.AssetID = O.ID and {querySuffix} and S.Object is not null and S.ObjectID is not null;",
			new { execution.ExecutionID, r = CurrentResourceID, dt, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			addMeasurement(metrics, $"De-index queue / Audit>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Cross-references

			Connection.Execute($@"
								delete	T
								from	AssetCrossReference T
										inner join api.ExecutionDeletedAsset S on S.[Uid] = T.[Uid] and {querySuffix};",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			addMeasurement(metrics, $"remove from Asset Cross-references>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Process diagram

			if (canHaveProcess)
			{
				Connection.Execute(
				$@"
				drop table if exists #delAssets
				create table #delAssets(
					uid uniqueidentifier,
					ObjectID int,
					AssetID int
				)

				drop table if exists #delRel
				create table #delRel(
					uid uniqueidentifier,
					ID int
				)

				insert into #delAssets
				select fromuid, a.ObjectID, a.id as AssetID from ProcessExpandedData pxd
					inner join asset a on a.uid = pxd.diagramassetuid
				where pxd.diagramassetuid in (select S.Uid from api.ExecutionDeletedAsset S where {querySuffix})
				union 
				select touid, a.ObjectID, a.id as AssetID from ProcessExpandedData pxd
					inner join asset a on a.uid = pxd.diagramassetuid
				where pxd.diagramassetuid in (select S.Uid from api.ExecutionDeletedAsset S where {querySuffix})


				insert into #delRel
				select i.uid,I.Id from #delAssets
					inner join Asset A on A.uid = #delAssets.uid
					inner join [Intersect] I on I.ObjectAssetID = A.ID
				union 
				select i.uid,I.Id from #delAssets
					inner join Asset A on A.uid = #delAssets.uid
					inner join [Intersect] I on I.SubjectAssetID = A.ID

				delete from Field where IntersectID in (select ID from #delRel)

				delete from Field where AssetID in (select AssetId from #delAssets)
				delete from asset where uid in (select uid from #delAssets)

				delete from assetpath where id in (select assetid from #delAssets) 
",
				new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
				addMeasurement(metrics, $"remove process assets>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
				sw.Restart();
			}

			#endregion

			#region Remove default value settings from FieldTypes

			Connection.Execute(
				$@"
				drop table if exists #tempassetobject;

				create table #tempassetobject (id [bigint] IDENTITY(1,1) NOT NULL, Object varchar(50), ObjectID int);

				insert into #tempassetobject (Object, ObjectID)
					select  a.Object,
							a.ObjectID
					from    Asset a
					where   exists (
								select  1
								from    api.ExecutionDeletedAsset S 
								where   s.Uid = A.Uid 
										and {querySuffix}
							);

				create nonclustered index cix_tempassetid on #tempassetobject (Object, ObjectID, id);

				update	T
				set     T.DefaultValue = null
				from	dbo.FieldType T
						inner join #tempassetobject S on S.Object = T.LookupObjectType and S.ObjectID = T.DefaultValue and T.LookupObjectType is not null and T.DefaultValue is not null and T.[Type] = 'Lookup';

				drop table if exists #tempassetobject;",
				new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			#endregion

			#region Delete surveys

			Connection.Execute($@"
				delete q
				from dbo.Question q
					left join dbo.Survey survey
						on q.SurveyID = survey.ID
					left join api.ExecutionDeletedAsset S
						on S.AssetID = survey.AssetID
				where {querySuffix};",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			addMeasurement(metrics, $"remove from surveys >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Asset table

			Connection.Execute(
				$@"
				declare @totalcount bigint = 0,
						@runcount bigint = 0,
						@struncount bigint = 0,
						@enruncount bigint = 0,
						@batchsize int = {SqlBulkIntersectFieldDeleteSize};

				drop table if exists #tempassetid;
				drop table if exists #tempruleresults;

				create table #tempassetid (id [bigint] IDENTITY(1,1) NOT NULL, assetid [bigint]);
				create table #tempruleresults ([Uid] uniqueidentifier);

				insert into #tempassetid (assetid)
					select  a.ID
					from    Asset a
					where   exists (
								select  1
								from    api.ExecutionDeletedAsset S 
								where   s.Uid = A.Uid 
										and {querySuffix}
							);

				create nonclustered index cix_tempassetid on #tempassetid (assetid, id);

				insert into #tempruleresults
					select	R.Uid
					from	dbo.Asset A
							inner Join
							dbo.AssetResult R on A.uid = R.OwningAssetUid
					where	A.Id in (select assetid from #tempassetid);

				create clustered index cix_tempruleresults on #tempruleresults (Uid);

				select @totalcount = count(id) from #tempassetid;
				while (@runcount <= @totalcount)
				begin
					set @struncount = @runcount + 1;
					set @enruncount = @runcount + @batchsize;

					delete  a
					from    Asset a
					where   exists (
								select  1
								from    #tempassetid S
								where   S.assetid = a.ID
										and S.id between @struncount and @enruncount
							);					

					set @runcount = @enruncount;
				end;

				delete	T
				from	dbo.AssetResult T
						inner join #tempruleresults S on S.Uid = T.Uid;

				drop table if exists #tempassetid; 
				drop table if exists #tempruleresults;",
				new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			addMeasurement(metrics, $"remove from asset table>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Delete Intersects

			Connection.Execute($@"
								declare @totalcount bigint = 0,
									@runcount bigint = 0,
									@struncount bigint = 0,
									@enruncount bigint = 0,
									@batchsize int = {SqlBulkIntersectFieldDeleteSize};

									drop table if exists #tempexecdelass;

									select IntersectID, [AssetID]
									into #tempexecdelass
									from api.ExecutionDeletedAsset S
									where {querySuffix};

									create nonclustered index [cix_tempexecdelass] on #tempexecdelass ([AssetID]);
									create nonclustered index [cix_tempexecdelass2] on #tempexecdelass (IntersectID);

									drop table if exists #tempintersect;
									create table #tempintersect(id [bigint] IDENTITY(1,1) NOT NULL, IntersectID int); 

									if(@predicateType = 1)
									begin
										insert into #tempintersect (IntersectID)
										select T.ID
										from [Intersect] T 
										where exists (select 1 from #tempexecdelass S where S.IntersectID = T.ID and S.IntersectID is not null);
									end;

									insert into #tempintersect (IntersectID)
									select T.ID
									from [Intersect] T 
									where exists (select 1 from #tempexecdelass S where S.AssetID = T.SubjectAssetID);

									insert into #tempintersect (IntersectID)
									select T.ID
									from [Intersect] T 
									where exists (select 1 from #tempexecdelass S where S.AssetID = T.ObjectAssetID);

									create nonclustered index [cix_tempintersect] on #tempintersect(IntersectID, id);

									delete T
									from #tempintersect T
									where T.ID > (select min(t1.ID)
										from #tempintersect t1
										where t.IntersectID = t1.IntersectID
										);

									select @totalcount = count(id) from #tempintersect;
									while (@runcount <= @totalcount)
									begin
										set @struncount = @runcount + 1;
										set @enruncount = @runcount + @batchsize;

										delete  T
										from    [Intersect] T
										where   exists (
													select  1
													from    #tempintersect S
													where   S.IntersectID = T.ID
															and S.id between @struncount and @enruncount
												);

										set @runcount = @enruncount;
									end;

									drop table if exists #tempexecdelass;
									drop table if exists #tempintersect;",
			new { execution.ExecutionID, beginItemNumber, endItemNumber, predicateType = predicateType.HasValue ? 1 : 0 }, transaction: trans, commandTimeout: timeout);

			sw.Restart();

			#endregion

			#region Delete Social tables

			Connection.Execute($@"
								delete	T
								from	CommentRelation T
										inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

								delete	T
								from	CommentVote T
										inner join Comment C on C.ID = T.CommentID
										inner join api.ExecutionDeletedAsset S on S.AssetID = C.AssetID and {querySuffix};

								delete	T
								from	Comment T
										inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

								delete	T
								from	Favorite T
										inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

								delete	T
								from	Follow T
										inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			addMeasurement(metrics, $"remove from social tables>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Delete subsidiary tables

			Connection.Execute($@"
								declare @totalcount bigint = 0,
									@runcount bigint = 0,
									@struncount bigint = 0,
									@enruncount bigint = 0,
									@batchsize int = {SqlBulkIntersectFieldDeleteSize};

									drop table if exists #tempfieldid;

									create table #tempfieldid (id [bigint] IDENTITY(1,1) NOT NULL, fieldid [bigint]);

									insert into #tempfieldid (fieldid)
									select T.ID
									from Field T
									where exists (
										select 1
										from api.ExecutionDeletedAsset S where S.AssetID = T.AssetID and {querySuffix}
									);

									create nonclustered index [cix_tempfieldid] on #tempfieldid (fieldid, id);

									select @totalcount = count(id) from #tempfieldid;
									while (@runcount <= @totalcount)
									begin
										set @struncount = @runcount + 1;
										set @enruncount = @runcount + @batchsize;

										delete T
										from Field T
										where exists (
											select 1
											from #tempfieldid S
											where S.FieldID = T.ID
											and S.id between @struncount and @enruncount
										);

										set @runcount = @enruncount;
									end;

									drop table if exists #tempfieldid;",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			addMeasurement(metrics, $"remove from subsidiary tables field>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			Connection.Execute($@"
								delete	T
								from	Issue T
										inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

								delete	T
								from	Nym T
										inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			addMeasurement(metrics, $"remove from subsidiary tables issue/nym>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			#region Delete owner tables

			Connection.Execute($@"
								declare @count bigint = 0;

								drop table if exists #temprestable;
								create table #temprestable (id bigint);
								create nonclustered index cix_temprestable on #temprestable ([ID] asc);

								insert into #temprestable
								select T.ID
								from ResponsibilityTypeRelationOverrideItem T
								where exists (select 1 from api.ExecutionDeletedAsset S where S.AssetID = T.AssetID and {querySuffix});

								select @count = count(1) from #temprestable;

								if(@count > 0)
								begin
									delete	T
									from	ResponsibilityTypeRelationOverrideItem T
											where exists (select 1 from #temprestable S where S.ID = T.ID);
								end;
								drop table if exists #temprestable;

								drop table if exists #temprestable2;
								create table #temprestable2 (RuleID bigint, AssetID bigint);
								create nonclustered index [ix_temprestable2] on #temprestable2 ([RuleID] asc, [AssetID] asc);

								insert into #temprestable2
								select T.RuleID, T.AssetID
								from	ResponsibilityRuleResultAsset T
								where exists (select 1 from api.ExecutionDeletedAsset S where S.AssetID = T.AssetID and {querySuffix});

								select @count = count(1) from #temprestable2;

								if(@count > 0)
								begin
									delete	T
									from	ResponsibilityRuleResultAsset T
											where exists (select 1 from #temprestable2 S where S.RuleID = T.RuleID and S.AssetID = T.AssetID);
								end;
								drop table if exists #temprestable2;",
			new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

			addMeasurement(metrics, $"remove from owner tables>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			#endregion

			return step;
		}

		private void MergeAssetDisplayValues(Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, AssetType at, int timeout = 3600, bool isInsert = false)
		{
			string jointablesql = " ";
			string DisplayValuesql;

			if (at.Class == AssetTypeClass.Reference)
			{
				jointablesql = $@" left join {ApiExecutionFieldTable} C on C.ExecutionID = A.ExecutionID and C.ItemNumber = A.ItemNumber and C.FieldName = 'Code' ";
				DisplayValuesql = $@" cross apply GetAssetDisplayValueById(A.AssetID)ADV ";
			}
			else
			{
				DisplayValuesql = $@" cross apply GetAssetDisplayValueById(A.AssetID)ADV ";
			}

			string fieldsSelectSql = $@"
				select  A.AssetID as ID,
							ADV.DisplayValue,
							CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
							SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
					from    api.ExecutionAsset A
							{jointablesql}
							{DisplayValuesql}
					where   A.ExecutionID = @executionID
							and A.ItemNumber between @beginItemNumber and @endItemNumber 
							and A.Success is null 
							and ADV.DisplayValue is not null";

			if (isInsert)
			{
				Connection.Execute($@"
					insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash,DisplayValuePrefix) 
						{fieldsSelectSql}
				",
				new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber, AssetTypeID = at.ID }, transaction: trans, commandTimeout: timeout);
			}
			else
			{
				Connection.Execute($@"
									merge       AssetDisplayValue as T
									using       (
													{fieldsSelectSql}
												) as S 
									on          ( T.AssetID = S.ID )
									when		matched then
									update		set
													T.DisplayValue = S.DisplayValue,
													T.DisplayValueHash = S.DisplayValueHash,
													T.[DisplayValuePrefix] = S.DisplayValuePrefix,
													T.UpdatedOn = @dt
									when		not matched by target then
									insert		(AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
									values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, @dt);",
				new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber, AssetTypeID = at.ID }, transaction: trans, commandTimeout: timeout);
			}
		}

		private void MergeGroupAssetDisplayValues(Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600, bool isInsert = false)
		{
			string fieldsSelectSql = $@"
										select  A.ID as ID,
													ADV.DisplayValue,
													CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
													SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
											from    api.ExecutionGroup EG
													inner join Asset A on A.Object = 'Group' and A.uid = EG.GroupUid
													inner join [Group] G on G.id = A.ObjectID
													cross apply GetAssetDisplayValueByID(A.ID) ADV
											where   EG.ExecutionID = @executionID
													and EG.ItemNumber between @beginItemNumber and @endItemNumber 
													and EG.Success is null 
													and ADV.DisplayValue is not null";

			if (isInsert)
			{
				Connection.Execute($@"
					insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash,DisplayValuePrefix) 
						{fieldsSelectSql}
				",
				new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			}
			else
			{
				Connection.Execute($@"
									merge       AssetDisplayValue as T
									using       (
													{fieldsSelectSql}
												) as S 
									on          ( T.AssetID = S.ID )
									when		matched then
									update		set
													T.DisplayValue = S.DisplayValue,
													T.DisplayValueHash = S.DisplayValueHash,
													T.[DisplayValuePrefix] = S.DisplayValuePrefix,
													T.UpdatedOn = @dt
									when		not matched by target then
									insert		(AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
									values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, @dt);",
				new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
			}
		}

		private string wildcardValue(string value, bool isContains = true)
        {
            value = value.Replace("*", "%").Replace("?", "_");
            value = isContains ? $"%{value}%" : $"{value}%";
            
            return value;
        }

		#endregion

		#region Methods

        public string DetermineSqlDataTypeForFieldType(FieldType f)
        {
            string sqlDataType = "nvarchar";

            if (f.Type == DataType.JsonElement.ToString())
            {
                FieldTypeDefinition_JsonElement jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
                sqlDataType = jsonElementDefinition.DataType;
                jsonElementDefinition = null;
            }
            else if (f.Type == DataType.Boolean.ToString())
            {
                sqlDataType = "bit";
            }
            else if (f.Type == DataType.Date.ToString())
            {
                sqlDataType = "date";
            }
            else if (f.Type == DataType.DateTime.ToString())
            {
                sqlDataType = "datetime";
            }
            else if (f.Type == DataType.Decimal.ToString())
            {
                sqlDataType = "decimal";
            }
            else if (f.Type == DataType.Number.ToString())
            {
                sqlDataType = "int";
            }

            return sqlDataType;
        }

		public async Task<AssetsQueryResults> ExecuteGetAssetsQuery(string getAllQuery, CancellationToken cancellationToken, DynamicParameters dbArgs, bool includeTotal, bool includeOwnershipData)
		{
			AssetsQueryResults model = new AssetsQueryResults();

			SqlMapper.GridReader gridReader = await Database.Connection.QueryMultipleAsync(
			  new CommandDefinition(getAllQuery,
			  cancellationToken: cancellationToken,
			  parameters: dbArgs,
			  commandTimeout: ApiTimeout
			));

			if (includeTotal)
			{
				model.total = gridReader.Read<int>().FirstOrDefault();
			}

			model.items = gridReader.Read<dynamic>().ToList();

			if (includeOwnershipData)
			{
				model.ownershipData = gridReader.Read<dynamic>().ToList();
			}

			return model;
		}

		public AssetDetail GetAssetDetail(long id)
		{
			AssetDetail model = Query<AssetDetail>(@"
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
													where   ID = @id", new { id }).SingleOrDefault();

			return model;
		}

		public AssetDetail GetAssetDetail(string objectType, long objectId)
		{
			AssetDetail model = Query<AssetDetail>(@"
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
													where   [ObjectID] = @id and [Object] = @type",
													new { id = objectId, type = new DbString { Value = objectType, IsFixedLength = true, Length = 20, IsAnsi = true } }
													).SingleOrDefault();

			return model;
		}

		public Guid GetAssetUid(int objectId, SystemObjects assetType)
		{
			try
			{
				return Assets.FirstOrDefault(x => x.Object == assetType.ToString() && x.ObjectID == objectId).uid;
			}
			catch
			{
				throw new ArgumentNullException(CompanyContextErrors.ObjectNotPartAssetTable);
			}
		}

		public string GetEscapedFilterString(string filter, bool isContains = false)
        {
            return wildcardValue(escapeForSQLLike(filter), isContains);
        }

		public List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500, bool sendGraphEvents = true, bool useTempTableForFields = false)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "ImportAssets";
			bool isLog = true; // trace info for all assets is extermely useful
			List<DatabaseBulkAssetResult> results = new List<DatabaseBulkAssetResult>();
			Dictionary<int, List<string>> importFields = new Dictionary<int, List<string>>();
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			int step = 0;
			bool hasDuplicateUids = false;
			bool enableJsonAttributes = false;
			bool hasCounterField = false;

			try
			{
				enableJsonAttributes = GetSettingValue<bool>(Setting.EnableJsonAttribute);
			}
			catch 
			{
				// Safely ignore. Just assume it is false.
			}

			FieldValidationFieldProperties fieldLoadProperties = new FieldValidationFieldProperties(); // properties of fields in the data load.  Returned from validate fields so we are efficient and dont keep going through the fields.

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			// duplicate items in load checks is only applicable if there is > 1 item
			if (import.Count() > 1)
			{
				Stopwatch sw = Stopwatch.StartNew();

				var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
				if (dupes.Any())
				{
					string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
					results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));

					hasDuplicateUids = true;
				}

				// check for duplicated asset uids if its a put.  
				if (!isInsert)
				{
					var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

					if (uidDupes.Any())
					{
						string message = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
						execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
						results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));

						hasDuplicateUids = true;
					}
				}

				addMeasurement(metrics, "Checks for duplicate uids in load", sw.ElapsedMilliseconds, ++step);

				sw.Restart();
			}

			// Only start processing if the duplication checks have passed
			if (!hasDuplicateUids)
			{
				Stopwatch sw = Stopwatch.StartNew();

				//check if trigger workflows is set to true and there are actually no workflows
				sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(at.ID, null, null, isInsert ? ChangeType.Add : ChangeType.Update);

				addMeasurement(metrics, "Check for workflows", sw.ElapsedMilliseconds, ++step);

				sw.Restart();

				#region Build data tables for bulk load.

				DataTable table = new DataTable();
				table.Columns.Add("ExecutionID", typeof(Guid));
				table.Columns.Add("ItemNumber", typeof(int));
				table.Columns.Add("ExecutionItemUid", typeof(Guid));

				table.Columns.Add("Message", typeof(string));
				table.Columns.Add("Success", typeof(bool));
				table.Columns.Add("Uid", typeof(Guid));
				table.Columns.Add("ParentUid", typeof(Guid));
				table.Columns.Add("ObjectType", typeof(string));
				table.Columns.Add("ObjectTypeID", typeof(int));
				table.Columns.Add("SourceID", typeof(string));

				table.Columns.Add("ParentAssetTypeID", typeof(int));

				table.Columns.Add("IntersectTypeUid", typeof(Guid));
				table.Columns.Add("IntersectTypeID", typeof(int));

				DataTable errorTable = new DataTable();
				errorTable.Columns.Add("ExecutionID", typeof(Guid));
				errorTable.Columns.Add("ItemNumber", typeof(int));
				errorTable.Columns.Add("ExecutionItemUid", typeof(Guid));
				errorTable.Columns.Add("Uid", typeof(Guid));
				errorTable.Columns.Add("Message", typeof(string));

				DataTable fieldTable = new DataTable();

				fieldTable.Columns.Add("ExecutionID", typeof(Guid));
				fieldTable.Columns.Add("ItemNumber", typeof(int));
				fieldTable.Columns.Add("FieldName", typeof(string));
				fieldTable.Columns.Add("FieldValue", typeof(string));
				fieldTable.Columns.Add("FieldTypeID", typeof(int));

				#endregion

				bool generalChecksCompleted = false;
				List<FieldTypeCore> fieldTypes = null;
				List<FieldTypeCore> jsonFieldTypes = null;
				List<string> requiredFieldTypeNames = null;
				PredicateType? predicateType = DeterminePredicateType(at.Object);
				IntersectType it = null;
				int? parentAssetTypeId = null;
				List<Guid> parentIntersectGuids = new List<Guid>();
				Guid? intersectTypeUid = null;
				int? intersectTypeID = null;
				CurrentExecutionLocationModel currentLocation = null;
				bool hasLookupFieldTypes = false;
				bool hasRelationshipFieldTypes = false;
				List<AssetFieldTypeUpdate> fieldTypeUpdates = new List<AssetFieldTypeUpdate>();

				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAsset");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<DatabaseBulkAssetResult>(
								$"select * from api.ExecutionAsset where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID }
							)
						);
					}

					addMeasurement(metrics, "BuildDatatable and initialization", sw.ElapsedMilliseconds, ++step);

					sw.Restart();

					fieldTypes = GetAssetTypeFieldTypesCore(at.Object, at.ObjectID);
					jsonFieldTypes = fieldTypes.Where(f => f.Type == DataType.JSON.ToString()).ToList();
					requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && !f.HasDefaultValue && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
					hasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());
					hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());
					hasCounterField = fieldTypes.Any(x => x.Type == DataType.Counter.ToString());
					addMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					#region Generate data sets

					if (predicateType.HasValue)
					{
						it = Filter<IntersectType>(i => i.ObjectAssetTypeID == at.ID && i.Predicate.Type == predicateType, i => i.Predicate).SingleOrDefault();
						if (it != null)
						{
							parentAssetTypeId = it.SubjectAssetTypeID;
							intersectTypeUid = it.uid;
							intersectTypeID = it.ID;
						}
					}
					addMeasurement(metrics, "Get predicateType.HasValue", sw.ElapsedMilliseconds, ++step);
					sw.Restart();
					int i = 1;

					foreach (IAssetUpsert model in import)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							List<DataRow> fieldRows = ValidateFields(at.Object, at.ObjectID, isInsert, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out bool success, out string errorMessage, validationFieldProperties: fieldLoadProperties, jsonElementsEnabled: enableJsonAttributes, IslookupFieldsPassedByValue: lookupFieldsPassedByValue);

							if (success && isInsert && parentAssetTypeId.HasValue && predicateType == PredicateType.InterTypeHierarchy)
							{
								// Check to ensure ParentUid is present.
								success = model.ParentUid.HasValue;

								if (!success)
								{
									errorMessage = "Asset is missing a required ParentUid value";
								}
							}

							if (success && isInsert && at.Object == "ReferenceItemType")
							{
								// Check to ensure Code is present.
								success = model.Fields.ContainsKey("Code");

								if (!success)
								{
									errorMessage = "Asset is missing a required Code field value";
								}
							}

							if (success)
							{
								importFields.Add(i, model.Fields.Keys.ToList());
								fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

								DataRow row = table.NewRow();

								row["ExecutionID"] = execution.ExecutionID;
								row["ItemNumber"] = i;

								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								if (model.Uid != Guid.Empty)
								{
									row["Uid"] = model.Uid;
								}

								if (!string.IsNullOrEmpty(model.SourceID) && !string.IsNullOrWhiteSpace(model.SourceID))
								{
									if (model.SourceID.Length > 500)
									{
										row["SourceID"] = model.SourceID.Substring(0, 500);
									}
									else
									{
										row["SourceID"] = model.SourceID;
									}
								}

								if (model.ParentUid.HasValue)
								{
									row["ParentUid"] = model.ParentUid;
								}

								row["ObjectType"] = at.Object;
								row["ObjectTypeID"] = at.ObjectID;

								if (parentAssetTypeId.HasValue)
								{
									row["ParentAssetTypeID"] = parentAssetTypeId.Value;
								}

								if (intersectTypeUid.HasValue)
								{
									row["IntersectTypeUid"] = intersectTypeUid.Value;
								}

								if (intersectTypeID.HasValue)
								{
									row["IntersectTypeID"] = intersectTypeID.Value;
								}

								table.Rows.Add(row);
							}
							else
							{
								DataRow errorRow = errorTable.NewRow();
								errorRow["ExecutionID"] = execution.ExecutionID;
								errorRow["ItemNumber"] = i;

								if (model.ExecutionItemUid.HasValue)
								{
									errorRow["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								errorRow["Uid"] = model.Uid;
								errorRow["Message"] = errorMessage;

								errorTable.Rows.Add(errorRow);

								results.Add(new DatabaseBulkAssetResult { IsNew = false, ItemNumber = i, ExecutionItemUid = model.ExecutionItemUid, Message = errorMessage, Success = false });
							}
						}

						i++;
					}

					addMeasurement(metrics, "ValidateFields", sw.ElapsedMilliseconds, ++step);

					sw.Restart();

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					#region Bulk Copy


					using (SqlTransaction transaction = Connection.BeginTransaction())
					{
						try
						{
							// if needed create temp tables for data
							CreateWorkareaTempTables(useTempTableForFields, transaction);

							addMeasurement(metrics, "Create work area temp tables", sw.ElapsedMilliseconds, ++step);

							sw.Restart();

							using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction))
							{
								// assets
								bulkCopy.BatchSize = SqlBulkBatchSize;
								bulkCopy.DestinationTableName = "api.ExecutionAsset";
								bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

								bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
								bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
								bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
								bulkCopy.ColumnMappings.Add("Uid", "Uid");
								bulkCopy.ColumnMappings.Add("ObjectType", "ObjectType");
								bulkCopy.ColumnMappings.Add("ObjectTypeID", "ObjectTypeID");
								bulkCopy.ColumnMappings.Add("SourceID", "SourceID");

								bulkCopy.ColumnMappings.Add("ParentUid", "ParentUid");
								bulkCopy.ColumnMappings.Add("ParentAssetTypeID", "ParentAssetTypeID");

								bulkCopy.ColumnMappings.Add("IntersectTypeUid", "IntersectTypeUid");
								bulkCopy.ColumnMappings.Add("IntersectTypeID", "IntersectTypeID");

								bulkCopy.WriteToServer(table);
							}

							if (errorTable.Rows.Count > 0)
							{
								using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction))
								{
									// asset errors
									bulkCopy.BatchSize = SqlBulkBatchSize;
									bulkCopy.DestinationTableName = "api.ExecutionAssetError";
									bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

									bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
									bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
									bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
									bulkCopy.ColumnMappings.Add("Uid", "Uid");
									bulkCopy.ColumnMappings.Add("Message", "Message");

									bulkCopy.WriteToServer(errorTable);
								}
							}

							using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, (useTempTableForFields ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default), transaction))
							{
								// fields
								bulkCopy.BatchSize = SqlBulkBatchSize;
								bulkCopy.DestinationTableName = ApiExecutionFieldTable;
								bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

								bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
								bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
								bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
								bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
								bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

								bulkCopy.WriteToServer(fieldTable);
							}

							transaction.Commit();

							addMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
						}

						catch (Exception)
						{
							if (transaction != null)
							{
								transaction.Rollback();
							}

							throw;
						}
					}

					sw.Restart();

					#endregion

					if (fieldLoadProperties.ContainsColorField)
					{
						addMeasurement(metrics, "ResolveColorValues-Begin", 0, ++step);
						ResolveColorValues(execution.ExecutionID, timeout);
						addMeasurement(metrics, "ResolveColorValues", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					if (hasLookupFieldTypes)
					{
						if (lookupFieldsPassedByValue)
						{
							addMeasurement(metrics, "CopyFieldLookupValuesAsIs-Begin", 0, ++step);
							CopyFieldLookupValuesAsIs(execution.ExecutionID, timeout, ApiExecutionFieldTable);
							addMeasurement(metrics, "CopyFieldLookupValuesAsIs", sw.ElapsedMilliseconds, ++step);
						}
						else
						{
							addMeasurement(metrics, "ResolveFieldLookupValues-Begin", 0, ++step);
							ResolveFieldLookupValues(execution.ExecutionID, ApiExecutionFieldTable, timeout);
							addMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
						}
						sw.Restart();

						addMeasurement(metrics, "LogFieldLookupErrors-Begin", 0, ++step);
						LogFieldLookupErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", lookupFieldsPassedByValue, timeout);
						addMeasurement(metrics, "LogFieldLookupErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					if (hasRelationshipFieldTypes)
					{
						addMeasurement(metrics, "LogRelationshipErrors-Begin", 0, ++step);
						LogRelationshipErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout, lookupFieldsPassedByValue);
						addMeasurement(metrics, "LogRelationshipErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					if (hasCounterField)
					{
						addMeasurement(metrics, "LogCounterFieldErrors-Begin", 0, ++step);
						LogCounterFieldErrors(execution.ExecutionID, timeout);
						addMeasurement(metrics, "LogCounterFieldErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					ValidateAssetAndParent(execution.ExecutionID, at.ID, timeout);
					addMeasurement(metrics, "ValidateAssetAndParent", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					// If you cannot find parent based on Uids provided.
					// special case is intratype hierarchy if guid.empty we need to allow this so we later know which items to remove the relationships from
					LogParentErrors(execution.ExecutionID, timeout, predicateType == PredicateType.IntraTypeHierarchy);
					addMeasurement(metrics, "LogParentErrors", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					if (!isInsert)
					{
						addMeasurement(metrics, "LogAssetErrors / LoadMissingKeyFields/ LogNullIsRequiredFields - Begin", 0, ++step);

						LogAssetErrors(execution.ExecutionID, timeout);             // If you cannot find asset based on Uids provided.
						LoadMissingKeyFields(execution.ExecutionID, at, timeout);   // Get missing key fields if this is an update.
						LogNullIsRequiredFields(execution.ExecutionID, timeout);    // Get IsRequired Field having Null value if this is an update.

						addMeasurement(metrics, "LogAssetErrors / LoadMissingKeyFields/ LogNullIsRequiredFields", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					//Policy/Model Check maximum hierarchy maximum level allowed 

					if (at.Class == AssetTypeClass.Policy || at.Class == AssetTypeClass.Model)
					{
						LogPolicyHierMaxLimitErrors(execution.ExecutionID, isInsert, intersectTypeID, at.HierarchyMaximumDepth, timeout);
					}

					addMeasurement(metrics, "Log Errors", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					#region Invalidate repetitious items in load

					// dont be a tool and look for duplicates in a load of 1 item
					if (execution.Total > 1)
					{

						Connection.Execute($@"
											update	T
											set		T.Success = 0,
													T.[Message] = coalesce(T.[Message] + '; ', '') + 'This asset is specified more than once based on the key fields defined on the asset type. Each asset must be unique within a given request.'
											from	api.ExecutionAsset T
													inner join	(
															select	min(ItemNumber) as ItemNumber,
																	ProposedKey
															from	api.ExecutionAsset
															where   ExecutionID = @ExecutionID
															group by ProposedKey
															) S on T.ExecutionID = @ExecutionID and S.ProposedKey = T.ProposedKey and S.ItemNumber < T.ItemNumber;

											update	T
											set		T.Success = 0,
													T.[Message] = coalesce(T.[Message] + '; ', '') + 'This asset is specified more than once based on the SourceID. Each asset must be unique within a given request.'
											from	api.ExecutionAsset T
													inner join	(
															select	min(ItemNumber) as ItemNumber,
																	SourceID
															from	api.ExecutionAsset
															where   ExecutionID = @ExecutionID 
																	and SourceID is not null
															group by SourceID
															) S on T.ExecutionID = @ExecutionID and S.SourceID = T.SourceID and S.ItemNumber < T.ItemNumber and T.SourceID is not null;",
						new { execution.ExecutionID }, commandTimeout: timeout);

						addMeasurement(metrics, "Invalidate repetitious items in load", sw.ElapsedMilliseconds, ++step);
					}

					sw.Restart();

					#endregion

					// Validate permissions
					LogAssetPermissionErrors(execution.ExecutionID, at, isInsert ? Permission.AddAsset : Permission.EditAsset, "ExecutionAsset");
					LogAssetPermissionErrors(execution.ExecutionID, at, isInsert ? Permission.AddAsset : Permission.EditAsset, isInsert, "ExecutionAsset");
					addMeasurement(metrics, "LogAssetPermissionErrors -  Permission.ModifyAsset- ExecutionAsset", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<DatabaseBulkAssetResult>();
					results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
				}
				sw.Restart();

				if (generalChecksCompleted)
				{
					int loopSize = mergeBlockSize;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							#region common sql

							string executionAssetWhereSql = $"ExecutionID = @ExecutionID and Success is null and ItemNumber between @beginItemNumber and @endItemNumber";
							string updateAssetInfoOnExecutionRecordsSql = $@"update  T
																			set     T.AssetID = S.ID, T.Uid = S.Uid
																			from    api.ExecutionAsset T
																					inner join Asset S on T.Executionid = @ExecutionID and S.AssetTypeID = @AssetTypeID and S.Object = T.Object and S.ObjectID = T.ObjectID and T.ItemNumber between @beginItemNumber and @endItemNumber;";

							#endregion

							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{
									if (at.Class == AssetTypeClass.Reference)
									{
										sw.Restart();
										if (isInsert)
										{
											Connection.Execute($@"
																create table #ObjectMergeTableResult (ID bigint, ObjectID int, ItemNumber int, [Operation] varchar(10));
																CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

																merge   [Asset] as T
																using   (
																		select  A.ItemNumber,
																				A.Uid,
																				A.SourceID,
																				C.FieldValue as [Code],
																				CR.LookupValue as [Color],
																				I.FieldValue as [Icon]
																		from    api.ExecutionAsset A
																				inner join {ApiExecutionFieldTable} C on C.ExecutionID = A.ExecutionID and C.ItemNumber = A.ItemNumber and C.FieldName = 'Code' 
																				left join {ApiExecutionFieldTable} CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
																				left join {ApiExecutionFieldTable} I on I.ExecutionID = A.ExecutionID and I.ItemNumber = A.ItemNumber and I.FieldName = 'Icon' 
																		where   A.ExecutionID = @ExecutionID
																				and A.Success is null
																				and A.ItemNumber between @beginItemNumber and @endItemNumber
																		) S
																on      (1 = 0)
																when    not matched then
																insert  (Uid, AssetTypeID,State,SourceID,[Object], [Code], [Color], [Icon], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
																values  (isnull(S.Uid,newid()), @AssetTypeID,1,S.SourceID,'ReferenceItem', S.[Code], S.[Color], S.[Icon], @R, @D, @R, @D)
																output  inserted.ID, inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

																update  T
																set     T.AssetID = S.ID,
																		T.Object = 'ReferenceItem',
																		T.ObjectID = S.ObjectID,
																		T.IsNew = 1
																from    api.ExecutionAsset T
																		inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

																{updateAssetInfoOnExecutionRecordsSql}",
											new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, at.ObjectID, AssetTypeID = at.ID }, transaction: trans, commandTimeout: timeout);
											addMeasurement(metrics, $"AssetTypeClass.Reference >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										}
										else
										{
											Connection.Execute($@"
																update	T
																set		T.[Code] = C.FieldValue,
																		T.[Color] = case when CR.ExecutionID is not null then CR.LookupValue else T.Color end,
																		T.[Icon] = I.FieldValue,
																		T.SourceID = coalesce(S.SourceID,T.SourceID),
																		T.UpdatedBy = @R,
																		T.UpdatedOn = @D
																from	Asset T
																		inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and S.[Object]=T.[Object] and T.[Object]='ReferenceItem'  and S.ExecutionID = @ExecutionID and S.Success is null and S.ItemNumber between @beginItemNumber and @endItemNumber
																		inner join {ApiExecutionFieldTable} C on C.ExecutionID = S.ExecutionID and C.ItemNumber = S.ItemNumber and C.FieldName = 'Code'
																		left join {ApiExecutionFieldTable} CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 
																		left join {ApiExecutionFieldTable} I on I.ExecutionID = S.ExecutionID and I.ItemNumber = S.ItemNumber and I.FieldName = 'Icon';

																update	api.ExecutionAsset
																set		IsNew = 0
																where	{executionAssetWhereSql};",
											new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
											addMeasurement(metrics, $"AssetTypeClass.Reference >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										}
									}
									else 
									{
										string @object = "Artifact";

										if (at.Class == AssetTypeClass.Policy)
										{
											@object = "Policy";
										}

										if (at.Class == AssetTypeClass.Rule)
										{
											@object = "Rule";
										}

										if (at.Class == AssetTypeClass.Diagram)
										{
											@object = "Task";
										}

										if (at.Class == AssetTypeClass.Model)
										{
											@object = "Taxonomy";
										}

										sw.Restart();
										if (isInsert)
										{
											Connection.Execute($@"
																	create table #ObjectMergeTableResult (ID bigint, ObjectID int, ItemNumber int, [Operation] varchar(10));
																	CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

																	drop table if exists #tempAssetData;

																	select  A.ItemNumber,
																			CR.LookupValue as Color,
																			isnull(A.Uid,newid()) Uid,
																			A.SourceID
																	into    #tempAssetData
																	from    api.ExecutionAsset A
																			left join {ApiExecutionFieldTable} CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
																	where   A.ExecutionID = @ExecutionID
																			and A.Success is null
																			and A.ItemNumber between @beginItemNumber and @endItemNumber

																	merge   [Asset] as T
																	using   (
																			select  A.ItemNumber,
																					A.Color,
																					A.Uid,
																					A.SourceID
																			from    #tempAssetData A
																			) S
																	on      1 = 0
																	when    not matched then
																	insert  (Uid,AssetTypeID,State,SourceID,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Color)
																	values  (S.Uid,@AssetTypeID,1,S.SourceID,@Object, @R, @D, @R, @D, S.Color)
																	output  inserted.ID, inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

																	update  T
																	set     T.AssetID = S.ID,
																			T.Object = @Object,
																			T.ObjectID = S.ObjectID,
																			T.IsNew = 1
																	from    api.ExecutionAsset T
																			inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

																	{updateAssetInfoOnExecutionRecordsSql}",
												new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, R = CurrentResourceID, D = DateTime.UtcNow, @object = new DbString { Value = @object, Length = 50, IsAnsi = true } }, transaction: trans, commandTimeout: timeout);
											addMeasurement(metrics, $"AssetTypeClass.{@object} >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										}
										else
										{
											Connection.Execute($@"
																	update	T
																	set		T.UpdatedBy = @R,
																			T.UpdatedOn = @D,
																			T.SourceID = coalesce(S.SourceID,T.SourceID),
																			T.Color = case when CR.ExecutionID is not null then CR.LookupValue else T.Color end
																	from	[Asset] T
																			inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and T.[Object] = @Object and {executionAssetWhereSql}
																			left join {ApiExecutionFieldTable} CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 


																	update	api.ExecutionAsset
																	set		IsNew = 0
																	where	{executionAssetWhereSql};",
										new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, @object = new DbString { Value = @object, Length = 50, IsAnsi = true }, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
											addMeasurement(metrics, $"AssetTypeClass.Policy - BusinessAsset >> TechnicalAsset >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										}
									}

									#region Parent/Child Relationship

									sw.Restart();
									if (intersectTypeID.HasValue)
									{
										parentIntersectGuids = Connection.Query<Guid>(@"
																						drop table if exists #ParentChildRelationships;
																						create table #ParentChildRelationships([operation] varchar(10), [uid] uniqueidentifier, ItemNumber int);
																						create index idx_parentchildrelationships on #ParentChildRelationships([uid]);

																						-- Log the parent removals into Dependent Change table
																						insert into api.ExecutionItemDependentChange (ExecutionID, ItemNumber, DependentChangeType, [Action], Payload)
																							select  EA.ExecutionID, EA.ItemNumber, 1, 2, '{ ""ParentAssetUid"": ""' + cast(P.Uid as varchar(50)) + '""}' 
																							from    api.ExecutionAsset EA 
																									inner join [Intersect] I on I.IntersectTypeID = EA.IntersectTypeID and EA.AssetId = I.ObjectAssetId
																									inner join Asset P on P.Id = I.SubjectAssetId
																							where    EA.ExecutionID = @ExecutionID 
																									and Success is null 
																									and EA.ItemNumber between @beginItemNumber and @endItemNumber
																									and EA.IntersectTypeID is not null
																									and EA.ParentAssetID is not null 
																									and EA.AssetID is not null 
																									and EA.ParentAssetID <> I.SubjectAssetID;

																						drop table if exists #tempintersectdata;
	
																						select  t.*,
																								P.AssetTypeID as SubjectAssetTypeID,
																								C.AssetTypeID as ObjectAssetTypeID
																						into	#tempintersectdata 
																						from    api.ExecutionAsset t
																								inner join Asset C on C.ID = t.AssetID 
																								inner join Asset P on P.ID = t.ParentAssetID 
																						where   ExecutionID = @ExecutionID 
																								and Success is null 
																								and ItemNumber between @beginItemNumber and @endItemNumber
																								and IntersectTypeID is not null	
																								and ParentAssetID is not null 
																								and AssetID is not null;

																						merge       [Intersect] as T
																						using		(
																									select  * 
																									from    #tempintersectdata 
																									) as S
																						on          ( T.IntersectTypeID = S.IntersectTypeID and S.AssetID = T.ObjectAssetID )
																						when	matched and (T.SubjectAssetID <> S.ParentAssetID) then
																							update 
																							set     T.SubjectAssetTypeID = S.SubjectAssetTypeID,
																									T.SubjectAssetID = S.ParentAssetID,
																									T.UpdatedBy = @R
																						when not matched by target then
																							insert  (IntersectTypeID, SubjectAssetTypeID, SubjectAssetID, ObjectAssetTypeID, ObjectAssetID, CreatedBy, UpdatedBy)
																							values  (S.IntersectTypeID, S.SubjectAssetTypeID, S.ParentAssetID, S.ObjectAssetTypeID, S.AssetID, @R, @R)
																						output $action, inserted.[uid], S.ItemNumber into #ParentChildRelationships;

																						-- Log the parent removals into Dependent Change table
																						insert into api.ExecutionItemDependentChange (ExecutionID, ItemNumber, DependentChangeType, [Action], Payload)
																							select  @ExecutionID, A.ItemNumber, 1, 1, '{ ""ParentAssetUid"": ""' + cast(P.Uid as varchar(50)) + '""}' 
																							from    #ParentChildRelationships A
																									inner join [Intersect] I on I.Uid = A.Uid
																									inner join Asset P on P.ID = I.SubjectAssetID;

																						select [uid] from #ParentChildRelationships",
											new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout)
											.ToList();
										addMeasurement(metrics, $"Parent/Child Relationship >> {currentLoop}", sw.ElapsedMilliseconds, ++step);

										// if its an intra type hierarchy models or policies and NOT an insert its possible that parent child relations are being removed IE an item moved to root
										if (predicateType == PredicateType.IntraTypeHierarchy && !isInsert)
										{
											sw.Restart();

											Connection.Execute(@"
																drop table if exists #DeletedRelationships;
																create table #DeletedRelationships ([ID] int, ItemNumber int, Payload varchar(200));

																insert into #DeletedRelationships
																	select I.ID,
																	EA.ItemNumber,
																	'{ ""ParentAssetUid"": ""' + cast(Parent.Uid as varchar(50)) + '""}'
																	from api.ExecutionAsset EA
																	inner join [Intersect] I on I.IntersectTypeID = EA.IntersectTypeID and I.ObjectAssetID = EA.AssetID
																	inner join Asset Parent on Parent.ID = I.SubjectAssetID
																	WHERE EA.ExecutionID = @ExecutionID 
																					and EA.ParentUid = '00000000-0000-0000-0000-000000000000'
																					and EA.Success is null 
																					and EA.ItemNumber between @beginItemNumber and @endItemNumber 
																					and EA.IntersectTypeID is not null;

																-- Log the parent removals into Dependent Change table
																insert into api.ExecutionItemDependentChange (ExecutionID, ItemNumber, DependentChangeType, [Action], Payload)
																	select distinct @ExecutionID, ItemNumber, 1, 3, Payload
																	from    #DeletedRelationships dr
																	where not exists (
																		select 1 from api.ExecutionItemDependentChange dc
																		where dc.ExecutionID = @ExecutionID
																		and dc.ItemNumber = dr.ItemNumber
																		and dc.DependentChangeType = 1
																		and dc.[Action] = 3
																	);

																delete  i
																from    [intersect] i 
																		inner join #DeletedRelationships d on d.ID = i.ID;",
																new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);

											addMeasurement(metrics, $"Parent/Child Delete Relationship >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										}
									}

									#endregion

									bool shouldRunMergeAssetPath = true;
									if (!isInsert)
									{
										addMeasurement(metrics, $"CheckIfKeyFieldsUpdated >> {currentLoop} > Begin", 0, ++step);

										string codeFieldCheck = string.Empty;
										if (at.Class == AssetTypeClass.Reference)
										{
											codeFieldCheck = @$"
												if @updatedFieldsCount = 0
												begin
													set @updatedFieldsCount = (select count(*) from {ApiExecutionFieldTable} EF
													inner join api.ExecutionAsset EA on EA.ItemNumber = EF.ItemNumber AND EA.ExecutionID = EF.ExecutionID
													WHERE EF.ExecutionID = @ExecutionID AND EF.ItemNumber between @beginItemNumber and @endItemNumber
													and EF.FieldName = 'Code')
												end";
										}

										var checkUpdatedKeyFields = $@"
											declare @result int = 0;

											declare @updatedFieldsCount int = (select count(*) from {ApiExecutionFieldTable} EF
											inner join api.ExecutionAsset EA on EA.ItemNumber = EF.ItemNumber AND EA.ExecutionID = EF.ExecutionID
											inner join FieldType FT ON FT.ID = EF.FieldTypeID AND FT.IsPartOfKey = 1
											left join Field F on F.AssetId = EA.AssetId and F.FieldTypeID = FT.ID
											WHERE EF.ExecutionID = @ExecutionID AND EF.ItemNumber between @beginItemNumber and @endItemNumber
											    and (F.FormattedValue <> EF.FieldValue COLLATE SQL_Latin1_General_CP1_CS_AS or (F.FormattedValue is null and EF.FieldValue is not null)))

											declare @updatedHierarcyRelationships int = (
																		select COUNT(*) from api.ExecutionItemDependentChange EIDC
																		where EIDC.ExecutionID = @ExecutionID AND EIDC.ItemNumber between @beginItemNumber and @endItemNumber)

											{codeFieldCheck}

											if @updatedFieldsCount > 0 or @updatedHierarcyRelationships > 0
											begin
												set @result = 1;
											end

											select @result;";

										var result = Connection.QueryFirst<int>(checkUpdatedKeyFields,
											new { executionID = execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, timeout);

										shouldRunMergeAssetPath = result > 0;

										addMeasurement(metrics, $"CheckIfKeyFieldsUpdated >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}

									sw.Restart();
									List<AssetFieldTypeUpdate> transationFieldUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionAsset", SystemObjects.Artifact, "A.AssetID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout, isInsert, hasLookupFieldTypes);
									addMeasurement(metrics, $"MergeFields >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
									sw.Restart();

									if (hasCounterField)
									{
										addMeasurement(metrics, $"UpdateCounteFields >> {currentLoop} > Begin", 0, ++step);
										UpdateCounterFields(at.ID, execution.ExecutionID, trans, beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
										addMeasurement(metrics, $"UpdateCounteFields >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}

									if (hasRelationshipFieldTypes)
									{
										addMeasurement(metrics, $"ImportRelationships >> {currentLoop} > Begin", 0, ++step);
										ImportRelationships(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, lookupFieldsPassedByValue);
										addMeasurement(metrics, $"ImportRelationships >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
									}

									// only populate json properties IF there are 1 json fields on the asset type, AND values have been specified for JSON fields IE if they didnt provide any optional json fields disregard.
									// Only save all properties to the database if we json attributes enabled
									if (enableJsonAttributes && jsonFieldTypes.Count > 0 && fieldLoadProperties.JsonFieldCount > 0)
									{
										sw.Restart();
										addMeasurement(metrics, $"MergeJsonFieldProperties >> {currentLoop} > Begin", 0, ++step);

										MergeJsonFieldProperties(execution.ExecutionID, trans, jsonFieldTypes, SystemObjects.Artifact, "api.ExecutionAsset", "A.AssetID", beginItemNumber, endItemNumber, timeout, metrics, step, isInsert);
										addMeasurement(metrics, $"MergeJsonFieldProperties >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
									}

									if (shouldRunMergeAssetPath)
									{
										//call new procedure.
										addMeasurement(metrics, $"MergeAssetPaths >> {currentLoop} > Begin", 0, ++step);

										Connection.Execute(
											"exec api.MergeAssetPaths @executionId, @class, @begin, @end, null, @isInsert",
											new { executionID = execution.ExecutionID, @class = (int)at.Class, begin = beginItemNumber, end = endItemNumber, isInsert },
											transaction: trans, timeout);
										addMeasurement(metrics, "MergeAssetPaths", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}
									else
									{
										addMeasurement(metrics, $"MergeAssetPaths >> {currentLoop} > Skipped", 0, ++step);
									}


									// Must execute BEFORE the Success flag is updated below.
									sw.Restart();
									MergeAssetDisplayValues(execution.ExecutionID, trans, beginItemNumber, endItemNumber, at, timeout, isInsert);
									addMeasurement(metrics, $"MergeAssetDisplayValues >> {currentLoop}", sw.ElapsedMilliseconds, ++step);

									//Delete all field without value ONLY do this if there are lookup fields AND this is an update.
									if (hasLookupFieldTypes && !isInsert)
									{
										sw.Restart();
										addMeasurement(metrics, $"DeleteEmptyAssetListFieldByApiExecutionUid >> {currentLoop} > Begin", 0, ++step);

										DeleteEmptyAssetListFieldByApiExecutionUid(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout);
										addMeasurement(metrics, $"DeleteEmptyAssetListFieldByApiExecutionUid >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
									}

									addMeasurement(metrics, $"CheckKeyHashes >> {currentLoop} > Begin", sw.ElapsedMilliseconds, ++step);
									#region Generate proposed key hash and compare against existing data.
									var invalidHashState = Connection.Query<dynamic>(@"
										declare @assetTypeId int =  (select top 1 a.AssetTypeID from api.ExecutionAsset ea
										inner join Asset a on a.ID = ea.AssetID
										where ExecutionId = @executionid and ea.AssetID is not null and  ItemNumber between @beginItemNumber and @endItemNumber )

										drop table if exists #HashData
										select AssetID, Ap.KeyPathHash, A.CreatedOn, ea.ItemNumber
											into #HashData
										from api.ExecutionAsset ea
											inner join Asset A on a.ID = ea.AssetID
											inner join AssetPath AP on AP.ID = A.ID
										where ea.ExecutionID = @executionid 
										and ItemNumber between @beginItemNumber and @endItemNumber 

										select hd.ItemNumber, a.id as AssetId, datediff(second,a.CreatedOn, hd.CreatedOn) as CreatedBefore from Asset A WITH (NOLOCK)
										inner join AssetPath ap WITH (NOLOCK) on ap.ID = a.ID
										inner join #HashData hd on hd.assetid != a.ID and hd.KeyPathHash = ap.KeyPathHash
										where a.AssetTypeID = @assetTypeId
										option(recompile)
										", new { executionID = execution.ExecutionID, beginItemNumber, endItemNumber },
										transaction: trans, commandTimeout: timeout).ToList();

									addMeasurement(metrics, $"CheckKeyHashes >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
									sw.Restart();

									if (invalidHashState.Count > 0)
									{
										string duplicates = string.Join(",", invalidHashState.Select(x => $"[ItemNumber:{x.ItemNumber},ID:{x.AssetId},CreatedBefore:{x.CreatedBefore}s]"));
										throw new DuplicateHashException("Key values match another asset under a different set of key fields or 2 or more concurrent requests contains same key field values." + duplicates);
									}

									#endregion

									sw.Restart();
									// Update success flag.
									Connection.Execute(
										$@"update api.ExecutionAsset set Success = 1 where {executionAssetWhereSql} and Object is not null and ObjectID is not null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
									metrics.Add($"{++step} Update success flag", sw.ElapsedMilliseconds);
									trans.Commit();
									addMeasurement(metrics, "Commit Loop of data", sw.ElapsedMilliseconds, ++step);
									sw.Restart();

									//Add items after commit, so we dont have dirty data if trans is rolled back
									if (transationFieldUpdates != null && transationFieldUpdates.Count > 0)
									{
										fieldTypeUpdates.AddRange(transationFieldUpdates);
									}

									runCompleted = true;
								}
								catch (Exception ex)
								{
									if (ex is DuplicateHashException)
									{
										retryCount = API_V2_RETRY_LIMIT;
									}

									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
										// If rollback fails, do not mess up the transaction. Just continue with looping.
									}
									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAsset", ex.GetFullExceptionData(false), timeout);
										addMeasurement(metrics, "LogLoopExecutionError", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}
									else
									{
										Thread.Sleep(API_V2_RETRY_INTERVAL);
									}
								}
							}
						}

						sw.Restart();
						results.AddRange(
							Query<DatabaseBulkAssetResult>(
								$"select * from api.ExecutionAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
								new { execution.ExecutionID, beginItemNumber, endItemNumber }
							)
						);
						addMeasurement(metrics, $"results.AddRange >> DatabaseBulkAssetResult", sw.ElapsedMilliseconds, ++step);
						beginItemNumber += loopSize;
						endItemNumber += loopSize;

						addMeasurement(metrics, "End of batch loop", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
					}

					Connection.Close();

					if (sendWorkflowEvents)
					{
						sw.Restart();
						addMeasurement(metrics, $"SendWorkflowEvents > Begin", 0, ++step);
						SendWorkflowEvents(at.Object, at.ObjectID, results, null, fieldTypeUpdates);
						addMeasurement(metrics, $"SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);
					}

					try
					{
						CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionAsset");
						SendBatchApiCompletedEvent(execution);

						addMeasurement(metrics, $"SendCompletedEvent", sw.ElapsedMilliseconds, ++step);
					}
					catch
					{
						// Should continue on here, and not fail the enire process.
					}

					#region Send score recalculation notifications.

					if (intersectTypeID.HasValue)
					{
						sw.Restart();
						addMeasurement(metrics, $"CreateParentAssetGovernanceRescoreExecution > Begin", 0, ++step);
						CreateParentAssetGovernanceRescoreExecution(execution.ExecutionID);
						addMeasurement(metrics, $"CreateParentAssetGovernanceRescoreExecution", 0, ++step);
					}

					if (Any<MetricAllocation>(i => i.AssetTypeUid == at.uid && i.ScoreType == ScoreType.Governance && !i.IsExternallyCalculated))
					{
						sw.Restart();
						addMeasurement(metrics, $"SendScoreEventWithPayload > Begin", 0, ++step);
						CreateImportAssetsExecution(execution.ExecutionID, at.uid);
						addMeasurement(metrics, $"SendScoreEventWithPayload", sw.ElapsedMilliseconds, ++step);
					}

					#endregion
				}
			}

			addMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			return results;
		}

		public List<DatabaseBulkAssetResult> RemoveAssets(ApiExecution execution, AssetType at, AssetDeletes import, int timeout = 3600, bool sendWorkflowEvents = true)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "RemoveAssets";
			bool isLog = true; // trace info for all assets is extermely useful
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			int step = 0;
			List<DatabaseBulkAssetResult> results = new List<DatabaseBulkAssetResult>();
			DateTime dt = DateTime.UtcNow;
			bool generalChecksCompleted = false;

			bool canHaveProcess = TypeHasProcessRelationshipTypes(at);

			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			//check if trigger workflows is set to true and there are actually no workflows in which case shut off triggering of workflows
			Stopwatch sw = Stopwatch.StartNew();
			sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(at.ID, null, null, ChangeType.Delete);

			addMeasurement(metrics, "Check for workflows", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			addMeasurement(metrics, "Checking for duplicate execution uids", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{

				var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

				addMeasurement(metrics, "Checking for duplicate asset uids", sw.ElapsedMilliseconds, ++step);
				sw.Restart();

				if (uidDupes.Any())
				{
					string message = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
					results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
				}
				else
				{
					try
					{
						currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeletedAsset");

						addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						if (currentLocation.HighestItemNumberProcessed > 0)
						{
							results.AddRange(
								Query<DatabaseBulkAssetResult>(
									$"select * from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
									new { execution.ExecutionID }
								)
							);
						}

						#region Build data tables.

						DataTable table = new DataTable();
						table.Columns.Add("ExecutionID", typeof(Guid));
						table.Columns.Add("ItemNumber", typeof(int));
						table.Columns.Add("ExecutionItemUid", typeof(Guid));
						table.Columns.Add("Uid", typeof(Guid));
						table.Columns.Add("AssetID", typeof(long));
						table.Columns.Add("Message", typeof(string));
						table.Columns.Add("Success", typeof(bool));
						table.Columns.Add("Cascade", typeof(bool));

						#endregion

						#region Generate data sets

						for (int i = 1; i <= import.Count; i++)
						{
							if (i > currentLocation.HighestItemNumber)
							{
								AssetDelete model = import[i - 1];
								DataRow row = table.NewRow();

								row["ExecutionID"] = execution.ExecutionID;
								row["ItemNumber"] = i;

								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								row["Uid"] = model.Uid;
								row["Cascade"] = model.Cascade ?? false;

								table.Rows.Add(row);
							}
						}

						#endregion

						if (Database.Connection.State != ConnectionState.Open)
						{
							Connection.Open();
						}

						#region Bulk Copy

						using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
						{

							bulkCopy.BatchSize = SqlBulkBatchSize;
							bulkCopy.DestinationTableName = "api.ExecutionDeletedAsset";
							bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

							bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
							bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
							bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
							bulkCopy.ColumnMappings.Add("Uid", "Uid");
							bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

							bulkCopy.WriteToServer(table);
						}

						addMeasurement(metrics, "BuildDatatable and initialization", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						#endregion

						#region Resolve assets based on UIDs

						Connection.Execute(@"
											update	T
											set		T.Object = S.Object, 
													T.ObjectID = S.ObjectID, 
													T.AssetID = S.ID
											from	api.ExecutionDeletedAsset T
													inner join Asset S on S.Uid = T.Uid and T.ExecutionID = @ExecutionID
											where 
													exists (select 1 from AssetType ST where ST.Uid = @uid and ST.ID = S.AssetTypeID);",
					new { execution.ExecutionID, at.uid }, commandTimeout: timeout);

						addMeasurement(metrics, "Resolve assets based on UIDs", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						#endregion

						#region Log lookup errors

						Connection.Execute($@"
											update	api.ExecutionDeletedAsset
											set		Success = 0,
													[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset when you are attempting to delete it'
											where	ExecutionID = @ExecutionID and ([Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)); 

											update	api.ExecutionDeletedAsset
											set		Success = 0,
													[Message] = coalesce([Message] + '; ', '') + 'Not found based on Uid provided'
											where	ExecutionID = @ExecutionID and AssetID is null;",
			new { execution.ExecutionID }, commandTimeout: timeout);

						addMeasurement(metrics, "Log lookup errors invalid asset uids or asset ids", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						//Check if asset Results exist 
						Connection.Execute($@"
											update	T
											set		T.Success = 0,
													T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(ARE.ResultCount as nvarchar) + ' results(s) present for this rule.'
											from    api.ExecutionDeletedAsset T
													inner join Asset AN on AN.ID = T.AssetID
													cross apply (select count(1) as ResultCount from AssetResult where AN.Uid = EvaluatedAssetUid or AN.Uid = OwningAssetUid having count(1) > 0) ARE
											where	T.ExecutionID = @ExecutionID
													and T.[Cascade] = 0
													and exists (select 1 from AssetType AT where AT.ID = AN.AssetTypeID and AT.Class = {(int)AssetTypeClass.Rule});",
			new { execution.ExecutionID }, commandTimeout: timeout);

						addMeasurement(metrics, "Log error asset result exists with not enabled cascade", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						#endregion

						// Validate permissions
						LogAssetPermissionErrors(execution.ExecutionID, at, Permission.DeleteAsset, "ExecutionDeletedAsset");
						addMeasurement(metrics, "LogAssetPermissionErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						generalChecksCompleted = true;
					}
					catch (Exception generalEx)
					{
						generalChecksCompleted = false;
						string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
						execution.ErrorMessage = msg;
						execution.Processed = 0;
						execution.Error = import.Count();

						results = new List<DatabaseBulkAssetResult>();
						results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
					}

					if (generalChecksCompleted)
					{
						PredicateType? predicateType = DeterminePredicateType(at.Object);
						int loopSize = 250;
						int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
						int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
						int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

						for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
						{
							bool runCompleted = false;
							bool isCascadeCheckCompleted = false;
							bool descendantsDeletionFailure = false;

							int retryCount = 0;

							while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
							{
								string querySuffix = $"S.Success is null and S.ExecutionID = @ExecutionID and S.ItemNumber between @beginItemNumber and @endItemNumber";
								if (!isCascadeCheckCompleted)
								{
									using (SqlTransaction trans = Connection.BeginTransaction())
									{

										try
										{
											if (predicateType.HasValue)
											{
												sw.Restart();

												Connection.Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(A.ChildCount as nvarchar) + ' child asset(s) present for this item.'
from    api.ExecutionDeletedAsset T
		cross apply (
			select  count(1) as ChildCount
			from	[Intersect] I
					inner join IntersectType It on IT.ID = I.IntersectTypeID
					inner join Asset A on I.ObjectAssetID = A.ID and I.SubjectAssetID = T.AssetID and A.[State] not in (3,4)
					inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] in (3,4)
		) A 
where	T.ExecutionID = @ExecutionID
		and T.[Cascade] = 0
		and T.ItemNumber between @beginItemNumber and @endItemNumber
		and A.ChildCount > 0;", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

												addMeasurement(metrics, $"Log parent and child relationships assets without cascade enabled>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
												sw.Restart();

											}

											Connection.Execute($@" 
																	if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
																		truncate TABLE #ExecutionDeletedAsset
																	else
																		begin
																			create table #ExecutionDeletedAsset (
																				ExecutionID	uniqueidentifier,
																				[Root] uniqueidentifier,
																				ItemNumber	int,
																				Uid	uniqueidentifier,
																				AssetID	bigint,
																				FromHierarchy	bit
																			);

																			create nonclustered index cix_tempExecutionDeletedAsset on #ExecutionDeletedAsset([Root], ExecutionID, ItemNumber)
																		end;

																	insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Root])
																		select distinct 
																				ExecutionID, 
																				ItemNumber, 
																				S.[Uid]
																		from	workflow.[Type] wt
																				inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
																				inner join workflow.[Version] wv on wt.id = wv.typeId
																				inner join workflow.Item wi on 	wv.id = wi.VersionID
																				inner join api.ExecutionDeletedAsset S on S.Object = wi.Object and S.ObjectID = wi.ObjectID 
																		where   {querySuffix} ;

																	insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Root])
																		select distinct 
																				ExecutionID, 
																				ItemNumber, 
																				S.[Uid]
																		from	workflow.Item wi
																				inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
																				inner join api.ExecutionDeletedAsset S on S.AssetID = i.AssetID
																		where   {querySuffix} ;

																	drop table if exists #tempworkflow;

																	select [Root] as UID,
																		ExecutionID,
																		ItemNumber
																	into #tempworkflow
																	from #ExecutionDeletedAsset
																	group by [Root], ExecutionID, ItemNumber
																	having count(1) > 0;

																	create nonclustered index cix_tempworkflow on #tempworkflow (UID, ExecutionID, ItemNumber);
			
																	update  S 
																	set     S.Success = 0 ,
																			[Message] ='You have not enabled Cascade, yet there are workflows for this asset.'
																	from    api.ExecutionDeletedAsset S 
																			inner join #tempworkflow E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
																	where	{querySuffix}  and AssetId is not null
																			and S.[Cascade] = 0;

																	drop table if exists #tempworkflow;", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

											addMeasurement(metrics, $"Log workflow for assets exists without cascade enabled>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
											sw.Restart();

											// Get the hierarchy items we also need to remove
											if (predicateType.HasValue)
											{
												sw.Restart();
												Connection.Execute($@"
																	with h as (
																		select	S.ExecutionID,
																				S.ItemNumber,
																				S.AssetID,
																				S.[Uid],
																				A.Object,
																				A.ObjectID, 
																				S.IntersectID,
																				0 as [Level]
																		from	api.ExecutionDeletedAsset S
																				inner join Asset A on  A.ID = S.AssetID
																		where	S.AssetID is not null
																				and {querySuffix}
																		union all
																		select	P.ExecutionID,
																				P.ItemNumber,
																				C.ID as AssetID,
																				C.[Uid],
																				C.Object,
																				C.ObjectID, 
																				I.IntersectID,
																				P.[Level] + 1 as [Level]
																		from	PredicateIntersect I 
																				inner join h as P on P.ExecutionID = @ExecutionID and I.PredicateType = {(int)predicateType} and P.AssetID = I.SubjectAssetID 
																				inner join Asset C on C.ID = I.ObjectAssetID 
																		where   P.ItemNumber between @beginItemNumber and @endItemNumber and P.[Level] <= 15
																	)
																	insert into api.ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Uid],[AssetID],[IntersectID],[FromHierarchy],[Object], [ObjectID], [Level])
																		select  distinct 
																				ExecutionID, 
																				ItemNumber, 
																				[Uid], 
																				AssetID, 
																				IntersectID, 
																				1,
																				Object,
																				ObjectID,
																				[Level]
																		from    h 
																		where   IntersectID is not null 
																				and [Level] > 0 
																				and not exists (select 1 from api.ExecutionDeletedAsset ed where ed.ExecutionID = @ExecutionID and ed.Uid = h.Uid)",
												new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

												addMeasurement(metrics, $"Get the hierarchy items we also need to remove>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
												sw.Restart();
											}
											isCascadeCheckCompleted = true;
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

											retryCount++;

											if (retryCount > API_V2_RETRY_LIMIT)
											{
												sw.Restart();
												LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedAsset", ex.GetFullExceptionData(false), timeout);
												addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
												sw.Restart();
											}
											isCascadeCheckCompleted = false;
										}
									}
								}

								if (!isCascadeCheckCompleted)
								{
									continue;
								}

								int numberOfItemsToDelete = Connection.Query<int>(
										$"select count(*) from api.ExecutionDeletedAsset S where {querySuffix} and S.AssetID is not null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, commandTimeout: timeout).FirstOrDefault();

								int numberOfChunkLoops = numberOfItemsToDelete == 0 ? 0 : ((numberOfItemsToDelete / SqlBulkAssetDeleteSize) + 1);

								if (numberOfChunkLoops == 0)
								{
									retryCount = API_V2_RETRY_LIMIT;
									runCompleted = true;
									continue;
								}

								// Log the parent removals into Dependent Change table.
								List<int> hierarchyPredicates =
									new List<int> { (int)PredicateType.InterTypeHierarchy, (int)PredicateType.IntraTypeHierarchy };

								Connection.Execute(@"
									drop table if exists #parent_relationship_types
									select IT.ID 
									into #parent_relationship_types
									from [IntersectType] IT
									inner join [Predicate] P on P.ID = IT.PredicateID
									where P.Type in @hierarchyPredicates;

									insert into api.ExecutionItemDependentChange (ExecutionID, ItemNumber, DependentChangeType, [Action], Payload)
										select S.ExecutionID, S.ItemNumber, 1, 2,'{""ParentAssetUid"": ""' + cast(P.Uid as varchar(50)) + '""}' from api.ExecutionDeletedAsset S 
										inner join [Intersect] I on I.ObjectAssetId = S.AssetId 
										inner join #parent_relationship_types IT on IT.ID = I.IntersectTypeID
										inner join Asset P on P.Id = I.SubjectAssetId
									where S.ExecutionID = @ExecutionID and S.ItemNumber between @beginItemNumber and @endItemNumber and ([Level] is null or [Level] = 0)
									", new { execution.ExecutionID, beginItemNumber, endItemNumber, hierarchyPredicates }, commandTimeout: timeout);

								addMeasurement(metrics, $"LogExecutionItemDependentChange >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
								sw.Restart();

								for (int i = 0; i < numberOfChunkLoops; i++)
								{
									int chunkDeletionRetryCount = 0;
									bool isChunkDeletionCompleted = false;

									while (!isChunkDeletionCompleted && chunkDeletionRetryCount <= API_V2_RETRY_LIMIT)
									{
										addMeasurement(metrics, $"Starting Chunk Asset Deletion >> {i + 1} >> {numberOfChunkLoops}", sw.ElapsedMilliseconds, ++step);
										sw.Restart();

										using (SqlTransaction trans = Connection.BeginTransaction())
										{
											string chunksQueryString = querySuffix + " and exists(select top 1 1 from #tempAssetsToDelete temp where temp.object = s.object and temp.objectid = s.objectid)";

											try
											{
												Connection.Execute(@"drop table if exists #tempAssetsToDelete
												create table #tempAssetsToDelete(Object nvarchar(255), ObjectID bigint)
												CREATE NONCLUSTERED INDEX ix_tempAssetsToDelete ON #tempAssetsToDelete (Object,ObjectID);", transaction: trans, commandTimeout: timeout);

												Connection.Execute($@"insert into #tempAssetsToDelete 
												select top {SqlBulkAssetDeleteSize} object, objectid from api.executiondeletedasset S
												where {querySuffix} 
												order by level desc, objectid desc",
													  new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);


												step = DeleteAssetsByChunk(execution, timeout, metrics, step, dt, canHaveProcess, sw, predicateType, beginItemNumber, endItemNumber, currentLoop, chunkDeletionRetryCount, chunksQueryString, trans);
												// Update success flag
												Connection.Execute(
													$"update S set S.Success = 1 from api.ExecutionDeletedAsset S where	{chunksQueryString} and S.AssetID is not null;",
													new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

												addMeasurement(metrics, $"Update status flag >> {currentLoop} >> {chunkDeletionRetryCount}", sw.ElapsedMilliseconds, ++step);
												sw.Restart();

												trans.Commit();
												isChunkDeletionCompleted = true;
											}
											catch (Exception ex)
											{
												chunkDeletionRetryCount++;
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


												if (chunkDeletionRetryCount > API_V2_RETRY_LIMIT)
												{
													sw.Restart();

													int characterLimit = constants.ERROR_MESSAGE_CHARACTER_LIMIT;
													Connection.Execute($@"
                                                            drop table if exists #tempAssetsToDelete
												            create table #tempAssetsToDelete(Object nvarchar(255), ObjectID bigint)
												            CREATE NONCLUSTERED INDEX ix_tempAssetsToDelete ON #tempAssetsToDelete (Object,ObjectID);

                                                            insert into #tempAssetsToDelete 
												            select top {SqlBulkAssetDeleteSize} object, objectid from api.executiondeletedasset S
												            where {querySuffix} 
												            order by level desc, objectid desc;

                                                            update	api.Execution
								                            set		[ErrorMessage] = LEFT(coalesce([ErrorMessage],'') + @msg,@characterLimit)
								                            where	ExecutionID = @executionID; 

								                            update	S
								                            set		S.Success = 0,
										                            S.[Message] = @msg
                                                            from api.ExecutionDeletedAsset S
								                            where	{chunksQueryString} and S.AssetID is not null;;",
												 new { execution.ExecutionID, msg = ex.GetFullExceptionData(false), beginItemNumber, endItemNumber, characterLimit }, commandTimeout: timeout);
													addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {chunkDeletionRetryCount}", sw.ElapsedMilliseconds, ++step);

													//if we couldnt delete child items in 10 tries stop all further executions
													descendantsDeletionFailure = true;
													i = numberOfChunkLoops + 1;
													sw.Restart();
												}
											}
										}
									}
								}
								runCompleted = true;
							}

							if (descendantsDeletionFailure)
							{
								Connection.Execute($@"
                                                            update	api.ExecutionDeletedAsset
								                            set		Success = 0,
                                                            [Message] = 'Deletion stopped (failed descendant deletion)'
								                            where	ExecutionID = @executionID and Success is null and [Message] is null; 

								                          ;", new { execution.ExecutionID }, commandTimeout: timeout);
							}

							results.AddRange(
								Query<DatabaseBulkAssetResult>(
									"select uid, ExecutionItemUid, Message, Success, 3 as ChangeType from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber", 
									new { execution.ExecutionID, beginItemNumber, endItemNumber }
								)
							);

							beginItemNumber += loopSize;
							endItemNumber += loopSize;
						}

						CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionDeletedAsset");
						Connection.Close();

						if (sendWorkflowEvents)
						{
							SendWorkflowEvents(at.Object, at.ObjectID, results, ChangeType.Delete);
							addMeasurement(metrics, "SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);
							sw.Restart();
						}

						// Data Quality Scoring - send to engine to determine what scores need to be recalculated.
						if (at.Class == AssetTypeClass.Rule)
						{
							CreateRulesRemovedExecution(execution.ExecutionID, at.ID);
						}

						// Rescore changes to parents based on the items removed here - possibly children.
						if (predicateType.HasValue)
						{
							CreateParentAssetGovernanceRescoreExecution(execution.ExecutionID);
						}
					}
				}
			}

			addMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			return results;
		}

		public List<DatabaseBulkAssetTypeResult> RemoveAssetTypes(ApiExecution execution, AssetTypeDeletes deletes, int timeout = 7200, bool stateChangeOnly = true)
		{
			bool isLog = true;
			Stopwatch sw = Stopwatch.StartNew();
			const string METHOD_NAME = "RemoveAssetTypes";
			Dictionary<string, double> metrics = new Dictionary<string, double>();

			var results = new List<DatabaseBulkAssetTypeResult>();

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var executionItemDupes = deletes.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(deletes.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				var uidDupes = deletes.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

				if (uidDupes.Any())
				{
					string message = $"Duplicate Asset Type Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
					results.AddRange(deletes.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
				}
				else
				{
					#region Build data tables.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("Uid", typeof(Guid));
					table.Columns.Add("Cascade", typeof(bool));

					#endregion

					#region Generate data sets

					for (int i = 1; i <= deletes.Count; i++)
					{
						AssetTypeDelete model = deletes[i - 1];

						DataRow row = table.NewRow();

						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = i;
						if (model.ExecutionItemUid.HasValue)
						{
							row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
						}

						row["Uid"] = model.Uid;
						row["Cascade"] = model.Cascade;

						table.Rows.Add(row);
					}

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					Connection.Execute($@"delete api.ExecutionDeletedAssetType where ExecutionID = @ExecutionID", new { execution.ExecutionID }, commandTimeout: timeout);

					#region Bulk Copy

					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
					{

						bulkCopy.BatchSize = SqlBulkBatchSize;
						bulkCopy.DestinationTableName = "api.ExecutionDeletedAssetType";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("Uid", "Uid");
						bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					Connection.Execute($@"exec api.DeleteAssetTypesByExecution @ExecutionID, @stateChangeOnly", new { execution.ExecutionID, stateChangeOnly }, commandTimeout: timeout);

					results = Connection.Query<DatabaseBulkAssetTypeResult>(
						"select * from api.ExecutionDeletedAssetType where ExecutionID = @ExecutionID",
						new { execution.ExecutionID }
						).ToList();

					// Data Quality Scoring - send to engine to determine what scores need to be recalculated.
					var assetUids = Connection.Query<Guid>(
						"select Uid from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID",
						new { execution.ExecutionID }
						).ToList();
					if (assetUids.Count > 0)
					{
						CreateRulesRemovedExecution(execution.ExecutionID, assetUids);
					}

					// Queue successfully deleted asset types for reindexing
					results.Where(r => r.Success).ToList().ForEach(r =>
					{
						Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
						{
							CompanyID = CurrentCompanyID,
							AssetTypeUid = r.uid,
							Origin = "RemoveAssetTypes, uid: " + r.uid.ToString()
						});
					});

					addMeasurement(metrics, "Building data tables and initialization completed", sw.ElapsedMilliseconds, 1);
					sw.Restart();
				}
			}

			CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionDeletedAssetType");

			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			return results;
		}

		#endregion
	}
}
