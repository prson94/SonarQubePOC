using System.Collections.Generic;

namespace d360.model.helpers.filters
{
    public interface IFilterToken
    {
        string GetSqlExpression(Dictionary<string, object> sqlParams);
    }
}
