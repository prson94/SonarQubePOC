using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    public class FusionActionBase
    {
        public FusionActionBase()
        {
            Stats = new FusionRuleStepStatistics();
        }

        protected int EXECUTION_TIMEOUT = 300;
        public FusionRuleStepModel Step { get; set; }
        public int CompanyId { get; set; }
        public d360.core.entities.FusionRule Rule { get; set; }
        public TextWriter Log { get; set; }

        public FusionRuleStepStatistics Stats
        {
            get;
            set;
        }

        public static async Task PrintTempTableContents(SqlConnection company, TextWriter log, string tempTableName)
        {            
            log.WriteLine($"====================DEBUG  {tempTableName.ToUpper()} PRINTING VALUES================================");

            var items = await company.QueryAsync($"select * from #{tempTableName}");

            foreach (var item in items)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kvp in item)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append($"{kvp.Key}={kvp.Value}");
                }
                log.WriteLine(sb.ToString());
            }

            log.WriteLine($"====================END {tempTableName.ToUpper()} DEBUG PRINTING VALUES================================");
        }
    }
}
