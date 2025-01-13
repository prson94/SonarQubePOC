using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class DuplicateNameException : BaseException
    {
        public DuplicateNameException(string objectName)
            : base(HttpStatusCode.Conflict, Error.DuplicateNameFound, string.Format(Error.NameConflicit, objectName))
        {
        }
    }

    public class DuplicateObjectException : BaseException
    {
        public DuplicateObjectException(string objectName)
            : base(HttpStatusCode.Conflict, Error.DuplicateFound, string.Format(Error.ItemConflicit, objectName))
        {
        }
    }
}
