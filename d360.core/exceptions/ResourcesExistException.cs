using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class ResourcesExistException : BaseException
    {
        public ResourcesExistException()
            : base(HttpStatusCode.Conflict, "Resources Currently Assigned", "Item could not be removed because there are resources currently assigned to it.")
        {
        }
    }


}
