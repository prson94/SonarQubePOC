using Microsoft.Azure.WebJobs;

namespace d360.jobs.CalculateAnalytics
{
    public class Functions: FunctionsBase
    {
        [NoAutomaticTrigger]
        public static void CallDatabase()
        {
            ExecuteActionOnAllCompanies("CalculateAnalytics.CallDatabase", "exec utility.CalculateStatistics", 600);
        }
    }
}
