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
