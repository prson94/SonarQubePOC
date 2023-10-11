using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.model;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
	public class PostExecutionJobProcessor
	{
		readonly TelemetryClient Telemetry;
		readonly string insertStatement = "insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, [Version], Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)";
		readonly string insertFieldStatement = "insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Value], PreviousValue)";

		public PostExecutionJobProcessor(TelemetryClient telemetry)
		{
			Telemetry = telemetry;
		}

		string GetCompanyConnectionString(int companyID)
		{
			string connectionString = "";
			
			using (var cnn = new SqlConnection(Environment.GetEnvironmentVariable("CommunityContext")))
			{
				if (cnn.State != System.Data.ConnectionState.Open)
				{
					cnn.Open();
				}

				var company = cnn.Query<dynamic>(
					@"select  ds.Server, ds.Username, ds.Password from company c inner join databaseserver ds on c.databaseserverid = ds.id and c.Id = @companyID",
					new { companyID }
				).FirstOrDefault();

				if (company != null)
				{
					connectionString = CompanyConnectionStringHelper.ConnectionString(companyID, company.Server, company.Username, company.Password);
				}
			}

			return connectionString;
		}

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

		[FunctionName("PostExecutionJobProcessor")]
		[ExponentialBackoffRetry(5, "00:00:10", "00:15:00")]
		public async Task Run([QueueTrigger("%AssetGraphQueue%", Connection = "AzureWebJobsQueueStorageAccount")] string message, ILogger log, ExecutionContext context)
        {
			var request = message.AsObject<PostExecutionQueueMessage>();

			string companyConnectionString = GetCompanyConnectionString(request.CompanyID);
			string commandText = "";

			using (var companyConnection = new SqlConnection(companyConnectionString))
			{
				await companyConnection.OpenIfClosed();
				
				var execution = await companyConnection.QueryFirstAsync<ApiExecution>("select * from api.Execution where Id = @id", new { id = request.ExecutionId });

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
							Telemetry.Context.User.AccountId = $"{request.CompanyID}";
							Telemetry.Context.User.AuthenticatedUserId = $"{execution.ResourceID}";
							Telemetry.TrackException(ex, new Dictionary<string, string>{
									{ "ExecutionId", $"{execution.Id}" },
									{ "RequestAction", $"{request.Action}" },
									{ "ExecutionAction", $"{execution.Action}" }
								});
						}
					}				
				}
				companyConnection.CloseIfOpened();
			}

			Telemetry.TrackTrace($"PostExecutionJobProcessor processed message:  {message}", SeverityLevel.Information);
			Telemetry.Flush();
		}

		#region History Generators

		string historyDeleteAssets()
		{
			return $@"
{insertStatement}
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
{insertStatement}
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
{insertStatement}
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
{insertStatement}
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
{insertStatement}
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
{insertStatement}
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

{insertFieldStatement}
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
			or (coalesce(cast(f.ValueId as nvarchar(max)), f.Value,'') <> coalesce(pv.Value,'')));";
		}

		string historyUpsertAssets(string actionText)
		{
			string commandText = "";

			// Record history for assets we are creating/updating.
			commandText += $@"
declare @tbl table (ID bigint, Object varchar(50), ObjectID int)
{insertStatement}
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
{insertFieldStatement}
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
			or (coalesce(fv.FormattedValue, f.FieldValue,'') <> coalesce(pv.Value,'')));";

			// Record the relationship changes via any relation fields on the assets above.
			commandText += $@"
{insertStatement}
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
select	f.ID,
		f.Type, 
		p.ObjectId,
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
		ObjectId,
		utility.GetFormattedFieldLookupValueWithMultiple(Type, LookupDisplayFormat, LookupObjectType, LookupObjectID, ObjectId, AllowMultipleValues) as FormattedValue
into	#formattedValues
from	#relyingFieldTypes;";

			// Now update the lookup fields themselves.
			commandText += $@"
UPDATE	F
SET		F.FormattedValue = FT.FormattedValue
from	Field F
		inner join #formattedValues FT on FT.FieldTypeID = F.FieldTypeID and F.[Value] = FT.ObjectId;";

			// Get the list of asset/field combinations we updated above to record history for them.
			commandText += $@"
select	F.AssetID,
		A.Object,
		A.ObjectID,
		A.TypeName,
		A.DisplayValue as ObjectName,
		T.Name as FieldName,
		F.FieldTypeID,
		V.FormattedValue as FieldValue
