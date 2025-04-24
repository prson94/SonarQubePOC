using d360.extensions;
using repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.web.Services
{
	public class CommunityFeatureFlagService
	{
		ICachingProvider Cache { get; set; }

		ICommunity Community { get; set; }

		ISecurityContextProvider Context { get; set; }

		public CommunityFeatureFlagService(ICachingProvider cache, ICommunity community, ISecurityContextProvider context)
		{
			Cache = cache;		
			Community = community;
			Context = context;
		}

		public async Task<bool> GetFlagValue(string flagName)
		{
			var flags = await GetFlags();

			if (flags != null && flags.ContainsKey(flagName))
			{
				return flags[flagName];
			}

			return false;
		}

		public async Task<Dictionary<string, bool>> GetFlags()
		{
			var key = $"Company_FF_{Context.CompanyID}";
			var flags = Cache.GetItem<Dictionary<string, bool>>(key);

			if (flags == null)
			{
				flags = await Community.ReadFeatureFlagsByTenantAsync(Context.CompanyID);
				if (flags != null)
				{
					Cache.SetItem(key, flags, true, 5);
				}
			}

			return flags;
		}


	}
}