using d360.core.enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IWorkspaces
	{
		int CompanyId { get; set; }
		string WorkspaceId { get; set; }
		Platform Platform { get; }

		Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync();

		Task<SettingInfo> ReadSettingAsync(Setting setting);

		Task<List<SettingInfo>> ReadSettingsAsync();

		Task<T> ReadSettingValueAsync<T>(Setting setting);

		Task<RepositoryResponse<bool>> RemoveSettingAsync(Setting setting);

		Task<RepositoryResponse<bool>> UpsertSettingAsync(Setting setting, string value);
	}
}
