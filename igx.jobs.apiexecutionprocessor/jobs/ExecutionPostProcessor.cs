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

		public ExecutionPostProcessor(IConfiguration config) : base(config) { }

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
				string companyConnectionString = GetCompanyConnectionString(request.CompanyID);

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
						await companyConnection.ExecuteAsync(commandText, new { execution.Id, r = execution.ResourceID, dt = execution.ProcessingStartedOn ?? execution.StartedOn }, commandTimeout: 1800);
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
			{maxVersionSql("p.Object", "p.ObjectID")};";
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
			'Relationship', 
			'This relationship has been removed.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p 
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
			string commandText = "";

			//Temp table for all updated fields
			commandText += $@"
	select	
			coalesce(f.FieldTypeID, 0) as FieldTypeId,
			f.FieldName,
			coalesce(fv.FormattedValue, f.FieldValue) as FieldValue,
			pv.[Value] as PreviousValue,
			p.Object,
			P.ObjectId
	into #updatedFieldsMap
	from	api.ExecutionLog a
			cross apply openjson(a.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
			inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id and a.SubTask is null
			inner join api.ExecutionField f on f.ExecutionID = e.ExecutionID and f.ItemNumber = p.ItemNumber and f.FieldTypeID not in (select ID from FieldType where ID = f.FieldTypeID and [Type] in ('Relationship'))
			outer apply (
						select	utility.GetFormattedFieldLookupValueWithMultiple([Type], LookupDisplayFormat, LookupObjectType, LookupObjectID, f.LookupValue, AllowMultipleValues) as FormattedValue
						from	FieldType
						where	ID = f.FieldTypeID
								and [Type] = 'Lookup'
						) fv
			outer apply (select top 1
		ROW_NUMBER() OVER (PARTITION BY i_a.Object, i_a.ObjectID, iif(i_p.FieldTypeID = 0, i_p.FieldName, cast(i_p.FieldTypeID  as nvarchar(100)) ) ORDER BY i_p.[AuditId] DESC) as RowNum,
		[Value]
from	reporting.Global_FieldAudit i_p
		inner join reporting.Global_Audit i_a on i_a.ID = i_p.AuditID and i_a.Object = p.Object and i_a.ObjectID = p.ObjectId and ( (i_p.FieldTypeID = f.FieldTypeID and f.FieldTypeID <> 0) or (i_p.FieldName = f.FieldName and f.FieldTypeID = 0))
		order by RowNum asc) pv
	where	((coalesce(pv.Value,'') = '' and  coalesce(fv.FormattedValue, f.FieldValue,'') != '') 
			or (coalesce(fv.FormattedValue, f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";

			// Record history for assets we are creating/updating.
			commandText += $@"
declare @tbl table (ID bigint, Object varchar(50), ObjectID int)

{INSERT_SQL}
output inserted.ID, inserted.Object, inserted.ObjectID into @tbl
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			'{actionText}', 
			p.Object, 
			p.ObjectId,
			p.TypeName, 
			p.ObjectName, 
			'This asset has been {actionText.ToLower(System.Globalization.CultureInfo.InvariantCulture)}.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id and l.SubTask is null
			cross apply openjson(l.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p 
			{maxVersionSql("p.Object", "p.ObjectId")}
			where exists(select top 1 1 from #updatedFieldsMap where Object = p.Object and ObjectId = p.ObjectId);";

			// Record field history using the audit Ids garnered above.
			commandText += $@"
{INSERT_FIELD_SQL}
	select distinct	tt.ID as AuditID,
			fields.FieldTypeId,
			fields.FieldName,
			fields.FieldValue,
			fields.PreviousValue
	from	api.ExecutionLog a
			cross apply openjson(a.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
			inner join @tbl tt on tt.Object = p.Object and tt.ObjectID = p.ObjectID
			inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id and a.SubTask is null
			inner join api.ExecutionField f on f.ExecutionID = e.ExecutionID and f.ItemNumber = p.ItemNumber and f.FieldTypeID not in (select ID from FieldType where ID = f.FieldTypeID and [Type] in ('Relationship'))
            inner join #updatedFieldsMap fields on fields.Object = p.Object and fields.ObjectId = p.ObjectId and f.FieldTypeID = fields.FieldTypeId

			drop table if exists #updatedFieldsMap;
";

			// Record the relationship changes via any relation fields on the assets above.
			commandText += $@"
{INSERT_SQL}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			case p.[Action]
				when 'D' then 'Deleted'
				when 'U' then 'Updated'
				else 'Created'
			end, 
			'Intersect',
			p.ActionObjectId,
			p.ActionObjectTypeName, 
			coalesce(p.ActionObjectName, 'Relationship'), 
			'This relationship has been ' + 
			case p.[Action]
				when 'D' then 'deleted'
				when 'U' then 'updated'
				else 'created'
			end	 + '.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
			cross apply openjson(l.Payload) with (ActionObjectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), ActionObjectName nvarchar(max), ActionObjectTypeName nvarchar(250), [Action] char(1)) p 
			{maxVersionSql("p.Object", "p.ObjectId")}
	where	l.SubTask = 'R';";

			// Get any field types where we use a lookup that relies on any assets from above.
			commandText += $@"
select	distinct f.ID,
		f.Type, 
		cast(p.ObjectId as int) ObjectId,
		f.LookupDisplayFormat, 
		f.LookupObjectType, 
		f.LookupObjectID, 
		f.AllowMultipleValues
into	#relyingFieldTypes
from	api.ExecutionLog l
		cross apply openjson(l.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
		inner join Asset a on a.Id = p.AssetId
		inner join AssetType t on t.Id = a.AssetTypeId
		inner join FieldType f on f.Type = 'Lookup' and f.LookupObjectType = replace(t.[Object],'Type','') and f.LookupObjectID = t.ObjectID
where	l.ExecutionId = @Id;

create clustered index idx_relyingFieldTypes on #relyingFieldTypes (ID);
";

			// Create Temporary #fields tale.
			commandText += $@"
drop table if exists #fields;
create table #fields(
AssetID bigint,
Object  varchar(100),
ObjectID int,
TypeName nvarchar(500),
ObjectName nvarchar(max),
FieldName nvarchar(256),
FieldTypeID int,
FieldValue nvarchar(max)
);
create clustered index cx_fields on #fields(AssetID);
";

			// Calculate any formatted values we will use to update the fields(AllowMultipleValues false).
			commandText += $@"
select	ID as FieldtypeId,
		cast(ObjectId as nvarchar(10)) ObjectId,
		utility.GetFormattedFieldLookupValueWithMultiple(Type, LookupDisplayFormat, LookupObjectType, LookupObjectID, ObjectId, AllowMultipleValues) as FormattedValue
into	#formattedValues
from	#relyingFieldTypes
where	AllowMultipleValues = 0;

create clustered index idx_formattedValues on #formattedValues (FieldtypeId,ObjectId);
";

			// Get Required Field from Field table.
			commandText += $@"
if exists(select 1 from #formattedValues)
begin
	drop table if exists #tempField;

	select	fv.FieldtypeId as FieldtypeId,
			f.AssetID as AssetID,
			f.ID as FieldID,
			fv.FormattedValue,
			case when coalesce(fv.FormattedValue,'') = coalesce(f.FormattedValue,'') then 1 else 0 end IsMatch
	into	#tempField
	from	#formattedValues fv
	inner join Field F on fv.FieldtypeId = F.FieldtypeId and fv.ObjectId = F.[Value] and coalesce(F.[Value],'') != ''

	create clustered index idx_tempField on #tempField (FieldID);

	UPDATE	F
	SET		F.FormattedValue = FT.FormattedValue
	from	Field F
	inner join #tempField FT on FT.FieldID = F.ID and FT.IsMatch = 0;

	insert into #fields
	select	F.AssetID,
			A.Object,
			A.ObjectID,
			A.TypeName,
			A.DisplayValue as ObjectName,
			T.Name as FieldName,
			F.FieldTypeID,
			F.FormattedValue as FieldValue
	from	#tempField F
			inner join FieldType T on T.ID = F.FieldTypeID
			inner join AssetDetail A on A.ID = F.AssetID;
end
";

			// Calculate any formatted values we will use to update the fields(AllowMultipleValues True).
			commandText += $@"
select	ID as FieldtypeId,
		cast(ObjectId as nvarchar(10)) ObjectId
into	#formattedValuesTrue
from	#relyingFieldTypes
where	AllowMultipleValues = 1;

create clustered index idx_formattedValuesTrue on #formattedValuesTrue (FieldtypeId);
";

			// Get Required Field from Field table.
			commandText += $@"
if exists(select 1 from #formattedValuesTrue)
begin
	drop table if exists #tempFieldTrue;

	select	F.ID as FieldID,
			cast(null as int)	as FieldtypeId,
			cast(null as bigint) as AssetID,
			cast(null as nvarchar(max)) FormattedValue,
			cast(null as nvarchar(max)) [FieldValue],
			cast(null as nvarchar(max)) FormattedValue_New
	into	#tempFieldTrue
	from	#formattedValuesTrue fv
	inner join Field F on fv.FieldtypeId = F.FieldtypeId and coalesce(F.[Value],'') != '' 
	cross apply (select [Value] 
				from string_split(F.[Value], ',') V 
				where V.[Value] = fv.ObjectId and coalesce(V.[Value],'') !='') C;

	if exists(select 1 from #tempFieldTrue)
	begin
		create clustered index idx_tempFieldTrue on #tempFieldTrue (FieldID);
	
		Update tempF
		Set FieldtypeId = F.FieldtypeId,
		AssetID = F.AssetID,
		FormattedValue = F.FormattedValue,
		FieldValue = F.[Value]
		from #tempFieldTrue tempF
		inner join Field F on F.ID = tempF.FieldID;

		Update F
		Set FormattedValue_New = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.[FieldValue], FT.AllowMultipleValues)
		from #tempFieldTrue F
		inner join #relyingFieldTypes FT on F.FieldtypeId = FT.ID;
	
		UPDATE	F
		SET		F.FormattedValue = FT.FormattedValue_New
		from	Field F
		inner join #tempFieldTrue FT on FT.FieldID = F.ID 
		and coalesce(FT.FormattedValue_New,'') <> coalesce(FT.FormattedValue,'');

		insert into #fields
		select	F.AssetID,
				A.Object,
				A.ObjectID,
				A.TypeName,
				A.DisplayValue as ObjectName,
				T.Name as FieldName,
				F.FieldTypeID,
				F.FormattedValue_New as FieldValue
		from	#tempFieldTrue F
				inner join FieldType T on T.ID = F.FieldTypeID
				inner join AssetDetail A on A.ID = F.AssetID;
	end
end
";

			// Clear out the audit header temp table variable from where we used it above. Using it again here.
			commandText += $@"
drop table if exists #relyingFieldTypes;
drop table if exists #formattedValues;
drop table if exists #formattedValuesTrue;
drop table if exists #tempField;
drop table if exists #tempFieldTrue;
delete @tbl;";

			// Add the audit history header records for the asset that rely on the first set of assets.
			// Only when updated field type affected display value of lookup field on target asset
			commandText += $@"
declare @StartedOn date,
@ExecutionID uniqueidentifier;

select @StartedOn = StartedOn , @ExecutionID = ExecutionID
from api.Execution e
where e.Id = @id;

drop table if exists #tempunqAssetFieldID;

select distinct ea.AssetID,ef.FieldTypeID
into #tempunqAssetFieldID
from api.ExecutionAsset ea
inner join api.ExecutionField ef on ea.ExecutionID = ef.ExecutionID and ea.ItemNumber = ef.ItemNumber
where ea.ExecutionID = @ExecutionID

select distinct ft.ID, ft.Name
into #updatedFieldTypeIds
from #tempunqAssetFieldID ea
inner join Field f on f.AssetID = ea.AssetID and f.FieldTypeID = ea.FieldTypeID
inner join Field fu on fu.ID = f.ID 
inner join FieldType ft on ft.ID = f.FieldTypeID
where fu.UpdatedOn > @StartedOn;

drop table if exists #tempunqAssetFieldID;
drop table if exists #tempfieldsdata;

select	distinct F.Object,
		F.ObjectId,
		F.ObjectName,
		F.TypeName
into #tempfieldsdata
from	#fields F
		inner join FieldType ft on ft.ID = F.FieldtypeId
		inner join #updatedFieldTypeIds uft on ft.LookupDisplayFormat like '%'+uft.Name+'%';

drop table if exists #updatedFieldTypeIds;


{INSERT_SQL}
output inserted.ID, inserted.Object, inserted.ObjectID into @tbl
	select	F.Object,
			F.ObjectId,
			F.ObjectName,
			@r, 
			@dt, 
			mv.[Version],
			'Updated', 
			F.Object, 
			F.ObjectId,
			F.TypeName, 
			F.ObjectName, 
			'Underlying asset from lookup was updated.' 
	from	#tempfieldsdata F
			{maxVersionSql("F.Object", "F.ObjectId")};

			drop table if exists #tempfieldsdata;
";

			// Add the field history records for the assets whose lookup fields we updated.
			commandText += $@"
{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			coalesce(f.FieldTypeID, 0),
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
	from	#fields f
			inner join @tbl tt on tt.Object = f.Object and tt.ObjectID = f.ObjectID
			outer apply {previousValueCrossApplySql("f.Object", "f.ObjectId", "f.FieldName")} pv
	where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
	or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));

	drop table if exists #fields;
	";
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
		p.ActionObjectTypeName, 
		p.ActionObjectName
into #tempdata
from	api.ExecutionLog l
		inner join api.Execution e on e.Id = l.ExecutionId 
		cross apply openjson(l.Payload) with (Object varchar(50), ObjectId int, ObjectName nvarchar(250), ActionObjectId int, ActionObjectName nvarchar(250), ActionObjectTypeName nvarchar(250), IsNew bit) p 
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
			p.ActionObjectTypeName, 
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

				drop table if exists #tempUpdateLookupFieldTable
				drop table if exists #updatedObjectIds;";
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
							sqlParameters, transaction: trans, commandTimeout: 3600);

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
