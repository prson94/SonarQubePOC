using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class MissingPropertiesException : BaseException
    {
        public MissingPropertiesException(string objectName)
            : base(HttpStatusCode.BadRequest, AssetTypeErrors.MissingPropertyTitle, string.Format(AssetTypeErrors.MissingPropertyMessage, objectName))
        {
        }
    }
}
