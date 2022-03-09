
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

using d360.core.resources;

namespace d360.core.enums
{
    public enum Setting
    {
        [
            DefaultValue(false),
            Description("DisableCommunityPosting_Desc", typeof(Settings)),
            Locked(false),
            Name("DisableCommunityPosting_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        DisableCommunityPosting = 1,

        [
            DefaultValue("/Content/images/PreciselyLogo@2x.png"),
            Description("CompanyLogo_Desc", typeof(Settings)),
            Locked(false),
            Name("CompanyLogo_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        CompanyLogo = 2,

        [
            DefaultValue("/favicon.ico"),
            Description("CompanyIcon_Desc", typeof(Settings)),
            Locked(false),
            Name("CompanyIcon_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        CompanyIcon = 3,

        [
            DefaultValue("<ips />"),
            Description("IpRestriction_Desc", typeof(Settings)),
            Locked(false),
            Name("IpRestriction_Name", typeof(Settings)),
            Type(SettingType.IPAddress)
        ]
        IpRestriction = 4,

        [
            DefaultValue(true),
            Description("HideData3SixtyUsers_Desc", typeof(Settings)),
            Locked(false),
            Name("HideData3SixtyUsers_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        HideData3SixtyUsers = 9,


        [
            DefaultValue(""),
            Description("DefaultSearchTypes_Desc", typeof(Settings)),
            Locked(false),
            Name("DefaultSearchTypes_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        DefaultSearchTypes = 13,

        [
            DefaultValue(false),
            Description("DisableIssueManagement_Desc", typeof(Settings)),
            Locked(false),
            Name("DisableIssueManagement_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        DisableIssueManagement = 17,

        [
            DefaultValue(false),
            Description("EnableOrganizations_Desc", typeof(Settings)),
            Locked(false),
            Name("EnableOrganizations_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        EnableOrganizations = 19,

        [
            DefaultValue(false),
            Description("EnableShoppingCart_Desc", typeof(Settings)),
            Locked(false),
            Name("EnableShoppingCart_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        EnableShoppingCart = 20,

        [
            DefaultValue(true),
            Description("EnableSagacity_Desc", typeof(Settings)),
            Locked(false),
            Name("EnableSagacity_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        EnableSagacity = 21,

        [
            DefaultValue(""),
            Description("DefaultRoute_Desc", typeof(Settings)),
            Locked(false),
            Name("DefaultRoute_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        DefaultRoute = 22,

        [
            DefaultValue(""),
            Description("CustomCSSLocation_Desc", typeof(Settings)),
            Locked(false),
            Name("CustomCSSLocation_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        CustomCSSLocation = 24,

        [
            DefaultValue(""),
            Description("AzureADTenant_Desc", typeof(Settings)),
            Locked(true),
            Name("AzureADTenant_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        AzureADTenant = 25,

        [
            DefaultValue(""),
            Description("AzureGraphAPIKey_Desc", typeof(Settings)),
            Locked(true),
            Name("AzureGraphAPIKey_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        AzureGraphAPIKey = 26,

        [
            DefaultValue(""),
            Description("AzureApplicationId_Desc", typeof(Settings)),
            Locked(true),
            Name("AzureApplicationId_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        AzureApplicationId = 27,

        [
            DefaultValue(true),
            Description("ShowResources_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowResources_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowResources = 28,

        [
            DefaultValue(true),
            Description("ShowFollowersSidebar_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowFollowersSidebar_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowFollowersSidebar = 29,

        [
            DefaultValue(true),
            Description("ShowOwnersSidebar_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowOwnersSidebar_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowOwnersSidebar = 30,

        [
            DefaultValue(true),
            Description("ShowImpactSidebar_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowImpactSidebar_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowImpactSidebar = 31,

        [
            DefaultValue(true),
            Description("ShowLineageSidebar_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowLineageSidebar_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowLineageSidebar = 32,

        [
            DefaultValue("Data360"),
            Description("BrowserTitlePrefix_Desc", typeof(Settings)),
            Locked(false),
            Name("BrowserTitlePrefix_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        BrowserTitlePrefix = 33,

        [
            DefaultValue(480),
            Description("SessionTimeout_Desc", typeof(Settings)),
            Locked(false),
            Name("SessionTimeout_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        SessionTimeout = 34,

        [
            DefaultValue(true),
            Description("ShowFavorites_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowFavorites_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowFavorites = 37,

        [
            DefaultValue(true),
            Description("ShowSocialScoreBar_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowSocialScoreBar_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowSocialScoreBar = 38,

        [
            DefaultValue(true),
            Description("ShowHomeAssignmentTile_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowHomeAssignmentTile_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowHomeAssignmentTile = 39,

        [
            DefaultValue(true),
            Description("ShowHomeBoardTile_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowHomeBoardTile_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowHomeBoardTile = 40,

        [
            DefaultValue(true),
            Description("ShowHomeActivityTile_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowHomeActivityTile_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowHomeActivityTile = 41,

        [
            DefaultValue(false),
            Description("ShowHomePageTitle_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowHomePageTitle_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowHomePageTitle = 42,

        [
            DefaultValue("14px"),
            Description("HomePageTitleSize_Desc", typeof(Settings)),
            Locked(false),
            Name("HomePageTitleSize_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        HomePageTitleSize = 43,

        [
            DefaultValue("#ffffff"),
            Description("HomePageTitleColor_Desc", typeof(Settings)),
            Locked(false),
            Name("HomePageTitleColor_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        HomePageTitleColor = 44,

        [
            DefaultValue(""),
            Description("HomePageBackgroundImage_Desc", typeof(Settings)),
            Locked(false),
            Name("HomePageBackgroundImage_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        HomePageBackgroundImage = 45,

        [
            DefaultValue("What item would you like to report a problem with?"),
            Description("ActionMessage_Desc", typeof(Settings)),
            Locked(false),
            Name("ActionMessage_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        ActionMessage = 47,

        [
            DefaultValue("Data360 Workflow"),
            Description("WorkflowFromName_Desc", typeof(Settings)),
            Locked(false),
            Name("WorkflowFromName_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        WorkflowFromName = 48,

        [
            DefaultValue("no-reply@data3sixty.com"),
            Description("WorkflowFromEmail_Desc", typeof(Settings)),
            Locked(false),
            Name("WorkflowFromEmail_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        WorkflowFromEmail = 49,

        [
            DefaultValue(false),
            Description("ShowCustomAPIAdmin_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowCustomAPIAdmin_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowCustomAPIAdmin = 50,

        [
            DefaultValue(false),
            Description("HasRegisterLink_Desc", typeof(Settings)),
            Locked(false),
            Name("HasRegisterLink_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        HasRegisterLink = 52,

        [
            DefaultValue(""),
            Description("JwtAuthority_Desc", typeof(Settings)),
            Locked(false),
            Name("JwtAuthority_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        JwtAuthority = 54,

        [
            DefaultValue("2ec97ecb-f620-40ba-a109-afcd2e89be0f"),
            Description("PowerBIClientId_Desc", typeof(Settings)),
            Locked(true),
            Name("PowerBIClientId_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        PowerBIClientId = 55,

        [
            DefaultValue(""),
            Description("PowerBIGroupId_Desc", typeof(Settings)),
            Locked(true),
            Name("PowerBIGroupId_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        PowerBIGroupId = 56,

        [
            DefaultValue(true),
            Description("ShowAllUsersAPIKey_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowAllUsersAPIKey_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowAllUsersAPIKey = 57,

        [
            DefaultValue(0),
            Description("WorkflowCatchAllGroup_Desc", typeof(Settings)),
            Locked(false),
            Name("WorkflowCatchAllGroup_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        WorkflowCatchAllGroup = 58,

        [
            DefaultValue(10000),
            Description("MaxDropdownItems_Desc", typeof(Settings)),
            Locked(false),
            Name("MaxDropdownItems_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        MaxDropdownItems = 60,

        [
            DefaultValue(true),
            Description("WriteActionDescription_Desc", typeof(Settings)),
            Locked(false),
            Name("WriteActionDescription_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        WriteActionDescription = 61,

        [
            DefaultValue("DRAFT"),
            Description("RequestCertificationDraft_Desc", typeof(Settings)),
            Locked(false),
            Name("RequestCertificationDraft_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        RequestCertificationDraft = 64,

        [
            DefaultValue(6),
            Description("UseAsTransformationLimit_Desc", typeof(Settings)),
            Locked(false),
            Name("UseAsTransformationLimit_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        UseAsTransformationLimit = 69,

        [
            DefaultValue(10000),
            Description("MaxExcelExportRows_Desc", typeof(Settings)),
            Locked(false),
            Name("MaxExcelExportRows_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        MaxExcelExportRows = 71,

        [
            DefaultValue(false),
            Description("ShowNavigationChildren_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowNavigationChildren_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowNavigationChildren = 72,

        [
            DefaultValue("00000000-0000-0000-0000-000000000000"),
            Description("GovernanceRoleReferenceListUid_Desc", typeof(Settings)),
            Locked(false),
            Name("GovernanceRoleReferenceListUid_Name", typeof(Settings)),
            Type(SettingType.Guid)
        ]
        GovernanceRoleReferenceListUid = 73,

        [
            DefaultValue(90),
            Description("ApiTimeout_Desc", typeof(Settings)),
            Locked(false),
            Name("ApiTimeout_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        ApiTimeout = 74,

        [
            DefaultValue(false),
            Description("EnableJsonAttribute_Desc", typeof(Settings)),
            Locked(false),
            Name("EnableJsonAttribute_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        EnableJsonAttribute = 75,

        [
            DefaultValue(""),
            Description("AllowedOrigins_Desc", typeof(Settings)),
            Locked(false),
            Name("AllowedOrigins_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        AllowedOrigins = 76,

        [
            DefaultValue(""),
            Description("FramingDomains_Desc", typeof(Settings)),
            Locked(false),
            Name("FramingDomains_Name", typeof(Settings)),
            Type(SettingType.Text)
        ]
        FramingDomains = 77,

        [
            DefaultValue(0),
            Description("WorkflowDigestEmailDays_Desc", typeof(Settings)),
            Locked(false),
            Name("WorkflowDigestEmailDays_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        WorkflowDigestEmailDays = 78,

        [
            DefaultValue(true),
            Description("ShowChangeLogTab_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowChangeLogTab_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowChangeLogTab = 79,

        [
            DefaultValue(true),
            Description("ShowCommentsTab_Desc", typeof(Settings)),
            Locked(false),
            Name("ShowCommentsTab_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        ShowCommentsTab = 80,

        [
            DefaultValue(365),
            Description("AssetDataProfileLifespan_Desc", typeof(Settings)),
            Locked(false),
            Name("AssetDataProfileLifespan_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        AssetDataProfileLifespan = 81,

        [
            DefaultValue(200),
            Description("AssetDefinitionColumnWidth_Desc", typeof(Settings)),
            Locked(false),
            Name("AssetDefinitionColumnWidth_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        AssetDefinitionColumnWidth = 82,
        [
            DefaultValue(false),
            Description("HideHeaderBarControls_Desc", typeof(Settings)),
            Locked(false),
            Name("HideHeaderBarControls_Name", typeof(Settings)),
            Type(SettingType.Boolean)
        ]
        HideHeaderBarControls = 83,
        [
            DefaultValue(250),
            Description("DiagramMaxAvoidNodesLinkCount_Desc", typeof(Settings)),
            Locked(false),
            Name("DiagramMaxAvoidNodesLinkCount_Name", typeof(Settings)),
            Type(SettingType.Number)
        ]
        DiagramMaxAvoidNodesLinkCount = 84
    }

    public class SettingInfo
    {
        public Setting ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Locked { get; set; }
        public SettingType Type { get; set; }
        public string DefaultValue { get; set; }
        public string Value { get; set; } // This will be populated by the SettingsRepo

        public bool IsOverridden { get { return !Value.Equals(DefaultValue); } }
    }

    #region Transitive classes/models

    public class IPs
    {
        public List<Ip> Ip { get; set; }
    }
    public class Ip
    {
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class CompanySettingApiModel
    {
        public CompanySettingApiModel() { }

        public CompanySettingApiModel(SettingInfo setting, string companyValue)
        {
            SettingID = (int)setting.ID;
            Description = setting.Description;
            Locked = setting.Locked;
            Name = setting.Name;

            switch (setting.Type)
            {
                case SettingType.Boolean:
                    BooleanSetting = new CompanySettingApiBooleanModel(setting, companyValue);
                    break;
                case SettingType.Number:
                    NumberSetting = new CompanySettingApiNumberModel(setting, companyValue);
                    break;
                case SettingType.IPAddress:
                    IpAddressSetting = new CompanySettingApiIpAddressModel(setting, companyValue);
                    break;
                case SettingType.Guid:
                    GuidSetting = new CompanySettingApiGuidModel(setting, companyValue);
                    break;
                default:
                    StringSetting = new CompanySettingApiStringModel(setting, companyValue);
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

    public class CompanySettingApiBooleanModel
    {
        public CompanySettingApiBooleanModel() { }

        public CompanySettingApiBooleanModel(SettingInfo setting, string companyValue)
        {
            if (bool.TryParse(setting.DefaultValue, out bool d))
            {
                Default = d;
            }

            if (bool.TryParse(companyValue, out bool v))
            {
                Value = v;
            }
        }

        public bool Value { get; set; }
        public bool Default { get; set; }
    }

    public class CompanySettingApiStringModel
    {
        public CompanySettingApiStringModel() { }

        public CompanySettingApiStringModel(SettingInfo setting, string companyValue)
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

        public CompanySettingApiNumberModel(SettingInfo setting, string companyValue)
        {
            if (int.TryParse(setting.DefaultValue, out int d))
            {
                Default = d;
            }

            if (int.TryParse(companyValue, out int v))
            {
                Value = v;
            }
        }

        public int Value { get; set; }
        public int Default { get; set; }
    }

    public class CompanySettingApiIpAddressModel
    {
        public CompanySettingApiIpAddressModel() { }
        public CompanySettingApiIpAddressModel(SettingInfo setting, string companyValue)
        {
            var value = string.IsNullOrEmpty(companyValue) ? setting.DefaultValue : companyValue;
            Value = new List<Ip>();

            if (!string.IsNullOrEmpty(value))
            {
                var xml = XElement.Parse(value);
                var ips = xml.Elements("ip").Select(i => new Ip { Name = i.Element("name").Value, Start = i.Element("start").Value, End = i.Element("end").Value });
                Value.AddRange(ips);
            }
        }

        public List<Ip> Value { get; set; }
    }

    public class CompanySettingApiGuidModel
    {
        public CompanySettingApiGuidModel() { }

        public CompanySettingApiGuidModel(SettingInfo setting, string companyValue)
        {
            if (Guid.TryParse(setting.DefaultValue, out Guid d))
            {
                Default = d;
            }

            if (Guid.TryParse(companyValue, out Guid v))
            {
                Value = v;
            }
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

    public static class SettingExtensions
    {
        public static string GetValue(this List<SettingInfo> settings, Setting s)
        {
            var value = settings.First(i => i.ID == s).Value;
            return value;
        }
        public static T GetValue<T>(this List<SettingInfo> settings, Setting s)
        {
            var value = settings.First(i => i.ID == s).Value;
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }

        }

        public static string GetName(this Setting type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this Setting type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static bool GetIsLocked(this Setting type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<LockedAttribute>().Locked;
        }

        public static List<SettingInfo> GetAsList(this Setting type)
        {
            var list = new List<SettingInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var enumValue = (Setting)Enum.Parse(typeof(Setting), tm.Name);

                list.Add(new SettingInfo
                {
                    Name = tm.GetCustomAttribute<NameAttribute>().Name,
                    Description = tm.GetCustomAttribute<DescriptionAttribute>().Description,
                    ID = enumValue,
                    Locked = tm.GetCustomAttribute<LockedAttribute>().Locked,
                    DefaultValue = tm.GetCustomAttribute<DefaultValueAttribute>().Value.ToString(),
                    Type = tm.GetCustomAttribute<TypeAttribute>().Type
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        public static SettingInfo AsInfoModel(this Setting type)
        {
            var info = new SettingInfo();

            var member = type.GetType().GetMember(type.ToString()).Single();

            info.Description = member.GetCustomAttribute<DescriptionAttribute>().Description;
            info.Name = member.GetCustomAttribute<NameAttribute>().Name;
            info.Locked = member.IsDefined(typeof(LockedAttribute)) ? member.GetCustomAttribute<LockedAttribute>().Locked : false;
            info.DefaultValue = member.GetCustomAttribute<DefaultValueAttribute>().Value.ToString();
            info.Type = member.GetCustomAttribute<TypeAttribute>().Type;
            info.ID = type;
            return info;
        }
    }
}
