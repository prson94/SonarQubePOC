using d360.core.entities;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;

namespace d360.featureflags
{
	public class FeatureFlagService: IFeatureFlagService
	{
		private readonly LdClient Ld;

		public FeatureFlagService(string apikey)
		{
			Ld = new LdClient(apikey);
		}

		public bool IsThisTrue(string flag, ClientUserModel user, bool defaultValue = false)
		{
			var ctx = GetContext(user);
			return Ld.BoolVariation(flag, ctx, defaultValue);
		}

		Context GetContext(ClientUserModel user)
		{
			var b = Context.Builder(user.Key);
			b.Set("FirstName", user.FirstName)
				.Set("LastName", user.LastName)
				.Set("Email", user.Email)
				.Set("tenantId", user.TenantId.ToString())
				.Set("tenantName", user.TenantName);
			return b.Build();
		}
	}
}
