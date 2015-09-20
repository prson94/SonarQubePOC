using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace d360.core.exceptions
{
    public class ConflictException : BaseException
    {
        public ConflictException(string message, string description)
            :base(HttpStatusCode.Conflict, message, description)
        {
        }
    }


}
