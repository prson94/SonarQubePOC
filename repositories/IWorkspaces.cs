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

		Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync();

		Task<SettingInfo> ReadSettingAsync(Setting setting);

		Task<List<SettingInfo>> ReadSettingsAsync();

		Task<T> ReadSettingValueAsync<T>(Setting setting);

		Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> RemoveGroupsAsync(int executionId, List<Guid> uids);
		
		Task<bool> RemoveMemberFromGroupAsync(Guid groupUid, Guid userUid);

		Task<RepositoryResponse<bool>> RemoveSettingAsync(Setting setting);

		Task<RepositoryResponse<int>> RemoveUsersAsync(int executionId, List<Guid> uids);

		Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> UpsertGroupsAsync(int executionId, List<UpdateGroupModel> items, bool isInsert, bool lookupFieldsPassedByValue = false);

		Task<RepositoryResponse<bool>> UpsertRebuildStatusAsync(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state, int timeOutInHours);

		Task<RepositoryResponse<bool>> UpsertSettingAsync(Setting setting, string value);

		Task<RepositoryResponse<IEnumerable<UserApiUpsertResult>>> UpsertUsersAsync(int executionId, List<UserApiModel> users, bool lookupFieldsPassedByValue = false);
	}
}
