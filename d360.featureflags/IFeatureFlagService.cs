using d360.core.entities;

namespace d360.featureflags
{
	public interface IFeatureFlagService
	{
		bool IsThisTrue(string flag, ClientUserModel user, bool defaultValue = false);
	}
}
