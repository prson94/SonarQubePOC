using System;
using MediatR;

namespace d360.web.Services
{
    public class ResponsibilityGetBreakdownByResourceRequest: IRequest<ResponsibilityGetBreakdownByResourceResponse>
    {
        public Guid ResourceUid { get; set; }

        public Guid? ResourceTypeUid { get; set; }
    }
}