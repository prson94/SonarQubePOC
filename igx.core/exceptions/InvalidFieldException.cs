using System.Net;

namespace d360.core.exceptions
{
    public class InvalidFieldException : BaseException
    {
        public InvalidFieldException(string name, string problem)
            :base(HttpStatusCode.BadRequest, "Invalid Field Entry", string.Format("{0} could not be updated because it is {1}.", name, problem))
        {
        }

        public InvalidFieldException(string message)
            : base(HttpStatusCode.BadRequest, "Invalid Field Entry", message)
        {
        }
    }
}
