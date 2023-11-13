using d360.core.enums;
using d360.model.DataAccessLayer.repositories;
using repositories;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer
{
	public class SettingsRepository : BaseRepository, ISettingsRepository
    {

        public SettingsRepository(ICompanyContext companyContext)
            : base(companyContext)
        {
        }

        public void DeleteSetting(Setting setting)
        {
            // In essence, this would set back to the default, if any.
            CompanyContext.DeleteSetting(setting);
        }

        public void UpsertSetting(Setting setting, string value)
        {
            CompanyContext.UpsertSetting(setting, value);
        }

        public SettingInfo GetSetting(Setting setting)
        {
            return CompanyContext.GetSetting(setting);
        }

        public T GetSettingValue<T>(Setting setting)
        {
            return CompanyContext.GetSettingValue<T>(setting);
        }

        public List<SettingInfo> GetSettings()
        {
            return CompanyContext.GetSettings();
        }

        public Dictionary<string, string> GetSettingsAsDictionary()
        {
            return CompanyContext.GetSettingsAsDictionary();
        }
    }
}
