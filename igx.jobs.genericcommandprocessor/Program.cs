using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.genericcommandprocessor
{
    class Program
    {
        static async Task Main()
        {
            using (var host = CoreFunction.JobHostConfig())
            {
                await host.RunAsync();
            }
        }
    }

    public static class GenericCommandProcessor
    {
        const string functionName = "GenericCommandProcessor";
        const string timerSettings = "0 0 */3 * * *";

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 1).ToList();
#endif

                var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                community.Open();
                var genericCommands = community.Query<GenericCommand>("select * from GenericCommand").ToList();
                community.Close();
                community.Dispose();

                genericCommands.ForEach(gc =>
                {
                    companies.AsParallel().ForAll(c =>
                        {
                            bool _continue = true;
                            if (gc.EnvironmentsLimit.Any() || gc.ClientsLimit.Any())
                            {
                                if (gc.EnvironmentsLimit.Any() && !gc.EnvironmentsLimit.Contains(c.EnvironmentLevel))
                                {
                                    _continue = false;
                                }
                                if (_continue && gc.ClientsLimit.Any() && !gc.ClientsLimit.Contains(c.ClientID))
                                {
                                    _continue = false;
                                }
                            }

                            if (_continue)
                            {
                                try
                                {
                                    using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                                    {
                                        companyConnection.Open();
                                        companyConnection.Execute(gc.CommandText, null, null, gc.CommandTimeout);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    CoreFunction.AITrackException(functionName, ex);
                                }
                            }
                        });
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }
    }
}
