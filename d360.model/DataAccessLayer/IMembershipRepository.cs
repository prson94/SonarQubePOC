using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IMembershipRepository
    {
        Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams);
        WorkHttpStatus DeleteResources(ApiExecution execution, IEnumerable<UserApiDeleteModel> resources);
        Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false);
        Task<IEnumerable<UserApiUpsertResult>> ProcessUpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false);
        Task<ApiExecutionInfo> UpsertBulkUsers(ApiExecution execution, UserUpsertModel model);

        [Obsolete]
        Task ClearFavorites(int resourceID);
        Task DeleteFavorites(int resourceID, List<int> favoriteIds);
        List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups);
        List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups);
        List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups);

        Task<List<OrganizationModel>> GetOrganizationsByType(Guid organizationTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<OrganizationDetailModel> GetOrganizationsDetails(Guid organizationUid);
    }
}
