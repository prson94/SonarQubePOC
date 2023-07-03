using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.entities.Contracts;
using d360.core.entities.Graph;
using d360.core.entities.Membership;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.queue;
using d360.model.DataAccessLayer;
using d360.model.helpers.filters;

using Dapper;

using Newtonsoft.Json.Linq;

namespace d360.model
{
    public partial interface ICompanyContext : IBaseContext
    {
		#region DbSets

		DbSet<AuditField> AuditFields { get; set; }
        
        DbSet<Audit> Audits { get; set; }
        
        DbSet<CommentRelation> CommentRelations { get; set; }
        
        DbSet<Comment> Comments { get; set; }
        
        DbSet<Favorite> Favorites { get; set; }
        
        DbSet<FieldApiModel> FieldApiModels { get; set; }
        
        DbSet<FieldLookupValue> FieldLookupValues { get; set; }
        
        DbSet<Field> Fields { get; set; }
        
        DbSet<FieldTypeLookup> FieldTypeLookups { get; set; }
        
        DbSet<FieldType> FieldTypes { get; set; }
        
        DbSet<FieldWithRelation> FieldWithRelations { get; set; }
        
        DbSet<FollowDetail> FollowDetails { get; set; }
        
        DbSet<Follow> Follows { get; set; }
                
        DbSet<GraphFilter> GraphFilters { get; set; }
        
        DbSet<HelpResource> HelpResources { get; set; }
        
        DbSet<IntersectDetail> IntersectDetails { get; set; }
        
        DbSet<Intersect> Intersects { get; set; }
        
        DbSet<IntersectTypeDetail> IntersectTypeDetails { get; set; }
        
        DbSet<IntersectType> IntersectTypes { get; set; }
        
        DbSet<Issue> Issues { get; set; }
        
        DbSet<IssueTypeRelation> IssueTypeRelations { get; set; }
        
        DbSet<IssueType> IssueTypes { get; set; }
        
        DbSet<NymRelation> NymRelations { get; set; }
        
        DbSet<Nym> Nyms { get; set; }
        
        DbSet<Predicate> Predicates { get; set; }
        
        DbSet<Question> Questions { get; set; }
        
        DbSet<QuestionTypeOption> QuestionTypeOptions { get; set; }
        
        DbSet<QuestionType> QuestionTypes { get; set; }
        
        DbSet<ReportResponsibility> ReportResponsibilities { get; set; }

		DbSet<ResourceSetting> ResourceSettings { get; set; }
		
		DbSet<Report> Reports { get; set; }
        
        DbSet<Semantic> Semantics { get; set; }
        
        DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        
        DbSet<ShoppingCart> ShoppingCarts { get; set; }
        
        DbSet<ShoppingCartType> ShoppingCartTypes { get; set; }
        
        DbSet<Shortcut> Shortcuts { get; set; }
        
        DbSet<SiteNav> SiteNav { get; set; }
        
        DbSet<SiteNavPermission> SiteNavPermissions { get; set; }
        
        DbSet<Survey> Surveys { get; set; }
        
        DbSet<SurveyType> SurveyTypes { get; set; }
        
        DbSet<Tag> Tags { get; set; }

        DbSet<Theme> Themes { get; set; }
        
        DbSet<ConnectorLabel> ConnectorLabels { get; set; }
        
        DbSet<AssetTag> AssetTags { get; set; }

		#endregion

		#region Methods

		new bool Add<T>(T item) where T : BaseObject;
        
        IntersectDetail AddIntersect(int intersectTypeID, string subject, int subjectID, string @object, int objectID);
        
        IntersectDetail AddIntersect(int intersectTypeID, SystemObjects subject, int subjectID, SystemObjects @object, int objectID);
        
        void AddOrUpdateFields(List<Field> items);

        void ClearInvalidRelationRuleResults();
        
        void CompleteItemStepAssignments(long itemStepID);
        
