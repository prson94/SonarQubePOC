using System.Net;

namespace d360.core.exceptions
{
    public class DuplicateNameException : BaseException
    {
        public DuplicateNameException(string objectName)
            :base(HttpStatusCode.Conflict, "Duplicate Name Found", string.Format("{0} could not be added or updated because an item with the same name already exists.", objectName))
        {
        }
    }

    public class DuplicateObjectException : BaseException
    {
        public DuplicateObjectException(string objectName)
            : base(HttpStatusCode.Conflict, "Duplicate Found", string.Format("{0} could not be added or updated because an existing item already found.", objectName))
        {
        }
    }
}
