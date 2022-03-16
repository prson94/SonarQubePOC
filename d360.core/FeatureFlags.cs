namespace d360.core
{
    public static class FeatureFlags
    {
        #region Permanent Feature Flags

        public static readonly string PERM_BRANDING_CUSTOM_CSS = "GovernBrandingCustomCssPerm";
        public static readonly string PERM_IS_DISTRIBUTED_CACHE = "GovernDistributedCachePerm";

        public static readonly string PERM_DATA_PROFILING = "GovernDataProfilingPerm";
        public static readonly string PERM_DATA_PROFILING_UI = "GovernDataProfileUiPerm";
        public static readonly string PERM_SEMANTIC_TYPES = "GovernSemanticTypesPerm";
        public static readonly string PERM_SEMANTIC_TYPES_API = "GovernSemanticTypesApiPerm";
        public static readonly string PERM_SEMANTIC_TYPES_UI = "GovernSemanticTypesUiPerm";


        #endregion

        #region Temporary Feature Flags

        public static readonly string TEMP_BRANDING_NEWUI_TEMP = "GovernBrandingUiTemp20220531";

        #endregion
    }
}
