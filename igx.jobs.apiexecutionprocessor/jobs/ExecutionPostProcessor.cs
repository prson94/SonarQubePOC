using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.model;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	public class ExecutionPostProcessor: BaseWebJob
	{
		private const string FUNCTION_NAME = "ExecutionPostProcessor";
		readonly string INSERT_SQL = "insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, [Version], Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)";
		readonly string INSERT_FIELD_SQL = "insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Value], PreviousValue)";

		public ExecutionPostProcessor(IConfiguration config): base(config) { }

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
		public async Task Run([QueueTrigger("%AssetGraphQueue%", Connection = "QueuesConnectionString")] string message, ILogger log)
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
				string commandText = "";

				using (var companyConnection = new SqlConnection(companyConnectionString))
				{
					await companyConnection.OpenIfClosed();
				
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
							case PostExecutionQueueMessageAction.Indexing:
								switch (execution.Action)
								{
									case ApiExecutionAction.DeleteAssets:
										commandText = indexDeleteAssets();
										break;
									case ApiExecutionAction.PostAssets:
									case ApiExecutionAction.PutAssets:
										actionText = execution.Action == ApiExecutionAction.PostAssets ? "A" : "U";
										commandText = indexUpsertAssets(actionText);
										break;
									case ApiExecutionAction.PatchCatalog:
										commandText = indexPatchCatalog();
										break;
									case ApiExecutionAction.DeleteGroups:
										commandText = indexDeleteGroups();
										break;
									case ApiExecutionAction.PostGroups:
									case ApiExecutionAction.PutGroups:
										actionText = execution.Action == ApiExecutionAction.PostGroups ? "A" : "U";
										commandText = indexUpsertGroups(actionText);
										break;
									case ApiExecutionAction.UpsertUsers:
										commandText = indexUpsertUsers();
										break;
									default:
										commandText = "";
										break;
								}
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
					companyConnection.CloseIfOpened();
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
			{maxVersionSql("p.Object", "p.ObjectId")};";

			// Record field history using the audit Ids garnered above.
			commandText += $@"
{INSERT_FIELD_SQL}
	select	tt.ID as AuditID,
			coalesce(f.FieldTypeID, 0),
			f.FieldName,
			coalesce(fv.FormattedValue, f.FieldValue),
			pv.[Value] as PreviousValue
	from	api.ExecutionLog a
			cross apply openjson(a.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
			inner join @tbl tt on tt.Object = p.Object and tt.ObjectID = p.ObjectID
			inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id and a.SubTask is null
			inner join api.ExecutionField f on f.ExecutionID = e.ExecutionID and f.ItemNumber = p.ItemNumber and f.FieldTypeID not in (select ID from FieldType where ID = f.FieldTypeID and [Type] in ('Relationship'))
			outer apply (
						select	utility.GetFormattedFieldLookupValueWithMultiple([Type], LookupDisplayFormat, LookupObjectType, LookupObjectID, f.LookupValue, AllowMultipleValues) as FormattedValue
						from	FieldType
						where	ID = f.FieldTypeID
								and [Type] = 'Lookup'
								and ISNUMERIC(f.LookupValue) = 1
						) fv
			outer apply {previousValueCrossApplySql("p.Object", "p.ObjectId", "f.FieldName")} pv
	where	((coalesce(pv.Value,'') = '' and  coalesce(fv.FormattedValue, f.FieldValue,'') != '') 
			or (coalesce(fv.FormattedValue, f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";

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
where	l.ExecutionId = @Id;";

			// Calculate any formatted values we will use to update the fields.
			commandText += $@"
select	ID as FieldtypeId,
		cast(ObjectId as nvarchar(255)) ObjectIdPrefix,
		cast(ObjectId as nvarchar(4000)) ObjectId,
		utility.GetFormattedFieldLookupValueWithMultiple(Type, LookupDisplayFormat, LookupObjectType, LookupObjectID, ObjectId, AllowMultipleValues) as FormattedValue
into	#formattedValues
from	#relyingFieldTypes;

create clustered index idx_formattedValues on #formattedValues (FieldtypeId,ObjectIdPrefix);

";

			// Get Required Field from Field table.
			commandText += $@"
drop table if exists #tempField;

select	fv.FieldtypeId as FieldtypeId,
		f.AssetID as AssetID,
		f.ID as FieldID,
		fv.FormattedValue,
		case when coalesce(fv.FormattedValue,'') = coalesce(f.FormattedValue,'') then 1 else 0 end IsMatch
into	#tempField
from	#formattedValues fv
inner join Field F on fv.FieldtypeId = F.FieldtypeId and fv.ObjectIdPrefix = substring(F.[Value], 1, 255) and Fv.ObjectId = F.[Value]

create clustered index idx_tempField on #tempField (FieldID);

";

			// Now update the lookup fields themselves.
			commandText += $@"
UPDATE	F
SET		F.FormattedValue = FT.FormattedValue
from	Field F
		inner join #tempField FT on FT.FieldID = F.ID and FT.IsMatch = 0;";

			// Get the list of asset/field combinations we updated above to record history for them.
			commandText += $@"
select	F.AssetID,
		A.Object,
		A.ObjectID,
		A.TypeName,
		A.DisplayValue as ObjectName,
		T.Name as FieldName,
		F.FieldTypeID,
		F.FormattedValue as FieldValue
into	#fields
from	#tempField F
		inner join FieldType T on T.ID = F.FieldTypeID
		inner join AssetDetail A on A.ID = F.AssetID;";

			// Clear out the audit header temp table variable from where we used it above. Using it again here.
			commandText += $@"
delete @tbl;";

			// Add the audit history header records for the asset that rely on the first set of assets.
			// Only when updated field type affected display value of lookup field on target asset
			commandText += $@"
select distinct ft.ID, ft.Name
into #updatedFieldTypeIds
from api.Execution e
	inner join api.ExecutionAsset ea on ea.ExecutionID = e.ExecutionID
	inner join api.ExecutionField ef on e.ExecutionID = ef.ExecutionID
	inner join Field f on f.AssetID = ea.AssetID and f.FieldTypeID = ef.FieldTypeID
	inner join FieldType ft on ft.ID = f.FieldTypeID
where e.Id = @id and f.UpdatedOn > e.StartedOn

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
	from	#fields F
			inner join FieldType ft on ft.ID = F.FieldtypeId
			inner join #updatedFieldTypeIds uft on ft.LookupDisplayFormat like '%'+uft.Name+'%'
			{maxVersionSql("F.Object", "F.ObjectId")};";

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
	or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'') COLLATE SQL_Latin1_General_CP1_CS_AS));";

			// Add the field history records for the assets whose lookup fields we updated.
			commandText += $@"
			drop table if exists #relyingFieldTypes;
			drop table if exists #formattedValues;
			drop table if exists #tempField;
			drop table if exists #fields;
			drop table if exists #updatedFieldTypeIds;
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

		#region Index Generators

		string indexDeleteAssets()
		{
			return $@"
insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [AssetID])
	select	'ObjectIndex',
			'D',
			p.Object, 
			p.ObjectId,
			p.AssetId
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (AssetId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
	where l.ExecutionId = @Id;";
		}

		string indexDeleteGroups()
		{
			return $@"
insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [AssetID])
	select	'ObjectIndex',
			'D',
			'Group', 
			p.ID,
			p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, AssetId bigint) p
	where l.ExecutionId = @Id;";
		}

		string indexPatchCatalog()
		{
			return @"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', 
			case 
				when l.IsDelete = 1 then 'D'
				when l.IsDelete = 0 and l.[Action] = 'A' then 'A'
				else 'U'
			end, 
			a.Object, 
			a.ObjectId, 
			@dt, 
			a.Id
	from	api.ExecutionCatalogItem l
			inner join Asset a on a.Id = l.Id and l.ExecutionId = @Id and l.[Type] = 'A' and l.Success = 1 and l.IsDelete = 0";
		}

		string indexUpsertAssets(string actionText)
		{
			return $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', '{actionText}', p.Object, p.ObjectId, @dt, coalesce(p.AssetId, 0)
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (AssetId bigint, Object varchar(50), ObjectId int) p
	where	l.ExecutionId = @Id;";
		}

		string indexUpsertGroups(string actionText)
		{
			return $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', '{actionText}', 'Group', p.ID, @dt, p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, AssetId bigint) p
	where	l.ExecutionId = @Id;";
		}

		string indexUpsertUsers()
		{
			return $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', iif(p.IsNew = 1, 'A', 'U'), 'Resource', p.ObjectId, @dt, p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ObjectId int, AssetId bigint, IsNew bit) p
	where	l.ExecutionId = @Id;";
		}

		#endregion
	}
}
