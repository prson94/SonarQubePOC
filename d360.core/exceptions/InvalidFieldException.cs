using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class InvalidFieldException : BaseException
    {
        public InvalidFieldException(string name, string problem)
            : base(HttpStatusCode.BadRequest, Error.InvalidFieldEntry, string.Format(Error.InvalidFieldMessage, name, problem))
        {
        }

        public InvalidFieldException(string message)
            : base(HttpStatusCode.BadRequest, Error.InvalidFieldEntry, message)
        {
        }
    }
}
