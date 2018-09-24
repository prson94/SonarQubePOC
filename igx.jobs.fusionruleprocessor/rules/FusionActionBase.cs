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

        private static int _executionTimout = 0;
        public static int ExecutionTimeout {
            get
            {
                if (_executionTimout > 0) return _executionTimout;

                var tmp = CoreFunction.GetConfigValueByKey("FusionRuleExecuteQueryTimeout");

                if (int.TryParse(tmp, out int tmpInt))
                {
                    _executionTimout = tmpInt;
                }
                else
                {
                    _executionTimout = 1200;
                }

                return _executionTimout;
            }
            
        }

        private static int _promotionChunkSize = 0;
        public static int PromotionChunkSize
        {
            get
            {
                if (_promotionChunkSize > 0) return _promotionChunkSize;

                string tmp = "";

                try
                {
                    tmp = CoreFunction.GetConfigValueByKey("FusionRulePromoteChunkSize");
                }
                catch
                {

                }

                if (int.TryParse(tmp, out int tmpInt))
                {
                    _promotionChunkSize = tmpInt;
                }
                else
                {
                    _promotionChunkSize = 200;
                }

                return _promotionChunkSize;
            }

        }
        public FusionRuleStepModel Step { get; set; }
        public int CompanyId { get; set; }
        public d360.core.entities.FusionRule Rule { get; set; }
        public TextWriter Log { get; set; }

        public FusionRuleStepStatistics Stats
        {
            get;
            set;
        }

        public static async Task PrintTempTableContents(SqlConnection company, TextWriter log, string tempTableName, SqlTransaction transaction = null)
        {            
            log.WriteLine($"====================DEBUG  {tempTableName.ToUpper()} PRINTING VALUES================================");

            var items = await company.QueryAsync($"select * from #{tempTableName}",transaction: transaction);

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
