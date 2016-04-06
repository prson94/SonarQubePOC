using System.ComponentModel;

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
        Challenge = 9
    }
}
