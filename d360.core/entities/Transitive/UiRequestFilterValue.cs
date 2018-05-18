using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class UiRequestFilterValue
    {
        public string Operator { get; set; } = "OR";
        public string RawValue { get; set; }
    }

    public class UiRequestAttributeFilterValue : UiRequestFilterValue
    {
        public int AttributeTypeID { get; set; }
    }

    public class UiRequestFieldFilterValue: UiRequestFilterValue
    {
        public string FieldName { get; set; }
        public string Condition { get; set; }
        public bool IsUnlistedFilterField { get; set; } = false;

        public bool IsParentField { get {  return (string.Compare(this.FieldName, "Parent", true) == 0); } }
    }

    public enum UiRequestOwnershipFilterType
    {
        Group,
        Organization,
        User
    }

    public class UiRequestOwnershipFilterItem
    {
        public UiRequestOwnershipFilterType FilterType { get; set; }
        public int ResponsibilityTypeID { get; set; }
        public int SecurityAssetID { get; set; }
    }

    public class UiRequestOwnershipFilterValue : UiRequestFilterValue
    {
        public List<UiRequestOwnershipFilterItem> Items { get; set; } = new List<UiRequestOwnershipFilterItem>();
    }

    public class UiRequestRelationshipFilterValue : UiRequestFilterValue
    {
        public int IntersectTypeID { get; set; }
        public string TargetObject { get; set; }
        public List<int> TargetObjectIDs { get; set; }
    }
}
