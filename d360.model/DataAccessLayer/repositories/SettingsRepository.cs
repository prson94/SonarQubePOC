using d360.core.enums;
using d360.model.DataAccessLayer.repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            companyContext.Execute("delete Setting where ID = @id", new { id = (int)setting });
        }

        public void UpsertSetting(Setting setting, string value)
        {
            companyContext.Execute(@"
if exists(select 1 from [Setting] where ID = @ID) 
begin 
    update [Setting] set [Value] = @value where ID = @ID 
end 
else 
begin 
    insert [Setting] values (@ID, @value) 
end", new { ID = (int)setting, value });
        }

        public List<SettingInfo> GetSettings()
        {
            // Get the list of settings from the D3S_###.dbo.Setting table.
            // Get the full list of settings from the Setting enum.
            // Return a list of SettingInfo, merging the values present from the environment into the SettingInfo.Value property.
            var overrides = companyContext.Query<dynamic>("select * from Setting").ToDictionary(k => (Setting)k.ID, v => v.Value);
            var settings = Setting.ActionMessage.GetAsList();

            settings.ForEach(s =>
            {
                if (overrides.ContainsKey(s.ID))
                {
                    s.Value = overrides[s.ID];
                }
                else 
                {
                    s.Value = s.DefaultValue;
                }
            });

            return settings;
        }
    }
}
