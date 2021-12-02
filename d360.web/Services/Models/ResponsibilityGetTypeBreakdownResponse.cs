using System.Collections.Generic;
using d360.core.entities;

namespace d360.web.Services
{
    internal class ResponsibilityGetTypeBreakdownResponse
    {
        public IReadOnlyList<ResponsibilityBreakdownResponse> Data { get; set; }
    }
}