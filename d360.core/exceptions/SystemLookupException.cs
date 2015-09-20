using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class SystemLookupException : BaseException
    {
        public SystemLookupException(string lookupName)
            :base(HttpStatusCode.Conflict, "System Lookup Cannot Be Removed", string.Format("{0} could not be removed because it is a system lookup.", lookupName))
        {
        }
    }


}
