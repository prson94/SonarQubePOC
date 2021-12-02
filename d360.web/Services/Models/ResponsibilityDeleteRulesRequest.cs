using System;
using System.Collections.Generic;
using MediatR;

namespace d360.web.Services
{
    public class ResponsibilityDeleteRulesRequest : IRequest<ResponsibilityDeleteRulesResponse>
    {
        public ResponsibilityDeleteRulesRequest()
        {
            RuleDeleteUidCollection = new List<Guid>();
        }

        public Guid TypeUid { get; set; }

        public IReadOnlyList<Guid> RuleDeleteUidCollection { get; set; }
    }
}