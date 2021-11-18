using System;
using System.Linq;
using System.Web;
using MediatR;

namespace d360.web.Services
{
    public class ResponsibilityGetTypeBreakdownRequest: IRequest<ResponsibilityGetTypeBreakdownResponse>
    {
        public ResponsibilityGetTypeBreakdownRequest()
        {
            
        }

        // change to init later
        public Guid? TypeUid { get; set; }
    }
}