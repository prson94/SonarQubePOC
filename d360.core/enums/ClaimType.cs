namespace d360.core.enums
{
	public enum ClaimType
	{
		[AllowedActions(ClaimAction.Lookup), AllowMultiple(false)]
		Tenant = 1,
		[AllowedActions(ClaimAction.Lookup), AllowMultiple(false)]
		NameIdentifier = 2,
		[AllowedActions(ClaimAction.Lookup), AllowMultiple(false)]
		Username = 3,
		[AllowedActions(ClaimAction.Replace), AllowMultiple(false)]
		Email = 4,
		[AllowedActions(ClaimAction.Replace), AllowMultiple(false)]
		FirstName = 5,
		[AllowedActions(ClaimAction.Replace), AllowMultiple(false)]
		LastName = 6,
		[AllowedActions(ClaimAction.Append, ClaimAction.Replace), AllowMultiple(true)]
		Groups = 7
	}
}

