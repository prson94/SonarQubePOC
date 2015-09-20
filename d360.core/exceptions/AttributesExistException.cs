using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class AttributesExistException : BaseException
    {
        public AttributesExistException(string name)
            :base(HttpStatusCode.Conflict, "Existing Attributes Found", string.Format("{0} could not be removed because there are existing attributes associated with it.", name))
        {
        }
    }


}
