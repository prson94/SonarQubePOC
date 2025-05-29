using d360.core.entities;
using d360.core.resources;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Workflow
	{
		public async Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetTypeAsync(Guid uid)
		{
			try
			{
				var dbArgs = new DynamicParameters();
				string whereClause = " where T.uid= @uid";
				dbArgs.Add("@uid", uid);

				string sql = $@"select I.uid,I.Name,I.Description,I.IsSystem,I.UpdatedOn
				from IssueTypeRelation R
				inner join AssetType T on T.ID = R.AssetTypeID
				inner join IssueType I on I.ID = R.IssueTypeID
				{whereClause}";

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var allocations = await connection.QueryAsync<IssueTypeApiModel>(sql, dbArgs, commandTimeout: CommandTimeout);
					return allocations;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<Issue> GetIssueByUIDAsync(Guid issueUid)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
				{
					var result = await connection.QuerySingleOrDefaultAsync<Issue>(@"
					SELECT * from dbo.Issue i
					WHERE i.Uid = @issueUid
					", new { issueUid });

					return result;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<IssueType> GetIssueTypeByUIDAsync(Guid issueTypeUid)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var result = await connection.QuerySingleOrDefaultAsync<IssueType>(@"
					SELECT * from dbo.IssueType i
					WHERE i.uid = @issueTypeUid
					", new { issueTypeUid });

					return result;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<IEnumerable<IssueTypeApiModel>> GetIssueTypesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			try
			{
				var dbArgs = new DynamicParameters();

				bool hasResourceParam = false;
				bool hasAssetParam = false;
				bool limitToActiveWorkflows = false;
				bool hasAssignments = false;

				List<string> issueConditions = new List<string>();
				List<string> assetConditions = new List<string>();

				List<string> issueJoins = new List<string>();
				List<string> assetJoins = new List<string>();

				Guid assetTypeUid = Guid.Empty;
				Guid assetUid = Guid.Empty;

				var assetSql = "";
				var resourceSql = "";
				var issueTypeSql = "";

				var orderBySql = $"Order by Name";

				var baseIssueTypesSql = $@"Select
										IT.Uid,
										IT.Name,
										IT.Description,
										IT.IsSystem,
										IT.UpdatedOn,
										UpdatedBy.Uid as UpdatedByUid,
										ADV_Created.DisplayValue as UpdatedByName
									from
										IssueType IT
										left join Asset UpdatedBy on UpdatedBy.ObjectID = IT.UpdatedBy and UpdatedBy.Object = 'Resource'
										left join AssetDisplayValue ADV_Created on ADV_Created.AssetID = UpdatedBy.ID";

				var workflowSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1)";

				var assetCondition = $@"(E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') IS NULL)
									OR ((E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') = AT.[Object]
									AND E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') = AT.ObjectID))";

				var workflowObjectSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1
									WHERE {assetCondition})";

				issueTypeSql = baseIssueTypesSql;

				assetConditions.Add("1 = 1");
				issueConditions.Add("1 = 1");

				var limitToActiveWorkflowsParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_limittoactiveworkflows", StringComparison.OrdinalIgnoreCase));
				var resourceUidParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_resourceuid", StringComparison.OrdinalIgnoreCase));
				var assetTypeUidParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_assettypeuid", StringComparison.OrdinalIgnoreCase));
				var assetUidParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_assetuid", StringComparison.OrdinalIgnoreCase));
				var actionTypeUidParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_actiontypeuid", StringComparison.OrdinalIgnoreCase));
				var nameParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_name", StringComparison.OrdinalIgnoreCase));
				var hasAssignmentsParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_hasassignments", StringComparison.OrdinalIgnoreCase));
				var hasAnyAssignmentsParam = queryParams.FirstOrDefault(x => string.Equals(x.Key.Trim(), "_hasanyassignments", StringComparison.OrdinalIgnoreCase));

				hasAssetParam = !string.IsNullOrWhiteSpace(assetTypeUidParam.Value) || !string.IsNullOrWhiteSpace(assetUidParam.Value);

				var hasAnyAssignments = false;

				if (hasAnyAssignmentsParam.Key != null)
				{
					if (hasAnyAssignmentsParam.Value != null && !string.IsNullOrWhiteSpace(hasAnyAssignmentsParam.Value) && !bool.TryParse(hasAnyAssignmentsParam.Value, out hasAnyAssignments))
					{
						throw new ArgumentException(Error.InvalidHasAnyAssignments);
					}
				}

				if (hasAssignmentsParam.Key != null)
				{
					if (hasAssignmentsParam.Value != null && !string.IsNullOrWhiteSpace(hasAssignmentsParam.Value) && !bool.TryParse(hasAssignmentsParam.Value, out hasAssignments))
					{
						throw new ArgumentException(Error.InvalidHasAssignments);
					}
				}

				var assignmentsSql = $@"SELECT 1
									  FROM [workflow].[EventRegistration] E
									  inner join workflow.Version V on E.TypeID=V.TypeID
									  inner join workflow.item wi on wi.VersionID = v.ID {(hasAnyAssignments ? "" : "and CompletedOn is null")}
									  {(string.IsNullOrWhiteSpace(assetUidParam.Value) && string.IsNullOrWhiteSpace(assetTypeUidParam.Value) ? "" :
										  $@"left join asset A2 on WI.Object <> 'Issue' and A2.object = WI.object and WI.objectID=A2.objectID AND {(!string.IsNullOrWhiteSpace(assetUidParam.Value) ? "A2.ID = A.ID" : "")} {(!string.IsNullOrWhiteSpace(assetTypeUidParam.Value) ? "A2.AssetTypeID = AT.ID" : "")}
									  left join issue I on WI.object = 'Issue' and WI.objectID=I.ID and I.AssetID = A.ID")}
									where
										E.[Object] = 'IssueType' and E.ObjectID = IT.ID";

				#region Action Type

				if (actionTypeUidParam.Key != null)
				{
					if (actionTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(actionTypeUidParam.Value) && (Guid.TryParse(actionTypeUidParam.Value, out Guid actionTypeUid) && actionTypeUid != Guid.Empty))
					{
						issueConditions.Add("IT.uid = @actionTypeUid");
						assetConditions.Add("IT.uid = @actionTypeUid");
						dbArgs.Add("@actionTypeUid", actionTypeUid);
					}
					else
					{
						throw new ArgumentException(Error.InvalidActionUid);
					}
				}

				#endregion Action Type

				#region Asset Type

				if (assetTypeUidParam.Key != null)
				{
					if (assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value) && (!Guid.TryParse(assetTypeUidParam.Value, out assetTypeUid) || assetTypeUid == Guid.Empty))
					{
						throw new ArgumentException(Error.Invalid);
					}
				}

				#endregion Asset Type

				#region Asset

				if (assetUidParam.Key != null)
				{
					if (assetUidParam.Value != null && !string.IsNullOrWhiteSpace(assetUidParam.Value) && (!Guid.TryParse(assetUidParam.Value, out assetUid) || assetUid == Guid.Empty))
					{
						throw new ArgumentException(Error.InvalidAssetUid);
					}
				}

				#endregion Asset

				#region Name

				if (nameParam.Key != null)
				{
					if (!string.IsNullOrWhiteSpace(nameParam.Value))
					{
						issueConditions.Add("IT.Name = @name");
						assetConditions.Add("IT.Name = @name");
						dbArgs.Add("@name", nameParam.Value);
					}
				}

				#endregion Name

				#region Limit By Active Workflows

				if (limitToActiveWorkflowsParam.Key != null)
				{
					if (limitToActiveWorkflowsParam.Value != null && !string.IsNullOrWhiteSpace(limitToActiveWorkflowsParam.Value) && bool.TryParse(limitToActiveWorkflowsParam.Value, out limitToActiveWorkflows))
					{
						if (limitToActiveWorkflows)
						{
							var activeWorkflowSql = hasAssetParam ? workflowObjectSql : workflowSql;

							issueConditions.Add(activeWorkflowSql);
							assetConditions.Add(workflowObjectSql);
						}
					}
					else
					{
						throw new ArgumentException(Error.InvalidLimitProvided);
					}
				}

				#endregion Limit By Active Workflows

				#region Limit By Actions with open assignments

				if (hasAssignments || hasAnyAssignments)
				{
					var assetAssignmentsSQL = $@"exists ({assignmentsSql} and ({assetCondition}) {(assetTypeUid != Guid.Empty || assetUid != Guid.Empty ? "and (A2.Id is not null or I.assetID is not null)" : "")})";
					var issueAssignmentsSql = hasAssetParam ? assetAssignmentsSQL : $@"exists({assignmentsSql})";

					issueConditions.Add(issueAssignmentsSql);
					assetConditions.Add(assetAssignmentsSQL);
				}

				#endregion Limit By Actions with open assignments

				#region Asset, Asset Type and Resource

				if (actionTypeUidParam.Key != null)
				{
					issueTypeSql = $@"{baseIssueTypesSql}
								  {string.Join("\n", issueJoins)}
								  where {string.Join(" AND ", issueConditions)}";
				}

				if (assetTypeUidParam.Key != null || assetUidParam.Key != null || resourceUidParam.Key != null)
				{
					assetJoins.Add("inner Join IssueTypeRelation ITR on IT.ID = ITR.IssueTypeID");
					assetJoins.Add("inner Join AssetType AT on AT.ID = ITR.AssetTypeID");

					issueJoins.Add("cross apply (select count(*) as Allocations from IssueTypeRelation R where R.IssueTypeID = IT.ID) C");

					if (assetTypeUid != Guid.Empty)
					{
						issueJoins.Add("left join AssetType AT on AT.uid = @assetTypeUid");
						issueJoins.Add("left join Asset A on A.AssetTypeID = AT.ID");
					}
					else if (assetUid != Guid.Empty)
					{
						issueJoins.Add("left join Asset A on A.uid = @assetUid");
						issueJoins.Add("left join AssetType AT on AT.ID = A.AssetTypeID");
					}

					issueConditions.Add("C.Allocations = 0");

					issueTypeSql = $@"{baseIssueTypesSql}
								  {string.Join("\n", issueJoins)}
								  where {string.Join(" AND ", issueConditions)}";

					if (assetTypeUid != Guid.Empty)
					{
						assetConditions.Add("AT.Uid = @assetTypeUid");
						dbArgs.Add("@assetTypeUid", assetTypeUid);
					}

					if (assetUid != Guid.Empty)
					{
						assetConditions.Add("A.Uid = @assetUid");
						dbArgs.Add("@assetUid", assetUid);
					}

					if (resourceUidParam.Key != null && !string.IsNullOrWhiteSpace(resourceUidParam.Value))
					{
						if (Guid.TryParse(resourceUidParam.Value, out Guid resourceUid))
						{
							if (resourceUid != Guid.Empty)
							{
								hasResourceParam = true;
								dbArgs.Add("@resourceUid", resourceUid);
							}
						}
						else
						{
							throw new ArgumentException(Error.InvalidResourceUID);
						}
					}

					if (hasAssetParam || hasResourceParam)
					{
						assetJoins.Add("inner Join Asset A on A.AssetTypeID = AT.ID");
						if (hasResourceParam)
						{
							resourceSql = $@"UNION
								{baseIssueTypesSql}
								{string.Join("\n", assetJoins)}
								inner Join IssueTypeRelationResponsibility RR on ITR.ID = RR.IssueTypeRelationID
								inner join ResponsibilityDetail RD on RD.ResponsibilityTypeID = RR.ResponsibilityTypeId and RD.ResourceUid = @resourceUid and ((RD.AssetID = A.ID) or (RD.AssetTypeID = A.AssetTypeID and RD.AssetID = 0))
								where {string.Join(" AND ", assetConditions)}";

							assetConditions.Add("RR.ID is null");

							assetJoins.Add("left join IssueTypeRelationResponsibility RR on RR.IssueTypeRelationID = ITR.ID");
						}

						assetSql = $@" UNION
								{baseIssueTypesSql}
								{string.Join("\n", assetJoins)}
								where {string.Join(" AND ", assetConditions)}";
					}
				}
				else if (issueConditions.Any())
				{
					issueTypeSql = $@"{baseIssueTypesSql}
								  {string.Join("\n", issueJoins)}
								  where {string.Join(" AND ", issueConditions)}";
				}

				#endregion Asset, Asset Type and Resource

				var sql = $@"{issueTypeSql}
						 {assetSql}
						 {resourceSql}
						 {orderBySql}";

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					return await connection.QueryAsync<IssueTypeApiModel>(sql, dbArgs, commandTimeout: CommandTimeout);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<IEnumerable<dynamic>> GetIssuesByUserAsync(int? CurrentUserId)
		{
			try
			{
				var sql = string.Format(@"
									select		distinct
												null as WorkflowID
												,wi.ID as WorkflowItemID
												,c.Body
												,I.CommentID as CommentID
												,I.CreatedBy as RaisedByResourceID
												,wi.StartedOn as DateStarted
												,wi.CompletedOn as DateCompleted
												,case when wi.CompletedOn is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted
												,'' as Step
												,coalesce(D.ObjectID, T.ObjectID) as ObjectID
												,coalesce(D.DisplayValue, T.[Name]) as [Name]
												,coalesce(D.[Object], T.[Object]) as [Object]
												,coalesce(DUrl.[Url], TUrl.[Url]) as [Url]
												,R.FirstName + ' ' + R.LastName as RaisedBy			
												,'' as Notes					
												,IT.ID as IssueType
												,IT.Name as IssueTypeName
												,I.ID as IssueID
												,case when wi.CompletedOn is null then datediff(day,wi.StartedOn,GetUtcDate()) else datediff(day, wi.StartedOn, wi.CompletedOn) end as EllapsedDays
												,case 
													when wi.CompletedOn is not null then 'Closed'
													else
														case cast(coalesce(IA.ResourceObjectID, 0) as bit)

															when 1 then 'Pending'
															else 'Waiting on user(s)'

														end

												end as ActivityName
									from	    Issue I
												inner join [workflow].item wi on (wi.[object] = 'Issue' and wi.[objectid] = i.id)
												inner join workflow.itemstep si on si.itemid = wi.id
												inner join IssueType IT on (I.IssueTypeID = IT.ID)							
												left join AssetDetail D on D.[ID] = I.AssetID
												outer apply [dbo].[GetAssetUrlById](D.ID) DUrl
												left join AssetType T on T.ID = I.AssetTypeID
												outer apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl
												left outer join reporting.Global_Resource R on R.ResourceID = I.CreatedBy
												left outer join Comment C on C.ID = I.CommentID
												left join workflow.ItemAssignment IA on IA.ItemID = wi.ID and IA.ResourceObject = 'Resource' {0}
									order by wi.StartedOn desc",
												CurrentUserId > 0 ? $"and IA.ResourceObjectID = {CurrentUserId}" : "");


				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var result = await connection.QueryAsync<IEnumerable<dynamic>>(sql);
					return result;
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}