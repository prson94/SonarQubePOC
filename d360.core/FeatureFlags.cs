using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core
{
    public static class FeatureFlags
    {
        #region Permanent Feature Flags

        public static readonly string PERM_BRANDING_CUSTOM_CSS = "GovernBrandingCustomCssPerm";
        public static readonly string PERM_IS_DISTRIBUTED_CACHE = "GovernDistributedCachePerm";

        #endregion

        #region Temporary Feature Flags

        public static readonly string TEMP_BRANDING_NEWUI_TEMP = "GovernBrandingUiTemp20220531";

        #endregion
    }
}
