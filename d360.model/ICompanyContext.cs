using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.entities.Contracts;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using d360.core.entities.Workflow;
using d360.core.entities.Graph;
using d360.core.enums;
using d360.core.queue;
using d360.model.DataAccessLayer;
using Dapper;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Data;
using d360.core.entities.Membership;

namespace d360.model
{
    public interface ICompanyContext : IBaseContext
    {
        DbSet<ApiEndpoint> ApiEndpoints { get; set; }
        DbSet<ApiEndpointVersion> ApiEndpointVersions { get; set; }
        DbSet<ApiEntity> ApiEntities { get; set; }
        DbSet<ApiEntityFieldTypeMultiSelectField> ApiEntityFieldTypeMultiSelectFields { get; set; }
        DbSet<ApiEntityFieldType> ApiEntityFieldTypes { get; set; }
        DbSet<ApiEntityUri> ApiEntityUris { get; set; }
        DbSet<ApiExecution> ApiExecutions { get; set; }
        DbSet<ApiNamespace> ApiNamespaces { get; set; }
        DbSet<ApiService> ApiServices { get; set; }
        DbSet<AssetApiModel> AssetApiModels { get; set; }
        DbSet<AssetDetail> AssetDetails { get; set; }
        DbSet<Asset> Assets { get; set; }
        DbSet<AssetProcessDiagram> AssetProcessDiagrams { get; set; }
        DbSet<AssetTypeExportTemplate> AssetTypeExportTemplates { get; set; }
        DbSet<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }
        DbSet<AssetTypeLevel> AssetTypeLevels { get; set; }
        DbSet<AssetTypeStyle> AssetTypeStyles { get; set; }
        DbSet<AssetType> AssetTypes { get; set; }
        DbSet<AuditField> AuditFields { get; set; }
        DbSet<Audit> Audits { get; set; }
        string BulkLoadStatusMsg { get; set; }
        DbSet<CommentRelation> CommentRelations { get; set; }
        DbSet<Comment> Comments { get; set; }
        DbSet<ContractAcceptance> ContractAcceptance { get; set; }
        DbSet<Contract> Contracts { get; set; }
        DbSet<Favorite> Favorites { get; set; }
        DbSet<FieldApiModel> FieldApiModels { get; set; }
        DbSet<FieldLookupValue> FieldLookupValues { get; set; }
        DbSet<Field> Fields { get; set; }
        DbSet<FieldTypeLookup> FieldTypeLookups { get; set; }
        DbSet<FieldType> FieldTypes { get; set; }
        DbSet<FieldWithRelation> FieldWithRelations { get; set; }
        DbSet<FollowDetail> FollowDetails { get; set; }
        DbSet<Follow> Follows { get; set; }
        DbSet<FusionAgentError> FusionAgentErrors { get; set; }
        DbSet<FusionAttribute> FusionAttributes { get; set; }
        DbSet<FusionAttributeTypeCustomQuery> FusionAttributeTypeCustomQueries { get; set; }
        DbSet<FusionAttributeType> FusionAttributeTypes { get; set; }
        DbSet<FusionExecution> FusionExecutions { get; set; }
        DbSet<FusionQueryAttribute> FusionQueryAttributes { get; set; }
        DbSet<FusionQueryAttributeType> FusionQueryAttributeTypes { get; set; }
        DbSet<FusionStatusLog> FusionStatusLogs { get; set; }
        DbSet<Fusion> FusionTypeConfigurations { get; set; }
        DbSet<FusionType> FusionTypes { get; set; }
        DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }
        DbSet<GraphFilter> GraphFilters { get; set; }
        DbSet<Group> Groups { get; set; }
        DbSet<IntegrationAssetTypeFieldItem> IntegrationAssetTypeFieldItems { get; set; }
        DbSet<IntegrationAssetTypeRelationItem> IntegrationAssetTypeRelationItems { get; set; }
        DbSet<IntegrationAssetTypeRelationItemTarget> IntegrationAssetTypeRelationItemTargets { get; set; }
        DbSet<IntegrationAssetTypeRoleItem> IntegrationAssetTypeRoleItems { get; set; }
        DbSet<IntegrationAssetType> IntegrationAssetTypes { get; set; }
        DbSet<IntegrationExecutionAssetType> IntegrationExecutionAssetTypes { get; set; }
        DbSet<IntegrationSetting> IntegrationSettings { get; set; }
        DbSet<IntegrationUnresolvedRelationItem> IntegrationUnresolvedRelationItems { get; set; }
        DbSet<IntersectDetail> IntersectDetails { get; set; }
        DbSet<Intersect> Intersects { get; set; }
        DbSet<IntersectTypeDetail> IntersectTypeDetails { get; set; }
        DbSet<IntersectType> IntersectTypes { get; set; }
        DbSet<Issue> Issues { get; set; }
        DbSet<IssueTypeRelation> IssueTypeRelations { get; set; }
        DbSet<IssueType> IssueTypes { get; set; }
        DbSet<LoadColumn> LoadColumns { get; set; }
        DbSet<LoadItemColumn> LoadItemColumns { get; set; }
        DbSet<LoadItem> LoadItems { get; set; }
        DbSet<Load> Loads { get; set; }
        DbSet<MapGroupItem> MapGroupItems { get; set; }
        DbSet<MapGroup> MapGroups { get; set; }
        DbSet<MapItem> MapItems { get; set; }
        DbSet<MapRuleItemMapItem> MapRuleItemMapItems { get; set; }
        DbSet<MapRuleItem> MapRuleItems { get; set; }
        DbSet<MapRule> MapRules { get; set; }
        DbSet<Map> Maps { get; set; }
        DbSet<MapSequenceContext> MapSequenceContexts { get; set; }
        DbSet<MapSequence> MapSequences { get; set; }
        DbSet<MapTypeOrder> MapTypeOrders { get; set; }
        DbSet<MapType> MapTypes { get; set; }
        DbSet<MetricAsset> MetricAssets { get; set; }
        DbSet<MetricAssetVersion> MetricAssetVersions { get; set; }
        DbSet<MetricAssetVersionCondition> MetricAssetVersionConditions { get; set; }
        DbSet<MetricAssetVersionConditionItem> MetricAssetVersionConditionItems { get; set; }
        DbSet<MetricAssetVersionConditionItemValue> MetricAssetVersionConditionItemValues { get; set; }
        DbSet<NymRelation> NymRelations { get; set; }
        DbSet<Nym> Nyms { get; set; }
        DbSet<OrganizationDetail> OrganizationDetails { get; set; }
        DbSet<OrganizationDomain> OrganizationDomains { get; set; }
        DbSet<OrganizationInvitationDetail> OrganizationInvitationDetails { get; set; }
        DbSet<OrganizationInvitation> OrganizationInvitations { get; set; }
        DbSet<OrganizationRegistration> OrganizationRegistrations { get; set; }
        DbSet<OrganizationResourceDetail> OrganizationResourceDetails { get; set; }
        DbSet<OrganizationResource> OrganizationResources { get; set; }
        DbSet<Organization> Organizations { get; set; }
        DbSet<OrganizationType> OrganizationTypes { get; set; }
        DbSet<Predicate> Predicates { get; set; }
        DbSet<Question> Questions { get; set; }
        DbSet<QuestionTypeOption> QuestionTypeOptions { get; set; }
        DbSet<QuestionType> QuestionTypes { get; set; }
        DbSet<ReportResponsibility> ReportResponsibilities { get; set; }
        DbSet<Report> Reports { get; set; }
        DbSet<ResourceGroup> ResourceGroups { get; set; }
        DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }
        DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }
        DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }
        DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }
        DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }
        DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }
        DbSet<d360.core.entities.Rule> Rules { get; set; }
        DbSet<Score> Scores { get; set; }
        DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        DbSet<ShoppingCart> ShoppingCarts { get; set; }
        DbSet<ShoppingCartType> ShoppingCartTypes { get; set; }
        DbSet<Shortcut> Shortcuts { get; set; }
        DbSet<SiteNav> SiteNav { get; set; }
        DbSet<SiteNavPermission> SiteNavPermissions { get; set; }
        DbSet<Survey> Surveys { get; set; }
        DbSet<SurveyType> SurveyTypes { get; set; }
        DbSet<Tag> Tags { get; set; }
        DbSet<ConnectorLabel> ConnectorLabels { get; set; }
        DbSet<AssetTag> AssetTags { get; set; }
        DbSet<WorkflowEventRegistration> WorkflowEventRegistrations { get; set; }
        DbSet<WorkflowItemAssignment> WorkflowItemAssignments { get; set; }
        DbSet<WorkflowItem> WorkflowItems { get; set; }
        DbSet<WorkflowItemStep> WorkflowItemSteps { get; set; }
        DbSet<WorkflowItemStepTransition> WorkflowItemStepTransitions { get; set; }
        DbSet<WorkflowTaskProcedure> WorkflowTaskProcedures { get; set; }
        DbSet<core.entities.Workflow.Type> WorkflowTypes { get; set; }
        DbSet<WorkflowVersion> WorkflowVersions { get; set; }
        DbSet<WorkflowVersionStep> WorkflowVersionSteps { get; set; }
        DbSet<WorkflowVersionStepTransition> WorkflowVersionStepTransitions { get; set; }
        DbSet<MetricAllocation> MetricAllocations { get; set; }

        int ApiTimeout { get; }
        event EventHandler<AssetsPartiallyProcessedEventArgs> AssetsPartiallyProcessed;
        event EventHandler<RelationshipsPartiallyProcessedEventArgs> RelationshipsPartiallyProcessed;

        new bool Add<T>(T item) where T : BaseObject;
        IQueryable<CommentDetail> AddComment(Comment comment, ICollection<CommentRelation> relations);
        IntersectDetail AddIntersect(int intersectTypeID, string subject, int subjectID, string @object, int objectID);
        IntersectDetail AddIntersect(int intersectTypeID, SystemObjects subject, int subjectID, SystemObjects @object, int objectID);
        bool AddObjectParentRelationship(SystemObjects type, int typeId, SystemObjects objectType, int parentID, int objectID, PredicateType predicateType = PredicateType.InterTypeHierarchy);
        void AddOrUpdateFields(List<Field> items);
        int AddWebStatistic(SystemObjects @object, int objectID, string ip, string userAgent, string host, string browserLanguage, string action, int resourceID, DateTime timestamp);
        bool AssignActivityWorkflowToNewObject(WorkflowEventRegistration reg, int itemId, int workflowId, int objectId, string @object);
        void BulkLoadParseFile(int loadID);
        List<ExternalScoreResultsApiResultsModel> BulkExternalResultsImport(List<ExternalScoreResultsApiPostModel> model, ApiExecution execution, ScoreType scoreType);
        List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution, ScoreType scoreType = ScoreType.Governance, bool useAllocation = false);
        Task BulkWorkflowFormReassign(List<WorkflowItemStep> itemSteps, GlobalReportingResource resource, int originalResourceId, bool sendFormEmails = true);
        void ClearInvalidRelationRuleResults();
        void CompleteItemStepAssignments(long itemStepID);
        void CreateOrUpdateTypeDisplayValuesAsync(int objectTypeId, string objectType);
        Task<bool> CreateWorkflowItem(int workflowTypeID, EventObjectInfo objectInfo, WorkflowEventRegistration registration, int requestorId, bool isTest = false);
        bool Delete(SystemObjects type, int id);
        new bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        new bool Delete<T>(T entity) where T : BaseObject;
        bool DeleteRelationship(int id);
        IQueryable<CommentDetail> EditComment(Comment comment, ICollection<CommentRelation> relations);
        void Enqueue(string queueName, QueueObject item);
        Task EvaluateWorkflowTransition(long versionStepTransitionID, long itemID, EventObjectInfo objectInfo);
        Task<bool> ExecuteScheduledWorkflow(WorkflowEventRegistration registration);
        Task ExecuteStep(long itemStepID, long itemID, EventObjectInfo objectInfo);
        bool ExecuteTimerSteps();
        string GenerateFormResponsesEmailContent(long itemId);
        Task GenerateMarkitBusinessLineage();
        Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type);
        List<AllocationPossibility> GetAllocationOptions();
        Task<IEnumerable<AllowedIntersectionType>> GetAllowedIntersectionTypes(string type, int id);
        IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAsset(long id);
        AssetDetail GetAssetDetail(long id);
        AssetDetail GetAssetDetail(string objectType, long objectId);
        string GetAssetTypeNoReadSqlStatement(string identifier = null);
        string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null);
        List<FusionAttributeItem> GetAttributesByFusion(int fusionID);
        IEnumerable<AssetType> GetChildTypes(int id, SystemObjects obj);
        IQueryable<CommentCount> GetCommentCountByFollower(int resourceID, int daysToGet = 0, string searchPhrase = "");
        IQueryable<CommentCount> GetCommentCountByType(SystemObjects type, int id, int daysToGet = 0, string searchPhrase = "");
        IQueryable<CommentDetail> GetCommentDetail(int id);
        IQueryable<CommentDetail> GetCommentDetailsByFollower(int resourceID, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "");
        IQueryable<CommentDetail> GetCommentDetailsByID(int id);
        IQueryable<CommentDetail> GetCommentDetailsByType(SystemObjects type, int id, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "");
        Task<T> GetDatabaseJsonAsObjectAsync<T>(string query, DynamicParameters dbArgs, int timeout = 90);
        Task<IEnumerable<FieldFilterModel>> GetFieldFiltersByType(SystemObjects type, int id);
        IQueryable<FieldWithRelation> GetFieldRelationsByObject(SystemObjects type, int id);
        IQueryable<FieldType> GetFieldTypesByObject(SystemObjects type, int id);
        IQueryable<FollowDetail> GetFollowersByObject(SystemObjects type, int id);
        Follow GetFollowingParent(SystemObjects type, int objectID, int? resourceID);
        string GetFormattedFieldLookupValue(int fieldTypeID, string fieldValue);
        Dictionary<string, object> GetFusionAsDictionary(int id);
        List<FusionOwnerOption> GetFusionOwnerOptions();
        string GetIntersectTypeName(IntersectType intersectType);
        List<IntersectTypeOption> GetIntersectTypeOptions(SystemObjects? subject = null, int? subjectID = null, SystemObjects? @object = null, int? objectID = null, int? predicateID = null, List<AssetTypeClass> limitToClasses = null);
        List<Predicate> GetPredicateOptions(int lineageVersion, SystemObjects subject, int subjectID, SystemObjects? @object = null, int? objectID = null, int? predicateID = null);
        IEnumerable<dynamic> GetLoadColumnDetails(int id);
        BulkLoadGetLoadColumnsModel GetLoadColumns(string action, string type, int id, bool includeLookupValues);
        BulkLoadGetLoadColumnsModel GetLoadColumns(string action, SystemObjects type, int id, bool includeLookupValues);
        LoadDetail GetLoadDetail(int id);
        IEnumerable<LoadDetail> GetLoadDetails();
        IEnumerable<dynamic> GetLoadItemDetails(int id);
        List<AssetMeasureModel> GetAssetMeasuresFromRuleResults(List<Guid> ruleResultUids);
        string GetNoReadSqlStatement(string identifier = null);
        string GetNoReadSqlStatement(Permission permission, string identifier = null);
        ObjectDetail GetObjectDetail(string type, long id);
        ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id);
        AssetTypeStyle GetAssetTypeStyle(int assetTypeId);
        AssetTypeStyle GetAssetTypeStyle(Guid assetTypeUid);
        AssetTypeStyle GetAssetTypeStyle(string type, int id);
        string GetObjectTypePath(string type, long id);
        JObject GetPageInformation(SystemObjects o, int oid);
        AssetDetail GetParentObject(int id, SystemObjects obj);
        AssetType GetParentType(int id, SystemObjects obj);
        List<PermissionInfo> GetPermissions(long assetId, int assetTypeId);
        Dictionary<string, object> GetRelationshipFieldItems(int fieldTypeID, string @object = null, int? objectID = null, int offset = 0, int rows = 25, string query = null, bool includeSelection = true);
        Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
        IEnumerable<SecurityResult> GetThenResults(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction trans = null);
        List<PermissionInfo> GetTypePermissions(string type, int typeID);
        string GetUserHomePage();
        IEnumerable<ObjectResult> GetWhenResults(ResponsibilityTypeRelationRule rule, SqlTransaction trans = null);
        IEnumerable<GlobalReportingResource> GetWorkflowUsersBasedOnResponsibility(int typeID, int stepID, long itemID);
        IEnumerable<GlobalReportingResource> GetWorkflowUsersBasedOnGroup(int groupId);
        bool HasAssetDefaultReadPermission(string type, int id, Permission permission = Permission.ReadAsset);
        bool HasAssetPermission(long id, Permission permission);
        bool HasAssetPermission(string type, int id, Permission permission);
        bool HasAssetPermission(SystemObjects type, int id, Permission permission);
        bool HasAssetTypePermission(string type, int id, Permission permission);
        bool HasAssetTypePermission(SystemObjects type, int id, Permission permission);
        int? GetAssetScore(long assetId, ScoreType type);
        List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeInsert> import, int timeout = 3600);
        List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeUpdate> import, int timeout = 3600);
        List<RelationshipTypeResult> DeleteRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeDelete> import, int timeout = 3600);
        List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500, bool sendGraphEvents = true, bool useTempTablesForField = false);
        List<DatabaseBulkRelationshipResult> ImportRelationships(ApiExecution execution, IntersectType rt, RelationshipInserts import, int timeout = 3600, bool sendWorkflowEvents = false, bool lookupFieldsPassedByValue = false, bool sendGraphEvents = true);
        List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType it, RelationshipDeletes import, int timeout = 3600, bool sendWorkflowEvents = false, bool sendGraphEvents = true);
        List<AssetCrossReferenceResult> ImportCrossReferences(ApiExecution execution, IEnumerable<AssetCrossReference> import, int timeout = 3600);
        bool IsUserFollowing(SystemObjects type, int objectID, int? resourceID);
        bool IsUserFollowingParent(SystemObjects type, int objectID, int? resourceID);
        bool IsValidReportingQuery(string statement);
        Task<int> MarkStepAsCompleteAndContinue(WorkflowItemStep itemStep, long itemID, EventObjectInfo objectInfo);
        bool ObjectHasChildren(SystemObjects type, int id);
        bool ObjectHasParent(SystemObjects type, int id);
        Task<string> ProcessMessageTokens(string bodyTemplate, EventObjectInfo objectInfo, string prefix, WorkflowItemStep itemStep, bool supportHtml = true);
        Task<string> ProcessMessageTokens(string bodyTemplate, int objectID, SystemObjects obj, string prefix, WorkflowItemStep itemStep, bool supportHtml);
        Task ProcessResponsibilityRelationRules(int? ruleID = null, int timeout = 7200);
        IEnumerable<T> Query<T>(string sql, object param = null, int timeout = 90);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int timeout = 90);
        Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, int timeout = 90);
        Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object param = null, int timeout = 90);
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, int timeout = 90);
        Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null, int timeout = 90);
        void RebuildDisplayValuesRequest();
        void RebuildAssetGraphRequest();
        void RebuildIndexRequest();
        List<DatabaseBulkAssetResult> RemoveAssets(ApiExecution execution, AssetType at, AssetDeletes import, int timeout = 3600, bool sendWorkflowEvents = true, bool sendGraphEvents = true);
        void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation);
        string RenderTooltip(string action, SystemObjects type, int id);
        void RequestObjectCertification(SystemObjects @object, int objectId, SystemObjects objectType, int objectTypeId);
        int SaveChanges();
        bool SaveOrUpdate<T>(T entity, List<Field> fields, int parentId = -1, bool forceUpdate = false) where T : BaseIntObject, IFieldsObject;
        bool SaveOrUpdateAsset(Asset asset, List<Field> fields, int parentId = -1);
        List<string> SelectQueryColumns(string statement);
        Task SendDigestEmails(EnvironmentLevel environmentLevel);
        void SendWorkflowEvents(string objectType, int objectTypeID, IEnumerable<IWorkflowEnabledAsset> results, core.enums.Workflow.ChangeType? changeTypeOverride = null, List<AssetFieldTypeUpdate> fieldUpdates = null);
        bool TypeHasChildren(SystemObjects type, int id);
        bool TypeHasParent(SystemObjects type, int id);
        new bool Update<T>(T item) where T : BaseObject;
        bool UpdateFollowStatus(SystemObjects type, int objectID, int? resourceID, bool includeChildren = false);
        bool UpdateObjectParentRelationship(SystemObjects type, int typeId, SystemObjects objectType, int parentID, int objectID, PredicateType predicateType = PredicateType.InterTypeHierarchy);
        IntersectType UpsertIntersectType(IntersectType model, int lineageVersion);
        IQueryable<CommentVote> VoteComment(int CommentID, int ResourceID, int Vote);
        Database Database { get; }
        DbEntityEntry Entry(object entity);

        DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        Task<int> SaveChangesAsync();

        void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null, string idColumn = "A.ID", bool ruleMeansEvent = true, bool enableRelationshipFields = true, bool includeKeyColumnOnly = false);
        List<RelationshipDirectionFieldInfo> getRelationFieldData(string fieldTypeRelationType, int typeID, List<FieldType> fields);

        Task<IEnumerable<TypeIdentifierInfoModel>> GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType type, Guid guid);
        Task BulkLoadAssets(Load load, IAssetRepository repository);

        void CreateOrUpdateDisplayValue(long assetId, string objectType = "", int objectId = -1);

        void AddAuditForCompanySettingChange(CompanySetting companySetting, string actionName, string key);

        int GetObjectId(Guid uid, SystemObjects objectType);

        Guid GetAssetUid(int objectId, SystemObjects assetType);

        List<PredicateDeleteResult> RemovePredicates(ApiExecution execution, PredicateDeletes import, int timeout = 3600);
        List<PredicateUpsertResult> UpdatePredicates(ApiExecution execution, PredicateUpserts import, int timeout = 3600);
        List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(ApiExecution execution, List<ResponsibilityTypeUpsertModel> import, int timeout = 3600);
        string GetIconText(string assetName);
        void SetApiExecutionProcessingStartTime(Guid ExecutionId);
        string GetEscapedFilterString(string filter);
        Dictionary<Guid, string> GetAssetTypePathsByAssetClasses(List<int> assetClassIds);
        void SendGraphAssetTypeEvent(Guid assetTypeUid);
        void SendApiGraphEvent(ApiExecutionInfo info);
        Task SaveScoreProcessingResultsAsync<T>(Guid executionUid, ScoreQueueChangeType changeType, string resultFileSuffix, T item, DateTime? startedOn = null);
        void SendScoreEventWithPayload<T>(Guid executionUid, ScoreQueueChangeType changeType, T item, DateTime? startedOn = null);
        int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeId, string value);
        List<DataQualityResponseModel> UpsertAssetResults(List<IDataQualityUpsert> request, ApiExecution execution, int timeout = 3600, bool sendWorkflowEvents = true);
        List<DataQualityDeleteResponseModel> DeleteAssetResults(List<DataQualityDeleteModel> request, ApiExecution execution, int timeout = 3600);
        void ResolveFieldLookupValues(Guid executionID, string fieldTable = "api.ExecutionField", int timeout = 3600, SqlTransaction trans = null);
        void CopyFieldLookupValuesAsIs(Guid executionID, int timeout = 3600, string fieldTable = "api.ExecutionField", SqlTransaction trans = null);
        List<DataRow> ValidateFields(string ot, int otid, bool isInsert, List<FieldType> fieldTypes, List<string> requiredFieldTypeNames, Dictionary<string, string> fields, Guid executionID, int itemNumber, DataTable fieldTable, out bool success, out string errorMessage, bool useFriendlyNames = false, bool allowTagFields = false, FieldValidationFieldProperties validationFieldProperties = null);
        List<ResponsibilityRuleUpsertResponseModel> UpsertResponsibilityRules(ApiExecution execution, Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> import, int timeout = 3600);
        List<DatabaseBulkAssetTypeResult> RemoveAssetTypes(ApiExecution execution, AssetTypeDeletes import, int timeout = 7200);
        List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups);
        List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups);
        bool LookupFieldHasColorItem(FieldType f);
        bool SetStateDeleteWorkFlowType(SystemObjects type, int id);
        string GetDiagramUrlForDiagramAsset(Guid assetUid);
        bool HasRelationshipInProcessDiagram(Guid intersectTypeUid);
        void CreateEventsForAddedActions(List<Issue> actions);
        List<AssetFieldTypeUpdate> MergeFields(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600, bool isInsert = false);
        void ImportRelationships(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool resolveRelationshipOnObjectId = false, bool sendGraphEvents = true);
        void SendAssetGraphEvents(IEnumerable<IGraphAsset> results, Dictionary<Guid, List<string>> fields = null, bool delayedDelivery = false);
    }
}