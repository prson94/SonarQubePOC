using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.helpers
{
    public class SystemObjectHelper
    {
        public static SystemObjects GetSystemObjects(AssetTypeClass assetTypeClass)
        {
            switch (assetTypeClass)
            {
                case AssetTypeClass.Glossary:
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

            }
            return SystemObjects.ArtifactType;//default
        }
    }
}
