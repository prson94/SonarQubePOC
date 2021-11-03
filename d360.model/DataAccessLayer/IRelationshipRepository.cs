using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;

namespace d360.model.DataAccessLayer
{
    public interface IRelationshipRepository
    {
        IntersectType GetRelationshipTypeByUID(Guid relationshipTypUid);
        Intersect GetRelationshipByUID(Guid relationshipUid);
        Task<IEnumerable<PredicateApiViewModel>> GetPredicates(Guid? PredicateUid, PredicateType? Type, string Name, string Inverse ,bool? IsUsed);
        Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
        IQueryable<IntersectType> GetIntersectTypeById(int id);
        IntersectType GetIntersectTypeByUid(Guid intersectTypeUid);
        Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
        Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type);
        Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool sendWorkflow = false);
        Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, ApiExecution execution, bool sendWorkflow = false);
        IEnumerable<dynamic> GetExportModelWithCustomFields(int id, IEnumerable<string> customColumns);
        IEnumerable<dynamic> GetExportModel(int id);
        Task<List<DatabaseBulkAssetResult>> GetBulkResults(ApiExecutionInfo info);
        List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType intersectType, RelationshipDeletes relationships, int timeout = 3600, bool triggerWorkflow = false);
        Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool sendWorkflow = false);
        Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, ApiExecution execution, bool sendWorkflow = false);

        bool AnyExists(Guid uid);
        bool AnyPredicateExists(Guid uid);
        List<PredicateDeleteResult> DeletePredicates(PredicateDeletes predicates, ApiExecution execution);
        List<PredicateUpsertResult> UpsertPredicates(PredicateUpserts predicates, ApiExecution execution);
        Task<bool> IsTransformPredicateExists(int assetTypeId);
        List<RelationshipTypeResult> PostRelationshipTypes(List<RelationshipTypeInsert> relationshipTypes, ApiExecution execution);
        List<RelationshipTypeResult> PutRelationshipTypes(List<RelationshipTypeUpdate> relationshipTypes, ApiExecution execution);
        List<RelationshipTypeResult> DeleteRelationshipTypes(List<RelationshipTypeDelete> relationshipTypes, ApiExecution execution);
        Task<SLDocument> GetRelationshipsExcel(IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<RelationshipUidResult> GetRelationshipsUids(int intersectTypeID, int pageSize, int pageNum, bool includeTotal, string owner);
        Task<JObject> GetRelationship(Guid uid);
    }
}
