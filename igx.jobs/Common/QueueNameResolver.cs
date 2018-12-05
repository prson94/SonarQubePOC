using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using System.Configuration;

namespace igx.jobs
{
    public class QueueNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return CloudConfigurationManager.GetSetting(name);
        }
    }
}
