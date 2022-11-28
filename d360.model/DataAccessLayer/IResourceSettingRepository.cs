using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public interface IResourceSettingRepository
	{
		Task DeleteSetting(int ResourceID, int AssetTypeID, string Setting);
		Task DeleteSetting(int ResourceID, Guid AssetTypeUID, string Setting);
		Task DeleteGlobalSetting(int ResourceID, string Setting);
		Task DeleteResourceSettings(int ResourceID);
		Task DeleteAssetTypeSettings(int AssetTypeID);
		Task DeleteAssetTypeSettings(Guid AssetTypeUID);
		Task UpsertSetting(int ResourceID, int AssetTypeID, string Setting, string Value);
		Task UpsertSetting(int ResourceID, Guid AssetTypeUID, string Setting, string Value);
		Task UpsertGlobalSetting(int ResourceID, string Setting, string Value);
		Task<Dictionary<string, string>> GetSettings(int ResourceID, int AssetTypeID);
		Task<Dictionary<string, string>> GetSettings(int ResourceID, Guid AssetTypeUID);
		Task<Dictionary<string, string>> GetGlobalSettings(int ResourceID);
		string GetSetting(int ResourceID, int AssetTypeID, string Setting);
		string GetSetting(int ResourceID, Guid AssetTypeUID, string Setting);
		string GetGlobalSetting(int ResourceID, string Setting);
	}
}
