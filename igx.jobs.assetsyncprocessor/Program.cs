using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace igx.jobs.assetsyncprocessor
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

#if DEBUG
            config.UseDevelopmentSettings();
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
