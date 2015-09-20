using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string objectName)
            :base(HttpStatusCode.NotFound, "Item Not Found", string.Format("{0} could not be found.", objectName))
        {
        }
    }


}
