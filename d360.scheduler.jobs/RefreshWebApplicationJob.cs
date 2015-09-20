using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.scheduler.jobs
{
    public class RefreshWebApplicationJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            var uris = context.MergedJobDataMap["Uri"] as List<string>;
            var client = new WebClient();
            foreach (var uri in uris)
            {
                client.DownloadData(uri);
            }
        }
    }
}
