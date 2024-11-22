using Microsoft.Extensions.Configuration;
using repositories;

namespace igx.jobs.apiexecutionprocessor
{
	public abstract class BaseTaskWebJob : BaseWebJob
	{
		protected BaseTaskWebJob(ICommunity community, IConfiguration config) : base(community, config) { }
	}
}
