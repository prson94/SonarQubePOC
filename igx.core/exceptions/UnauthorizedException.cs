using System.Net;

namespace d360.core.exceptions
{
    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message, string description)
            : base(HttpStatusCode.Unauthorized, message, description)
        {

        }
    }
}
