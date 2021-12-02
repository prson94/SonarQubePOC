using d360.core.entities;
using d360.core.enums;
using d360.core.resources;

namespace d360.web.Services
{
    internal sealed class AssetService : IAssetService
    {
        public string GetAssetName(AssetType assetType)
        {
            var prefix = GetPrefix(assetType);
            return string.IsNullOrWhiteSpace(prefix) ? assetType.Name : $"{prefix}: {assetType.Name}";
        }

        private string GetPrefix(AssetType assetType)
        {
            var result = string.Empty;

            switch (assetType.Object)
            {
                case "ArtifactType":
                    switch (assetType.Class)
                    {
                        case AssetTypeClass.BusinessAsset:
                            result = CommonNames.AssetTypeClass_Business;
                            break;
                        case AssetTypeClass.TechnicalAsset:
                            result = CommonNames.AssetTypeClass_Technical;
                            break;
                    }
                    break;
                case "PolicyType":
                    result = CommonNames.AssetTypeClass_Policy;
                    break;
                case "ReferenceItemType":
                    result = "Reference";
                    break;
                case "RuleType":
                    result = CommonNames.AssetTypeClass_Rule;
                    break;
                case "TaxonomyType":
                    result = CommonNames.AssetTypeClass_Model;
                    break;
                case "AttributeType":
                    result = "Attribute";
                    break;
                case "GroupType":
                    result = "Group";
                    break;
                case "OrganizationType":
                    result = "Organization";
                    break;
                case "ResourceType":
                    result = "Resource";
                    break;
            }

            return result;
        }
    }
}