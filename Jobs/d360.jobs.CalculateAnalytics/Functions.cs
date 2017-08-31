using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System.Linq;
using Dapper;
using System.Data.SqlClient;
using System;
using d360.core;

namespace d360.jobs.CalculateAnalytics
{
    public class Functions: FunctionsBase
    {
        [NoAutomaticTrigger]
        public static void CallDatabase()
        {
            var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();

#if DEBUG
            companies = companies.Where(i => i.CompanyID == 4).ToList();
#endif

            companies.ForEach(company =>
            {
                SqlConnection companyConnection = null;

                try
                {
                    companyConnection = GetCompanyConnection(company.CompanyID);
                    companyConnection.Open();

                    companyConnection.ExecuteAsync("exec utility.CalculateScores", commandTimeout: 1400);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetFullExceptionData());
                }
                finally
                {
                    if (companyConnection != null)
                        companyConnection.Close();
                }
            });
        }
    }
}
