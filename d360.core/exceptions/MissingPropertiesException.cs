using System.Net;

namespace d360.core.exceptions
{
    public class MissingPropertiesException : BaseException
    {
        public MissingPropertiesException(string objectName)
            : base(HttpStatusCode.BadRequest, "Item Missing Properties", string.Format("{0} is missing required properties for the request to proceed.", objectName))
        {
        }
    }
}
