using d360.core.enums;
using d360.model.DataAccessLayer.repositories;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace d360.model.DataAccessLayer
{
    public class SettingsRepository : BaseRepository, ISettingsRepository
    {
        ICompanyContext companyContext;
        public SettingsRepository(ICompanyContext companyContext)
            : base(companyContext)
        {
            this.companyContext = companyContext;
        }

        public void DeleteSetting(Setting setting)
        {
            // In essence, this would set back to the default, if any.
            companyContext.DeleteSetting(setting);
        }

        public void UpsertSetting(Setting setting, string value)
        {
            companyContext.UpsertSetting(setting, value);
        }

        public SettingInfo GetSetting(Setting setting)
        {
            return companyContext.GetSetting(setting);
        }

        public T GetSettingValue<T>(Setting setting)
        {
            return companyContext.GetSettingValue<T>(setting);
        }

        public List<SettingInfo> GetSettings()
        {
            return companyContext.GetSettings();
        }

        public Dictionary<string, string> GetSettingsAsDictionary()
        {
            return companyContext.GetSettingsAsDictionary();
        }
    }
}
