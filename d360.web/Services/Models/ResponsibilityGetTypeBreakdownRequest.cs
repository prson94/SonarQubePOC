using System;
using MediatR;

namespace d360.web.Services
{
    public class ResponsibilityGetTypeBreakdownRequest: IRequest<ResponsibilityGetTypeBreakdownResponse>
    {
        public ResponsibilityGetTypeBreakdownRequest()
        {
            
        }

        //TODO: change to init setter later
        /// <summary>
        /// Resource Type UID
        /// </summary>
        public Guid? ResponsibilityTypeUid { get; set; }
    }
}