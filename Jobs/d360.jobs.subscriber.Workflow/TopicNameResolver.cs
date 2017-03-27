using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.jobs.subscriber.Workflow
{
    public class TopicNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
#if DEBUG
            return "events-debug";
#else
            return ConfigurationManager.AppSettings[name].ToString();
#endif
        }
    }
}
