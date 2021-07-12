using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeAllocationViewModel : BaseObject
    {

        [DataMember]
        public Guid ResponsibilityTypeUid { get; set; }

        [DataMember]
        public string ResponsibilityTypeName { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string AssetTypeName { get; set; }

        [DataMember]
        public string AssetTypePath { get; set; }

        public AssetTypeClass? AssetClass { get; set; }

        [DataMember]
        public AssetTypeClassInfo AssetTypeClass {
            get
            {
                if (AssetClass.HasValue)
                {
                    
                    MemberInfo tm = AssetClass.GetType().GetMember(AssetClass.ToString()).First();
                    return new AssetTypeClassInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), tm.Name),
                        Value = tm.Name
                    };
                }
                return null;
            }
        }

        [DataMember]
        public int PermissionsMask { get; set; }

        [DataMember]
        public List<PermissionInfo> Permissions
        {
            get
            {
                var permissions = Permission.DeleteAsset.GetList();
                
                permissions.ForEach(p =>
                {
                    p.Selected = (PermissionsMask & p.Value) == p.Value;
                });

                return permissions;
            }
        }
    }
}