        void CreateOrUpdateTypeDisplayValuesAsync(int objectTypeId, string objectType);

        bool Delete(SystemObjects type, int id);

        [Obsolete("Please, use DeleteAsync. Delete is non-async & not transactional")]
        new bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        
        new bool Delete<T>(T entity) where T : BaseObject;
        
        Task DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        
        bool DeleteRelationship(int id);
        
        void Enqueue(string queueName, QueueObject item);
                
        List<AllocationPossibility> GetAllocationOptions();
        
        Task<IEnumerable<AllowedIntersectionType>> GetAllowedIntersectionTypes(Guid subjectUid, Guid? predicateUid = null);
        
        IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAsset(long id);
        
        Task<T> GetDatabaseJsonAsObjectAsync<T>(string query, DynamicParameters dbArgs, int timeout = 90);

        Task<T> ExecuteGetRelationshipQuery<T>(string query, CancellationToken cancellationToken, DynamicParameters dbArgs, int timeout = 90);

        Task<IEnumerable<FieldFilterModel>> GetFieldFiltersByType(SystemObjects type, int id);
        
        IQueryable<FieldType> GetFieldTypesByObject(SystemObjects type, int id);

		IQueryable<FollowDetail> GetFollowersByObject(int? assetTypeid, long? assetid);

		Follow GetFollowingParent(int? AssetTypeID, long? AssetID, int? resourceID);

		string GetFormattedFieldLookupValue(int fieldTypeID, string fieldValue);
        
        string GetIntersectTypeName(IntersectType intersectType);
        
        List<IntersectTypeOption> GetIntersectTypeOptions(Guid? subjectUid = null, Guid? objectUid = null, Guid? predicateUid = null, List<AssetTypeClass> limitToClasses = null);
        
        List<Predicate> GetPredicateOptions(Guid subjectUid, Guid? objectUid, Guid? predicateUid);

		ObjectDetail GetObjectDetailByAssetAssetTypeId(long? assetId, int? assetTypeId);

		ObjectDetail GetObjectDetail(string type, long id);
        
        string GetObjectTypePath(string type, long id);
        
        JObject GetPageInformation(Guid assetUid);
        
        AssetDetail GetParentAsset(long assetId);
        
        AssetType GetParentType(int id);
        
        List<PermissionInfo> GetPermissions(long assetId, int assetTypeId);
        
        string GetCheckPermissionResult(int PermissionMask, int perm);
        
        public bool GetPermissionsRead(long assetId, int assetTypeId);
        
        Dictionary<string, object> GetRelationshipFieldItems(int fieldTypeID, long assetId, int offset = 0, int rows = 25, string query = null, bool includeSelection = true, IntersectType intersectType = null, FieldType ft = null, bool onlyQueries = false);
        
        string GetUserHomePage();
        
		bool IsUserFollowing(int? AssetTypeID, long? AssetID, int? resourceID);

		bool IsUserFollowingParent(int? AssetTypeID, long? AssetID, int? resourceID);

        Task<string> ProcessMessageTokens(string bodyTemplate, EventObjectInfo objectInfo, string prefix, WorkflowItemStep itemStep, bool supportHtml = true, bool forJson = false, bool lookupFieldsPassedByValue = false);
        
        Task<string> ProcessMessageTokens(string bodyTemplate, int objectID, SystemObjects obj, string prefix, WorkflowItemStep itemStep, bool supportHtml, bool forJson, bool lookupFieldsPassedByValue);

