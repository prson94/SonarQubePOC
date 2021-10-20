using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core
{
    public static class FeatureFlags
    {
        #region Permanent Feature Flags

        // Naming is "TeamName-FeatureName-Perm

        public static readonly string PERM_IS_DISTRIBUTED_CACHE = "govern-distributed-cache-perm";

        #endregion

        #region Temporary Feature Flags

        // Naming is "TeamName-FeatureName-Temp-YYYYMMDD

        #endregion
    }
}
