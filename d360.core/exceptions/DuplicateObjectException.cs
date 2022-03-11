using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class DuplicateNameException : BaseException
    {
        public DuplicateNameException(string objectName)
            : base(HttpStatusCode.Conflict, AssetTypeErrors.DuplicateNameFound, string.Format(AssetTypeErrors.NameConflicit, objectName))
        {
        }
    }

    public class DuplicateObjectException : BaseException
    {
        public DuplicateObjectException(string objectName)
            : base(HttpStatusCode.Conflict, AssetTypeErrors.DuplicateFound, string.Format(AssetTypeErrors.ItemConflicit, objectName))
        {
        }
    }
}
