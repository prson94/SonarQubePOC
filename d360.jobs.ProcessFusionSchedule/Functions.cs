using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.ProcessFusionSchedule
{
    public class Functions: FunctionsBase
    {
        [NoAutomaticTrigger]
        public static void CallDatabase()
        {
            ExecuteActionOnAllCompanies("ProcessFusionSchedule.CallDatabase", "exec ProcessSchedule", 180);
        }
    }
}
