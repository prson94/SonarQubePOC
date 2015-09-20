using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.exceptions
{
    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message, string description)
            : base(HttpStatusCode.Unauthorized, message, description)
        {

        }
    }
}
