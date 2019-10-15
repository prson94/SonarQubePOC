using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IResponsibilityRepository
    {
        Task<AssetResponsibilitiesApiModel> GetResponsibilities(IEnumerable<KeyValuePair<string, string>> queryParams, string responsibilityUidFilter, string assigneeUidFilter, string assetUidFilter, string assetTypeUidFilter, int pageSize, int pageNum, int timeout);
        Task<ResponsibilityTypeRuleStatsViewModel> GetResponsibilityRuleStats(Guid responsibilityTypeRuleUid);
        Task<IEnumerable<ResponsibilityTypeRuleViewModel>> GetResponsibilityRules(Guid responsibilityTypeUid);
        Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocations(Guid responsibilityTypeUid);
        Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypesByAssetUid(Guid assetTypeUid);
        Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypes();
        List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypeUpserts, ApiExecution execution);
        ResponsibilityTypeDeleteResult DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel responsibilityTypeDelete);
        Task<IEnumerable<ClaimsViewModel>> GetClaims();
    }
}
