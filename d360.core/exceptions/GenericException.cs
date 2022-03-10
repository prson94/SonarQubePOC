using System.Net;

namespace d360.core.exceptions
{
    public class GenericException : BaseException
    {
        public GenericException(HttpStatusCode status, string title, string error)
            : base(status, title, error)
        {
        }

        public GenericException(HttpStatusCode status, string error)
            : base(status, error, error)
        {
        }
    }
}
