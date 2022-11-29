using d360.core.entities;
using d360.core.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
	class ResourceSettingRepository : BaseRepository, IResourceSettingRepository
	{
		private readonly ICompanyContext CompanyContext;
		public ResourceSettingRepository(ICompanyContext companyContext)
			: base(companyContext)
		{
			CompanyContext = companyContext;
		}

		public async Task DeleteSetting(int ResourceID, int AssetTypeID, string Setting)
		{
			await CompanyContext.DeleteAsync<ResourceSetting>(s => s.ResourceID == ResourceID && s.AssetTypeID == AssetTypeID && s.Setting == Setting);
		}

		public async Task DeleteSetting(int ResourceID, Guid AssetTypeUID, string Setting)
		{
			await DeleteSetting(ResourceID, AssetTypeUID2ID(AssetTypeUID), Setting);
		}

		public async Task DeleteGlobalSetting(int ResourceID, string Setting)
		{
			await DeleteSetting(ResourceID, 0, Setting);
		}

		public async Task DeleteResourceSettings(int ResourceID)
		{
			await CompanyContext.DeleteAsync<ResourceSetting>(s => s.ResourceID == ResourceID);
		}

		public async Task DeleteAssetTypeSettings(int AssetTypeID)
		{
			await CompanyContext.DeleteAsync<ResourceSetting>(s => s.AssetTypeID == AssetTypeID);
		}

		public async Task DeleteAssetTypeSettings(Guid AssetTypeUID)
		{
			await DeleteAssetTypeSettings(AssetTypeUID2ID(AssetTypeUID));
		}

		public async Task UpsertSetting(int ResourceID, int AssetTypeID, string Setting, string Value)
		{
			var add = false;

			var model = CompanyContext.ResourceSettings.Where(s => s.ResourceID == ResourceID && s.AssetTypeID == AssetTypeID && s.Setting == Setting).FirstOrDefault();

			if (model == null)
			{
				add = true;
				model = new ResourceSetting
				{
					ResourceID = ResourceID,
					AssetTypeID = AssetTypeID,
					Setting = Setting
				};
			}

			model.Value = Value;

			if(add)
			{
				CompanyContext.Add(model);
			}
			else
			{
				CompanyContext.Update(model);
			}
			await CompanyContext.SaveChangesAsync();
		}

		public async Task UpsertSetting(int ResourceID, Guid AssetTypeUID, string Setting, string Value)
		{
			await UpsertSetting(ResourceID, AssetTypeUID2ID(AssetTypeUID), Setting, Value);
		}

		public async Task UpsertGlobalSetting(int ResourceID, string Setting, string Value)
		{
			await UpsertSetting(ResourceID, 0, Setting, Value);
		}

		public async Task<Dictionary<string, string>> GetSettings(int ResourceID, int AssetTypeID)
		{
			Dictionary<string, string> res = null;
			await Task.Run(() =>
			{
				res = CompanyContext
				.ResourceSettings
				.Where(s => s.ResourceID == ResourceID && s.AssetTypeID == AssetTypeID)
				.ToDictionary(k => k.Setting, v => v.Value);
			}).ConfigureAwait(false);
			
			return res;
		}
		public async Task<Dictionary<string, string>> GetSettings(int ResourceID, Guid AssetTypeUID)
		{
			if (AssetTypeUID == Guid.Empty)
			{
				return await GetGlobalSettings(ResourceID);
			}
			else
			{
				return await GetSettings(ResourceID, AssetTypeUID2ID(AssetTypeUID));
			}
		}

		public async Task<Dictionary<string, string>> GetGlobalSettings(int ResourceID)
		{
			return await GetSettings (ResourceID, 0);
		}

		public string GetSetting(int ResourceID, int AssetTypeID, string Setting)
		{
			return CompanyContext
				.ResourceSettings
				.Where(s => s.ResourceID == ResourceID && s.AssetTypeID == AssetTypeID && s.Setting == Setting)
				.FirstOrDefault().Value;
		}
		public string GetSetting(int ResourceID, Guid AssetTypeUID, string Setting)
		{
			return GetSetting(ResourceID, AssetTypeUID2ID(AssetTypeUID), Setting);
		}

		public string GetGlobalSetting(int ResourceID, string Setting)
		{
			return GetSetting(ResourceID, 0, Setting);
		}

		private int AssetTypeUID2ID(Guid AssetTypeUID)
		{
			var AssetType = CompanyContext.AssetTypes.Where(t => t.uid == AssetTypeUID).FirstOrDefault();
			if(AssetType == null)
			{
				throw new ArgumentException(OthersError.InvalidAssetTypeUid);
			}
			return AssetType.ID;
		}

	}
}
