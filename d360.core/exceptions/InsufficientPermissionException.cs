using System.Net;

namespace d360.core.exceptions
{
    public class InsufficientPermissionException : BaseException
    {
        public InsufficientPermissionException(string message)
            : base(HttpStatusCode.Forbidden, message, "")
        {

        }
    }
}
