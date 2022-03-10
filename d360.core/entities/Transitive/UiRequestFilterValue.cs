using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace d360.core.entities
{
    public class UiRequestFilterValue
    {
        public string Operator { get; set; } = "OR";
        
        public string RawValue { get; set; }
    }

    public class UiRequestFieldFilterValue : UiRequestFilterValue
    {
        public string FieldName { get; set; }
        
        public string Condition { get; set; }
        
        public bool IsUnlistedFilterField { get; set; } = false;

        public bool IsParentField { get { return (string.Compare(FieldName, "Parent", true) == 0); } }
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

        public JObject GetAsJsonDbQueryObject()
        {
            var obj = new JObject();

            var securityAsset = "";
            switch (FilterType)
            {
                case UiRequestOwnershipFilterType.Group:
                    securityAsset = "G";
                    break;
                case UiRequestOwnershipFilterType.Organization:
                    securityAsset = "O";
                    break;
                case UiRequestOwnershipFilterType.User:
                    securityAsset = "R";
                    break;
            }

            obj.Add("SecurityAsset", securityAsset);
            obj.Add("SecurityAssetID", SecurityAssetID);
            obj.Add("ResponsibilityTypeID", ResponsibilityTypeID);

            return obj;
        }
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
