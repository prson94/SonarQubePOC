using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer
{
    public interface IRelationshipRepository
    {
        IntersectType GetRelationshipByUID(Guid relationshipTypUid);
        Task<IEnumerable<PredicateApiViewModel>> GetPredicates();
        Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "");
     }
}
