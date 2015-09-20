using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class RelationsExistException : BaseException
    {
        public RelationsExistException(string name, int count)
            :base(HttpStatusCode.Conflict, "Existing Relations Found", string.Format("{0} could not be removed because it is related to {1} item{2}.", name, count, (count> 1 ? "s" : "")))
        {
        }
    }


}
