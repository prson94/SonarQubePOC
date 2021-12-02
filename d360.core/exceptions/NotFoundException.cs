using System.Net;
using d360.core.resources;


namespace d360.core.exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string objectName)
            :base(HttpStatusCode.NotFound, Messages.ItemNotFound,  string.Format(AssetTypeErrors.ItemNotFound, objectName))
        {
        }
    }


}
