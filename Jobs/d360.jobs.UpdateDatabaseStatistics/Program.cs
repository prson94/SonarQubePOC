using Microsoft.Azure.WebJobs;

namespace d360.jobs.UpdateDatabaseStatistics
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));
            Functions.CallDatabase();

            //var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            //Task callTask = host.CallAsync(typeof(Functions).GetMethod("CallDatabase"));

            //Console.WriteLine("Waiting for async operation...");
            //callTask.Wait();
            //Console.WriteLine("Task completed: " + callTask.Status);
        }
    }
}
