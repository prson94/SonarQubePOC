using System;
using System.Runtime.Serialization;
using d360.core.resources;

namespace d360.web.Services
{
    [Serializable]
    public class ForbiddenBusinessLayerException : UnrecoverableBusinessLayerException
    {
        public ForbiddenBusinessLayerException() : this(Error.ForbiddenUserNotAuthorizedMessage)
        {
        }

        public ForbiddenBusinessLayerException(string message) : base(message)
        {
        }
    }
}
