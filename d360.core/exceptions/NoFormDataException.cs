using System.Net;

namespace d360.core.exceptions
{
    public class NoFormDataException : BaseException
    {
        public NoFormDataException(string objectName)
            :base(HttpStatusCode.BadRequest, "No Form Data", string.Format("No form data present to created {0}.", objectName))
        {
        }
    }


}
