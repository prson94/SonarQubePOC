using System;
using System.Collections.Generic;

namespace d360.web.Services
{
    public class ResponsibilityGetBreakdownByResourceResponse
    {
        public ResponsibilityGetBreakdownByResourceResponse()
        {
            ItemCollection = Array.Empty<ResponsibilityGetBreakdownByResourceModel>();
        }

        public IReadOnlyList<ResponsibilityGetBreakdownByResourceModel> ItemCollection { get; set; }
    }
}