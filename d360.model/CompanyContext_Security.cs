using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext 
	{
		#region DbSets

		DbSet<Group> Groups { get; set; }

		DbSet<ResourceGroup> ResourceGroups { get; set; }

		DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

		DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }

		DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

		DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

		DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

		DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

		#endregion

		#region Methods

		Task BulkInsertResponsibilityOverrideAsync(List<BulkResponsibilityOverridePostModel> request, ApiExecution execution, int timeout = 3600);

		List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups);

		string GetNoReadSqlStatement(string identifier = null);

		string GetNoReadSqlStatement(Permission permission, string identifier = null);

		string GetAssetTypeNoReadSqlStatement(string identifier = null);

		string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null);

		string GetThenResultsSql(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction transaction, bool includeName = true, string assetIDColumn = "", bool includeUid = true);

		List<PermissionInfo> GetTypePermissions(string type, int typeID);

		/// <summary>
		/// Derives from SQL Function dbo.UserAssetPermissions which performs slower as we cannot utilize some sql optimizations on sql functions.
		/// </summary>
		/// <param name="tempTableName"></param>
		/// <param name="userParam"></param>
		/// <param name="typeParam"></param>
		/// <returns></returns>
		string GetUserPermissionQuery(string tempTableName, string userParam, string typeParam);

		Task<ResponsibilityWhenQueryData> GetWhenResultsSql(ResponsibilityTypeRelationRule rule, SqlTransaction transaction, bool includeName = true, bool includeUid = true);

		bool HasAssetPermission(long id, Permission permission);

		bool HasAssetPermission(string type, int id, Permission permission);

		bool HasAssetPermission(SystemObjects type, int id, Permission permission);

		bool HasAssetPermissionByUid(Guid uid, Permission permission);

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <param name="permission"></param>
		/// <param name="permission"></param>
		/// <returns></returns>
		bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId);

		bool HasAssetTypePermission(string type, int id, Permission permission);

		bool HasAssetTypePermission(SystemObjects type, int id, Permission permission);

		bool HasAssetTypePermission(int id, Permission permission);

		void ParseResponsibilityRuleModel(Guid executionId, SqlTransaction trans = null, int timeout = 3600, string sourceTable = "api.ExecutionResponsibilityRule");

		/// <summary>
		/// Re-process responsibility rules. By default this will re-process ALL rules unless passing a specific rule ID.
		/// </summary>
		/// <param name="ruleID">Optionally pass a specific rule by its ID.</param>
		Task ProcessResponsibilityRelationRules(int? ruleID = null, int timeout = 7200);

		Task ProcessRulesForExecution(Guid executionId, int beginItemNumber, int endItemNumber);

		void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation);

		List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups);

		Task<List<ResponsibilityRuleUpsertResponseModel>> UpsertResponsibilityRules(ApiExecution execution, Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> import, int timeout = 3600);

		List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(ApiExecution execution, List<ResponsibilityTypeUpsertModel> import, int timeout = 3600);

		#endregion
	}

	public partial class CompanyContext : BaseContext
	{
		#region DbSets

		public DbSet<Group> Groups { get; set; }

		public DbSet<ResourceGroup> ResourceGroups { get; set; }

		public DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

		public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

		public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

		public DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

		public DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

		public DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

		public DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

		#endregion

		#region Utility

		/// <summary>
		/// Default to read unless the user explicitly has no read access to an asset.
		/// </summary>
		private bool HasAssetDefaultReadPermission(string type, int id)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();

				if (assetTypeID <= 0)
				{
					return true; // objects not in asset table we grant permission               
				}

				hasPermission = HasReadPermission(type, id, assetTypeID);
			}

			return hasPermission;
		}
		
		/// <summary>
		/// Used to determine if a user has read permissions on a given asset type.  Read is assumed to be present unless denied.
		/// </summary>        
		/// <param name="assetTypeId"></param>        
		/// <returns></returns>
		private bool HasAssetTypeReadPermission(int assetTypeId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = 0 and ua.AssetTypeID = @t and ua.AssetID = 0)
																						begin
																							select 0;
																						end				                                                                        
																						else
																						begin
																							select 1;
																						end", new { t = assetTypeId, r = CurrentResourceID });
		}
		
		private bool HasPermission(long assetId, int assetTypeId, Permission permission)
		{
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);

			if (isReadPermission)
			{
				Asset asset = Assets.Single(a => a.ID == assetId);

				if (permission == Permission.ReadAsset)
				{
					return HasUserReadPermission(asset.Object, asset.ObjectID, assetTypeId, CurrentResourceID);
				}

				return HasPermission(asset.Object, asset.ObjectID, assetTypeId, permission);
			}
			else
			{
				return Database.Connection.QuerySingle<bool>($@"if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t and ua.AssetId = 0)
																						begin
																							select 1;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetID = @assetId)
																						begin
																							select 1;
																							end
																						else
																						begin
																							select 0;
																						end", new { assetId, t = assetTypeId, r = CurrentResourceID });
			}
		}
		
		private bool HasPermission(string type, int objectId, int assetTypeId, Permission permission)
		{
			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																						begin
																							select 1;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua inner join asset a on(ua.AssetID = a.id and a.Object = @type and a.ObjectID = @id) where ua.PermissionsBitMask & {(int)permission} = {(int)permission})
																						begin
																							select 1;
																							end
																						else
																						begin
																							select 0;
																						end", new { type, id = objectId, t = assetTypeId, r = CurrentResourceID });
		}

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <returns></returns>
		private bool HasReadPermission(string type, int objectId, int assetTypeId)
		{
			return HasUserReadPermission(type, objectId, assetTypeId, CurrentResourceID);
		}

		/// <summary>
		/// Set a rule as having already been processed with the current date / time
		/// </summary>
		/// <param name="cnn"></param>
		/// <param name="ruleId"></param>
		/// <returns></returns>
		private async Task MarkResponsibilityRuleAsRan(int ruleId, SqlTransaction transaction)
		{
			await Connection.ExecuteAsync("update ResponsibilityTypeRelationRule set LastRunOn = @date where ID = @id", new { date = DateTime.UtcNow, id = ruleId }, transaction: transaction);
		}

		private async Task ProcessRuleForAsset(ResponsibilityTypeRelationRule rule, List<ResponsibilityAssetMeasureProcessedResult> results, int timeout = 3600)
		{
			string sqlToExecute = "";
			string declareVar = "";

			using (SqlTransaction transaction = Connection.BeginTransaction())
			{
				try
				{
					string thenSql = GetThenResultsSql(rule, false, transaction, false, "", false);
					var whenQueryData = await GetWhenResultsSql(rule, transaction, false, false).ConfigureAwait(false);
					declareVar = whenQueryData.DeclareVariable;

					thenSql = string.Format(thenSql, "");

					//create impacted assets temporary table.
					sqlToExecute = "create table #changes (ActionType varchar(50), RuleID int, AssetID bigint)";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//merge into the asset table 
					sqlToExecute = $@"
							{declareVar}

							{whenQueryData.TempTableQuery}

							merge [dbo].[ResponsibilityRuleResultAsset] as T
									using	(
												{whenQueryData.SqlQuery}
											) as S
									on		@ruleId = T.RuleID and S.AssetID = T.AssetID
									when	matched then
											update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
									when	not matched by target then
											insert (RuleID, AssetID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetID,getutcdate(),0)
									when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
											delete
									output  $action as ActionType, 
											iif($action = 'DELETE', deleted.RuleID, inserted.RuleID), 
											iif($action = 'DELETE', deleted.AssetID, inserted.AssetID)
									into #changes;";
					whenQueryData.DbParameters.Add("ruleId", rule.ID);
					await Connection.ExecuteAsync(sqlToExecute, whenQueryData.DbParameters, transaction: transaction, commandTimeout: timeout);

					//merge into the resource table
					if (thenSql != null && thenSql.Length > 0)
					{
						sqlToExecute = $@"
							merge [dbo].[ResponsibilityRuleResultSecurityAsset] as T
									using	(
												{thenSql}
											) as S
									on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
									when	matched then
											update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
									when	not matched by target then
											insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
									when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
											delete;";
						await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID, appliesToType = rule.ApplyToType }, transaction: transaction);
					}

					sqlToExecute = @"
									select  A.Uid as AssetUid, 
											M.AllocationUid,
											M.Uid as MetricAssetUid,
											V.Uid as MetricAssetVersionUid
									from    #changes C 
										inner join Asset A on A.ID = C.AssetID and C.ActionType in ('DELETE', 'INSERT') 
										inner join AssetType T on T.ID = A.AssetTypeID
										inner join ResponsibilityTypeRelationRule R on R.ID = C.RuleID 
										inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
										inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
										inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
										inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
										and ( 
											(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
											(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
											) 
										and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
										and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
										and V.Definition <> '{}'";
					IEnumerable<ResponsibilityAssetMeasureProcessedResult> ruleResults =
						await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(sqlToExecute, new { today = DateTime.UtcNow.Date }, transaction: transaction);

					results.AddRange(ruleResults);

					//drop impacted assets temporary table.
					sqlToExecute = "drop table if exists #changes";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//First time a rule runs, queue the asset type for search re-indexing
					if (rule.LastRunOn == null)
					{
						sqlToExecute = "select * from [dbo].[AssetType] at WHERE at.[Object] = @Object and at.ObjectID = @ObjectID";
						AssetType assetType = Connection.Query<AssetType>(
							sqlToExecute,
							new { rule.Object, rule.ObjectID },
							transaction: transaction
						).SingleOrDefault();

						Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
						{
							CompanyID = CurrentCompanyID,
							AssetTypeUid = assetType.uid,
							Origin = "ProcessRuleForAsset, rule: " + rule.ID.ToString() + ", " + rule.Name
						});
					}

					await MarkResponsibilityRuleAsRan(rule.ID, transaction);

					transaction.Commit();
				}
				catch (Exception ex)
				{
					try
					{
						if (transaction != null)
						{
							transaction.Rollback();
						}
					}
					catch
					{
						//possible invalid rule ignore
					}

					throw new ApplicationException($"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n");
				}
			}
		}

		private async Task ProcessRuleForAssetType(ResponsibilityTypeRelationRule rule, List<ResponsibilityAssetMeasureProcessedResult> results)
		{
			string sqlToExecute = "";

			using (SqlTransaction transaction = Connection.BeginTransaction())
			{
				try
				{
					string thenSql = GetThenResultsSql(rule, false, transaction, false);
					thenSql = string.Format(thenSql, "");

					//create impacted assets temporary table.
					sqlToExecute = "create table #changes (ActionType varchar(50), RuleID int, AssetTypeID int)";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//merge into the asset table 
					sqlToExecute = @"
							merge   [dbo].[ResponsibilityRuleResultAsset] as T
							using	(
									select	T.ID as AssetTypeID,		
											R.ID as RuleID
									from	AssetType T
											inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID							                
										where 
												R.ID = @ruleId
									) as S
							on		S.RuleID = T.RuleID and S.AssetTypeID = T.AssetTypeID
							when	matched then
									update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
							when	not matched by target then
									insert (RuleID, AssetTypeID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetTypeID,getutcdate(),0)
							when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
									delete
							output  $action as ActionType, 
									iif($action = 'DELETE', deleted.RuleID, inserted.RuleID), 
									iif($action = 'DELETE', deleted.AssetTypeID, inserted.AssetTypeID)
							into #changes;";
					await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID }, transaction: transaction);

					//merge into the resource table
					sqlToExecute = $@"
							merge   [dbo].[ResponsibilityRuleResultSecurityAsset] as T
							using	(
									{thenSql}
									) as S
							on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
							when	matched then
									update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
							when	not matched by target then
									insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
							when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
									delete;";
					await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID }, transaction: transaction);

					sqlToExecute = @"
							select  A.Uid as AssetUid, 
									M.AllocationUid,
									M.Uid as MetricAssetUid,
									V.Uid as MetricAssetVersionUid
							from    #changes C 
									inner join AssetType T on T.ID = C.AssetTypeID 
									inner join Asset A on A.AssetTypeID = T.ID and C.ActionType in ('DELETE', 'INSERT') 
									inner join ResponsibilityTypeRelationRule R on R.ID = C.RuleID 
									inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
									inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
									inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
									inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
										and ( 
											(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
											(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
											) 
										and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
										and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
										and V.Definition <> '{}'";
					IEnumerable<ResponsibilityAssetMeasureProcessedResult> ruleResults =
						await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(sqlToExecute, new { today = DateTime.UtcNow.Date }, transaction: transaction);

					results.AddRange(ruleResults);

					//drop impacted assets temporary table.
					sqlToExecute = "drop table if exists #changes";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//First time a rule runs, queue the asset type for search re-indexing
					if (rule.LastRunOn == null)
					{
						sqlToExecute = "select * from [dbo].[AssetType] at WHERE at.[Object] = @Object and at.ObjectID = @ObjectID";
						AssetType assetType = Connection.Query<AssetType>(
							sqlToExecute,
							new { rule.Object, rule.ObjectID },
							transaction: transaction
						).SingleOrDefault();
						Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
						{
							CompanyID = CurrentCompanyID,
							AssetTypeUid = assetType.uid,
							Origin = "ProcessRuleForAssetType, rule: " + rule.ID.ToString() + ", " + rule.Name
						});
					}

					await MarkResponsibilityRuleAsRan(rule.ID, transaction);

					transaction.Commit();
				}
				catch (Exception ex)
				{
					try
					{
						if (transaction != null)
						{
							transaction.Rollback();
						}
					}
					catch
					{
						// ignore invalid rules
					}

					throw new ApplicationException($"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n");
				}
			}
		}

		/// <summary>
		/// Does the current rule id need to be run?
		/// </summary>
		/// <param name="cnn">DB connection</param>
		/// <param name="ruleId">RUle ID to look at</param>
		/// <returns></returns>
		private async Task<bool> ShouldRuleRun(int ruleId)
		{
			return await Connection.QueryFirstAsync<bool>("exec ResponsibilityRuleShouldRun @id", new { id = ruleId });
		}

		/// <summary>
		/// Return all responsibility rules that should be rerun
		/// </summary>
		/// <param name="cnn">DB connection</param>
		/// <returns></returns>
		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesForRerun(int? ruleId)
		{
			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>("exec GetRulesForRerun @id", new { id = ruleId });
		}

		/// <summary>
		/// Load the Responsibility Rules that the rebuild process should run
		/// </summary>
		/// <param name="ruleID">Specific responsibilty rule id to go after</param>
		/// <returns></returns>
		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(int? ruleID)
		{
			if (ruleID.HasValue)
			{
				return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value });
			}

			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule");
		}

		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(Guid executionId, int beginItemNumber, int endItemNumber)
		{
			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"
					select	R.* 
					from	ResponsibilityTypeRelationRule R 
							inner join api.executionresponsibilityrule E on E.uid = R.uid 
								and E.Success != 0
								and E.ItemNumber between @beginItemNumber and @endItemNumber
								and E.ExecutionID = @executionId"
			, new { executionId, beginItemNumber, endItemNumber });
		}

		private string ThenSqlConnector(ResponsibilityRuleDefinitionThen then)
		{
			return then.MatchType == ResponsibilityMatchType.And ? "and" : "or";
		}

		#endregion

		#region Methods

        public async Task BulkInsertResponsibilityOverrideAsync(List<BulkResponsibilityOverridePostModel> request, ApiExecution execution, int timeout = 3600)
        {
            Stopwatch swBegin = Stopwatch.StartNew();
            const string METHOD_NAME = "BulkInsertResponsibilityOverride";
            bool isLog = true; // trace info for all assets is extermely useful

            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            CurrentExecutionLocationModel currentLocation = null;
            Dictionary<string, double> metrics = new Dictionary<string, double>();
            Stopwatch sw = Stopwatch.StartNew();
            int step = 0;

            var dups = request.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            var dupRecords = request.GroupBy(i => new { i.AssetUid, i.ResponsibilityTypeUid, i.AssignedUid }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            addMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            if (dups.Any() || dupRecords.Any())
            {
                if (dups.Any())
                {
                    string message = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                    execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
                }
                else
                {
                    string message = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"AssetUid: {i.keyFields.AssetUid}, ResponsibilityTypeUid: {i.keyFields.ResponsibilityTypeUid}, AssignedUid: {i.keyFields.AssignedUid}"))}. AssetUid, ResponsibilityTypeUid, AssignedUid are key fields and the combination must be unique within a batch.";
                    execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
                }
            }
            else
            {
                try
                {
					addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityTypeRelationOverrideItem");
                    sw.Restart();

                    #region Build data tables.
                    
					DataTable ResponsibilityTypeRelationOverrideTable = new DataTable();

                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ExecutionID", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ItemNumber", typeof(int));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ExecutionItemUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ResponsibilityTypeUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("AssetUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("SecurityAssetUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Context", typeof(string));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Message", typeof(string));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Success", typeof(bool));

					#endregion

                    #region Populate Data Tables

                    foreach (BulkResponsibilityOverridePostModel item in request)
                    {
                        DataRow row = ResponsibilityTypeRelationOverrideTable.NewRow();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = itemNumber;
                        if (item.ExecutionItemUid.HasValue)
                        {
                            row["ExecutionItemUid"] = item.ExecutionItemUid;
                        }
                        else
                        {
                            row["ExecutionItemUid"] = DBNull.Value;
                        }
                        row["ResponsibilityTypeUid"] = item.ResponsibilityTypeUid;
                        row["AssetUid"] = item.AssetUid;

                        row["SecurityAssetUid"] = item.AssignedUid;
                        row["Context"] = item.Description.SanitizeHtml();

                        ResponsibilityTypeRelationOverrideTable.Rows.Add(row);

                        itemNumber++;
                    }

					#endregion

					#region Bulk Copy

					await Connection.OpenIfClosed();

                    using (SqlTransaction transaction = Connection.BeginTransaction())
                    {
                        try
                        {
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
                            {
                                BatchSize = ResponsibilityTypeRelationOverrideTable.Rows.Count,
                                DestinationTableName = "[api].[ExecutionResponsibilityTypeRelationOverrideItem]",
                                BulkCopyTimeout = SqlBulkBatchTimeout
                            })
                            {
                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                bulkCopy.ColumnMappings.Add("ResponsibilityTypeUid", "ResponsibilityTypeUid");
                                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                                bulkCopy.ColumnMappings.Add("SecurityAssetUid", "SecurityAssetUid");
                                bulkCopy.ColumnMappings.Add("Context", "Context");

                                bulkCopy.WriteToServer(ResponsibilityTypeRelationOverrideTable);
                            }

							transaction.Commit();

                            addMeasurement(metrics, "BulkCopy to api.ExecutionResponsibilityTypeRelationOverrideItem table", sw.ElapsedMilliseconds, ++step);
                        }
                        catch
                        {
							if (transaction != null)
							{
								transaction.Rollback();
							}
                        }
                    }

                    #endregion

                    Connection.Execute($@"
						update	api.ExecutionResponsibilityTypeRelationOverrideItem
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid ResponsibilityTypeUid.'
						where	ExecutionID = @ExecutionID and ([ResponsibilityTypeUid] is null or [ResponsibilityTypeUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	api.ExecutionResponsibilityTypeRelationOverrideItem
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid AssetUid.'
						where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	api.ExecutionResponsibilityTypeRelationOverrideItem
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid SecurityAssetUid.'
						where	ExecutionID = @ExecutionID and ([SecurityAssetUid] is null or [SecurityAssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	ERTROI
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Asset not found based on AssetUid provided'
						from
							api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							left Join
							Asset A on ERTROI.AssetUid = A.Uid
						where	ExecutionID = @ExecutionID and A.Uid is null;         

						update	ERTROI
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'ResponsibilityType not found based on ResponsibilityTypeUid provided'
						from
							api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							left Join
							ResponsibilityType RT on ERTROI.ResponsibilityTypeUid = rt.Uid
						where	ExecutionID = @ExecutionID and rt.Uid is null;  

						update	ERTROI
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'SecurityAsset not found based on SecurityAssetUid provided'
						from
							api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							left Join
							Asset SA on SA.Uid = ERTROI.SecurityAssetUid and SA.Object in ('Resource', 'Group')
						where	ExecutionID = @ExecutionID and SA.Uid is null;
						
						update	ERTROI
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Responsibility Type not valid for Asset.'
						from
							  api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							  inner join ResponsibilityType RT on RT.[uid] = ERTROI.ResponsibilityTypeUid
							  inner join Asset A on A.uid = ERTROI.AssetUid
							  inner join assettype att on att.id = A.AssetTypeID							  
							  left join responsibilitytyperelation rtr on rtr.responsibilitytypeid = rt.id and att.object = rtr.ObjectType and att.ObjectID = rtr.ObjectID            
						where	ExecutionID = @ExecutionID and rtr.ResponsibilityTypeID is null;					

						update	ERTROI
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Responsibility override already exists with AssetUid '+ convert(nvarchar(36), ERTROI.AssetUid) +' and ResponsibilityTypeUid '+ convert(nvarchar(36), ERTROI.ResponsibilityTypeUid) +' and SecurityAssetUid '+ convert(nvarchar(36), ERTROI.SecurityAssetUid)
						from
							api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							inner join 
							Asset A on ERTROI.AssetUid = A.Uid
							inner join 
							ResponsibilityType RT on RT.Uid = ERTROI.ResponsibilityTypeUid
							inner join
							Asset SA on SA.Uid = ERTROI.SecurityAssetUid and SA.Object in ('Resource', 'Group')
							inner join
							ResponsibilityTypeRelationOverrideItem RTROI on RTROI.ResponsibilityTypeId = RT.ID and RTROI.AssetId = A.Id and RTROI.SecurityAssetId = SA.ObjectId
						where ExecutionID = @ExecutionID;",
						new { execution.ExecutionID }, commandTimeout: timeout
					);

                    addMeasurement(metrics, "LogResponsibilityTypeRelationOverrideItemErrors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = request.Count();
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            string querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";
                            using (SqlTransaction trans = Connection.BeginTransaction())
                            {
                                #region Load valid items into table

                                try
                                {
                                    Connection.Execute($@"
										DROP TABLE IF EXISTS #mergeResultTable
										CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

										MERGE INTO ResponsibilityTypeRelationOverrideItem RTROI
										USING (
												SELECT
													A.ID as AssetId, 
													RT.ID as ResponsibilityTypeId, 
													CASE SA.Object
														WHEN 'Resource' THEN 'R'
														WHEN 'Group' THEN 'G'
														END as SecurityAsset,                                            
													SA.ObjectID as SecurityAssetId, E.*
												FROM  
													api.ExecutionResponsibilityTypeRelationOverrideItem E
												INNER JOIN
													Asset A ON A.Uid = E.AssetUid
												inner join 
													ResponsibilityType RT on RT.Uid = E.ResponsibilityTypeUid
												inner join
													Asset SA on SA.Uid = E.SecurityAssetUid and SA.Object in ('Resource', 'Group')
												WHERE {querySuffix}
												) ERTROI
										ON (ERTROI.AssetId = RTROI.AssetID AND ERTROI.ResponsibilityTypeId = RTROI.ResponsibilityTypeId and ERTROI.AssetId = RTROI.AssetID)                                        
										WHEN NOT MATCHED THEN
										INSERT
											([ResponsibilityTypeID]
											,[AssetID]
											,[SecurityAsset]
											,[SecurityAssetID]
											,[Context]
											,[UpdatedBy]
											,[UpdatedOn])
										VALUES
											(ERTROI.ResponsibilityTypeID
											,ERTROI.AssetID
											,ERTROI.SecurityAsset
											,ERTROI.SecurityAssetID
											,ERTROI.Context
											,@CurrentResourceID
											,GETDATE())                               
										OUTPUT  inserted.ID INT, ERTROI.ItemNumber INTO #mergeResultTable;                                                                                   
											", new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                                    #endregion

                                    // Update success flag.
                                    Connection.Execute(
                                        $@"update E 
											set Success = 1 
									   From api.ExecutionResponsibilityTypeRelationOverrideItem E
									   where {querySuffix};",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();
                                    runCompleted = true;
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
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityTypeRelationOverrideItem", ex.GetFullExceptionData(false), timeout);
                                        addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
                                    }
                                }
                            }
                        }

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
						sw.Restart();
                    }
                }
            }

			completeApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionResponsibilityTypeRelationOverrideItem");
			addMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);
            addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);
			
			Connection.CloseIfOpened();
        }

		public void ClearInvalidRelationRuleResults()
		{
			Connection.Execute("delete [dbo].[ResponsibilityRuleResultAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
			Connection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
		}
		
		public List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups)
		{
			DynamicParameters dbArgs = new DynamicParameters();
			bool generalChecksCompleted = false;
			int itemNumber = 1;
			List<GroupResponseResult> results = new List<GroupResponseResult>();
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			try
			{
				#region Build data tables.

				currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "[api].[ExecutionDeletedGroup]");

				DataTable table = new DataTable();

				table.Columns.Add("ExecutionID", typeof(Guid));
				table.Columns.Add("ItemNumber", typeof(int));
				table.Columns.Add("GroupUid", typeof(Guid));

				#region Generate data sets

				foreach (DeleteGroupModel item in groups)
				{
					DataRow row = table.NewRow();
					row["ExecutionID"] = execution.ExecutionID;
					row["ItemNumber"] = itemNumber;
					row["GroupUid"] = item.Uid;

					table.Rows.Add(row);

					itemNumber++;
				}

				#endregion

				if (Database.Connection.State != ConnectionState.Open)
				{
					Connection.Open();
				}

				#region Bulk Copy

				using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
				{
					BatchSize = table.Rows.Count,
					DestinationTableName = "[api].[ExecutionDeletedGroup]",
					BulkCopyTimeout = 3600
				})
				{

					bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("GroupUid", "GroupUid");

					bulkCopy.WriteToServer(table);
				}

				#endregion

				string checkSQL = $@"update	[api].[ExecutionDeletedGroup]
					set		Success = 0,
							[Message] = coalesce([Message] + '; ', '') + 'Not a valid group'
					from [api].[ExecutionDeletedGroup] EP
					left join Asset A on A.UID = EP.GroupUid and A.Object = 'Group'
					where	ExecutionID = @ExecutionID and A.uid is null";

				Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

				#endregion

				generalChecksCompleted = true;
			}
			catch (Exception generalEx)
			{
				generalChecksCompleted = false;
				string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = msg;
				execution.Processed = 0;
				execution.Error = groups.Count();

				results = new List<GroupResponseResult>();
				results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = msg, Success = false }));
			}

			itemNumber = 1;

			if (generalChecksCompleted)
			{
				int loopSize = 250;
				int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
				int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
				int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

				for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
				{
					bool runCompleted = false;
					int retryCount = 0;

					while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
					{
						using (SqlTransaction trans = Connection.BeginTransaction())
						{
							try
							{
								string deleteSQL = $@"
										insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
											select	distinct
													'Group', 
													G.ID,
													SUBSTRING(G.Name,1,250),
													@CurrentResourceID, 
													getutcdate(), 
													'Deleted', 
													'Group', 
													G.ID,
													'Group', 
													SUBSTRING(G.Name,1,250), 
													'This group has been removed.'
											from [api].[ExecutionDeletedGroup] EDG
											inner join [Asset] A on A.Uid = EDG.GroupUid and A.[Object] = 'Group'
											inner join [Group] G on G.ID = A.ObjectID 
											where	ExecutionID = @ExecutionID

										DELETE G
										FROM [Group] G
										inner join api.ExecutionDeletedGroup EG on EG.Success is null and EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber
										inner join Asset A on A.uid = EG.GroupUid and A.[Object] = 'Group'
										where A.ObjectID = G.ID";

								Connection.Execute(deleteSQL,
										new { execution.ExecutionID, CurrentResourceID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

								Connection.Execute($"update EG set EG.Success = 1, EG.Message = 'Deleted Successfully' from api.ExecutionDeletedGroup EG where EG.Success is null and EG.ExecutionID = @ExecutionID;",
													new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

								trans.Commit();
								runCompleted = true;
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
									LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedGroup", ex.GetFullExceptionData(false), timeout);
								}
							}
						}
					}
				}
			}

			results.AddRange(
							Query<GroupResponseResult>(
								$"select [ItemNumber],[GroupUid] as uid,[ExecutionID] as ExecutionItemUid,[Message],[Success] from api.ExecutionDeletedGroup where ExecutionID = @ExecutionID",
								new { execution.ExecutionID }
							)
						);

			return results;
		}
		
		public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAsset(long id)
		{
			try
			{
				return Database.Connection.Query<ResponsibilityType>(@"
																	select	RT.*
																	from	ResponsibilityType RT
																			inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = RT.ID
																			inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID
																			inner join Asset A on A.AssetTypeID = T.ID and A.ID = @id
																	order by RT.Name", new { id }).AsQueryable();
			}
			catch (SqlException ex)
			{
				throw CheckAndTranslateSqlException(ex, "Responsibility Type");
			}
		}

		public string GetNoReadSqlStatement(string identifier = null)
		{
			return GetNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetAssetTypeNoReadSqlStatement(string identifier = null)
		{
			return GetAssetTypeNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetID from ResponsibilityDetail where ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? CurrentResourceID.ToString() : identifier)}";
		}

		public string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetTypeID from ResponsibilityDetail where AssetID = 0 and ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? CurrentResourceID.ToString() : identifier)}";
		}

		public List<PermissionInfo> GetTypePermissions(string type, int typeID)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

			List<int> responsibilityAssignments = Filter<ResponsibilityDetail>(i =>
				i.Type == type && i.TypeID == typeID &&
				i.AssetID == 0 &&
				i.ResourceID == CurrentResourceID
			).Select(i => i.PermissionsBitMask).Distinct().ToList();

			permissions.ForEach(p =>
			{
				p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
			});

			//AddAsset no longer requires ApplyToType (GOV-13993). But because the ResponsibilityDetail view relies on the ...RuleResults tables,
			//we will need to check for this permission with HasAssetTypePermission() if it has not already been selected
			permissions
				.Where(p => p.Value == (int)Permission.AddAsset && !p.Selected)
				.ToList()
				.ForEach(p => p.Selected = HasAssetTypePermission(type, typeID, Permission.AddAsset)
			);

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		public List<PermissionInfo> GetPermissions(long assetId, int assetTypeId)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

			IEnumerable<int> responsibilityAssignments = Query<int>(@"select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = 0
														union select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = @assetId", new { r = CurrentResourceID, assetTypeId, assetId });

			if (responsibilityAssignments.Any())
			{
				permissions.ForEach(p =>
				{
					p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
				});
			}

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		public bool GetPermissionsRead(long assetId, int assetTypeId)
		{
			IEnumerable<int> responsibilityAssignments = Query<int>(@"select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = 0
														union select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = @assetId", new { r = CurrentResourceID, assetTypeId, assetId });

			if (responsibilityAssignments.Any())
			{
				return responsibilityAssignments.Any(i => (i & (int)Permission.ReadAsset) == (int)Permission.ReadAsset);
			}
			else
			{
				return true;
			}
		}

		public string GetThenResultsSql(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction transaction, bool includeName = true, string assetIDColumn = "", bool includeUid = true)
		{
			StringBuilder thenSql = new StringBuilder();

			string obj = "";
			string uniqueIdField = "ID";

			if ((rule.StructuredDefinition != null) && (rule.StructuredDefinition.Then != null) && (rule.StructuredDefinition.Then.Object != null || (rule.StructuredDefinition.Then.Conditions != null && rule.StructuredDefinition.Then.Conditions.All(c => c.Object != null))))
			{
				if (rule.StructuredDefinition.Then.Conditions != null && rule.StructuredDefinition.Then.Conditions.Count > 0)
				{
					var rulegroups = rule.StructuredDefinition.Then.Conditions.GroupBy(c => c.Object);
					foreach (var rulegroup in rulegroups)
					{
						//As it was discussed here https://infogix.slack.com/archives/GCYCRNR54/p1663685002231019
						//we can not be inside this loop whitout that key (https://infogix.slack.com/archives/GCYCRNR54/p1663751303398119?thread_ts=1663685002.231019&cid=GCYCRNR54)
						var rulegroupKey = rulegroup.Key ?? rule.StructuredDefinition.Then.Object;

						Dictionary<string, string> objectIds = new Dictionary<string, string>()
						{
							{ "Resource", "RO" },
							{ "Group", "OG" },
							{ "DefaultValue", "OG"}
						};
						StringBuilder rulegroupSql = new StringBuilder();
						StringBuilder whenSuffix = new StringBuilder();
						obj = "DefaultValue";
						uniqueIdField = "ID";

						rulegroupSql.Append($@"select distinct {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}");

						if (rulegroupKey == "GroupType")
						{
							obj = "Group";
							rulegroupSql.Append($"'G' as SecurityAsset, OG.ID as SecurityAssetID{(includeName ? ", OG.Name" : "")} {(includeUid ? ", OG.Name as Path, Z.uid " : "")} from	[Group] OG {(includeUid ? " inner join Asset Z on Z.ObjectID=OG.ID and Z.Object='Group' " : "")}");
						}

						if (rulegroupKey == "ResourceType")
						{
							obj = "Resource";
							uniqueIdField = "ResourceID";
							rulegroupSql.Append($@"'R' as SecurityAsset, RO.ResourceID as SecurityAssetID{(includeName ? ", RO.FirstName + ' ' + RO.LastName as Name" : "")} {(includeUid ? ", RO.FirstName + ' ' + RO.LastName as Path, RO.uid " : "")} from reporting.Global_Resource RO ");
						}

						foreach (var rc in rulegroup)
						{
							var sqlEscapedValue = rc.Value == null ? "" : rc.Value.Replace("'", "''");

							if (rc.FieldTypeID > 0)
							{

								var thenFieldType = Connection.Query<FieldType>("select * from FieldType where ID = @FieldTypeID", new { rc.FieldTypeID }, transaction: transaction).SingleOrDefault();
								whenSuffix.Append(whenSuffix.Length == 0 ? $" where ( " : $" {ThenSqlConnector(rule.StructuredDefinition.Then)} ");

								var fieldDetailInitialSql = $"select 1 from FieldDetail where Object = '{obj}' and ObjectID = {objectIds[obj]}.{uniqueIdField}";

								if (thenFieldType != null)
								{
									if (thenFieldType.AllowMultipleValues)// multiselect list
									{
										whenSuffix.Append(
											$"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and '{sqlEscapedValue}' in (select value from string_split([Value],',')) ) ");
									}
									else if (thenFieldType.Type == "Text")
									{
										switch (rc.Operator)
										{
											case Operator.NotEquals:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue = '{sqlEscapedValue}' )  ");
												break;
											case Operator.Contains:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}%' )  ");
												break;
											case Operator.NotContains:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}%' )  ");
												break;
											case Operator.StartsWith:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '{sqlEscapedValue}%' )  ");
												break;
											case Operator.EndsWith:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}' )  ");
												break;
											case Operator.Populated:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and (FormattedValue is not null or LEN(FormattedValue)>0) ) ");
												break;
											case Operator.NotPopulated:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and (FormattedValue is not null or LEN(FormattedValue)>0) ) ");
												break;
											default:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue = '{sqlEscapedValue}' )  ");
												break;
										}

									}
									else // all other field types including single select list
									{
										whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and [Value] = '{sqlEscapedValue}' )  ");  // all field types plus single select list
									}
								}
								else
								{
									whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and [Value] = '{sqlEscapedValue}' )  ");
								}
							}
							else
							{
								if (!string.IsNullOrEmpty(rc.FieldTypeName) && !string.IsNullOrEmpty(rc.Value))
								{
									if (rc.FieldTypeName == "Name")
									{
										whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"{objectIds[obj]}.{uniqueIdField} = {rc.Value}");
									}
									else
									{
										whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"{objectIds[obj]}.{rc.FieldTypeName} = '{sqlEscapedValue}'");
									}
								}
							}
						}

						if (rulegroupKey == "ResourceType")
						{
							whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : " and ") + $"RO.[State] = 1");
							if (IsHideData3SixtyUsers)
							{
								whenSuffix.Append(" and (RO.Email not like '%@data3sixty.com' and RO.Email not like '%@infogix.com' and RO.Email not like '%@precisely.com')");
							}
						}

						if (whenSuffix.Length > 0)
						{
							whenSuffix.Append(" ) ");
						}

						if (rulegroupSql.Length > 0 || whenSuffix.Length > 0)
						{
							rulegroupSql.Append(" {0} " + whenSuffix);
						}

						thenSql.Append($"{(thenSql.Length > 0 ? " UNION " : "")}{rulegroupSql}");
					}
				}
				else
				{
					thenSql.Append($@"select distinct {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}");

					if (rule.StructuredDefinition.Then.Object == "GroupType")
					{
						thenSql.Append($"'G' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")} {(includeUid ? ", O.Name as Path, Z.uid " : "")} from	[Group] O {(includeUid ? " inner join Asset Z on Z.ObjectID=O.ID and Z.Object='Group' " : "")}");
					}

					if (rule.StructuredDefinition.Then.Object == "ResourceType")
					{
						thenSql.Append($@"'R' as SecurityAsset, O.ResourceID as SecurityAssetID{(includeName ? ", O.FirstName + ' ' + O.LastName as Name" : "")} {(includeUid ? ", O.FirstName + ' ' + O.LastName as Path, O.uid " : "")} from reporting.Global_Resource O ");
					}
				}
			}

			return thenSql.ToString();
		}

		public async Task<ResponsibilityWhenQueryData> GetWhenResultsSql(ResponsibilityTypeRelationRule rule, SqlTransaction transaction, bool includeName = true, bool includeUid = true)
		{
			var whenSql = new StringBuilder();
			var declareVar = new StringBuilder();
			var whenWhereConditions = new List<string>();
			var whenTempTables = new StringBuilder();
			Dictionary<string, object> dbArgs = new Dictionary<string, object>();

			declareVar.Append($@"declare @AssetTypeIDValue int;
							     select @AssetTypeIDValue = id from assettype t where T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID};
								 ");

			whenSql.Append("select distinct A.ID as AssetID ");
			if (includeName)
			{
				whenSql.Append(", ADV.DisplayValue as Name ");
			}

			if (includeUid)
			{
				whenSql.Append(", A.uid, P.DisplayPath as Path ");
			}

			whenSql.Append($@"
		from	Asset A 
		inner join AssetPath P on P.ID = A.ID and A.AssetTypeID =  @AssetTypeIDValue
		inner join AssetDisplayValue ADV on ADV.AssetID = A.ID
		");

			int fCount = 1;
			int rCount = 1;

			if (rule.StructuredDefinition != null && rule.StructuredDefinition.When != null)
			{
				foreach (ResponsibilityRuleDefinitionWhen w in rule.StructuredDefinition.When)
				{
					if (w.CheckType == "F")
					{
						if (w.FieldTypeID > 0)
						{
							string fieldWhere = "";
							var whenFieldType = Connection.QueryFirstOrDefaultAsync<FieldType>("select * from FieldType where ID = @FieldTypeID", new { w.FieldTypeID }, transaction: transaction).Result;

							if (whenFieldType != null)
							{
								string dbParameterName = $"@filter_{fCount}";
								string dbParameterDefaultValue = $"@DefaultValue_{fCount}";
								string value = w.Value;

								string defaultValue = "";
								if (!string.IsNullOrEmpty(whenFieldType.DefaultValue))
								{
									defaultValue = whenFieldType.DefaultValue;
								}


								if (whenFieldType.AllowMultipleValues)// multiselect list
								{
									fieldWhere =
										$" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and {dbParameterName} in (select value from string_split(coalesce(F.Value, {dbParameterDefaultValue}),','))";
								}
								else if (whenFieldType.Type == "Text")
								{
									switch (w.Operator)
									{
										case Operator.NotEquals:
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) != {dbParameterName}";
											break;
										case Operator.Contains:
											value = $"%{w.Value.Trim()}%";
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) LIKE {dbParameterName}";
											break;
										case Operator.NotContains:
											value = $"%{w.Value.Trim()}%";
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) NOT LIKE {dbParameterName}";
											break;
										case Operator.StartsWith:
											value = $"{w.Value}%";
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) LIKE {dbParameterName}";
											break;
										case Operator.EndsWith:
											value = $"%{w.Value}";
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) LIKE {dbParameterName}";
											break;
										case Operator.Populated:
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and (coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) is not null or LEN(coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}))>0)";  // all field types plus single select list
											break;
										case Operator.NotPopulated:
											fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and (coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) is null or LEN(coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}))=0)";  // all field types plus single select list
											break;
										default:
											fieldWhere = $" where ff.AssetId is null and  f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) = {dbParameterName}";  // all field types plus single select list
											break;
									}

								}
								else // all other field types including single select list
								{
									fieldWhere = $" where ff.AssetId is null and f.FieldTypeID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, {dbParameterDefaultValue}) = '{w.Value}'";  // all field types plus single select list
								}

								whenTempTables.Append($@"
								drop table if exists #filtered_field{fCount};
								create table #filtered_field{fCount}(AssetID Bigint);
								create clustered index icx_filtered_field{fCount} on #filtered_field{fCount}(AssetID);");

								dbArgs.Add(dbParameterName, value);
								dbArgs.Add(dbParameterDefaultValue, defaultValue);
								//load filtered field data into temp table
								whenTempTables.Append($@"

									insert into #filtered_field{fCount}
									select f.AssetID
									from Field f
									left join #filtered_field{fCount} ff on ff.AssetId = f.AssetID
									{fieldWhere}");

								//filter by using inner join 

								whenSql.AppendLine($"inner join #filtered_field{fCount} ftf{fCount} on ftf{fCount}.AssetId = A.Id");
							}
							else // invalid field type ID so the when is always not going to return anything
							{
								whenSql.Append($" where 1 =0 ");
							}
						}
						fCount++;
					}

					if (w.CheckType == "R")
					{
						whenWhereConditions.Add(
							$@"( 
								{(w.Operator == Operator.NotIn ? "Not" : "")} exists(
										SELECT TargetAsset.Uid as TargetAssetId
										FROM  
											[Intersect] I
											inner join [Asset] TargetAsset on TargetAsset.Object = '{w.TargetObject}' and TargetAsset.ObjectID = {w.TargetObjectID} and
											(I.ObjectAssetId = TargetAsset.Id or I.SubjectAssetId = TargetAsset.Id)
										WHERE 
											I.IntersectTypeID = {w.IntersectTypeID}			   								   
											and 
											((I.SubjectAssetId = A.Id and I.ObjectAssetId = TargetAsset.Id) 
											or 
											(I.ObjectAssetId = A.Id and I.SubjectAssetId = TargetAsset.Id))
										)
							)"
							);
						rCount++;
					}
				}
			}

			if (whenWhereConditions.Count > 0)
			{
				whenSql.Append(" where ");
				whenSql.Append(string.Join(" and ", whenWhereConditions));
			}

			return new ResponsibilityWhenQueryData
			{
				SqlQuery = whenSql.ToString(),
				TempTableQuery = whenTempTables.ToString(),
				DbParameters = dbArgs,
				DeclareVariable = declareVar.ToString()
			};
		}

		public string GetUserPermissionQuery(string tempTableName = "PermissiondAssets", string userParam = "ResourceID", string typeParam = "AssetTypeID")
		{
			return $@"drop table if exists #{tempTableName};
					create table #{tempTableName}(
						AssetId int,
						AssetTypeID bigint,
						PermissionsBitMask int
					)

					drop table if exists #AssetRule;
					with cte as (
					select a.AssetTypeID,
								   rasset.AssetID,
								   rasset.RuleID
							from  [dbo].[asset] a
							inner join [dbo].[ResponsibilityRuleResultAsset] rasset 
							on (rasset.AssetID = a.ID)
							where a.AssetTypeID = cast(@{typeParam} as int)
							union all 
							select att.ID as AssetTypeID,
								   rasset.AssetID,
								   rasset.RuleID
							from  [dbo].[assettype] att
							inner join [dbo].[ResponsibilityRuleResultAsset] rasset
							on (rasset.AssetID = 0 and rasset.AssetTypeID = cast(@{typeParam} as int))
							where att.ID = cast(@{typeParam} as int)
					)
					select * into #AssetRule from cte;

						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	rel.PermissionsBitMask,
								rasset.AssetID,
								rasset.AssetTypeID
						from	#AssetRule rasset
								inner join [dbo].[responsibilitytyperelationrule] r on (r.id = rasset.RuleID)		
								inner join [dbo].[ResponsibilityTypeRelation] rel on (rel.ObjectID = r.ObjectID and rel.ResponsibilityTypeID = r.ResponsibilityTypeID and rel.ObjectType = r.[Object])
								inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rresource on (r.id = rresource.RuleID)
						where	rresource.SecurityAsset = 'R' and rresource.SecurityAssetID = cast(@{userParam} as int)
						option (recompile);

						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	rel.PermissionsBitMask,
								rasset.AssetID,
								rasset.AssetTypeID
						from	#AssetRule rasset
								inner join [dbo].[responsibilitytyperelationrule] r on (r.id = rasset.RuleID)
								inner join [dbo].[ResponsibilityTypeRelation] rel on (rel.ObjectID = r.ObjectID and rel.ResponsibilityTypeID = r.ResponsibilityTypeID and rel.ObjectType = r.[Object])
								inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rresource on (r.id = rresource.RuleID)
								inner join dbo.[Group] G on G.ID = rresource.SecurityAssetID and rresource.SecurityAsset = 'G'
								inner join dbo.ResourceGroup RG on RG.GroupID = G.ID 	
						where	RG.ResourceID = cast(@{userParam} as int)
						option (recompile);

						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	rr.PermissionsBitMask,
								oride.AssetID,
								a.AssetTypeID
						from	[dbo].[ResponsibilityTypeRelationOverrideItem] oride
								inner join [dbo].ResponsibilityType RT on RT.ID = oride.ResponsibilityTypeID  
								inner join [dbo].asset a on (a.id = oride.assetID)
								inner join [dbo].assettype att on (att.id = a.assettypeid)
								inner join [dbo].[ResponsibilityTypeRelation] RR on (att.[object] = RR.[objectType] and att.objectid = RR.[Objectid] and RR.ResponsibilityTypeID = oride.ResponsibilityTypeID)					
								inner join reporting.Global_Resource RES on RES.ResourceID = oride.SecurityAssetID and oride.SecurityAsset = 'R'
						where	a.AssetTypeID = cast(@{typeParam} as int) and RES.ResourceID = cast(@{userParam} as int)	
						option (recompile);

						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	rr.PermissionsBitMask,
								oride.AssetID,
								a.AssetTypeID
						from	[dbo].[ResponsibilityTypeRelationOverrideItem] oride
								inner join [dbo].ResponsibilityType RT on RT.ID = oride.ResponsibilityTypeID  
								inner join [dbo].asset a on (a.id = oride.assetID)
								inner join [dbo].assettype att on (att.id = a.assettypeid)
								inner join [dbo].[ResponsibilityTypeRelation] RR on (att.[object] = RR.[objectType] and att.objectid = RR.[Objectid] and RR.ResponsibilityTypeID = oride.ResponsibilityTypeID)										
								inner join dbo.[Group] G on G.ID = oride.SecurityAssetID and oride.SecurityAsset = 'G'
								inner join dbo.ResourceGroup RG on RG.GroupID = G.ID and a.AssetTypeID = cast(@{typeParam} as int)		
						where	a.AssetTypeID = cast(@{typeParam} as int) and RG.ResourceID = cast(@{userParam} as int)
						option (recompile);
	
						--The following two select statements mimics AssetType-wide AddAsset permissions where the responsibility relation does not ApplyToType
						--AssetID is NULL to prevent these virtual rules from blocking read permissions
						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	2 as PermissionsBitMask,
								null as AssetID,
								att.id as AssetTypeID
						from	[dbo].[responsibilitytyperelationrule] r
								inner join [dbo].[ResponsibilityTypeRelation] rel on (r.ResponsibilityTypeID = rel.ResponsibilityTypeID and r.[Object] = rel.[ObjectType] and r.ObjectID = rel.ObjectID)
								inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rresource on (r.ID = rresource.RuleID)
								inner join [dbo].[AssetType] att on att.[Object] = rel.ObjectType and att.objectid = rel.objectid
						where	r.ApplyToType = 0 and rel.PermissionsBitMask & 2 = 2
								and rresource.SecurityAsset = 'R' and rresource.SecurityAssetID = cast(@{userParam} as int) and att.id = cast(@{typeParam} as int)
						option (recompile);
	
						insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
						select	2 as PermissionsBitMask,
								null as AssetID,
								att.id as AssetTypeID
						from	[dbo].[responsibilitytyperelationrule] r
								inner join [dbo].[ResponsibilityTypeRelation] rel on (r.ResponsibilityTypeID = rel.ResponsibilityTypeID and r.[Object] = rel.[ObjectType] and r.ObjectID = rel.ObjectID)
								inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rresource on (r.ID = rresource.RuleID)
								inner join [dbo].[Group] G on G.ID = rresource.SecurityAssetID and rresource.SecurityAsset = 'G'
								inner join [dbo].[ResourceGroup] RG on RG.GroupID = G.ID 	
								inner join [dbo].[AssetType] att on att.[Object] = rel.ObjectType and att.objectid = rel.objectid
						where	r.ApplyToType = 0 and rel.PermissionsBitMask & 2 = 2
								and RG.ResourceID = cast(@{userParam} as int) and att.id = cast(@{typeParam} as int)
						option (recompile);
	
						if not exists(select 1 from reporting.Global_Resource where ResourceID = cast(@{userParam} as int) and IsAdministrator = 1)
							and exists(select 1 from AssetType T where T.DefaultPermissions = 0  and T.ID = cast(@{typeParam} as int))
						begin
							drop table if exists #resourceResponsibilities
		
							select AssetId 
							into #resourceResponsibilities
							from dbo.ResponsibilityDetail where ResourceID = cast(@{userParam} as int)

							create nonclustered index ix_resourceResponsibilities_assetid on #resourceResponsibilities(AssetId)

							insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
							select	
								0 as PermissionsBitMask,
								A.ID as AssetID,
								cast(@{typeParam} as int) AssetTypeID
							from dbo.Asset A
							left join #resourceResponsibilities r on r.AssetID = A.ID
							where	A.AssetTypeID = cast(@{typeParam} as int) and r.AssetID is null
							option (recompile);
						end";
		}

		public bool HasAssetPermission(string type, int id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{

				if (permission == Permission.ReadAsset)
				{
					hasPermission = HasAssetDefaultReadPermission(type, id);
				}
				else
				{
					int? assetTypeID = null;

					if (type.EndsWith("Type"))
					{
						assetTypeID = Query<int?>("select ID from AssetType where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
					}
					else
					{
						assetTypeID = Query<int?>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
					}

					if (assetTypeID.HasValue)
					{
						hasPermission = HasPermission(type, id, assetTypeID.Value, permission);
					}
				}
			}

			return hasPermission;
		}

		public bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = 0 and ua.AssetTypeID = @t and ua.AssetID is not null)
																						begin
																							select 0;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua inner join asset a on(ua.AssetID = a.id and a.Object = @type and a.ObjectID = @id) where ua.PermissionsBitMask & {(int)permission} = 0)
																						begin
																							select 0;
																							end
																						else
																						begin
																							select 1;
																						end", new { type, id = objectId, t = assetTypeId, r = resourceId });
		}

		public bool HasAssetPermission(long id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where ID = @id", new { id }).Single();
				hasPermission = HasPermission(id, assetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermissionByUid(Guid uid, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				Asset asset = Assets.Single(a => a.uid == uid);
				hasPermission = HasPermission(asset.ID, asset.AssetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetPermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(string type, int id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);


			if (!hasPermission)
			{
				if (isReadPermission)
				{
					hasPermission = HasAssetTypeReadPermission(id);
				}
				else
				{
					int assetTypeID = Query<int>("select ID from AssetType where [Object] = @type and [ObjectID] = @id", new { id, type }).Single();
					hasPermission = Database.Connection.QuerySingle<bool>($@"if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																						begin
																							select 1;
																						end				                                                                        
																						else
																						begin
																							select 0;
																						end", new { t = assetTypeID, r = CurrentResourceID });
				}
			}

			return hasPermission;
		}

		public bool HasAssetTypePermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetTypePermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(int assetTypeId, Permission permission)
		{
			AssetType assetType = Query<AssetType>("select * from AssetType where ID = @id", new { id = assetTypeId }).Single();

			return HasAssetTypePermission(assetType.Object, assetTypeId, permission);
		}
        
		public void ParseResponsibilityRuleModel(Guid executionId, SqlTransaction trans = null, int timeout = 3600, string sourceTable = "api.ExecutionResponsibilityRule")
        {
            List<string> invalidFieldTypes = new List<string> {
            DataType.Path.ToString(),
            DataType.ComplexRelationLookup.ToString(),
            DataType.FieldFromRelationship.ToString(),
            DataType.DataTableSelect.ToString(),
            DataType.OwnershipLookup.ToString(),
            DataType.RefListRelationship.ToString(),
            DataType.JsonElement.ToString(),
            DataType.Tag.ToString(),
            DataType.JSON.ToString(),
            DataType.Score.ToString(),
            DataType.Relationship.ToString()
            };

            string jsonParseSql = $@"
				drop table if exists #tempData				  
				create table #tempData
				(
					ItemNumber int, 
					ExecutionId uniqueidentifier, 
					AssetTypeUid uniqueidentifier,
					AssigneeTypeUid uniqueidentifier, 
					MatchType nvarchar(20),
					RelIntersectTypeUid uniqueidentifier, 
					RelAssetUid uniqueidentifier,				
					FieldApiName nvarchar(250),
					FieldValue nvarchar(250),				
					AssigneeUid uniqueidentifier,
					RelOperator nvarchar(250),
					FieldOperator nvarchar(250),
					ValueAsUid uniqueidentifier,
					CondAssigneeTypeUid uniqueidentifier													
				)

				insert into #tempData
				select
					ItemNumber,
					ExecutionId,
					AssetTypeUid,
					case when ThenCond.AssigneeTypeUid is not null then ThenCond.AssigneeTypeUid
					else ThenData.AssigneeTypeUid end as AssigneeTypeUid,
					ThenData.MatchType,
					ThenCond.*,				
					case 
					when ThenCond.IntersectTypeUid is not null then cast(thencond.intersecttypeuid as uniqueidentifier)
					else null
					end as ValueAsUid
				from {sourceTable}
					cross apply OPENJSON (Definition, N'$.Then')
					WITH (
					AssigneeTypeUid uniqueidentifier N'$.AssigneeTypeUid',
					MatchType nvarchar(20) N'$.MatchType',
					Conditions nvarchar(max) N'$.Conditions' as Json
					) AS ThenData
					outer apply OPENJSON(ThenData.Conditions)
					with(
						IntersectTypeUid uniqueidentifier N'$.Relation.IntersectTypeUid',
						AssetUid uniqueidentifier N'$.Relation.AssetUid',
						FieldApiName nvarchar(250) N'$.Field.ApiName',
						Value nvarchar(250) N'$.Field.Value',
						AssetUid uniqueidentifier  N'$.Assignee.Uid',
						RelOperator nvarchar(250) N'$.Relation.Operator',
						FieldOperator nvarchar(250) N'$.Field.Operator',
						AssigneeTypeUid uniqueidentifier N'$.AssigneeTypeUid'	
					) as ThenCond
				where 
					executionid = @executionId and success is null
							  
				insert into #tempData
				select
					ItemNumber,
					ExecutionId,
					AssetTypeUid,
					null as AssigneeTypeUid,
					null as MatchType,
					WhenData.*,
					null as CondAssigneeTypeUid,				
					case 
					when WhenData.IntersectTypeUid is not null then cast(WhenData.value as uniqueidentifier)
					else null
					end as ValueAsUid
				from {sourceTable}
					cross apply OPENJSON (Definition, N'$.When')
					WITH (
						IntersectTypeUid uniqueidentifier N'$.Relation.IntersectTypeUid',
						AssetUid uniqueidentifier N'$.Relation.AssetUid',					
						FieldApiName nvarchar(250) N'$.Field.ApiName',
						Value nvarchar(250) N'$.Field.Value',
						AssetUid uniqueidentifier  N'$.Assignee.Uid',
						RelOperator nvarchar(250) N'$.Relation.Operator',
						FieldOperator nvarchar(250) N'$.Field.Operator'
					) AS WhenData
				where 
					executionid = @executionId and success is null
							 
				drop table if exists #parsedData
				select  d.itemnumber, 
					d.executionid ,
					at.object,
					at.objectid,
					d.valueasuid,
					d.AssigneeTypeUid,
					d.MatchType,
					d.AssigneeUid,
					d.RelAssetUid,
					case 
						when d.RelIntersectTypeUid is null then 'F'
						else 'R'
					end as CheckType,
					case 
						when at.uid is not null then ft.id
						else ft2.id
					end as FieldTypeId,
					case
						when at.uid is not null then isnull(ft.friendlyname,d.fieldapiname)
						else isnull(ft2.friendlyname, d.fieldapiname)
					end as FieldTypeName,
					case 
						when it.id is null then d.FieldValue
						else a.Object+'|'+ cast(a.objectid as nvarchar(20)) 
					end as Value,
					it.id as IntersectTypeId,
					a.object as TargetObject,
					isnull(a.objectid,0) as TargetObjectId,
					cast('' as nvarchar(max)) as ErrorMessage,
					ROW_NUMBER() OVER(ORDER BY(SELECT NULL)) as rowNumber,
					ft2.type as FieldType,
					case 
						when d.RelIntersectTypeUid is null then FieldOperator
						else RelOperator
					end as Operator
				into #parsedData
				from #tempData d
					left join assettype at on d.assigneetypeuid = at.uid
					left join FieldType ft on at.ID = ft.AssetTypeID and ft.Name = d.FieldApiName
					left join IntersectType it on it.uid = d.RelIntersectTypeUid
					left join assettype at2 on d.AssetTypeUid = at2.uid
					left join FieldType ft2 on ft2.AssetTypeID = at2.ID and ft2.name = d.fieldapiname
					left join asset a on a.uid = d.RelAssetUid								

				update #parsedData
				set FieldTypeId = 0,
				FieldTypeName = 'Name',
				Value = a.ObjectID
				from #parsedData
				inner join asset a on a.uid = AssigneeUid
				inner join assettype at on a.assettypeid = at.id and at.objectid = #parsedData.objectid and at.object = #parsedData.object
				where AssigneeUid is not null

				update #parsedData
				set Value = LOWER(pd.value)
				from #parsedData pd
				inner join fieldtype ft on pd.fieldtypeid = ft.id
				where pd.fieldtypeid is not null and ft.type = 'Boolean'

		        update #parsedData
                set 
	                Value = lookupValue.Value
                from #parsedData pd 
	                inner join fieldtype ft on pd.fieldtypeid = ft.id
	                left join (
		                select distinct
			                flv.FieldTypeID,
			                flv.Text,
                            flv.Value
		                from #parsedData pd
		                left join FieldLookupValue flv on flv.FieldTypeID = pd.FieldTypeId
	                ) lookupValue
		                on lookupValue.FieldTypeID = pd.FieldTypeId 
		                and try_cast(trim(pd.Value) as int) = lookupValue.Value 
                where pd.fieldtypeid is not null and ft.type = 'Lookup'

				update #parsedData
				set ErrorMessage = 'Invalid Field name.'
				where isnull(fieldtypeid,0) = 0 and fieldtypename <> '' and AssigneeUid is null

				update #parsedData
				set ErrorMessage = 'Invalid Field Type.'
				where 
				isnull(fieldtypeid,0) != 0 
				AND 
				fieldtypename <> '' 
				AND 
				AssigneeUid is null
				AND
				FieldType in @invalidFieldTypes

				update #parsedData
				set ErrorMessage = 'Invalid Lookup value.'
				from #parsedData pd
				inner join fieldtype ft on pd.fieldtypeid = ft.id
				where pd.fieldtypeid is not null and ft.type = 'Lookup' and Value is null

				update #parsedData
				set ErrorMessage = 'Invalid AssetUid for condition.'
				where isnull(value,0) = 0 and fieldtypename <> '' and AssigneeUid is not null

				update #parsedData
				set ErrorMessage = 'Invalid Intersect Type Uid for condition.'
				where CheckType = 'R' and IntersectTypeId is null

				update #parsedData
				set ErrorMessage = 'Invalid Asset UID for condition value.'
				where CheckType = 'R' and isnull(targetobjectid,0) = 0

				update #parsedData
				set ErrorMessage =  'Invalid Assignee Type. Allowed Types are ''Resource'' or ''Group'''
				where object is not null and object not in('ResourceType','GroupType')

				update #parsedData
				set ErrorMessage = 'Invalid Asset UID for Intersect Type.'
				from #parsedData
				left join IntersectType it on it.ID= IntersectTypeId
				left join Asset A on a.object = TargetObject and a.objectid = targetobjectid
				left join assettype at on a.AssetTypeID = at.ID 
				where CheckType = 'R' and (at.ID <> it.SubjectAssetTypeID and at.ID <> it.ObjectAssetTypeID)

				update #parsedData
				set ErrorMessage = 'AssigneeType not found.'
				from #parsedData pd
				left join AssetType at on at.uid = pd.assigneetypeuid
				where pd.AssigneeTypeUid is not null and at.id is null

				update #parsedData
				set ErrorMessage = 'Invalid AssigneeType. Allowed types are ResourceType or GroupType.'
				from #parsedData pd
				inner join AssetType at on at.uid = pd.assigneetypeuid
				where pd.AssigneeTypeUid is not null and at.Object not in ('ResourceType', 'GroupType')

				update #parsedData
				set ErrorMessage = 'Invalid Assignee for Assignee Type.'
				from #parsedData pd
				left join AssetType at on at.uid = pd.assigneetypeuid
				left join asset a on a.uid = assigneeuid
				where pd.AssigneeTypeUid is not null and at.id is not null and at.id <> a.assettypeid

				update #parsedData
				set ErrorMessage = 'Invalid JSON Data.'
				where fieldtypeid is null and fieldtypename is null and value is null and intersecttypeid is null and TargetObject is null and errormessage is null
								
				MERGE {sourceTable} err
				USING (select itemnumber,executionid,trim(string_agg(errormessage,',')) as msg from #parsedData
				where isnull(errormessage,'') <> ''
				group by itemnumber,executionid
				) cd
				ON cd.itemnumber = err.itemnumber and cd.executionid = err.executionid and cd.msg <> '' 
				WHEN MATCHED
				THEN UPDATE
				SET [Message] = coalesce([Message] + '; ', '') + cd.msg,
				Success = 0;

				drop table if exists #convertedData
				create table #convertedData
				(
				ItemNumber int, 
				ExecutionId uniqueidentifier, 
				[When] nvarchar(max),
				[Then] nvarchar(max),
				[Definition] nvarchar(max)
				)

				insert into #convertedData
				select ItemNumber,ExecutionId, null,null,null
				from #parsedData
				group by ItemNumber,ExecutionId

				;with conditions as (select 
				ItemNumber,
				ExecutionId,
				ConditionsThen.json as [Then],
				ConditionsWhen.json as [When]
				from #parsedData pd
				cross apply (
				select top 1 Object,ObjectID, MatchType, Conditions.json as Conditions
				from #parsedData
					outer apply(select
						CheckType,
						isnull(FieldTypeID,0) as FieldTypeID,
						FieldTypeName,
						Value,
						isnull(IntersectTypeID,0) as IntersectTypeID,
						TargetObject,
						TargetObjectId,
						Operator,
						Object,
						ObjectID
						from #parsedData
						where 
							ItemNumber =pd.ItemNumber 
							and 
							ExecutionId = pd.ExecutionId  
							and 
							[Object] is not null
							and 
							((FieldTypeName is not null and FieldTypeID <> 0 or AssigneeUid is not null))
						for json path, include_null_values
					)Conditions(json)
				where 
					ItemNumber =pd.ItemNumber 
					and 
					ExecutionId = pd.ExecutionId 
					and 
					[Object] is not null
				for json path, include_null_values, without_array_wrapper
				)ConditionsThen(json)

				cross apply (
				select
						CheckType,
						isnull(FieldTypeID,0) as FieldTypeID,
						FieldTypeName,
						Value,
						isnull(IntersectTypeID,0) as IntersectTypeID,			   
						TargetObject,
						TargetObjectId,
						Operator
						from #parsedData
						where ItemNumber =pd.ItemNumber and ExecutionId = pd.ExecutionId and Object is null
						for json path, include_null_values
				)ConditionsWhen(json)
				where Object is not null
				group by ItemNumber,ExecutionId,ConditionsThen.json, ConditionsWhen.json)
							
				update #convertedData 
				set [When] = c.[When],
				[Then] = c.[Then],
				[Definition] = '{{'+Concat_ws(',','""When"":' + c.[When],'""Then"":' + c.[Then]) + '}}'
				from conditions c
				where #convertedData.itemnumber = c.itemnumber and #convertedData.executionid = c.executionid								

				MERGE {sourceTable} err
				USING #convertedData cd
				ON cd.itemnumber = err.itemnumber and cd.executionid = err.executionid
				WHEN MATCHED
				THEN UPDATE
				SET DefinitionConverted = cd.[Definition];";

            Connection.Execute(jsonParseSql, new { executionId, invalidFieldTypes }, transaction: trans, commandTimeout: timeout);
        }

		public async Task ProcessResponsibilityRelationRules(int? ruleID = null, int timeout = 7200)
		{
			List<ResponsibilityAssetMeasureProcessedResult> results = new List<ResponsibilityAssetMeasureProcessedResult>();

			if (Connection.State != System.Data.ConnectionState.Open)
			{
				Connection.Open();
			}

			IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesForRerun(ruleID).ConfigureAwait(false);

			List<int> rulesRequiringRun = new List<int>();

			string ruleExceptionMessages = "";

			foreach (ResponsibilityTypeRelationRule rule in rules)
			{
				try
				{
					rule.SetDefinitionFromRaw();

					if (rule.ApplyToType)
					{
						await ProcessRuleForAssetType(rule, results).ConfigureAwait(false);
					}
					else
					{
						await ProcessRuleForAsset(rule, results).ConfigureAwait(false);
					}
				}
				catch (ApplicationException ex)
				{
					ruleExceptionMessages += ex.Message;
				}
			}

			// Send measure results to score engine.
			DateTime today = DateTime.UtcNow.Date;
			List<AssetMeasureModel> structuredMeasures = results.GroupBy(m => new { m.AssetUid })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = today,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).Distinct().ToList()
				}).ToList();

			CreateMeasureChangedResultExecution(structuredMeasures);

			if (!string.IsNullOrEmpty(ruleExceptionMessages))
			{
				throw new ApplicationException(ruleExceptionMessages);
			}
		}

		public async Task ProcessRulesForExecution(Guid executionId, int beginItemNumber, int endItemNumber)
		{
			List<ResponsibilityAssetMeasureProcessedResult> results = new List<ResponsibilityAssetMeasureProcessedResult>();

			if (Connection.State != System.Data.ConnectionState.Open)
			{
				Connection.Open();
			}

			IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesToRun(executionId, beginItemNumber, endItemNumber);

			List<int> rulesRequiringRun = new List<int>();

			string ruleExceptionMessages = "";

			foreach (ResponsibilityTypeRelationRule rule in rules)
			{
				try
				{
					if (await ShouldRuleRun(rule.ID))
					{
						rulesRequiringRun.Add(rule.ID);
						rule.SetDefinitionFromRaw();

						if (rule.ApplyToType)
						{
							await ProcessRuleForAssetType(rule, results);
						}
						else
						{
							await ProcessRuleForAsset(rule, results);
						}
					}
				}
				catch (ApplicationException ex)
				{
					ruleExceptionMessages += ex.Message;
				}
			}

			// Send measure results to score engine.
			DateTime today = DateTime.UtcNow.Date;
			List<AssetMeasureModel> structuredMeasures = results.GroupBy(m => new { m.AssetUid })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = today,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).Distinct().ToList()
				}).ToList();

			CreateMeasureChangedResultExecution(structuredMeasures);

			if (!string.IsNullOrEmpty(ruleExceptionMessages))
			{
				throw new ApplicationException(ruleExceptionMessages);
			}

		}

		public void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation)
		{
			List<AssetMeasureModel> structuredMeasures = null;

			try
			{
				AssetType assetType = Filter<AssetType>(a => a.Object == relation.ObjectType && a.ObjectID == relation.ObjectID).SingleOrDefault();
				ResponsibilityType responsibility = Filter<ResponsibilityType>(a => a.ID == relation.ResponsibilityTypeID).SingleOrDefault();

				if (assetType != null && responsibility != null)
				{
					// Scoring - get asset measures that are impacted
					structuredMeasures = GetMeasureModelsBasedOnResponsibilityAllocation(assetType, responsibility);
				}
			}
			catch
			{
				throw;
			}
			using (DbContextTransaction trans = Database.BeginTransaction())
			{
				try
				{
					Database.ExecuteSqlCommand(@"
												delete	O 
												from	ResponsibilityTypeRelationOverrideItem O
														inner join Asset A on A.ID = O.AssetID and O.ResponsibilityTypeID = @ResponsibilityTypeID
														inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @ObjectType and T.ObjectID = @ObjectID;

												delete	O 
												from	[dbo].[ResponsibilityRuleResultSecurityAsset] O
														inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID		    

												delete	O 
												from	[dbo].[ResponsibilityRuleResultAsset] O
														inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID

												delete	ResponsibilityTypeRelationRule
												where	ResponsibilityTypeID = @ResponsibilityTypeID
														and Object = @ObjectType 
														and ObjectID = @ObjectID;

												delete	ResponsibilityTypeRelation
												where	ResponsibilityTypeID = @ResponsibilityTypeID
														and ObjectType = @ObjectType 
														and ObjectID = @ObjectID;",
														new SqlParameter("@ResponsibilityTypeID", relation.ResponsibilityTypeID),
														new SqlParameter("@ObjectType", relation.ObjectType),
														new SqlParameter("@ObjectID", relation.ObjectID)
														);
					trans.Commit();
				}
				catch
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

					throw;
				}
			}

			// If you made it this far, then send to scoring engine.
			if (structuredMeasures != null)
			{
				CreateMeasureChangedResultExecution(structuredMeasures);
			}
		}

		public List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			DynamicParameters dbArgs = new DynamicParameters();
			bool generalChecksCompleted = false;
			bool hasCounterField = false;
			int itemNumber = 1;
			List<GroupResponseResult> results = new List<GroupResponseResult>();
			Dictionary<int, List<string>> importFields = new Dictionary<int, List<string>>();
			CurrentExecutionLocationModel currentLocation = null;
			bool isInsert = execution.Method == "POST";

			var dups = groups.GroupBy(x => x.Name.Trim()).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

			Add(execution);
			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			FieldValidationFieldProperties fieldLoadProperties = new FieldValidationFieldProperties(); // properties of fields in the data load.  Returned from validate fields so we are efficient and dont keep going through the fields.

			if (dups.Any())
			{
				string message = $"Duplicate Names: {string.Join(", ", dups.Select(i => i.Items.First().Name.Trim()))}. Name must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionGroup");

					DataTable table = new DataTable();

					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("GroupUid", typeof(Guid));
					table.Columns.Add("Name", typeof(string));
					table.Columns.Add("Description", typeof(string));
					table.Columns.Add("PrimaryOwnerUid", typeof(Guid));
					table.Columns.Add("SecondaryOwnerUid", typeof(Guid));
					table.Columns.Add("IsActiveDirectoryGroup", typeof(bool));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));

					DataTable fieldTable = new DataTable();

					fieldTable.Columns.Add("ExecutionID", typeof(Guid));
					fieldTable.Columns.Add("ItemNumber", typeof(int));
					fieldTable.Columns.Add("FieldName", typeof(string));
					fieldTable.Columns.Add("FieldValue", typeof(string));
					fieldTable.Columns.Add("FieldTypeID", typeof(int));

					#region Generate data sets

					foreach (UpdateGroupModel item in groups)
					{
						DataRow row = table.NewRow();
						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = itemNumber;

						if (item.Uid.HasValue && item.Uid.Value != Guid.Empty)
						{
							row["GroupUid"] = item.Uid;
						}

						if (item.Name == null)
						{
							row["Name"] = "";
						}
						else
						{
							row["Name"] = item.Name.Trim();
						}

						row["Description"] = item.Description.SanitizeHtml();

						if (item.PrimaryOwnerUid != null)
						{
							row["PrimaryOwnerUid"] = item.PrimaryOwnerUid;
						}

						if (item.SecondaryOwnerUid != null)
						{
							row["SecondaryOwnerUid"] = item.SecondaryOwnerUid;
						}

						row["IsActiveDirectoryGroup"] = item.IsActiveDirectoryGroup;
						row["ExecutionItemUid"] = Guid.NewGuid();

						table.Rows.Add(row);

						itemNumber++;
					}

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					#region Bulk Copy

					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
					{
						BatchSize = table.Rows.Count,
						DestinationTableName = "[api].[ExecutionGroup]",
						BulkCopyTimeout = 3600
					})
					{
						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("GroupUid", "GroupUid");
						bulkCopy.ColumnMappings.Add("Name", "Name");
						bulkCopy.ColumnMappings.Add("Description", "Description");
						bulkCopy.ColumnMappings.Add("PrimaryOwnerUid", "PrimaryOwnerUid");
						bulkCopy.ColumnMappings.Add("SecondaryOwnerUid", "SecondaryOwnerUid");
						bulkCopy.ColumnMappings.Add("IsActiveDirectoryGroup", "IsActiveDirectoryGroup");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					#region Handle Custom Fields

					// Get field types.                    
					List<FieldTypeCore> fieldTypes = GetAssetTypeFieldTypesCore("GroupType", 1);

					List<string> requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && !f.HasDefaultValue && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
					hasCounterField = fieldTypes.Any(x => x.Type == DataType.Counter.ToString());

					int i = 1;
					foreach (UpdateGroupModel group in groups)
					{
						List<DataRow> fieldRows = ValidateFields("Group", 1, isInsert, fieldTypes, requiredFieldTypeNames, group.Fields, execution.ExecutionID, i, fieldTable, out bool success, out string errorMessage, validationFieldProperties: fieldLoadProperties);

						if (success)
						{
							importFields.Add(i, group.Fields.Keys.ToList());
							fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });
						}
						else
						{
							Connection.Execute(@"update	[api].[ExecutionGroup]
												set		Success = 0,
														[Message] = coalesce([Message], '') + @errorMessage
												where	ExecutionID = @ExecutionID;", new { execution.ExecutionID, emptyUid = Guid.Empty, errorMessage }, commandTimeout: timeout);
						}

						i++;
					}

					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
					{
						BatchSize = SqlBulkBatchSize,
						DestinationTableName = ApiExecutionFieldTable,
						BulkCopyTimeout = SqlBulkBatchTimeout
					})
					{

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
						bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
						bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

						bulkCopy.WriteToServer(fieldTable);
					}

					#endregion

					string checkSQL = $@"update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Name field cannot be empty;'
										where	ExecutionID = @ExecutionID and (Name is null or TRIM(Name) = '');

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Already a group called ' + EG.[Name] + ';'
										from [api].[ExecutionGroup] EG 
										inner join [Group] G on G.[Name] = EG.[Name]
										left join [Asset] A on A.ObjectID = G.[ID] and A.Object = 'Group' and A.uid = EG.[GroupUid]
										where	ExecutionID = @ExecutionID and A.uid is null and G.Name is not null;

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Uid provided is not a group uid;'
										from [api].[ExecutionGroup] EG 
										Inner Join [api].[Execution] E on E.ExecutionID = EG.ExecutionID
										left join [Asset] A on A.[uid] = EG.[GroupUid] and A.Object = 'Group'
										where	E.Method = 'PUT' and EG.ExecutionID = @ExecutionID and A.uid is null and EG.[GroupUid] is not null;

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Uid already exists;'
										from [api].[ExecutionGroup] EG 
										Inner Join [api].[Execution] E on E.ExecutionID = EG.ExecutionID
										left join [Asset] A on A.[uid] = EG.[GroupUid]
										where	E.Method = 'POST' and EG.ExecutionID = @ExecutionID and A.uid is not null and EG.[GroupUid] is not null;

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Primary Owner Uid provided is not a resource uid;'
										from [api].[ExecutionGroup] EG 
										left join [Asset] A on A.[uid] = EG.[PrimaryOwnerUid] and A.Object = 'Resource'
										where	ExecutionID = @ExecutionID and coalesce(EG.[PrimaryOwnerUid], @emptyUid) <> @emptyUid and A.uid is null;

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Secondary Owner Uid provided is not a resource uid;'
										from [api].[ExecutionGroup] EG 
										left join [Asset] A on A.[uid] = EG.[SecondaryOwnerUid] and A.Object = 'Resource'
										where	ExecutionID = @ExecutionID and A.uid is null and EG.SecondaryOwnerUid is not null;

										update	[api].[ExecutionGroup]
										set		Success = 0,
												[Message] = coalesce([Message], '') + 'Lookup Field has invalid values;'
										from [api].[ExecutionGroup] EG 
										inner join api.executionfield ef on ef.ExecutionID = eg.ExecutionID and ef.FieldValue is not null
										inner join FieldType ft on ft.id = ef.fieldtypeid
										cross apply (select Value from string_split(ef.FieldValue, ','))Val(Value)
										left join FieldLookupValue flv on flv.FieldTypeID = ef.FieldTypeID and flv.Value = try_parse(Val.Value as int)
										where EG.ExecutionID = @ExecutionID  and ft.type = 'Lookup' and flv.Value is null and ef.FieldValue is not null";

					Connection.Execute(checkSQL, new { execution.ExecutionID, emptyUid = Guid.Empty }, commandTimeout: timeout);

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = groups.Count();

					results = new List<GroupResponseResult>();
					results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = msg, Success = false }));
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							string querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{
									string insertSQL = $@"
														drop table if exists #mergeResultTable
														create table #mergeResultTable (GroupID int, [Action] nvarchar(10), GroupName varchar(250), ExecutionItemUid uniqueidentifier) 

														drop table if exists #auditRecords
														create table #auditRecords (ItemNumber int, FieldName nvarchar(200), OldValue nvarchar(max), NewValue nvarchar(max))
															;with cte as (
															select EG.ItemNumber, 
															G.Name as OldName, 
															EG.Name as NewName, 
															G.Description as OldDesc,
															eg.Description as NewDesc,
															G.IsActiveDirectoryGroup as OldIsActiveDirectoryGroup,
															eg.IsActiveDirectoryGroup as NewIsActiveDirectoryGroup
															 from api.ExecutionGroup eg
															inner join Asset AG on AG.uid = eg.GroupUid and AG.[Object] = 'Group'
															inner join [Group] G on G.ID = AG.ObjectID
															where EG.ExecutionID = @ExecutionID
															and EG.ItemNumber between @beginItemNumber and @endItemNumber
															and EG.Success is null)
														insert into #auditRecords
														select ItemNumber, 'Name' as FieldName, OldName as OldValue, NewName as NewValue from cte
														union 
														select ItemNumber, 'Description' as FieldName, OldDesc as OldValue, NewDesc as NewValue from cte
														union
														select ItemNumber, 'IsActiveDirectoryGroup' as FieldName, try_cast(OldIsActiveDirectoryGroup as nvarchar(10)) as OldValue, try_cast(NewIsActiveDirectoryGroup as nvarchar(10)) as NewValue from cte
														union
														select EF.ItemNumber, ef.FieldName, f.FormattedValue as OldValue, ef.FieldValue as NewValue from api.ExecutionField ef 
														inner join api.executiongroup eg on eg.executionid = ef.executionid and eg.itemnumber = ef.itemnumber
														left join [Asset] AGR on AGR.uid = eg.GroupUid and AGR.[Object] = 'Group'
														left join [Group] G on G.ID = AGR.ObjectID
														left join [Field] F on F.ObjectType = 'Group' and F.ObjectID = G.ID and f.FieldTypeID = ef.FieldTypeID
														where ef.ExecutionID = @executionid and isnull(ef.FieldValue,'') <> isnull(f.FormattedValue,'') and EG.ItemNumber between @beginItemNumber and @endItemNumber and EG.Success is null;

											
														merge into [Group] G
														using ( 
															select AG.ObjectID as GroupID ,
															EG.Name,EG.Description,
															EG.ExecutionItemUid,
															EG.IsActiveDirectoryGroup,
															PO.ObjectID as PrimaryID,
															SO.ObjectID as SecondaryID,
															EG.GroupUid
																from api.ExecutionGroup EG
																left join Asset AG on AG.uid = EG.GroupUid and AG.Object = 'Group'
																left join Asset PO on PO.uid = EG.PrimaryOwnerUid and PO.Object = 'Resource'
																left join Asset SO on SO.uid = EG.SecondaryOwnerUid and SO.Object = 'Resource'
																where EG.ExecutionID = @ExecutionID
																		and EG.ItemNumber between @beginItemNumber and @endItemNumber
																		and EG.Success is null
																) S
														on (G.ID = GroupID)
														when matched then
															update  
																set G.Name = TRIM(S.Name),
																G.Description = S.Description,
																G.PrimaryOwnerResourceID = PrimaryID,
																G.SecondaryOwnerResourceID = SecondaryID,
																G.UpdatedBy = @CurrentResourceID,
																G.UpdatedOn = GETUTCDATE(),
																G.IsActiveDirectoryGroup = S.IsActiveDirectoryGroup
															when not matched then
																insert (Name, Description, PrimaryOwnerResourceID, SecondaryOwnerResourceID,IsActiveDirectoryGroup,UpdatedOn,UpdatedBy)
																values (TRIM(S.Name),S.Description, S.PrimaryID, S.SecondaryID,S.IsActiveDirectoryGroup,GETDATE(),@CurrentResourceID)
															output inserted.ID, $action, TRIM(S.Name), S.ExecutionItemUid into #mergeResultTable;

															INSERT INTO [dbo].[Asset] ([uid], [AssetTypeID],[State],SourceID,[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
															SELECT	coalesce(EG.GroupUid, newid()), T.ID, 1, M.GroupID, 'Group', M.GroupID, G.UpdatedOn, coalesce(G.UpdatedBy, 0), G.UpdatedOn, coalesce(G.UpdatedBy, 0)
															FROM	#mergeResultTable M
																	INNER JOIN api.ExecutionGroup EG on EG.ExecutionItemUid = M.ExecutionItemUid
																	INNER JOIN [Group] G on G.ID = M.GroupID
																	INNER JOIN AssetType T on T.Object = 'GroupType' and T.ObjectID = 1
															WHERE EG.ExecutionID = @ExecutionID and M.[Action] = 'INSERT';

				
															INSERT INTO [ResourceGroup](GroupID,[ResourceID])
															SELECT G.ID, G.PrimaryOwnerResourceID
															FROM [Group] G
															inner join api.ExecutionGroup EG on EG.Name = G.Name
															where EG.ExecutionID = @ExecutionID 
															and EG.ItemNumber between @beginItemNumber and @endItemNumber
															and EG.Success is null
															and EG.GroupUid is null
															and coalesce(EG.PrimaryOwnerUid, 0x0) <> 0x0
															and G.PrimaryOwnerResourceID is not null;

															INSERT INTO [ResourceGroup](GroupID,[ResourceID])
															SELECT G.ID, G.SecondaryOwnerResourceID
															FROM [Group] G
															inner join api.ExecutionGroup EG on EG.Name = G.Name
															where EG.ExecutionID = @ExecutionID 
															and EG.ItemNumber between @beginItemNumber and @endItemNumber
															and EG.Success is null
															and EG.GroupUid is null
															and G.SecondaryOwnerResourceID is not null
															and G.PrimaryOwnerResourceID != G.SecondaryOwnerResourceID;

															IF NOT EXISTS    
															(
															SELECT 1    
															FROM [ResourceGroup] RG
															inner join api.ExecutionGroup EG on EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber and EG.Success is null
															inner join [Group] G on G.Name = EG.Name     
															WHERE ResourceID = G.PrimaryOwnerResourceID and [GroupID] = G.ID 
															)    
															BEGIN
																INSERT INTO [ResourceGroup](GroupID,[ResourceID])
																			SELECT G.ID, G.PrimaryOwnerResourceID
																			FROM [Group] G
																			inner join api.ExecutionGroup EG on EG.Name = G.Name
																			where EG.ExecutionID = @ExecutionID 
																			and EG.ItemNumber between @beginItemNumber and @endItemNumber
																			and EG.Success is null
																			and coalesce(EG.PrimaryOwnerUid, 0x0) <> 0x0
															END

															IF NOT EXISTS    
															(
															SELECT 1    
															FROM [ResourceGroup] RG
															inner join api.ExecutionGroup EG on EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber and EG.Success is null
															inner join [Group] G on G.Name = EG.Name     
															WHERE ResourceID = G.SecondaryOwnerResourceID and [GroupID] = G.ID and G.SecondaryOwnerResourceID is not null
															)    
															BEGIN
																INSERT INTO [ResourceGroup](GroupID,[ResourceID])
																			SELECT G.ID, G.SecondaryOwnerResourceID
																			FROM [Group] G
																			inner join api.ExecutionGroup EG on EG.Name = G.Name
																			where EG.ExecutionID = @ExecutionID 
																			and EG.ItemNumber between @beginItemNumber and @endItemNumber
																			and EG.Success is null
																			and G.SecondaryOwnerResourceID is not null
															END

															update EG
															set EG.GroupUid = AG.uid
															from api.ExecutionGroup EG
															inner join #mergeResultTable Res on Res.ExecutionItemUid = EG.ExecutionItemUid
															inner join [Group] G on G.Name = Res.GroupName
															inner join Asset AG on AG.ObjectID = G.ID and AG.[Object] ='Group'
															where EG.ExecutionID = @ExecutionID and EG.Success is null

															declare @audit table (auditId int)
															insert into reporting.Global_Audit
															OUTPUT INSERTED.ID
															INTO @audit
															select distinct 'Group', g.id, G.Name, @currentresourceid, GETUTCDATE(), 'Updated', 'Group', g.ID, 'Group', G.Name,'Group updated' 
															from #auditRecords ar
															inner join api.ExecutionGroup EF on EF.ExecutionID = @executionID and EF.ItemNumber = ar.ItemNumber
															inner join [Asset] AG on AG.uid = ef.GroupUid and AG.[Object] = 'Group'
															inner join [Group] G on G.ID = AG.ObjectID

															insert into reporting.global_fieldaudit
															select a.auditid,0, ar.fieldname, 1, ar.newvalue, ar.oldvalue from @audit a
															inner join reporting.Global_Audit ga on ga.id = a.auditid
															inner join [Group] G on G.Id = ga.ObjectId
															inner join [Asset] AG on AG.Object = 'Group' and AG.ObjectID = g.ID
															inner join api.ExecutionGroup EF on EF.ExecutionID = @executionID and AG.uid = EF.GroupUid
															inner join #auditRecords ar on ar.ItemNumber = EF.ItemNumber
															where isnull(ar.newvalue,'') <> isnull(ar.oldvalue,'')

															insert into queue.task (Action, Custom, Object, ObjectID, Date, AssetID)
															select 'ObjectIndex', 'U', a.[Object], a.[ObjectID], getdate(), a.id as AssetID
															from api.ExecutionGroup EG
															inner join dbo.asset a on EG.GroupUid = a.uid
															where EG.ExecutionID = @executionID and EG.Success is null";

									Connection.Execute(insertSQL,
											new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);


									if (hasCounterField)
									{
										UpdateGroupCounterFields(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout: timeout);
									}

									string fieldValuesSql = $@"select
										F.FieldTypeID as [FieldTypeID]
										,case 
											when FT.Type = 'Lookup' then F.FieldValue
											else null
										end as [Value]
										,case 
											when FT.Type = 'Lookup' then null
											else F.FieldValue
										end as [FormattedValue]
										,getutcdate() as [UpdatedOn]
										,@resourceId as [UpdatedBy]
										,A.Id as [AssetID]                                        
								from    api.ExecutionGroup EG
										inner join Asset A on A.uid = eg.GroupUid and A.[Object] = 'Group'
										inner join [Group] G on g.ID = A.ObjectID
										inner join api.executionfield F on F.ExecutionID = EG.ExecutionID
											and F.ItemNumber = EG.ItemNumber 
											and A.ObjectID is not null 
											and F.FieldTypeID is not null
											and EG.Success is null
										inner join FieldType FT on FT.Id = F.FieldTypeID
								where   EG.ExecutionID = @executionID
										and EG.ItemNumber between @beginItemNumber and @endItemNumber 
										and (F.Ignore = 0 or F.Ignore is null)
										and FT.Type != 'Relationship'
										and FT.Type != 'Counter'
										and FieldValue is not null";

									if (isInsert)
									{
										Connection.Execute(
											$@"
										INSERT INTO 
										dbo.[Field] ([FieldTypeID],[Value],[FormattedValue],[UpdatedOn],[UpdatedBy],[AssetID])                         
										{fieldValuesSql}"
											, new { execution.ExecutionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
									}
									else
									{

										Connection.Execute($@"
															DELETE Field
															FROM Field F
																inner join api.ExecutionGroup EG on EG.ExecutionID = @executionID 
																inner join Asset A on A.uid = eg.GroupUid and A.[Object] = 'Group'
																inner join [Group] G on G.ID = A.ObjectID
																inner join {ApiExecutionFieldTable} EF on EF.ExecutionId = EG.ExecutionId and EF.ItemNumber = EG.ItemNumber
															WHERE EF.ItemNumber between @beginItemNumber and @endItemNumber
															 and EF.Ignore is null
															 and EF.FieldTypeID is not null
															 and F.AssetID = A.ID
															 and F.FieldTypeID = EF.FieldTypeID
															 and EF.FieldValue is null 
															 and EF.LookupValue is null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);


										Connection.Execute($@"
															merge       Field as T
															using       (
																			{fieldValuesSql}
																		) as S 
															on          ( T.FieldTypeID = S.FieldTypeID and T.AssetID = S.AssetID)
															when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS then
															update set T.Value = S.Value,T.FormattedValue = S.FormattedValue, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate()                     
															when		not matched by target then
															insert		(FieldTypeID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID)
															values		(S.FieldTypeID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID);",
														new { execution.ExecutionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

									}

									MergeGroupAssetDisplayValues(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout: timeout, isInsert);

									Connection.Execute($"update [api].[ExecutionGroup] set Success = 1, Message = 'Success' where	Success is null and ExecutionID = @ExecutionID;",
														new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;
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
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionGroup", ex.GetFullExceptionData(false), timeout);
									}
								}
							}
						}

						results.AddRange(
								Query<GroupResponseResult>(
									$"select [ItemNumber],[GroupUid] as uid,[ExecutionItemUid],[Message],[Success] from api.ExecutionGroup where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
									new { execution.ExecutionID, beginItemNumber, endItemNumber }
								)
							);

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}

					Connection.Close();
				}
			}

			// TODO: Add event grid calls here.
			return results;
		}

		public async Task<List<ResponsibilityRuleUpsertResponseModel>> UpsertResponsibilityRules(ApiExecution execution, Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> import, int timeout = 3600)
		{
			List<ResponsibilityRuleUpsertResponseModel> results = new List<ResponsibilityRuleUpsertResponseModel>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
			var uidDupes = import.Where(x => x.Uid.HasValue).GroupBy(x => x.Uid).Where(x => x.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
			}
			else if (uidDupes.Any())
			{
				string message = $"Duplicate uid item identifiers: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { Uid = i.Uid.Value, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityRule");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<ResponsibilityRuleUpsertResponseModel>(
								$"select * from api.ExecutionResponsibilityRule where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID }
							)
						);
					}

					#region Build data tables.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ResponsibilityTypeUid", typeof(Guid));
					table.Columns.Add("AssetTypeUid", typeof(Guid));
					table.Columns.Add("uid", typeof(Guid));
					table.Columns.Add("Name", typeof(string));
					table.Columns.Add("IsVisible", typeof(bool));
					table.Columns.Add("ApplyToType", typeof(bool));
					table.Columns.Add("Context", typeof(string));
					table.Columns.Add("Definition", typeof(string));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));

					#endregion

					#region Generate data sets

					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							ResponsibilityRuleUpsertModel model = import[i - 1];
							string rowError = string.Empty;

							DataRow row = table.NewRow();

							row["ExecutionID"] = execution.ExecutionID;
							row["ItemNumber"] = i;

							if (model.ExecutionItemUid.HasValue)
							{
								row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
							}
							else
							{
								row["ExecutionItemUid"] = Guid.NewGuid();
							}

							row["ResponsibilityTypeUid"] = responsibilityTypeUid;

							if (model.AssetTypeUid.HasValue)
							{
								row["AssetTypeUid"] = model.AssetTypeUid;
							}

							row["Name"] = model.Name;
							row["IsVisible"] = model.IsVisible;
							row["ApplyToType"] = model.ApplyToType;
							row["Context"] = model.Context;

							if (model.Definition != null)
							{
								if (model?.Definition?.Then != null)
								{
									model.Definition.Then.ForEach(th => {
										if (th.AssigneeTypeUid.HasValue && th.Conditions.All(c => !c.AssigneeTypeUid.HasValue))
										{
											th.Conditions.ForEach(co => co.AssigneeTypeUid = th.AssigneeTypeUid);
										}
									});
								}

								row["Definition"] = JsonConvert.SerializeObject(model.Definition);
							}

							if (execution.Method.ToLower() == "put" && !model.Uid.HasValue)
							{
								rowError += ";UID cannot be empty!";
							}

							if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
							{
								row["uid"] = model.Uid.Value;
							}

							//initial validation
							if (!model.AssetTypeUid.HasValue || model.AssetTypeUid.Value == Guid.Empty)
							{
								rowError += ";AssetTypeUid is not valid!";
							}

							if (string.IsNullOrEmpty(model.Name))
							{
								rowError += ";Name cannot be empty.";
							}

							if (model.Definition == null)
							{
								rowError += ";Definition cannot be empty/null.";
							}

							if (model.ApplyToType == true && (model.Definition.When != null && model.Definition.When.Count > 0))
							{
								rowError += "Cannot use When conditions when ApplyToType value is set to true.";
							}

							if (model.ApplyToType == false && (model.Definition.When == null || model.Definition?.When?.Count == 0))
							{
								rowError += Messages.Error_Responsibility_ApplyToType_False;
							}

							model.Definition.Then.ForEach(th =>
							{
								if (th.AssigneeTypeUid == null || th.AssigneeTypeUid == Guid.Empty || th.Conditions.Any(c => { return (c.AssigneeTypeUid == null || c.AssigneeTypeUid == Guid.Empty); }))
								{
									rowError += ";AssigneeTypeUid cannot be null or empty.";
								}
								th.Conditions.ForEach(cond =>
								{
									if (cond.Assignee == null && cond.Field == null)
									{
										rowError += ";Then condition should have either Field or Assignee values set.";
									}

									if (cond.Assignee != null && cond.Field != null)
									{
										rowError += ";Condition cannot have Field and Asignee within same condition.";
									}

									if (cond.Assignee != null)
									{
										if (!cond.Assignee.Uid.HasValue || cond.Assignee.Uid.Value == Guid.Empty)
										{
											rowError += ";Assignee Uid is required field.";
										}
									}

									if (cond.Field != null)
									{
										if (string.IsNullOrEmpty(cond.Field.ApiName))
										{
											rowError += ";ApiName is required field.";
										}

										if (string.IsNullOrEmpty(cond.Field.Value))
										{
											rowError += ";Value is required field.";
										}
									}
								});

							});

							if (model.Definition.When != null && model.Definition.When.Count > 0)
							{
								model.Definition.When.ForEach(cond =>
								{
									if (cond.Relation == null && cond.Field == null)
									{
										rowError += ";Then condition should have either Field or Relation value set.";
									}

									if (cond.Relation != null && cond.Field != null)
									{
										rowError += ";Condition cannot have Field and Relation within same condition.";
									}

									if (cond.Relation != null)
									{
										if (!cond.Relation.IntersectTypeUid.HasValue)
										{
											rowError += ";IntersectTypeUid is required field.";
										}

										if (!cond.Relation.AssetUid.HasValue)
										{
											rowError += ";AssetUid is required field.";
										}
									}

									if (cond.Field != null)
									{
										if (string.IsNullOrEmpty(cond.Field.ApiName))
										{
											rowError += ";ApiName is required field.";
										}

										if (string.IsNullOrEmpty(cond.Field.Value))
										{
											rowError += ";Value is required field.";
										}
									}
								});
							}

							if (!string.IsNullOrEmpty(rowError))
							{
								row["Message"] = rowError.Trim(';');
								row["Success"] = false;
							}

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
						bulkCopy.DestinationTableName = "api.ExecutionResponsibilityRule";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("Uid", "uid");
						bulkCopy.ColumnMappings.Add("ResponsibilityTypeUid", "ResponsibilityTypeUid");
						bulkCopy.ColumnMappings.Add("AssetTypeUid", "AssetTypeUid");
						bulkCopy.ColumnMappings.Add("Name", "Name");
						bulkCopy.ColumnMappings.Add("IsVisible", "IsVisible");
						bulkCopy.ColumnMappings.Add("ApplyToType", "ApplyToType");
						bulkCopy.ColumnMappings.Add("Context", "Context");
						bulkCopy.ColumnMappings.Add("Definition", "Definition");
						bulkCopy.ColumnMappings.Add("Success", "Success");
						bulkCopy.ColumnMappings.Add("Message", "Message");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					#region Log data errors

					string checkSQL = $@"
										update	api.ExecutionResponsibilityRule 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Responsibility Rule with specified Uid not found!'
										from api.ExecutionResponsibilityRule EP
										inner join api.execution ae on ae.executionid = ep.executionid
										left join ResponsibilityTypeRelationRule rtrr on rtrr.uid = ep.uid
										where	ep.ExecutionID = @ExecutionID and EP.Uid is not null and rtrr.uid is null and ae.Method = 'Put';

										update	api.ExecutionResponsibilityRule 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Invalid Asset Type Uid'
										from api.ExecutionResponsibilityRule EP
										left join AssetType AT on AT.uid = EP.AssetTypeUid
										where	ExecutionID = @ExecutionID and AT.Id is null;

										drop table if exists #allowedTypes
										select distinct at.uid 
										into #allowedTypes
										from api.ExecutionResponsibilityRule EP
											inner join ResponsibilityType RT on rt.uid = EP.ResponsibilityTypeUid
											inner join [ResponsibilityTypeRelation] RR on RR.ResponsibilityTypeID = RT.Id
											inner join assettype at on rr.ObjectType=at.Object and rr.ObjectID = at.ObjectID
										where ExecutionID = @executionId;

										update	api.ExecutionResponsibilityRule 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Invalid Asset Type Uid for Responsibility Type.'
										from api.ExecutionResponsibilityRule EP
										left join AssetType AT on AT.uid = EP.AssetTypeUid
										where	ExecutionID = @ExecutionID and (AT.Id is null or AT.uid not in (select * from #allowedTypes));";

					Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

					#endregion

					#region Parse new json to old format

					ParseResponsibilityRuleModel(execution.ExecutionID, null, timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<ResponsibilityRuleUpsertResponseModel>();
					results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					List<PredicateType> predicateTypes = Enum.GetValues(typeof(PredicateType)).Cast<PredicateType>().ToList();

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							string querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{

									string insertSQL = $@"
									DECLARE @mergeResults table(  
										uid uniqueidentifier,  
										executionid uniqueidentifier,  
										itemnumber int); 

									MERGE dbo.ResponsibilityTypeRelationRule RTRR
									USING (
									select 
									xrr.executionid,
									xrr.itemnumber,
									xrr.uid,
									rt.id as ResponsibilityTypeId,
									at.object as Object,
									at.objectid as ObjectId,
									xrr.Name,
									xrr.Context,
									xrr.IsVisible,
									xrr.ApplyToType, 
									xrr.DefinitionConverted,
									ae.method
										from api.executionresponsibilityrule xrr
									inner join api.execution ae on ae.executionid = xrr.executionid
									inner join assettype at on at.uid = xrr.AssetTypeUid
									inner join ResponsibilityType rt on rt.uid = xrr.ResponsibilityTypeUid
									where xrr.executionid = @ExecutionID and xrr.ItemNumber between @beginItemNumber and @endItemNumber and xrr.success is null
									)Data
									ON (RTRR.uid = Data.uid and method = 'PUT')
									WHEN MATCHED
										THEN update set 
											name = data.name,
											ResponsibilityTypeId = data.ResponsibilityTypeId,
											object = data.Object,
											objectId = data.ObjectId,
											context = data.context,
											isvisible = data.isvisible,
											applytotype = data.applytotype,
											definition = data.DefinitionConverted,
											lastrunon = '1/1/2000',
											updatedon = getdate(),
											updatedby = @resourceId
									WHEN NOT MATCHED
										THEN insert (uid,ResponsibilityTypeId,Object,ObjectId,Name,Context,IsVisible, ApplyToType,CreatedOn,CreatedBy,Definition)
										values (isnull(data.uid, newid()), data.ResponsibilityTypeId,data.Object, data.ObjectId, data.Name, data.Context, data.IsVisible, data.ApplyToType, getdate(), @resourceId,data.DefinitionConverted)
										output inserted.uid, data.executionid, data.itemnumber into @mergeResults;

									update api.executionresponsibilityrule
										set uid = mr.uid
									from @mergeResults mr 
										where executionresponsibilityrule.executionid = mr.executionid and executionresponsibilityrule.itemnumber = mr.itemnumber";

									Connection.Execute(insertSQL,
											new { execution.ExecutionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

									Connection.Execute(
										$"update P set P.Success = 1 from api.ExecutionResponsibilityRule P where	{querySuffix};",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;

									try
									{
										await ProcessRulesForExecution(execution.ExecutionID, beginItemNumber, endItemNumber);

									}
									catch (Exception ex)
									{
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityRule", ex.GetFullExceptionData(false), timeout);
									}

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
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityRule", ex.GetFullExceptionData(false), timeout);
									}
								}
							}
						}

						results.AddRange(
							Query<ResponsibilityRuleUpsertResponseModel>(
								$"select * from api.ExecutionResponsibilityRule where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
								new { execution.ExecutionID, beginItemNumber, endItemNumber }
							)
						);

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}

					Connection.Close();
				}
			}

			return results;
		}

		public List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(ApiExecution execution, List<ResponsibilityTypeUpsertModel> import, int timeout = 3600)
		{
			List<ResponsibilityTypeUpsertResult> results = new List<ResponsibilityTypeUpsertResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
			var nameDupes = import.GroupBy(i => i.Name).Where(i => i.Count() > 1).Select(i => new { Name = i.Key, Count = i.Count() }).ToList();

			if (uidDupes.Any() && execution.Method == "PUT")
			{
				string message = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new ResponsibilityTypeUpsertResult { Uid = i.Uid.Value, Message = execution.ErrorMessage, Success = false }));
			}
			else if (nameDupes.Any())
			{
				for (int idx = 0; idx < import.Count; idx++)
				{
					var dupe = nameDupes.FirstOrDefault(x => x.Name == import[idx].Name);
					results.Add(new ResponsibilityTypeUpsertResult()
					{
						ItemNumber = idx,
						Success = false,
						Message = dupe == null ? "Names must be unique within a batch." : $"Duplicate Name '{dupe.Name}'. Names must be unique within a batch."
					});
				}
			}
			else
			{

				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityType");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<ResponsibilityTypeUpsertResult>(
								$"select * from api.ExecutionResponsibilityType where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID }
							)
						);
					}

					#region Build data tables.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ResponsibilityTypeId", typeof(long));
					table.Columns.Add("Uid", typeof(Guid));
					table.Columns.Add("Name", typeof(string));
					table.Columns.Add("Description", typeof(string));
					table.Columns.Add("IsNew", typeof(bool));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));

					#endregion

					#region Generate data sets

					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							ResponsibilityTypeUpsertModel model = import[i - 1];

							DataRow row = table.NewRow();

							row["ExecutionID"] = execution.ExecutionID;
							row["ExecutionItemUid"] = Guid.NewGuid();
							row["ItemNumber"] = i;
							if (model.Name == null)
							{
								row["Name"] = "";
							}
							else
							{
								row["Name"] = model.Name.Trim();
							}
							row["Description"] = model.Description.SanitizeHtml();
							if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
							{
								row["Uid"] = model.Uid;
							}

							if (model.IsNew == true)
							{
								row["IsNew"] = true;
							}
							else
							{
								row["IsNew"] = false;
							}

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
						bulkCopy.DestinationTableName = "api.ExecutionResponsibilityType";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("Name", "Name");
						bulkCopy.ColumnMappings.Add("Description", "Description");
						bulkCopy.ColumnMappings.Add("Uid", "Uid");
						bulkCopy.ColumnMappings.Add("IsNew", "IsNew");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					#region Log data errors
					string checkSQL = $@"
										update api.ExecutionResponsibilityType
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Invalid UID value'
										from api.ExecutionResponsibilityType ERT
											inner join api.Execution AE on AE.ExecutionID = ERT.ExecutionID
										where   AE.Method = 'PUT' and ERT.ExecutionID = @ExecutionID and (ERT.Uid is null or ERT.Uid = '00000000-0000-0000-0000-000000000000')

										update	api.ExecutionResponsibilityType 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Responsibility type with same Name already exists'
										from api.ExecutionResponsibilityType ERT
										inner join [ResponsibilityType] RT on RT.Name = ERT.Name
										where	ExecutionID = @ExecutionID  and (RT.Uid <> ERT.Uid or ERT.Uid is null);

										update	api.ExecutionResponsibilityType 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Responsibility type with this Uid does not exists'
										from api.ExecutionResponsibilityType ERT
										inner join api.Execution AE on AE.ExecutionID = ERT.ExecutionID
										left join [ResponsibilityType] RT on RT.Uid = ERT.Uid
										where	 AE.Method = 'PUT' and ERT.ExecutionID = @ExecutionID and ERT.Uid is not null and RT.Uid is null;

										update	api.ExecutionResponsibilityType 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Name field cannot be empty'
										where	ExecutionID = @ExecutionID and (Name is null or Name = '');";

					Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<ResponsibilityTypeUpsertResult>();
					results.AddRange(import.Select(i => new ResponsibilityTypeUpsertResult { Message = msg, Success = false }));
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					List<PredicateType> predicateTypes = Enum.GetValues(typeof(PredicateType)).Cast<PredicateType>().ToList();

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							string querySuffix = $"ERT.Success is null and ERT.ExecutionID = @ExecutionID and ERT.ItemNumber between @beginItemNumber and @endItemNumber";
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{

									string insertSQL = $@"
										drop table if exists #mergeResultTable
										create table #mergeResultTable (ResponsibilityTypeId int, ResponsibilityTypeUid uniqueidentifier, ExecutionItemUid uniqueidentifier) 

										merge into [ResponsibilityType] RT
										using ( select * 
												from api.ExecutionResponsibilityType
												where ExecutionID = @ExecutionID
														and ItemNumber between @beginItemNumber and @endItemNumber
														and ResponsibilityTypeId is null
														and Success is null
												) S
										on (RT.Uid = S.Uid and S.IsNew = 0)
										when matched then
										update  
											set RT.Name = S.Name,
											RT.Description = S.Description,
											UpdatedOn = getutcdate(),
											UpdatedBy = @CurrentResourceID
										when not matched then
											insert (Name, Description, Uid, CreatedOn, CreatedBy)
											values (S.Name,S.Description, ISNULL(S.Uid,newid()), getutcdate(), @CurrentResourceID)
										output inserted.ID, inserted.Uid, S.ExecutionItemUid into #mergeResultTable;

										update RT
										set RT.ResponsibilityTypeId = Res.ResponsibilityTypeId,
											RT.Uid = Res.ResponsibilityTypeUid,
											RT.Success = 1
										from api.ExecutionResponsibilityType RT
												inner join #mergeResultTable Res on Res.ExecutionItemUid = RT.ExecutionItemUid
										where RT.ExecutionID = @ExecutionID and RT.Success is null";

									Connection.Execute(insertSQL,
											new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

									Connection.Execute(
										$"update ERT set ERT.Success = 1 from api.ExecutionResponsibilityType ERT where	{querySuffix} and ERT.ResponsibilityTypeId is not null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;

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
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityType", ex.GetFullExceptionData(false), timeout);
									}
								}
							}
						}

						results.AddRange(
							Query<ResponsibilityTypeUpsertResult>(
								$"select * from api.ExecutionResponsibilityType where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
								new { execution.ExecutionID, beginItemNumber, endItemNumber }
							)
						);


						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}

					Connection.Close();
				}
			}

			return results;
		}

		#endregion
	}
}
