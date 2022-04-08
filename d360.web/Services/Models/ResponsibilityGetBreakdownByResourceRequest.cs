using System;

using MediatR;

namespace d360.web.Services
{
    public class ResponsibilityGetBreakdownByResourceRequest : IRequest<ResponsibilityGetBreakdownByResourceResponse>
    {
        /// <summary>
        /// Resource UID
        /// </summary>
        public Guid ResourceUid { get; set; }

        /// <summary>
        /// Responsibility Type UID
        /// </summary>
        public Guid? ResponsibilityTypeUid { get; set; }
    }
}
