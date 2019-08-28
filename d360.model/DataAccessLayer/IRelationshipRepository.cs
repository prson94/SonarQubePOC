using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.queue;
using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer
{
    public interface IRelationshipRepository
    {
        IntersectType GetRelationshipTypeByUID(Guid relationshipTypUid);
        Intersect GetRelationshipByUID(Guid relationshipUid);
        Task<IEnumerable<PredicateApiViewModel>> GetPredicates();
        Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
        IQueryable<IntersectType> GetIntersectTypeById(int id);
        IntersectType GetIntersectTypeByUid(Guid intersectTypeUid);
        Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
        Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type);
        Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool sendWorkflow = false);
        IEnumerable<dynamic> GetExportModelWithCustomFields(int id, IEnumerable<string> customColumns);
        IEnumerable<dynamic> GetExportModel(int id);
        List<DatabaseBulkAssetResult> GetBulkResults(ApiExecutionInfo info);
        Task<RelationshipDeleteResult> DeleteRelationships(IntersectType intersectType, RelationshipDeletes relationships, bool triggerWorkflow = false);
        bool AnyExists(Guid uid);
        bool AnyPredicateExists(Guid uid);
     }
}
