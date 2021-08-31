using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public interface IFilterToken
    {
        string GetSqlExpression(Dictionary<string, object> sqlParams);
    }
}
