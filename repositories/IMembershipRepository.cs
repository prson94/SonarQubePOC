using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IMembershipRepository
    {
        [Obsolete]
        Task ClearFavorites(int resourceID);
        
        Task DeleteFavorites(int resourceID, List<int> favoriteIds);
        
        List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups);
        
        List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups);
	}
}
