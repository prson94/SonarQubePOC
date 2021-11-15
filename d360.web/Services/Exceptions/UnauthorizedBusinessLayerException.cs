using System;
using System.Runtime.Serialization;
using Resources;

namespace d360.web.Services
{
    [Serializable]
    public class UnauthorizedBusinessLayerException: UnrecoverableBusinessLayerException
    {
        public UnauthorizedBusinessLayerException(): this(ApiMessages.EndpointNotAuthorizedMessage)
        {
        }

        public UnauthorizedBusinessLayerException(string message) : base(message)
        {
        }

        public UnauthorizedBusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnauthorizedBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}