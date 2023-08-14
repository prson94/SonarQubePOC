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
		TelemetryClient Telemetry;

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
        public async Task Run([QueueTrigger("%AssetGraphQueue%", Connection = "AzureWebJobsQueueStorageAccount")] string message, ILogger log, ExecutionContext context)
        {
			//var metric = Telemetry.GetMetric("PostExecution");

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

					switch (request.Action)
					{
						case PostExecutionQueueMessageAction.History:
							string insertStatement = "insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)";
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
			'Deleted', 
			p.Object, 
			p.ObjectId,
			p.TypeName, 
			p.ObjectName, 
			'This asset has been removed.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
	where l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.PostAssets:
								case ApiExecutionAction.PutAssets:
									var actionText = execution.Action == ApiExecutionAction.PostAssets ? "Created" : "Updated";
									commandText = $@"
declare @tbl table (ID bigint, Object varchar(50), ObjectID int)
{insertStatement}
output inserted.ID, inserted.Object, inserted.ObjectID into @tbl
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			'{actionText}', 
			p.Object, 
			p.ObjectId,
			p.TypeName, 
			p.ObjectName, 
			'This asset has been {actionText.ToLower()}.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
	where l.ExecutionId = @Id;

insert into reporting.Global_FieldAudit (AuditID, FieldTypeID, FieldName, [Version], [Value], PreviousValue)
	select	tt.ID as AuditID,
			f.FieldTypeID,
			f.FieldName,
			coalesce(pv.[Version],0)+1 as [Version],
			f.FieldValue, --f.LookupValue,
			--pv.[Version] as PreviousVersion,
			pv.[Value] as PreviousValue
from	api.ExecutionLog a
		cross apply openjson(a.Payload) with (ItemNumber int, AssetId bigint, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
		inner join @tbl tt on tt.Object = p.Object and tt.ObjectID = p.ObjectID
		inner join api.Execution e on e.Id = a.ExecutionId
		inner join api.ExecutionField f on f.ExecutionID = e.ExecutionID and f.ItemNumber = p.ItemNumber
		outer apply (
			select	top 1
					ROW_NUMBER() OVER (PARTITION BY i_a.Object, i_a.ObjectID, i_p.FieldTypeID ORDER BY i_p.[Version] DESC) as RowNum,
					[Version],
					[Value]
			from	reporting.Global_FieldAudit i_p
					inner join reporting.Global_Audit i_a on i_a.ID = i_p.AuditID and i_a.Object = p.Object and i_a.ObjectID = p.ObjectID and i_p.FieldTypeID = f.FieldTypeID
		) pv
where	a.ExecutionId = @Id and pv.Value is null or (f.FieldValue <> pv.Value)";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = $@"
{insertStatement}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			'Deleted', 
			'Intersect',
			p.IntersectId,
			p.TypeName, 
			'Relationship', 
			'This relationship has been removed.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
	where l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = $@"
{insertStatement}
	select	p.Object, 
			p.ObjectId,
			p.ObjectName, 
			@r, 
			@dt, 
			'Deleted', 
			'Intersect',
			p.IntersectId,
			p.TypeName, 
			'Relationship', 
			'$IntersectName created.' 
	from	api.ExecutionLog l
			inner join api.Execution e on e.Id = l.ExecutionId 
			cross apply openjson(l.Payload) with (IntersectId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
	where l.ExecutionId = @Id;";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PostAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostCrossReferences:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
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
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.PutAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectID, ObjectID from api.ExecutionDeletedRelationship";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.DeleteAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostCrossReferences:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteUsers:
									commandText = "";
									break;
							}
							break;
						case PostExecutionQueueMessageAction.Scoring:
							switch (execution.Action)
							{
								case ApiExecutionAction.DeleteAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.PutAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectID, ObjectID from api.ExecutionDeletedRelationship";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.DeleteAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostCrossReferences:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteUsers:
									commandText = @"";
									break;
							}
							break;
						case PostExecutionQueueMessageAction.Workflow:
							switch (execution.Action)
							{
								case ApiExecutionAction.DeleteAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.PutAssets:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionAsset";
									break;
								case ApiExecutionAction.DeleteRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectID, ObjectID from api.ExecutionDeletedRelationship";
									break;
								case ApiExecutionAction.PostRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.PutRelationships:
									commandText = @"select Uid, IntersectID as ID, SubjectAssetID, ObjectAssetID from api.ExecutionRelationship";
									break;
								case ApiExecutionAction.DeleteAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutAssetTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostCrossReferences:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.UpsertUsers:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PatchCatalog:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutGroups:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PostResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutResponsibilityTypes:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.PutDataQualityResults:
									commandText = @"select Uid, AssetID as ID, Object, ObjectID from api.ExecutionDeletedAsset";
									break;
								case ApiExecutionAction.DeleteUsers:
									commandText = @"";
									break;
							}
							break;
					}

					if (!string.IsNullOrEmpty(commandText))
					{ 
						await companyConnection.ExecuteAsync(commandText, new { execution.Id, r = execution.ResourceID, dt = execution.ProcessingStartedOn ?? execution.StartedOn }, commandTimeout: 600);
					}				
				}
				companyConnection.CloseIfOpened();
			}

			Telemetry.TrackTrace($"PostExecutionJobProcessor processed message:  {message}", SeverityLevel.Information);
			Telemetry.Flush();
		}


	}
}