into	#fields
from	Field F
		inner join #formattedValues V on V.FieldTypeID = F.FieldTypeID and F.[Value] = V.ObjectId and ISNUMERIC(F.[Value]) = 1
		inner join FieldType T on T.ID = F.FieldTypeID
		inner join AssetDetail A on A.ID = F.AssetID;";

			// Clear out the audit header temp table variable from where we used it above. Using it again here.
			commandText += $@"
delete @tbl;";

			// Add the audit history header records for the asset that rely on the first set of assets.
			commandText += $@"
{insertStatement}
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
			{maxVersionSql("F.Object", "F.ObjectId")};";

			// Add the field history records for the assets whose lookup fields we updated.
			commandText += $@"
{insertFieldStatement}
	select	tt.ID as AuditID,
			coalesce(f.FieldTypeID, 0),
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
	from	#fields f
			inner join @tbl tt on tt.Object = f.Object and tt.ObjectID = f.ObjectID
			outer apply {previousValueCrossApplySql("f.Object", "f.ObjectId", "f.FieldName")} pv
	where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
	or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'')));";

			return commandText;
		}

		string historyUpsertGroups(string actionText)
		{ 
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{insertStatement}
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

{insertFieldStatement}
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
		or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'')));";
		}

		string historyUpsertPredicates()
		{
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{insertStatement}
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

{insertFieldStatement}
	select	tt.ID as AuditID,
			0,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (Id int, Name nvarchar(250), Inverse nvarchar(250), IsNew bit) p
		inner join @tbl tt on tt.ObjectID = p.Id
		cross apply (
					select 0 as FieldTypeID, 'Name' as FieldName, p.Name as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'Inverse' as FieldName, p.Inverse as FieldValue, cast(null as nvarchar(max)) as LookupValue
					) f
		outer apply {previousValueCrossApplySql("'Predicate'", "p.Id", "f.FieldName")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'')));";
		}

		string historyUpsertRelations()
		{ 
			return $@"
{insertStatement}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			iif(p.IsNew = 1, 'Created', 'Updated'), 
			'Intersect',
			p.ActionObjectId,
			p.ActionObjectTypeName, 
			p.ActionObjectName, 
			'Relationship created.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (Object varchar(50), ObjectId int, ObjectName nvarchar(250), ActionObjectId int, ActionObjectName nvarchar(250), ActionObjectTypeName nvarchar(250), IsNew bit) p 
			{maxVersionSql("p.Object", "p.ObjectId")}
	where	l.ExecutionId = @Id;";
		}

		string historyUpsertScoreAllocation() 
		{
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{insertStatement}
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
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (Id int, CalculationMethod nvarchar(250), ScoreType nvarchar(250), IsExternallyCalculated varchar(10), LowerThreshold int, UpperThreshold int, IsNew bit) p 
			{maxVersionSql("'MetricAllocation'", "p.Id")}
	where l.ExecutionId = @Id;

{insertFieldStatement}
	select	distinct
			tt.ID as AuditID,
			0,
			f.FieldName,
			f.FieldValue,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (Id int, CalculationMethod nvarchar(250), ScoreType nvarchar(250), IsExternallyCalculated varchar(10), LowerThreshold int, UpperThreshold int, IsNew bit) p 
		inner join @tbl tt on tt.ObjectID = p.Id
		cross apply (
					select 0 as FieldTypeID, 'CalculationMethod' as FieldName, p.CalculationMethod as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'ScoreType' as FieldName, p.ScoreType as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'IsExternallyCalculated' as FieldName, p.IsExternallyCalculated as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'LowerThreshold' as FieldName, cast(p.LowerThreshold as nvarchar(50)) as FieldValue, cast(null as nvarchar(max)) as LookupValue
					union
					select 0 as FieldTypeID, 'UpperThreshold' as FieldName, cast(p.UpperThreshold as nvarchar(50)) as FieldValue, cast(null as nvarchar(max)) as LookupValue					
					) f
		outer apply {previousValueCrossApplySql("'MetricAllocation'", "p.Id", "f.FieldName")} pv
where	((coalesce(pv.Value,'') = '' and  coalesce(f.FieldValue,'') != '') 
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'')));";
		}

		string historyUpsertUsers()
		{ 
			return $@"
declare @tbl table (ID bigint, ObjectID int)
{insertStatement}
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

{insertFieldStatement}
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
or (coalesce(f.FieldValue,'') <> coalesce(pv.Value,'')));";
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
