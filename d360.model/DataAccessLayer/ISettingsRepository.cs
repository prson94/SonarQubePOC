using d360.core.enums;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer
{
    public interface ISettingsRepository
    {
        void DeleteSetting(Setting setting);
        void UpsertSetting(Setting setting, string value);
        List<SettingInfo> GetSettings();
    }
}