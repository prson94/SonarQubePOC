using d360.core.queue;
using d360.extensions.storage;
using d360.utils.company;
using System.Data.SqlClient;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public abstract class ProcessBase
    {
        public ScoreQueueInfo Info { get; set; }
        public AzureStorageProvider Storage { get; set; }

        string companyConnectionString = null;

        internal SqlConnection GetEnvironmentConnection()
        {
            if (string.IsNullOrEmpty(companyConnectionString))
            {
                companyConnectionString = CompanyConnectionUtils.GetCompanyConnectionString(Info.CompanyID);
            }
            return new SqlConnection(companyConnectionString);
        }
    }
}
