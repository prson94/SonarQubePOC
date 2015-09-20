using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    public enum CommentType
    {
        [ReadOnly(true)]
        System = 1,
        Social = 2,
        Governance = 3,
        Relationship = 4,
        Issue = 5,
        Task = 6,
        RedFlag = 7,
        DataEvent = 8,
        Question = 9
    }
}
