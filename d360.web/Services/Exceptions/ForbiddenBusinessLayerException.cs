using System;
using System.Runtime.Serialization;
using Resources;

namespace d360.web.Services
{
    [Serializable]
    public class ForbiddenBusinessLayerException : UnrecoverableBusinessLayerException
    {
        public ForbiddenBusinessLayerException() : this(ApiMessages.ForbiddenUserNotAuthorizedMessage)
        {
        }

        public ForbiddenBusinessLayerException(string message) : base(message)
        {
        }

        public ForbiddenBusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ForbiddenBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}