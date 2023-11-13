using System.Collections.Generic;

using d360.core.enums;

namespace repositories
{
    public interface ISettingsRepository
    {
        void DeleteSetting(Setting setting);
        
        void UpsertSetting(Setting setting, string value);
        
        SettingInfo GetSetting(Setting setting);
        
        T GetSettingValue<T>(Setting setting);
        
        List<SettingInfo> GetSettings();
        
        Dictionary<string, string> GetSettingsAsDictionary();
    }
}
