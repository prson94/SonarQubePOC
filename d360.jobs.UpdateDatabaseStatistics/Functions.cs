using Microsoft.Azure.WebJobs;

namespace d360.jobs.UpdateDatabaseStatistics
{
    public class Functions: FunctionsBase
    {
        [NoAutomaticTrigger]
        public static void CallDatabase()
        {
            ExecuteActionOnAllCompanies("UpdateDatabaseStatistics.CallDatabase", "sp_updatestats", 600);
        }
    }
}
