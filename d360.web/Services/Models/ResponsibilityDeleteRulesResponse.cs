using System.Collections.Generic;
using d360.core.entities;

namespace d360.web.Services
{
    public class ResponsibilityDeleteRulesResponse
    {
        public ResponsibilityDeleteRulesResponse()
        {
            Data = new List<ResponsibilityRuleDeleteResponse>();
        }

        public IReadOnlyList<ResponsibilityRuleDeleteResponse> Data { get; set; }
    }
}