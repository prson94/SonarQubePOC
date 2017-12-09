using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.function.Fusion.rules
{
    internal interface IFusionRelateAction
    {
        Task Relate(List<int> itemsToPromote, SqlConnection company);
    }
}