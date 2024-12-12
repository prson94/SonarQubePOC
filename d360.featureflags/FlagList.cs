namespace d360.featureflags
{
	public static class FlagList
	{
		#region Permanent Feature Flags

		public static readonly string PERM_BRANDING_CUSTOM_CSS = "GovernBrandingCustomCssPerm";
		public static readonly string PERM_IS_DISTRIBUTED_CACHE = "GovernDistributedCachePerm";
		public static readonly string PERM_IS_DASHBOARDING_ENABLED = "govern-dashboarding-functionality-permanent";

		public static readonly string PERM_DATA_PROFILING_UI = "GovernDataProfileUiPerm";
		public static readonly string PERM_SEMANTIC_TYPES_API = "GovernSemanticTypesApiPerm";
		public static readonly string PERM_SEMANTIC_TYPES_UI = "GovernSemanticTypesUiPerm";

		#endregion

		#region Temporary Feature Flags
		
		public static readonly string TEMP_CUSTOM_SYNONYMS = "govern-custom-synonyms-temp";
		public static readonly string TEMP_TAGS_LIMITED_VALUES = "govern-tagging-new-administration-ui-temp";
		public static readonly string TEMP_TAGS_ADMIN_UI_V2 = "govern-tagging-value-limitation-temp";
		public static readonly string TEMP_NEW_SECURITY_MODEL = "govern-security-security-policy-conversion-temp";

		#endregion
	}
}
