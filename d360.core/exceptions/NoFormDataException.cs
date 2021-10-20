using System.Net;
using d360.core.resources;

namespace d360.core.exceptions
{
    public class NoFormDataException : BaseException
    {
        public NoFormDataException(string objectName)
            :base(HttpStatusCode.BadRequest, AssetTypeErrors.NoFormDataTitle, string.Format(AssetTypeErrors.NoFormDataMessage, objectName))
        {
        }
    }


}
