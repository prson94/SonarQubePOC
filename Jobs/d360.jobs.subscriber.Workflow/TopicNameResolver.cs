using Microsoft.Azure;
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
            var topicName = "";
#if DEBUG            
            topicName = "events-debug";
#else
            topicName = CloudConfigurationManager.GetSetting(name);            
#endif
            Console.WriteLine($"TOPIC NAME RESOLVER : WEBJOB IS LISTENING ON TOPIC NAME {topicName}");

            return topicName;
        }
    }
}
