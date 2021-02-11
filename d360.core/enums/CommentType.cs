using System.ComponentModel;

namespace d360.core.enums
{
    public enum CommentType
    {
        [ReadOnly(true)]
        System = 1,
        Social = 2,
        Issue = 5
    }
}
