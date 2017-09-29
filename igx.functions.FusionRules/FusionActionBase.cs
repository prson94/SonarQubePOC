using d360.core.entities;
using Dapper;
using igx.functions.FusionRules.Models;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.functions.FusionRules
{    
    public class FusionActionBase
    {
        public FusionActionBase()
        {
            Stats = new FusionRuleStepStatistics();
        }
        public FusionRuleStepModel Step { get; set; }
        public int CompanyId { get; set; }
        public FusionRule Rule { get; set; }
        public TraceWriter Log { get; set; }

        public FusionRuleStepStatistics Stats
        {
            get;
            set;
        }

        public static async Task PrintTempTableContents(SqlConnection company, TraceWriter log, string tempTableName)
        {
            log.Info($"====================DEBUG  {tempTableName.ToUpper()} PRINTING VALUES================================");

            var items = await company.QueryAsync($"select * from #{tempTableName}");

            foreach (var item in items)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kvp in item)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append($"{kvp.Key}={kvp.Value}");
                }
                log.Info(sb.ToString());
            }

            log.Info($"====================END {tempTableName.ToUpper()} DEBUG PRINTING VALUES================================");
        }
    }
}