		IEnumerable<T> Query<T>(string sql, object param = null, int timeout = 90);
        
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int timeout = 90);
        
        Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, int timeout = 90);
        
        Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object param = null, int timeout = 90);
        
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, int timeout = 90);
        
        Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null, int timeout = 90);
        
        void RebuildDisplayValuesRequest();
        
        void RebuildIndexRequest();
        
        string RenderTooltip(string action, SystemObjects type, int id);
        
        void RequestObjectCertification(SystemObjects @object, int objectId, SystemObjects objectType, int objectTypeId);
        
        int SaveChanges();
        
        bool SaveOrUpdate<T>(T entity, List<Field> fields, int parentId = -1, bool forceUpdate = false) where T : BaseIntObject, IFieldsObject;
        
        Task SendDigestEmails(EnvironmentLevel environmentLevel);
        
        bool TypeHasParent(SystemObjects type, int id, PredicateType parentFunctionalType = PredicateType.InterTypeHierarchy);
        
        new bool Update<T>(T item) where T : BaseObject;

		bool UpdateFollowStatus(int? assetTypeid, long? assetid, int? resourceID, bool includeChildren = false);

		IntersectType UpsertIntersectType(IntersectType model);
        
        Database Database { get; }
        
        DbEntityEntry Entry(object entity);

        DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        
        Task<int> SaveChangesAsync();

        void GetDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null, string idColumn = "A.ID", bool ruleMeansEvent = true, bool enableRelationshipFields = true, bool includeKeyColumnOnly = false);
        
        List<RelationshipDirectionFieldInfo> GetRelationFieldData(int assetTypeId, List<FieldType> fields);

        Task<TypeIdentifierInfoModel> GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType type, Guid guid);

        void CreateOrUpdateDisplayValue(long assetId, string objectType = "", int objectId = -1);

        int GetObjectId(Guid uid, SystemObjects objectType);



        
        
        string GetEscapedFilterString(string filter, bool isContains = false);
        
        Dictionary<Guid, string> GetAssetTypePathsByAssetClasses(List<int> assetClassIds);
        
        int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeId, string value);


        void CopyFieldLookupValuesAsIs(Guid executionID, int timeout = 3600, string fieldTable = "api.ExecutionField", SqlTransaction trans = null);
        
        

        bool LookupFieldHasColorItem(FieldType f);
        
        string GetDiagramUrlForDiagramAsset(Guid assetUid);
        
        bool HasRelationshipInProcessDiagram(Guid intersectTypeUid);
        
        void CreateEventsForAddedActions(List<Issue> actions);

		Task<IEnumerable<BulkTagAsset>> GetBulkTagAssetsAsync(int loadId, Guid executionId);

		List<Guid> GetImpactedMeasureVersionsBy(MetricGovernanceCheckType check, int typeId);

        string GetCounterFieldValue(int fieldTypeId, long assetId);
        
        string GetOutputFieldValue(int stepId, long itemId, string fieldId);

        #region API Query Parameter Parsing

        void ParseAdvancedFilterQueryParameter(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fieldList, out DynamicParameters dbArgs, out List<string> whereStatements);
        
        void ParseSimpleFilterQueryParameter(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fieldList, out DynamicParameters dbArgs, out List<string> whereStatements);
        
        int ParsePageNumber(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultPage = 1);
        
        int ParsePageSize(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultSize = 250);
        
        string ParseOrderColumn(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fields, string defaultColumn);
        
        string ParseOrderDirection(IEnumerable<KeyValuePair<string, string>> queryParams, string defaultDirection = "desc");
        
        string ParsePageOffsetSql(int pageNumber, int pageSize, int pageSizeLimit = 10000);

        #endregion

        #region Environment Settings

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        void DeleteSetting(Setting setting);

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        SettingInfo GetSetting(Setting setting);

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        T GetSettingValue<T>(Setting setting);

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        List<SettingInfo> GetSettings();

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        Dictionary<string, string> GetSettingsAsDictionary();

        /// <summary>
        /// When at all possible, do not call directly. You should use the SettingsRepository instead.
        /// </summary>
        void UpsertSetting(Setting setting, string value);

        #endregion

        #region Rebuild job status

        Task<List<CompanyRebuildJobStatus>> GetRebuildJobStatuses(int timeOutInHours);

        Task<WorkHttpStatus> UpdateRebuildJobStatus(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state, int timeOutInHours);

		#endregion

		#endregion
	}
}