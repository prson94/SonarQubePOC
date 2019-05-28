using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.model
{
    public class WorkHttpStatus
    {
            public HttpStatusCode StatusCode { get; set; }
            public string Error { get; set; }
            public string Message { get; set; }
            public WorkHttpStatus(HttpStatusCode hsc, string err, string msg)
            {
                this.StatusCode = hsc;
                this.Error = err;
                this.Message = msg;
            }
    }
}
