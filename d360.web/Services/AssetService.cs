using d360.core.entities;
using d360.core.enums;
using d360.core.resources;

namespace d360.web.Services
{
    internal sealed class AssetService : IAssetService
    {
        public string GetAssetName(AssetType assetType)
        {
            var defaultName = string.Empty;

            switch (assetType.Object)
            {
                case "ArtifactType":
                    switch (assetType.Class)
                    {
                        case AssetTypeClass.BusinessAsset:
                            return CommonNames.AssetTypeClass_Business;
                        case AssetTypeClass.TechnicalAsset:
                            return CommonNames.AssetTypeClass_Technical;
                        default:
                            return defaultName;
//                            return $"Unknown {assetType.Class} class {assetType.Class}";
                    }
                case "PolicyType":
                    return CommonNames.AssetTypeClass_Policy;
                case "ReferenceItemType":
                    return "Reference: ";
                case "RuleType":
                    return CommonNames.AssetTypeClass_Rule;
                case "TaxonomyType":
                    return CommonNames.AssetTypeClass_Model;
                case "AttributeType":
                    return "Attribute: ";
                case "GroupType":
                    return "Group: ";
                case "OrganizationType":
                    return "Organization: ";
                case "ResourceType":
                    return "Resource: ";
                default:
                    return defaultName;
            }
        }
    }
}