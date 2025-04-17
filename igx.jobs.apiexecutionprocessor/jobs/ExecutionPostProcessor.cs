using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.model;
using Dapper;
using igx.jobs.apiexecutionprocessor.helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	public class ExecutionPostProcessor : BaseWebJob
	{
		private const string FUNCTION_NAME = "ExecutionPostProcessor";
		readonly string INSERT_SQL = "insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, [Version], Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)";
		readonly string INSERT_FIELD_SQL = "insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Value], PreviousValue)";

		public ExecutionPostProcessor(ICommunity community, IConfiguration config) : base(community, config) { }

		string maxVersionSql(string objectColumn, string objectIdColumn)
		{
			return $"cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = {objectColumn} and ObjectID = {objectIdColumn}) mv";
		}

		string previousValueCrossApplySql(string objectColumn, string objectIdColumn, string fieldNameColumn)
		{
			return $@"(select top 1
		ROW_NUMBER() OVER (PARTITION BY i_a.Object, i_a.ObjectID, iif(i_p.FieldTypeID = 0, i_p.FieldName, cast(i_p.FieldTypeID  as nvarchar(100)) ) ORDER BY i_p.[AuditId] DESC) as RowNum,
		[Value]
from	reporting.Global_FieldAudit i_p
		inner join reporting.Global_Audit i_a on i_a.ID = i_p.AuditID and i_a.Object = {objectColumn} and i_a.ObjectID = {objectIdColumn} and ( (i_p.FieldTypeID = f.FieldTypeID and f.FieldTypeID <> 0) or (i_p.FieldName = {fieldNameColumn} and f.FieldTypeID = 0))
		order by RowNum asc)";
		}

		[FunctionName(FUNCTION_NAME), ExponentialBackoffRetry(5, "00:00:10", "00:15:00")]
		public async Task Run([QueueTrigger(constants.Queue.PostExecution, Connection = constants.Setting.Storage)] string message, ILogger log)
		{
			var request = message.AsObject<PostExecutionQueueMessage>();

			var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME },
					{ "CompanyID", request.CompanyID },
					{ "ExecutionId", request.ExecutionId }
				};

			using (log.BeginScope(logProperties))
			{
				string companyConnectionString = Community.GetConnectionStringForTenant(request.CompanyID);
				using (var companyConnection = new SqlConnection(companyConnectionString))
				{
					await companyConnection.OpenIfClosed();
					if (request.ExecutionId > 0)
					{
						await HandleExecutions(log, request, companyConnection);
					}
					else if (request.ObjectInfo != null)
					{
						dynamic tracker;
						switch (request.ObjectInfo.Object)
						{
							case "FieldType":
								tracker = new ChangeLogTracker<FieldType>(log);
								var ft = companyConnection.Query<FieldType>("select top 1 * from dbo.FieldType where Id = @ObjectId", new { request.ObjectInfo.ObjectId }).FirstOrDefault();

								if (ft == null)
								{
									ft = new FieldType() { ID = (int)request.ObjectInfo.ObjectId, AssetTypeID = request.ObjectInfo.AssetTypeId, IssueTypeID = request.ObjectInfo.IssueTypeId, IntersectTypeID = request.ObjectInfo.IntersectTypeId };
								}
								tracker.Set(ft, request.ObjectInfo.ResourceId, companyConnection, request.ObjectInfo.ChangeType);
								tracker.ParseAndSaveAuditRecord();
								break;
							default:
								//default handle asset types
								var at = companyConnection.Query<AssetType>("select top 1 * from dbo.AssetType where Object = @object and ObjectId = @objectId", new { request.ObjectInfo.Object, request.ObjectInfo.ObjectId }).FirstOrDefault();
								var fieldTypeCTT = new ChangeLogTracker<FieldType>(log);
								tracker = new ChangeLogTracker<AssetType>(log);
								tracker.Set(at, request.ObjectInfo.ResourceId, companyConnection, request.ObjectInfo.ChangeType);
								tracker.ParseAndSaveAuditRecord();
								break;
						}

					}
					companyConnection.CloseIfOpened();
				}
			}
		}

		private async Task HandleExecutions(ILogger log, PostExecutionQueueMessage request, SqlConnection companyConnection)
		{
			string commandText = string.Empty;
			var execution = await companyConnection.QueryFirstOrDefaultAsync<ApiExecution>("select * from api.Execution where Id = @id", new { id = request.ExecutionId });

			if (execution != null)
			{
				string actionText = "";

				switch (request.Action)
				{
					case PostExecutionQueueMessageAction.History:
						switch (execution.Action)
						{
							case ApiExecutionAction.DeleteAssets:
								commandText = historyDeleteAssets();
								break;
							case ApiExecutionAction.DeleteGroups:
								commandText = historyDeleteGroups();
								break;
							case ApiExecutionAction.DeletePredicates:
								commandText = historyDeletePredicates();
								break;
							case ApiExecutionAction.DeleteRelationships:
								commandText = historyDeleteRelations();
								break;
							case ApiExecutionAction.DeleteScoreAllocation:
								commandText = historyDeleteScoreAllocation();
								break;
							case ApiExecutionAction.PostGroups:
							case ApiExecutionAction.PutGroups:
								actionText = execution.Action == ApiExecutionAction.PostGroups ? "Created" : "Updated";
								commandText = historyUpsertGroups(actionText);
								break;
							case ApiExecutionAction.PostAssets:
							case ApiExecutionAction.PutAssets:
								actionText = execution.Action == ApiExecutionAction.PostAssets ? "Created" : "Updated";
								commandText = historyUpsertAssets(actionText);
								break;
							case ApiExecutionAction.PostScoreAllocation:
							case ApiExecutionAction.PutScoreAllocation:
								commandText = historyUpsertScoreAllocation();
								break;
							case ApiExecutionAction.PostRelationships:
							case ApiExecutionAction.PutRelationships:
								commandText = historyUpsertRelations();
								break;
							case ApiExecutionAction.PatchCatalog:
								commandText = historyPatchCatalog();
								break;
							case ApiExecutionAction.UpsertPredicates:
								commandText = historyUpsertPredicates();
								break;
							case ApiExecutionAction.UpsertUsers:
								commandText = historyUpsertUsers();
								break;
							default:
								commandText = "";
								break;
						}
						break;
					case PostExecutionQueueMessageAction.UpdateAssetLookupValues when execution.Action == ApiExecutionAction.PutAssets:
						commandText = updateLookupValues();
						break;
					case PostExecutionQueueMessageAction.UpdateAssetPaths:
						UpdateAssetPaths(companyConnection, execution, log);
						break;
					default:
						commandText = "";
						break;
				}

				if (!string.IsNullOrEmpty(commandText))
				{
					try
					{
						if (commandText == "historyUpsertAssets")
						{
							Guid processGuid = Guid.NewGuid();
							DateTime dt = DateTime.Now;
							await companyConnection.ExecuteAsync("exec api.PostAuditLogAssetUpsert @id, @processGuid, @processDateTime, @actionText, @r ", new { execution.Id, processGuid, r = execution.ResourceID, actionText, processDateTime = execution.ProcessingStartedOn ?? execution.StartedOn }, commandTimeout: 1800);

							await companyConnection.ExecuteAsync("exec api.PostAuditLogData @id, @processGuid, @dt ", new { execution.Id, processGuid, r = execution.ResourceID, actionText, dt }, commandTimeout: 1800);

							await companyConnection.ExecuteAsync("exec api.PostLookAssetPathPK @processGuid ", new { processGuid }, commandTimeout: 1800);

							await clearInProcessTables(companyConnection, processGuid, execution, log);
						}
						else
						{
							await companyConnection.ExecuteAsync(commandText, new { execution.Id, r = execution.ResourceID, dt = execution.ProcessingStartedOn ?? execution.StartedOn }, commandTimeout: 1800);
						}
					}
					catch (Exception ex)
					{
						log.LogCritical(ex, "Error when post-processing execution.");
					}
				}
			}
		}



		#region History Generators

		string historyDeleteAssets()
		{
			return $@"
{INSERT_SQL}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			'Deleted', 
			p.Object, 
			p.ObjectId,
			p.TypeName, 
			p.ObjectName, 
			'This asset has been removed.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id 
			cross apply openjson(l.Payload) with (Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
			{maxVersionSql("p.Object", "p.ObjectID")}
	where l.subtask is null;


{INSERT_SQL}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			'Deleted', 
			'Intersect',
			p.IntersectId,
			p.TypeName, 
			coalesce(p.ActionObjectName, 'Relationship'), 
			'The relationship was removed because one of the associated assets was deleted.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250),ActionObjectName nvarchar(250)) p 
			{maxVersionSql("p.Object", "p.ObjectId")}
	where l.ExecutionId = @Id and l.subtask = 'R';";
		}

		string historyDeleteGroups()
		{
			return $@"
{INSERT_SQL}
	select	distinct
			'Group', 
			p.ID,
			p.ObjectName,
			@r, 
			@dt, 
			mv.[Version],
			'Deleted',
			'Group', 
			p.ID,
			'Group', 
			p.ObjectName, 
			'This group has been removed.'
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
			cross apply openjson(l.Payload) with (ID int, ObjectName nvarchar(250)) p
			{maxVersionSql("'Group'", "p.ID")};";
		}

		string historyDeletePredicates()
		{
			return $@"
{INSERT_SQL}
	select	distinct
			'Predicate', 
			p.Id,
			p.Name,
			@r, 
			@dt, 
			mv.[Version],
			'Deleted',
			'Predicate', 
			p.ID,
			'Predicate', 
			p.Name, 
			'This predicate has been removed.'
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
			cross apply openjson(l.Payload) with (Id int, Name nvarchar(250)) p
			{maxVersionSql("'Predicate'", "p.Id")};";
		}

		string historyDeleteRelations()
		{
			return $@"
{INSERT_SQL}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			'Deleted', 
			'Intersect',
			p.IntersectId,
			p.TypeName, 
			coalesce(p.ActionObjectName, 'Relationship'), 
			'The relationship was removed.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250) ,ActionObjectName nvarchar(250)) p 
			{maxVersionSql("p.Object", "p.ObjectId")}
	where l.ExecutionId = @Id;";
		}

		string historyDeleteScoreAllocation()
		{
			return $@"
{INSERT_SQL}
	select	distinct
			'MetricAllocation', 
			p.ID,
			'Score Definition',
			@r, 
			@dt, 
			mv.[Version],
			'Deleted',
			'MetricAllocation', 
			p.ID,
			'MetricAllocation', 
			'Score Definition', 
			'Score definition removed.'
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
			cross apply openjson(l.Payload) with (Id int) p
			{maxVersionSql("'MetricAllocation'", "p.Id")};";
		}

		string historyPatchCatalog()
		{
			return $@"
declare @tbl table (ID bigint, Object varchar(50), ObjectID int)
{INSERT_SQL}
output inserted.ID, inserted.Object, inserted.ObjectID into @tbl
	select	a.Object, 
			a.ObjectId,
			d.DisplayValue, 
			@r, 
			@dt, 
			mv.[Version],
			iif(l.[Action] = 'A', 'Created', 'Updated'), 
			a.Object, 
			a.ObjectId,
			t.Name, 
			d.DisplayValue, 
			'This asset has been ' + iif(l.[Action] = 'A', 'created', 'updated') + '.' 
	from	api.ExecutionCatalogItem l
			inner join Asset a on a.Id = l.Id and l.ExecutionId = @Id and l.[Type] = 'A' and l.Success = 1 and l.IsDelete = 0 
			inner join AssetDisplayValue d on d.AssetID = a.Id
			inner join AssetType t on t.Id = a.AssetTypeId 
			{maxVersionSql("a.Object", "a.ObjectID")};

{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			coalesce(f.FieldTypeID, 0),
			f.Name,
			f.[Value],
			pv.[Value] as PreviousValue
from	api.ExecutionCatalogItem l
		inner join Asset a on a.Id = l.Id and l.ExecutionId = @Id and l.[Type] = 'A' and l.Success = 1 and l.IsDelete = 0 
		inner join @tbl tt on tt.Object = a.Object and tt.ObjectID = a.ObjectID
		inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
		inner join api.ExecutionCatalogItemProperty f on f.ExecutionId = l.ExecutionID and f.SourceId = l.SourceId
		outer apply {previousValueCrossApplySql("a.Object", "a.ObjectId", "f.Name")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(cast(f.ValueId as nvarchar(max)), f.Value,'') != '') 
			or (coalesce(cast(f.ValueId as nvarchar(max)), f.Value,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";
		}

		string historyUpsertAssets(string actionText)
		{
			string commandText = "historyUpsertAssets";
			return commandText;

		}

		string historyUpsertGroups(string actionText)
		{
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{INSERT_SQL}
output inserted.ID, inserted.ObjectID into @tbl
	select	'Group', 
			p.ID,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			'{actionText}', 
			'Group', 
			p.ID,
			'Group', 
			p.ObjectName, 
			'This group has been {actionText.ToLower(System.Globalization.CultureInfo.InvariantCulture)}.' 
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, ItemNumber int, ObjectName nvarchar(250), Description nvarchar(max), IsActiveDirectoryGroup bit, PrimaryOwnerResourceID int, SecondaryOwnerResourceID int) p 
			{maxVersionSql("'Group'", "p.ID")}
	where	l.ExecutionId = @Id;

{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			f.FieldTypeID,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (ID int, ItemNumber int, ObjectName nvarchar(250), Description nvarchar(max), IsActiveDirectoryGroup bit, PrimaryOwnerResourceID int, SecondaryOwnerResourceID int) p
		inner join @tbl tt on tt.ObjectID = p.ID
		inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id
		cross apply (
					select	FieldTypeID,
							FieldName,
							FieldValue,
							LookupValue
					from	api.ExecutionField 
					where	ExecutionID = e.ExecutionID 
							and ItemNumber = p.ItemNumber
					union
					select 0 as FieldTypeID, 'Name' as FieldName, p.ObjectName as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'Description' as FieldName, p.Description as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'IsActiveDirectoryGroup' as FieldName, iif(p.IsActiveDirectoryGroup = 1, 'true', 'false') as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'PrimaryOwnerResourceID' as FieldName, cast(p.PrimaryOwnerResourceID as nvarchar(max)) as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'SecondaryOwnerResourceID' as FieldName, cast(p.SecondaryOwnerResourceID as nvarchar(max)) as FieldValue, cast(null as nvarchar(max)) as LookupValue
					) f
		outer apply {previousValueCrossApplySql("'Group'", "p.ID", "f.FieldName")} pv
		where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
		or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";
		}

		string historyUpsertPredicates()
		{
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{INSERT_SQL}
output inserted.ID, inserted.ObjectID into @tbl
	select	'Predicate', 
			p.Id,
			p.Name, 
			@r, 
			@dt, 
			mv.[Version],
			iif(p.IsNew = 1, 'Created', 'Updated'), 
			'Predicate', 
			p.Id,
			'Predicate', 
			p.Name, 
			'This predicate has been ' + iif(p.IsNew = 1, 'created', 'updated') + '.'
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (Id int, Name nvarchar(250), Inverse nvarchar(250), IsNew bit) p 
			{maxVersionSql("'Predicate'", "p.Id")}
	where l.ExecutionId = @Id;

{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			0,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (Id int, Name nvarchar(250), Inverse nvarchar(250), Type int, IsNew bit) p
		inner join @tbl tt on tt.ObjectID = p.Id
		inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id
		cross apply (
					select 0 as FieldTypeID, 'Name' as FieldName, p.Name as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union all
					select 0 as FieldTypeID, 'Inverse' as FieldName, p.Inverse as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union all
					select 0 as FieldTypeID, 'Functional Type' as FieldName, [utility].[GetPredicateFunctionalTypeValue](Type) as FieldValue, cast(null as nvarchar(max)) as LookupValue
					) f
		outer apply {previousValueCrossApplySql("'Predicate'", "p.Id", "f.FieldName")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";
		}

		string historyUpsertRelations()
		{
			return $@"
drop table if exists #tempdata;

select	cast(l.id as bigint) id,
		p.Object, 
		p.ObjectId,
		p.ObjectName, 
		iif(p.IsNew = 1, 'Created', 'Updated') Action, 
		p.ActionObjectId,
		p.TypeName, 
		p.ActionObjectName
into #tempdata
from	api.ExecutionLog l
		inner join api.Execution e on e.Id = l.ExecutionId 
		cross apply openjson(l.Payload) with (Object varchar(50), ObjectId int, ObjectName nvarchar(250), ActionObjectId int, ActionObjectName nvarchar(250), TypeName nvarchar(250), IsNew bit) p 
where	l.ExecutionId = @Id;

create clustered index idx_tempdata on #tempdata (id);

{INSERT_SQL}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			p.Action, 
			'Intersect',
			p.ActionObjectId,
			p.TypeName, 
			p.ActionObjectName, 
			'Relationship created.' 
	from	#tempdata p
			{maxVersionSql("p.Object", "p.ObjectId")}
	order by p.id;

drop table if exists #tempdata;";
		}

		string historyUpsertScoreAllocation()
		{
			return $@"
drop table if exists #tempScore;

select	cast(p.Id as bigint) ExecutionId,
		p.ID,
		p.CalculationMethod, 
		p.ScoreType,
		p.IsExternallyCalculated, 
		p.LowerThreshold,
		p.UpperThreshold,
		p.IsNew, 
		p.AssetTypeName
into #tempScore
from	api.ExecutionLog l
cross apply openjson(l.Payload) with (Id int, CalculationMethod nvarchar(250), ScoreType nvarchar(250), 
									  IsExternallyCalculated varchar(10), LowerThreshold int, 
									  UpperThreshold int, IsNew bit,AssetTypeName nvarchar(250)) p 
where	l.ExecutionId = @Id;


if exists(select 1 from #tempScore where IsNew = 1)
begin
	drop table if exists #tempdeletedAuditIds;

	select distinct ga.ID
	into #tempdeletedAuditIds
	from #tempScore t
	inner join [reporting].[Global_Audit] ga on ga.[Object] = 'MetricAllocation' and ga.[Objectid] = t.id
	where t.IsNew = 1;

	if exists(select 1 from #tempdeletedAuditIds)
	begin
		create clustered index idx_tempdeletedAuditIds on #tempdeletedAuditIds (id);

		delete gfa 
		from [reporting].[Global_FieldAudit] gfa 
		inner join #tempdeletedAuditIds da on gfa.AuditID=da.ID;

		delete ga 
		from [reporting].[Global_Audit] ga 
		inner join #tempdeletedAuditIds da on ga.ID=da.ID;
	end
	drop table if exists #tempdeletedAuditIds;
end


create clustered index idx_tempScore on #tempScore (id);

declare @tbl table (ID bigint, ObjectID int)
{INSERT_SQL}
output inserted.ID, inserted.ObjectID into @tbl
	select	'MetricAllocation', 
			p.Id,
			'Score Definition',--p.Name, 
			@r, 
			@dt, 
			mv.[Version],
			iif(p.IsNew = 1, 'Created', 'Updated'), 
			'MetricAllocation', 
			p.Id,
			'MetricAllocation', 
			'Score Definition',--p.Name, 
			'Score definition ' + iif(p.IsNew = 1, 'created', 'updated') + '.'
	from	#tempScore p
			{maxVersionSql("'MetricAllocation'", "p.Id")};

{INSERT_FIELD_SQL}
	select	distinct
			tt.ID as AuditID,
			0,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	#tempScore p
		inner join @tbl tt on tt.ObjectID = p.Id
		cross apply (
					select 0 as FieldTypeID, 'CalculationMethod' as FieldName, p.CalculationMethod as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union All
					select 0 as FieldTypeID, 'ScoreType' as FieldName, p.ScoreType as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union All
					select 0 as FieldTypeID, 'IsExternallyCalculated' as FieldName, p.IsExternallyCalculated as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union All
					select 0 as FieldTypeID, 'LowerThreshold' as FieldName, cast(p.LowerThreshold as nvarchar(50)) as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union All
					select 0 as FieldTypeID, 'UpperThreshold' as FieldName, cast(p.UpperThreshold as nvarchar(50)) as FieldValue, cast(null as nvarchar(max)) as LookupValue					
					union All
					select 0 as FieldTypeID, 'AssetTypeName' as FieldName, cast(p.AssetTypeName as nvarchar(250)) as FieldValue, cast(null as nvarchar(max)) as LookupValue					
					) f
		outer apply {previousValueCrossApplySql("'MetricAllocation'", "p.Id", "f.FieldName")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));
drop table if exists #tempScore;";
		}

		string historyUpsertUsers()
		{
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{INSERT_SQL}
output inserted.ID, inserted.ObjectID into @tbl
	select	'Resource', 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			iif(p.IsNew = 1, 'Created', 'Updated'), 
			'Resource', 
			p.ObjectId,
			'Resource', 
			p.ObjectName, 
			'This user has been ' + iif(p.IsNew = 1, 'created', 'updated') + '.'
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ObjectId int, ItemNumber int, FirstName nvarchar(500), LastName nvarchar(500), Username nvarchar(500), IsAdministrator bit, ObjectName nvarchar(250), IsNew bit) p 
			{maxVersionSql("'Resource'", "p.ObjectId")}
	where l.ExecutionId = @Id;

{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			f.FieldTypeID,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (ObjectId int, ItemNumber int, FirstName nvarchar(500), LastName nvarchar(500), Username nvarchar(500), IsAdministrator bit, ObjectName nvarchar(250), IsNew bit) p
		inner join @tbl tt on tt.ObjectID = p.ObjectId
		inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id
		cross apply (
					select	FieldTypeID,
							FieldName,
							FieldValue,
							LookupValue
					from	api.ExecutionField 
					where	ExecutionID = e.ExecutionID 
							and ItemNumber = p.ItemNumber
					union
					select 0 as FieldTypeID, 'FirstName' as FieldName, p.FirstName as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'LastName' as FieldName, p.LastName as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'Username' as FieldName, p.Username as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'IsAdministrator' as FieldName, iif(p.IsAdministrator = 1, 'true', 'false') as FieldValue, cast(null as nvarchar(max)) as LookupValue
					) f
		outer apply {previousValueCrossApplySql("'Resource'", "p.ObjectId", "f.FieldName")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";
		}

		#endregion

		string updateLookupValues()
		{
			return $@"
				declare @assetTypeUid uniqueidentifier
				declare @lookupObjectId int
				declare @lookupObject nvarchar(max)

				select @assetTypeUid = JSON_VALUE(Fields,'$.AssetTypeUid') from api.Execution where Id = @Id

				select @lookupObjectId = ObjectID, @lookupObject = REPLACE(Object,'Type','') from AssetType where uid = @assetTypeUid

				declare @lookupFieldTypes table (FieldTypeId int, AllowMultipleValues bit, Type nvarchar(255),LookupDisplayFormat nvarchar(255), LookupObjectType nvarchar(255),LookupObjectID int)

				insert into @lookupFieldTypes (FieldTypeId, AllowMultipleValues, Type, LookupDisplayFormat, LookupObjectID, LookupObjectType)
				select Id, AllowMultipleValues, Type, LookupDisplayFormat, LookupObjectID, LookupObjectType 
				from 
				dbo.FieldType where LookupObjectID = @lookupObjectId and LookupObjectType = @lookupObject


				if (select count(*) from @lookupFieldTypes) = 0
				begin
					return
				end

				select distinct ObjectID
				into #updatedObjectIds
				from api.Execution e
				inner join api.ExecutionAsset S on S.ExecutionID = e.ExecutionID
				where e.Id = @Id

				create nonclustered index ix_id on #updatedObjectIds (ObjectId)

				UPDATE F
				set F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(ft.Type, ft.LookupDisplayFormat, ft.LookupObjectType, ft.LookupObjectID, f.Value, ft.AllowMultipleValues)
					from dbo.Field f
					inner join @lookupFieldTypes ft on ft.FieldTypeId = f.FieldTypeID
					inner join #updatedObjectIds a on f.Value = a.ObjectId
					where ft.AllowMultipleValues = 0

				select distinct id 
				into #tempUpdateLookupFieldTable
				from dbo.Field f
				inner join @lookupFieldTypes ft on ft.FieldTypeId = f.FieldTypeID
				cross apply (select * from string_split(f.Value,','))vals
				inner join #updatedObjectIds a on a.ObjectId = vals.value
				where ft.AllowMultipleValues = 1

				UPDATE F
				set F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(ft.Type, ft.LookupDisplayFormat, ft.LookupObjectType, ft.LookupObjectID, f.Value, ft.AllowMultipleValues)
					from dbo.Field f
					inner join @lookupFieldTypes ft on ft.FieldTypeId = f.FieldTypeID
					inner join #tempUpdateLookupFieldTable tempF on tempF.ID = f.ID
					where ft.AllowMultipleValues = 1

				update ft
					set ft.DefaultFormattedValue = adv.DisplayValue
				from dbo.FieldType ft
					inner join dbo.AssetType at on at.Object = concat(ft.LookupObjectType,'Type') and at.ObjectID = ft.LookupObjectID
					inner join dbo.Asset a on a.AssetTypeID = at.ID and a.ObjectID = ft.DefaultValue
					inner join dbo.AssetDisplayValue adv on adv.AssetID = a.ID
				where ft.LookupObjectType = @lookupObject
					and ft.LookupObjectID = @lookupObjectId 
					and ft.Type = 'Lookup' 
					and ft.DefaultValue is not null 
					and ft.DefaultFormattedValue <> adv.DisplayValue

				drop table if exists #tempUpdateLookupFieldTable
				drop table if exists #updatedObjectIds;";
		}

		async Task clearInProcessTables(SqlConnection companyConnection, Guid processGuid, ApiExecution execution, ILogger log)
		{
			List<string> listSqlstmt = new List<string> {
"delete t from api.InProcessPostAssetPath t where ProcessUid = @processGuid",
"delete t from api.InProcessAudit t where ProcessUid = @processGuid",
"delete t from api.InProcessAuditField t where ProcessUid = @processGuid",
"delete t from api.InProcessLookUpField t where ProcessUid = @processGuid",
"delete t from api.InProcessHisUpdExeLog t where ProcessUid = @processGuid",
"delete t from api.InProcessHisUpdField t where ProcessUid = @processGuid",
"delete t from api.InProcessLookUpFieldType t where ProcessUid = @processGuid",
"delete t from api.InProcessLookUpTempField t where ProcessUid = @processGuid",
"delete t from api.InProcessLookUpTempFieldMulti t where ProcessUid = @processGuid",
"delete t from api.InProcessAssetIDFieldTypeID t where ProcessUid = @processGuid"
};
			foreach (var sql in listSqlstmt)
			{
				try
				{
					await companyConnection.ExecuteAsync(sql, new { processGuid }, commandTimeout: 600);
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, $"Error when clear in process data:{execution.Id}-Process Uid: {processGuid.ToString()}]. {sql}");
				}
			}
		}

		void UpdateAssetPaths(SqlConnection companyConnection, ApiExecution execution, ILogger log)
		{
			try
			{
				var assetTypeUid = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(execution.Fields).AssetTypeUid;
				var assetType = companyConnection.QueryFirst<AssetType>("select * from dbo.AssetType where uid = @assetTypeUid", new { assetTypeUid });

				int itemsPerLoop = 500;
				decimal numberOfLoops = Math.Ceiling(execution.Total / (decimal)itemsPerLoop);

				for (int i = 0; i < numberOfLoops; i++)
				{
					var sqlParameters = new
					{
						executionID = execution.ExecutionID,
						@class = (int)assetType.Class,
						begin = itemsPerLoop * i,
						end = itemsPerLoop * i + itemsPerLoop,
						isInsert = false
					};
					using (SqlTransaction trans = companyConnection.BeginTransaction())
					{
						try
						{
							companyConnection.Execute("exec api.MergeAssetPaths @executionId, @class, @begin, @end, null, @isInsert",
							
							//Timeout after 2.5 hours if there is a big hierarchy to be calculated
							sqlParameters, transaction: trans, commandTimeout: 9000);

							trans.Commit();
						}
						catch(Exception ex)
						{
							trans.Rollback();
							log.LogCritical(ex, $"Error when post-processing execution [UpdateAssetPaths:{execution.Id}].");
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.LogCritical(ex, $"Error when post-processing execution [UpdateAssetPaths:{execution.Id}].");
			}
		}
	}
}
