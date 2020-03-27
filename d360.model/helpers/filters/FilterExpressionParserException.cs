using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class FilterExpressionParserException : Exception
    {
        public FilterExpressionParserException()
        {
        }

        public FilterExpressionParserException(string message)
            : base(message)
        {
        }

        public FilterExpressionParserException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
