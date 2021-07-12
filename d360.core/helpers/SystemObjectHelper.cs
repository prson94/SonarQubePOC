using d360.core.enums;

namespace d360.core.helpers
{
    public class SystemObjectHelper
    {
        public static SystemObjects GetSystemObjects(AssetTypeClass assetTypeClass)
        {
            switch (assetTypeClass)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                    return SystemObjects.ArtifactType;
                case AssetTypeClass.Organization:
                    return SystemObjects.OrganizationType;
                case AssetTypeClass.Policy:
                    return SystemObjects.PolicyType;
                case AssetTypeClass.Reference:
                    return SystemObjects.ReferenceItemType;
                case AssetTypeClass.Rule:
                    return SystemObjects.RuleType;
                case AssetTypeClass.Model:
                    return SystemObjects.TaxonomyType;
                case AssetTypeClass.FusionAttribute:
                    return SystemObjects.FusionAttributeType;
                case AssetTypeClass.Diagram:
                    return SystemObjects.TaskType;
            }
            return SystemObjects.ArtifactType;//default
        }
    }
}
