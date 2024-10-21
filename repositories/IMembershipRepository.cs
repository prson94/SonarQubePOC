using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.queue;
using d360.core.validators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IMembershipRepository
    {
        Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        Task<IEnumerable<UserApiModel>> UpsertUsers(ApiExecution execution, IEnumerable<UserApiModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false);
        
        Task<IEnumerable<UserApiModel>> ProcessUpsertUsers(ApiExecution execution, IEnumerable<UserApiModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false);
        
        Task<ApiExecutionInfo> UpsertBulkUsers(ApiExecution execution, UserUpsertModel model);

        [Obsolete]
        Task ClearFavorites(int resourceID);
        
        Task DeleteFavorites(int resourceID, List<int> favoriteIds);
        
        List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups);
        
        List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups);
        
        List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups);
	}
}
