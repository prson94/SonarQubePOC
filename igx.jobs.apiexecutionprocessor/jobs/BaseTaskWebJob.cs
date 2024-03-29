using Microsoft.Extensions.Configuration;

namespace igx.jobs.apiexecutionprocessor
{
	public abstract class BaseTaskWebJob : BaseWebJob
	{
		protected BaseTaskWebJob(IConfiguration config) : base(config) { }
	}
}
