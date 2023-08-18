using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.model;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
	public class PostExecutionJobProcessor
	{
		readonly TelemetryClient Telemetry;

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

		[FunctionName("PostExecutionJobProcessor")]
		[ExponentialBackoffRetry(5, "00:00:10", "00:15:00")]
		public async Task Run([QueueTrigger("%AssetGraphQueue%", Connection = "AzureWebJobsQueueStorageAccount")] string message, ILogger log, ExecutionContext context)
        {
			var request = message.AsObject<PostExecutionQueueMessage>();

			Telemetry.Context.User.AccountId = $"{request.CompanyID}";

			string companyConnectionString = GetCompanyConnectionString(request.CompanyID);
			string commandText = "";

			using (var companyConnection = new SqlConnection(companyConnectionString))
			{
				await companyConnection.OpenIfClosed();
				
				var execution = await companyConnection.QueryFirstAsync<ApiExecution>("select * from api.Execution where Id = @id", new { id = request.ExecutionId });

				if (execution != null) 
				{
					Telemetry.Context.User.AuthenticatedUserId = $"{execution.ResourceID}";
					string actionText = "";

					switch (request.Action)
					{
						case PostExecutionQueueMessageAction.History:
							string insertStatement = "insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, [Version], Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)";
							string insertFieldStatement = "insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Value], PreviousValue)";
							Func<string, string, string> maxVersionSql = (objectColumn, objectIdColumn) =>
							{
								return $"cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = {objectColumn} and ObjectID = {objectIdColumn}) mv";
							};

							Func<string, string, string, string> previousValueCrossApplySql = (objectColumn, objectIdColumn, fieldNameColumn) => {
								return $@"(select top 1
		ROW_NUMBER() OVER (PARTITION BY i_a.Object, i_a.ObjectID, iif(i_p.FieldTypeID = 0, i_p.FieldName, i_p.FieldTypeID) ORDER BY i_p.[AuditId] DESC) as RowNum,
		[Value]
from	reporting.Global_FieldAudit i_p
		inner join reporting.Global_Audit i_a on i_a.ID = i_p.AuditID and i_a.Object = {objectColumn} and i_a.ObjectID = {objectIdColumn} and ( (i_p.FieldTypeID = f.FieldTypeID and f.FieldTypeID <> 0) or (i_p.FieldName = {fieldNameColumn} and f.FieldTypeID = 0) ))";
							};

							switch (execution.Action)
							{
								case ApiExecutionAction.DeleteAssets:
									commandText = $@"
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
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = $@"
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
									break;
								case ApiExecutionAction.PostGroups:
								case ApiExecutionAction.PutGroups:
									actionText = execution.Action == ApiExecutionAction.PostGroups ? "Created" : "Updated";
									commandText = $@"
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
			'This group has been {actionText.ToLower()}.' 
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, ItemNumber int, ObjectName nvarchar(250), Description nvarchar(max), IsActiveDirectoryGroup bit, PrimaryOwnerResourceID int, SecondaryOwnerResourceID int) p 
			{maxVersionSql("'Group'", "p.ID")}
	where	l.ExecutionId = @Id;

{insertFieldStatement}
	select	tt.ID as AuditID,
			0,
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
where	pv.Value is null or (f.FieldValue <> pv.Value);";
									break;
								case ApiExecutionAction.PostAssets:
								case ApiExecutionAction.PutAssets:
									actionText = execution.Action == ApiExecutionAction.PostAssets ? "Created" : "Updated";
									
									// Record history for assets we are creating/updating.
									commandText = $@"
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
			'This asset has been {actionText.ToLower()}.' 
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
			f.FieldValue,
			pv.[Value] as PreviousValue
	from	api.ExecutionLog a
			cross apply openjson(a.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
			inner join @tbl tt on tt.Object = p.Object and tt.ObjectID = p.ObjectID
			inner join api.Execution e on e.Id = a.ExecutionId and e.Id = @Id and a.SubTask is null
			inner join api.ExecutionField f on f.ExecutionID = e.ExecutionID and f.ItemNumber = p.ItemNumber
			outer apply {previousValueCrossApplySql("p.Object", "p.ObjectId", "f.FieldName")} pv
	where	pv.Value is null or (coalesce(f.LookupValue, f.FieldValue) <> pv.Value);";

									// Record the relationship chanegs via any relation fields on the assets above.
									commandText += $@"
{insertStatement}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			mv.[Version],
			iif(p.[Action] = 'D', 'Deleted', 'Created'), 
			'Intersect',
			p.IntersectId,
			p.TypeName, 
			'Relationship', 
			'This relationship has been ' + iif(p.[Action] = 'D', 'removed', 'created') + '.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId and e.Id = @Id
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250), [Action] char(1)) p 
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

									// Gett the list of asset/field combinations we updated above to record history for them.
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
	where	pv.Value is null or f.FieldValue <> pv.Value;";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = $@"
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
									break;
								case ApiExecutionAction.PostRelationships:
								case ApiExecutionAction.PutRelationships:
									commandText = $@"
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
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = $@"
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
where	pv.Value is null or ( coalesce(cast(f.ValueId as nvarchar), f.Value) <> pv.Value );";
									break;
								case ApiExecutionAction.UpsertUsers:
									actionText = execution.Action == ApiExecutionAction.PostGroups ? "Created" : "Updated";
									commandText = $@"
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
			0,
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
where	pv.Value is null or (f.FieldValue <> pv.Value);";
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
									commandText = $@"
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
									break;
								case ApiExecutionAction.PostAssets:
								case ApiExecutionAction.PutAssets:
									actionText = execution.Action == ApiExecutionAction.PostAssets ? "A" : "U";
									commandText = $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', '{actionText}', p.Object, p.ObjectId, @dt, p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (AssetId bigint, Object varchar(50), ObjectId int) p
	where	l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = @"
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
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = $@"
insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [AssetID])
	select	'ObjectIndex',
			'D',
			'Group', 
			p.ID,
			p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, AssetId bigint) p
	where l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.PostGroups:
								case ApiExecutionAction.PutGroups:
									actionText = execution.Action == ApiExecutionAction.PostGroups ? "A" : "U";
									commandText = $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', '{actionText}', 'Group', p.ID, @dt, p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ID int, AssetId bigint) p
	where	l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = $@"
insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
	select	'ObjectIndex', iif(p.IsNew = 1, 'A', 'U'), 'Resource', p.ObjectId, @dt, p.AssetId
	from	api.ExecutionLog l
			cross apply openjson(l.Payload) with (ObjectId int, AssetId bigint, IsNew bit) p
	where	l.ExecutionId = @Id;";
									break;
							}
							break;
						case PostExecutionQueueMessageAction.Scoring:
							switch (execution.Action)
							{
								case ApiExecutionAction.DeleteAssets:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssets:
									commandText = "";// @"Table api.ExecutionAsset";
									break;
								case ApiExecutionAction.PutAssets:
									commandText = "";// @"Table api.ExecutionAsset";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = "";// @"Table api.ExecutionDeletedRelationship";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = "";// @"Table api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = "";// @"Table api.ExecutionRelationship";
									break;
								case ApiExecutionAction.DeleteAssetTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssetTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutAssetTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostCrossReferences:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostGroups:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutGroups:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutResponsibilityTypes:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteUsers:
									commandText = "";
									break;
								default:
									commandText = "";
									break;
							}
							break;
						case PostExecutionQueueMessageAction.Workflow:
							switch (execution.Action)
							{
								case ApiExecutionAction.DeleteAssets:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssets:
									commandText = "";// @"Table api.ExecutionAsset";
									break;
								case ApiExecutionAction.PutAssets:
									commandText = "";// @"Table api.ExecutionAsset";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = "";// @"Table api.ExecutionDeletedRelationship";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = "";// @"Table api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = "";// @"Table api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataQualityResults:
									commandText = "";// @"Table api.ExecutionDeletedAsset";
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
						await companyConnection.ExecuteAsync(commandText, new { execution.Id, r = execution.ResourceID, dt = execution.ProcessingStartedOn ?? execution.StartedOn }, commandTimeout: 1800);
					}				
				}
				companyConnection.CloseIfOpened();
			}

			Telemetry.TrackTrace($"PostExecutionJobProcessor processed message:  {message}", SeverityLevel.Information);
			Telemetry.Flush();
		}


	}
}
