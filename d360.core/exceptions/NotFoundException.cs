using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string objectName)
            : base(HttpStatusCode.NotFound, Error.ItemNotFound, string.Format(Error.ItemNotFound, objectName))
        {
        }
    }
}
