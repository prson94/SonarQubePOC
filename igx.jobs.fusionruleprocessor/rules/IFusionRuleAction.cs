using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    internal interface IFusionRuleAction
    {
        Task Execute(List<int> itemsToPromote, SqlConnection company);

        FusionRuleStepStatistics Stats { get; set; }
    }
}