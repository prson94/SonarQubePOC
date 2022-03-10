using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using d360.core.enums;

using Newtonsoft.Json;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRuleViewModel : BaseObject
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Context { get; set; }

        public string DefinitionRaw { get; set; }

        [DataMember]
        public ResponsibilityRuleDefinition Definition
        {
            get
            {
                ResponsibilityRuleDefinition def = new ResponsibilityRuleDefinition();
                try
                {
                    if (string.IsNullOrEmpty(DefinitionRaw))
                    {
                        return def;
                    }

                    def = JsonConvert.DeserializeObject<ResponsibilityRuleDefinition>(DefinitionRaw);
                }
                catch { }
                return def;
            }
        }

        [DataMember]
        public bool IsVisible { get; set; }

        [DataMember]
        public bool ApplyToType { get; set; }

        [DataMember]
        public DateTime? LastRunOn { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string AssetTypeName { get; set; }

        public AssetTypeClass? AssetClass { get; set; }

        [DataMember]
        public AssetTypeClassInfo AssetTypeClass
        {
            get
            {
                if (AssetClass.HasValue)
                {

                    MemberInfo tm = AssetClass.GetType().GetMember(AssetClass.ToString()).First();
                    return new AssetTypeClassInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), tm.Name)
                    };
                }
                return null;
            }
        }
    }
}
