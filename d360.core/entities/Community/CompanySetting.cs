using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanySetting: BaseObject
    {
         [DataMember, Key, Column(Order = 1)]
        public int CompanyID { get; set; }
        
        [DataMember, Key, Column(Order = 2)]
        public int SettingID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }

        [IgnoreDataMember, ForeignKey("SettingID")]
        public virtual Setting Setting { get; set; }
    }


    public class CompanySettingApiModel
    {
        public CompanySettingApiModel() { }

        public CompanySettingApiModel(Setting setting, string companyValue) 
        {
            SettingID = setting.ID;
            Description = setting.Description;
            Locked = setting.Locked;
            Name = setting.Name;
            
            switch(setting.SettingType)
            {
                case SettingType.Boolean:
                    BooleanSetting = new CompanySettingApiBooleanModel(setting, companyValue);
                    break;
                case SettingType.Number:
                    NumberSetting = new CompanySettingApiNumberModel(setting, companyValue);
                    break;
                case SettingType.Text:
                    StringSetting = new CompanySettingApiStringModel(setting, companyValue);
                    break;
                case SettingType.IPAddress:
                    IpAddressSetting = new CompanySettingApiIpAddressModel(setting, companyValue);
                    break;
                case SettingType.Guid:
                    GuidSetting = new CompanySettingApiGuidModel(setting, companyValue);
                    break;
            }

        }


        public int SettingID { get; set; }
        public bool Locked { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public CompanySettingApiGuidModel GuidSetting { get; set; }
        public CompanySettingApiIpAddressModel IpAddressSetting { get; set; }
        public CompanySettingApiBooleanModel BooleanSetting { get; set; }
        public CompanySettingApiNumberModel NumberSetting { get; set; }
        public CompanySettingApiStringModel StringSetting { get; set; }

    }

    #region Settings Data Types

    public class CompanySettingApiBooleanModel
    {
        public CompanySettingApiBooleanModel() { }

        public CompanySettingApiBooleanModel(Setting setting, string companyValue)
        {
            if (bool.TryParse(setting.DefaultValue, out bool d))
                Default = d;
            if (bool.TryParse(companyValue, out bool v))
                Value = v;
        }

        public bool Value { get; set; }
        public bool Default { get; set; }
    }

    public class CompanySettingApiStringModel
    {
        public CompanySettingApiStringModel() { }

        public CompanySettingApiStringModel(Setting setting, string companyValue)
        {
            Default = setting.DefaultValue;
            Value = companyValue;
        }

        public string Value { get; set; }
        public string Default { get; set; }
    }

    public class CompanySettingApiNumberModel
    {
        public CompanySettingApiNumberModel() { }

        public CompanySettingApiNumberModel(Setting setting, string companyValue)
        {
            if (int.TryParse(setting.DefaultValue, out int d))
                Default = d;
            if (int.TryParse(companyValue, out int v))
                Value = v;
        }

        public int Value { get; set; }
        public int Default { get; set; }
    }

    public class CompanySettingApiIpAddressModel
    {
        public CompanySettingApiIpAddressModel() { }
        public CompanySettingApiIpAddressModel(Setting setting, string companyValue)
        {
            var value = string.IsNullOrEmpty(companyValue) ? setting.DefaultValue : companyValue;
            Addresses = new List<Ip>();

            if (!string.IsNullOrEmpty(value))
            {
                var xml = XElement.Parse(value);
                var ips = xml.Elements("ip").Select(i => new Ip { Name = i.Element("name").Value, Start = i.Element("start").Value, End = i.Element("end").Value });
                Addresses.AddRange(ips);
            }
        }

        public List<Ip> Addresses { get; set; }
    }

    public class CompanySettingApiGuidModel
    {
        public CompanySettingApiGuidModel() { }

        public CompanySettingApiGuidModel(Setting setting, string companyValue)
        {
            if (Guid.TryParse(setting.DefaultValue, out Guid d))
                Default = d;
            if (Guid.TryParse(companyValue, out Guid v))
                Value = v;
        }

        public Guid Value { get; set; }
        public Guid Default { get; set; }
    }

    public class CompanySettingApiUpdateModel
    {
        public int SettingID { get; set; }
        public CompanySettingApiUpdateStringModel StringSetting { get; set; }
        public CompanySettingApiUpdateNumberModel NumberSetting { get; set; }
        public CompanySettingApiUpdateBooleanModel BooleanSetting { get; set; }
        public CompanySettingApiUpdateIpAddressModel IpAddressSetting { get; set; }
        public CompanySettingApiUpdateGuidModel GuidSetting { get; set; }

        [IgnoreDataMember]
        public bool HasExactlyOneValue
        {
            get
            {
                return
                    ((StringSetting == null ? 0 : 1) +
                    (NumberSetting == null ? 0 : 1) +
                    (BooleanSetting == null ? 0 : 1) +
                    (IpAddressSetting == null ? 0 : 1) +
                    (GuidSetting == null ? 0 : 1)) == 1;
            }
        }

    }

    public class CompanySettingApiUpdateStringModel
    {
        public string Value { get; set; }
    }

    public class CompanySettingApiUpdateNumberModel
    {
        public string Value { get; set; }
    }

    public class CompanySettingApiUpdateBooleanModel
    {
        public string Value { get; set; }
    }

    public class CompanySettingApiUpdateIpAddressModel
    {
        public List<Ip> Value { get; set; }
    }

    public class CompanySettingApiUpdateGuidModel
    {
        public Guid Value { get; set; }
    }
    #endregion
}
