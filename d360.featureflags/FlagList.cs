namespace d360.featureflags
{
	public static class FlagList
	{
		#region Permanent Feature Flags

		public const string PERM_BRANDING_CUSTOM_CSS = "GovernBrandingCustomCssPerm";
		public const string PERM_IS_DISTRIBUTED_CACHE = "GovernDistributedCachePerm";

		public const string PERM_DATA_PROFILING = "GovernDataProfilingPerm";
		public const string PERM_DATA_PROFILING_UI = "GovernDataProfileUiPerm";
		public const string PERM_SEMANTIC_TYPES = "GovernSemanticTypesPerm";
		public const string PERM_SEMANTIC_TYPES_API = "GovernSemanticTypesApiPerm";
		public const string PERM_SEMANTIC_TYPES_UI = "GovernSemanticTypesUiPerm";

		#endregion

		#region Temporary Feature Flags

		public const string TEMP_ASSIGNMENTS = "GovernAssignmentsTemp20230815";

		#endregion

		#region Micro Service Feature Flags

		public const string CATALOG_MICRO = "UseCatalogMicroserviceTemp20231011";

		#endregion
	}
}
