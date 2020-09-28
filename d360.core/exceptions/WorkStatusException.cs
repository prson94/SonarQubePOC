using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.exceptions
{
    public class WorkStatusException : Exception
    {
        public HttpStatusCode Status { get; private set; }

        public WorkStatusException() { }
        public WorkStatusException(HttpStatusCode status, string message): base(message)
        {
            Status = status;
        }
    }
}
