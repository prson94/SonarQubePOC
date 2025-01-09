using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IWorkspaces
	{
		int CompanyId { get; set; }
		string WorkspaceId { get; set; }
		Platform Platform { get; }

		Task<RepositoryResponse<bool>> AddMembersToGroupAsync(Guid groupUid, List<Guid> userUids);

		Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadGroupsAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<IEnumerable<CompanyRebuildJobStatus>> ReadRebuildStatusesAsync();

		Task<bool> RemoveFavoritesAsync(int resourceId, List<int> favoriteIds);

		Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> RemoveGroupsAsync(int executionId, List<Guid> uids);
		
		Task<bool> RemoveMemberFromGroupAsync(Guid groupUid, Guid userUid);

		Task<RepositoryResponse<int>> RemoveUsersAsync(int executionId, List<Guid> uids);

		Task<RepositoryResponse<List<GroupResponseResult>>> UpsertGroupsAsync(int executionId, List<UpdateGroupModel> items, bool isInsert, bool lookupFieldsPassedByValue = false);

		Task<RepositoryResponse<bool>> UpsertRebuildStatusAsync(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state, int timeOutInHours);

		Task<RepositoryResponse<List<UserApiUpsertResult>>> UpsertUsersAsync(int executionId, List<UserUpsertValidateModel> users, bool lookupFieldsPassedByValue = false);

		Task<List<UserUpsertValidateModel>> ValidateUserData(List<UserApiModel> users, bool isNew, bool IsAdministrator, bool lookupFieldsPassedByValue);
	}
}
