namespace d360.featureflags
{
	public static class FlagList
	{
		#region Permanent Feature Flags

		public static string PERM_BRANDING_CUSTOM_CSS = "GovernBrandingCustomCssPerm";
		public static string PERM_IS_DISTRIBUTED_CACHE = "GovernDistributedCachePerm";

		public static string PERM_DATA_PROFILING = "GovernDataProfilingPerm";
		public static string PERM_DATA_PROFILING_UI = "GovernDataProfileUiPerm";
		public static string PERM_SEMANTIC_TYPES = "GovernSemanticTypesPerm";
		public static string PERM_SEMANTIC_TYPES_API = "GovernSemanticTypesApiPerm";
		public static string PERM_SEMANTIC_TYPES_UI = "GovernSemanticTypesUiPerm";

		#endregion

		#region Temporary Feature Flags

		public static string TEMP_ASSIGNMENTS = "GovernAssignmentsTemp20230815";

		#endregion

		#region Micro Service Feature Flags

		public static string CATALOG_MICRO = "UseCatalogMicroserviceTemp20231011";

		#endregion
	}
}
